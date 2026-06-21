using System;
using System.Text.Json.Serialization;

namespace Vertex.Services.Models
{
    public class PayOSSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = "http://localhost:5173/dashboard";
        public string CancelUrl { get; set; } = "http://localhost:5173/dashboard";
        public int CheckoutExpiresMinutes { get; set; } = 30;
    }

    public record CreateBillingCheckoutInput(string Plan, string BillingCycle);

    public record BillingCheckoutResult(
        Guid TransactionId,
        long OrderCode,
        string PaymentLinkId,
        long Amount,
        string Currency,
        string Plan,
        string BillingCycle,
        string CheckoutUrl,
        string QrCode,
        string Status,
        DateTimeOffset ExpiredAt,
        string Message
    );

    public record BillingTransactionResult(
        Guid TransactionId,
        long OrderCode,
        string Plan,
        string BillingCycle,
        long Amount,
        string Currency,
        string Status,
        string? CheckoutUrl,
        DateTimeOffset? PaidAt,
        DateTimeOffset ExpiredAt
    );

    public class PayOSWebhookRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public PayOSWebhookDataRequest Data { get; set; } = new();

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    public class PayOSWebhookDataRequest
    {
        [JsonPropertyName("orderCode")]
        public long OrderCode { get; set; }

        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;

        [JsonPropertyName("reference")]
        public string Reference { get; set; } = string.Empty;

        [JsonPropertyName("transactionDateTime")]
        public string TransactionDateTime { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "VND";

        [JsonPropertyName("paymentLinkId")]
        public string PaymentLinkId { get; set; } = string.Empty;

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("desc")]
        public string Description2 { get; set; } = string.Empty;

        [JsonPropertyName("counterAccountBankId")]
        public string? CounterAccountBankId { get; set; }

        [JsonPropertyName("counterAccountBankName")]
        public string? CounterAccountBankName { get; set; }

        [JsonPropertyName("counterAccountName")]
        public string? CounterAccountName { get; set; }

        [JsonPropertyName("counterAccountNumber")]
        public string? CounterAccountNumber { get; set; }

        [JsonPropertyName("virtualAccountName")]
        public string? VirtualAccountName { get; set; }

        [JsonPropertyName("virtualAccountNumber")]
        public string? VirtualAccountNumber { get; set; }
    }
}
