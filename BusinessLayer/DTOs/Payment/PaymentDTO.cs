using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Payment
{
    public sealed class CreatePaymentResponse
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public long OrderCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? CheckoutUrl { get; set; }
        public string? QrCode { get; set; }
    }

    public sealed class PayOsWebhookRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool Success { get; set; }
        public PayOsWebhookData? Data { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public sealed class PayOsWebhookData
    {
        public long OrderCode { get; set; }
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string TransactionDateTime { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
    }
}
