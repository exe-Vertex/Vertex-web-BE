using System;
using Vertex.Entities.Organizations;
using Vertex.Entities.Users;

namespace Vertex.Entities.Billing
{
    public class PaymentTransaction
    {
        public Guid Id { get; set; }
        public Guid OrgId { get; set; }
        public Guid UserId { get; set; }
        public long OrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
        public string Provider { get; set; } = "payos";
        public string Plan { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = "VND";
        public string Status { get; set; } = "pending";
        public string? CheckoutUrl { get; set; }
        public string? QrCode { get; set; }
        public string? PayosReference { get; set; }
        public string? FailureReason { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Organization? Organization { get; set; }
        public User? User { get; set; }
    }
}
