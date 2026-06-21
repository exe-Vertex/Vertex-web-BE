using System;
using System.Threading.Tasks;
using Vertex.Services.Models;

namespace Vertex.Services.Interfaces
{
    public interface IBillingService
    {
        Task<BillingCheckoutResult> CreateCheckoutAsync(Guid orgId, Guid userId, CreateBillingCheckoutInput input);
        Task<BillingTransactionResult> GetTransactionAsync(Guid orgId, Guid userId, Guid transactionId);
        Task<BillingTransactionResult> HandlePayOSWebhookAsync(PayOSWebhookRequest request);
    }
}
