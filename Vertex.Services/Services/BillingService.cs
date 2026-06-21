using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Exceptions;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Vertex.Entities.AuditLogs;
using Vertex.Entities.Notifications;
using Vertex.Repositories;
using Vertex.Services.Interfaces;
using Vertex.Services.Models;

namespace Vertex.Services.Services
{
    public class BillingService : IBillingService
    {
        private const string Provider = "payos";
        private const string Pending = "pending";
        private const string Paid = "paid";
        private const string Failed = "failed";
        private const string Expired = "expired";

        private readonly AppDbContext _context;
        private readonly PayOSSettings _settings;
        private readonly ILogger<BillingService> _logger;

        public BillingService(AppDbContext context, IOptions<PayOSSettings> settings, ILogger<BillingService> logger)
        {
            _context = context;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<BillingCheckoutResult> CreateCheckoutAsync(Guid orgId, Guid userId, CreateBillingCheckoutInput input)
        {
            EnsurePayOSConfigured();
            await EnsureBillingAdminAccessAsync(orgId, userId);

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == orgId);
            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            var plan = NormalizePlan(input.Plan);
            var billingCycle = NormalizeBillingCycle(input.BillingCycle);
            var amount = CalculateAmount(plan, billingCycle);
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.AddMinutes(Math.Max(5, _settings.CheckoutExpiresMinutes));
            var orderCode = await CreateUniqueOrderCodeAsync();

            var transaction = new Vertex.Entities.Billing.PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrgId = orgId,
                UserId = userId,
                OrderCode = orderCode,
                Provider = Provider,
                Plan = plan,
                BillingCycle = billingCycle,
                Amount = amount,
                Currency = "VND",
                Status = Pending,
                ExpiredAt = expiresAt,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            var client = CreateClient();
            var request = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = $"VTX{orderCode}",
                ReturnUrl = AppendQuery(_settings.ReturnUrl, transaction.Id, orderCode),
                CancelUrl = AppendQuery(_settings.CancelUrl, transaction.Id, orderCode),
                ExpiredAt = expiresAt.ToUnixTimeSeconds(),
                Items = new List<PaymentLinkItem>
                {
                    new()
                    {
                        Name = $"Vertex {plan.ToUpperInvariant()} {billingCycle}",
                        Quantity = 1,
                        Price = amount
                    }
                }
            };

            try
            {
                var paymentLink = await client.PaymentRequests.CreateAsync(request);

                transaction.PaymentLinkId = paymentLink.PaymentLinkId;
                transaction.CheckoutUrl = paymentLink.CheckoutUrl;
                transaction.QrCode = paymentLink.QrCode;
                transaction.Status = paymentLink.Status == PaymentLinkStatus.Pending ? Pending : paymentLink.Status.ToString().ToLowerInvariant();
                transaction.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();

                return ToCheckoutResult(transaction, "Created PayOS checkout successfully.");
            }
            catch (PayOSException ex)
            {
                transaction.Status = Failed;
                transaction.FailureReason = ex.Message;
                transaction.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException($"Could not create PayOS checkout: {ex.Message}", ex);
            }
        }

        public async Task<BillingTransactionResult> GetTransactionAsync(Guid orgId, Guid userId, Guid transactionId)
        {
            await EnsureOrgMemberAsync(orgId, userId);

            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId && t.OrgId == orgId);

            if (transaction == null)
                throw new InvalidOperationException("Payment transaction not found.");

            if (transaction.Status == Pending && transaction.ExpiredAt <= DateTimeOffset.UtcNow)
            {
                transaction.Status = Expired;
                transaction.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ToTransactionResult(transaction);
        }

        public async Task<BillingTransactionResult> HandlePayOSWebhookAsync(PayOSWebhookRequest request)
        {
            EnsurePayOSConfigured();

            var client = CreateClient();
            var webhook = ToPayOSWebhook(request);
            WebhookData data;

            try
            {
                data = await client.Webhooks.VerifyAsync(webhook);
            }
            catch (Exception ex) when (ex is PayOSException or InvalidOperationException)
            {
                _logger.LogWarning(ex, "Rejected invalid PayOS webhook signature for order {OrderCode}.", request.Data?.OrderCode);
                throw new UnauthorizedAccessException("Invalid PayOS webhook signature.");
            }

            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.OrderCode == data.OrderCode && t.Provider == Provider);

            if (transaction == null)
                throw new InvalidOperationException("Payment transaction not found.");

            if (transaction.Status == Paid)
                return ToTransactionResult(transaction);

            if (transaction.Amount != data.Amount)
            {
                transaction.Status = Failed;
                transaction.FailureReason = $"Amount mismatch. Expected {transaction.Amount}, received {data.Amount}.";
                transaction.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Payment amount does not match the pending transaction.");
            }

            if (!string.Equals(data.Currency, transaction.Currency, StringComparison.OrdinalIgnoreCase))
            {
                transaction.Status = Failed;
                transaction.FailureReason = $"Currency mismatch. Expected {transaction.Currency}, received {data.Currency}.";
                transaction.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Payment currency does not match the pending transaction.");
            }

            if (!request.Success || request.Code != "00" || data.Code != "00")
            {
                transaction.Status = Failed;
                transaction.FailureReason = request.Description;
                transaction.UpdatedAt = DateTimeOffset.UtcNow;
                await _context.SaveChangesAsync();
                return ToTransactionResult(transaction);
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            transaction.Status = Paid;
            transaction.PaymentLinkId = string.IsNullOrWhiteSpace(data.PaymentLinkId) ? transaction.PaymentLinkId : data.PaymentLinkId;
            transaction.PayosReference = data.Reference;
            transaction.PaidAt = ParsePayOSTransactionTime(data.TransactionDateTime) ?? DateTimeOffset.UtcNow;
            transaction.UpdatedAt = DateTimeOffset.UtcNow;

            await ApplyPaidPlanAsync(transaction);
            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return ToTransactionResult(transaction);
        }

        private PayOSClient CreateClient()
        {
            return new PayOSClient(new PayOSOptions
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ChecksumKey = _settings.ChecksumKey
            });
        }

        private void EnsurePayOSConfigured()
        {
            if (string.IsNullOrWhiteSpace(_settings.ClientId)
                || string.IsNullOrWhiteSpace(_settings.ApiKey)
                || string.IsNullOrWhiteSpace(_settings.ChecksumKey))
            {
                throw new InvalidOperationException("PayOS is not configured. Set PayOS:ClientId, PayOS:ApiKey, and PayOS:ChecksumKey.");
            }
        }

        private async Task ApplyPaidPlanAsync(Vertex.Entities.Billing.PaymentTransaction transaction)
        {
            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Id == transaction.OrgId);
            if (org == null)
                throw new InvalidOperationException("Organization not found.");

            org.Plan = transaction.Plan;
            org.UpdatedAt = DateTimeOffset.UtcNow;

            if (transaction.Plan == "pro")
            {
                org.MaxMembers = 20;
                org.MaxProjects = 15;
                org.AiQuota = 200;
                org.StorageLimit = 10L * 1024 * 1024 * 1024;
            }
            else if (transaction.Plan == "business")
            {
                org.MaxMembers = 200;
                org.MaxProjects = 100;
                org.AiQuota = 1000;
                org.StorageLimit = 50L * 1024 * 1024 * 1024;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                AdminId = transaction.UserId,
                Action = "change_price",
                TargetUserId = null,
                Detail = $"Upgraded organization '{org.Name}' to {transaction.Plan.ToUpperInvariant()} via PayOS order {transaction.OrderCode}.",
                CreatedAt = DateTimeOffset.UtcNow
            });

            var members = await _context.OrganizationMembers
                .Where(m => m.OrgId == transaction.OrgId)
                .ToListAsync();

            foreach (var member in members)
            {
                _context.Notifications.Add(new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = member.UserId,
                    Type = "info",
                    Message = $"Organization '{org.Name}' has been upgraded to {transaction.Plan.ToUpperInvariant()} successfully.",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        private async Task EnsureBillingAdminAccessAsync(Guid orgId, Guid userId)
        {
            var member = await _context.OrganizationMembers
                .FirstOrDefaultAsync(m => m.OrgId == orgId && m.UserId == userId);

            if (member == null)
                throw new UnauthorizedAccessException("You are not a member of this organization.");

            if (!string.Equals(member.Role, "owner", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(member.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Only organization owners or admins can manage billing.");
            }
        }

        private async Task EnsureOrgMemberAsync(Guid orgId, Guid userId)
        {
            var exists = await _context.OrganizationMembers
                .AnyAsync(m => m.OrgId == orgId && m.UserId == userId);

            if (!exists)
                throw new UnauthorizedAccessException("You are not a member of this organization.");
        }

        private async Task<long> CreateUniqueOrderCodeAsync()
        {
            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (await _context.PaymentTransactions.AnyAsync(t => t.OrderCode == orderCode))
            {
                orderCode++;
            }

            return orderCode;
        }

        private static string NormalizePlan(string plan)
        {
            var value = plan?.Trim().ToLowerInvariant();
            return value switch
            {
                "pro" => "pro",
                "business" => "business",
                _ => throw new InvalidOperationException("Invalid billing plan. Only Pro and Business are supported.")
            };
        }

        private static string NormalizeBillingCycle(string billingCycle)
        {
            var value = billingCycle?.Trim().ToLowerInvariant();
            return value switch
            {
                "monthly" => "monthly",
                "yearly" => "yearly",
                _ => throw new InvalidOperationException("Invalid billing cycle. Use monthly or yearly.")
            };
        }

        private static long CalculateAmount(string plan, string billingCycle)
        {
            return (plan, billingCycle) switch
            {
                ("pro", "monthly") => 99000,
                ("pro", "yearly") => 948000,
                ("business", "monthly") => 249000,
                ("business", "yearly") => 2388000,
                _ => throw new InvalidOperationException("Unsupported billing plan.")
            };
        }

        private static string AppendQuery(string url, Guid transactionId, long orderCode)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            var separator = url.Contains('?') ? "&" : "?";
            return $"{url}{separator}paymentTransactionId={transactionId}&orderCode={orderCode}";
        }

        private static Webhook ToPayOSWebhook(PayOSWebhookRequest request)
        {
            return new Webhook
            {
                Code = request.Code,
                Description = request.Description,
                Success = request.Success,
                Signature = request.Signature,
                Data = new WebhookData
                {
                    OrderCode = request.Data.OrderCode,
                    Amount = request.Data.Amount,
                    Description = request.Data.Description,
                    AccountNumber = request.Data.AccountNumber,
                    Reference = request.Data.Reference,
                    TransactionDateTime = request.Data.TransactionDateTime,
                    Currency = request.Data.Currency,
                    PaymentLinkId = request.Data.PaymentLinkId,
                    Code = request.Data.Code,
                    Description2 = request.Data.Description2,
                    CounterAccountBankId = request.Data.CounterAccountBankId,
                    CounterAccountBankName = request.Data.CounterAccountBankName,
                    CounterAccountName = request.Data.CounterAccountName,
                    CounterAccountNumber = request.Data.CounterAccountNumber,
                    VirtualAccountName = request.Data.VirtualAccountName,
                    VirtualAccountNumber = request.Data.VirtualAccountNumber
                }
            };
        }

        private static DateTimeOffset? ParsePayOSTransactionTime(string value)
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var offset))
                return offset.ToUniversalTime();

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dateTime))
                return new DateTimeOffset(dateTime).ToUniversalTime();

            return null;
        }

        private static BillingCheckoutResult ToCheckoutResult(Vertex.Entities.Billing.PaymentTransaction transaction, string message)
        {
            return new BillingCheckoutResult(
                transaction.Id,
                transaction.OrderCode,
                transaction.PaymentLinkId ?? string.Empty,
                transaction.Amount,
                transaction.Currency,
                transaction.Plan,
                transaction.BillingCycle,
                transaction.CheckoutUrl ?? string.Empty,
                transaction.QrCode ?? string.Empty,
                transaction.Status,
                transaction.ExpiredAt,
                message
            );
        }

        private static BillingTransactionResult ToTransactionResult(Vertex.Entities.Billing.PaymentTransaction transaction)
        {
            return new BillingTransactionResult(
                transaction.Id,
                transaction.OrderCode,
                transaction.Plan,
                transaction.BillingCycle,
                transaction.Amount,
                transaction.Currency,
                transaction.Status,
                transaction.CheckoutUrl,
                transaction.PaidAt,
                transaction.ExpiredAt
            );
        }
    }
}
