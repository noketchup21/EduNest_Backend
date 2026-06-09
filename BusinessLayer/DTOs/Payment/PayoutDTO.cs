using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Payment
{
    public sealed class RequestPayoutRequest
    {
        [Range(1000, double.MaxValue)]
        public decimal Amount { get; set; }
    }

    public sealed class PayoutResponse
    {
        public int PayoutId { get; set; }
        public int TutorId { get; set; }
        public decimal Amount { get; set; }

        // Pending, Processing, Paid, ManualQrRequired, Rejected, Failed
        public string Status { get; set; } = string.Empty;

        // ManualQr, PayOSChi
        public string PayoutMethod { get; set; } = string.Empty;

        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // payOS Chi tracking
        public string? PayOSChiReferenceId { get; set; }
        public string? PayOSChiBatchId { get; set; }
        public string? PayOSChiPayoutItemId { get; set; }
        public string? PayOSChiApprovalState { get; set; }
        public string? PayOSChiTransactionState { get; set; }
        public string? PayOSChiFailureReason { get; set; }

        // Tutor bank info for admin manual QR backup
        public string? TutorBankName { get; set; }
        public string? TutorBankBin { get; set; }
        public string? TutorAccountNumber { get; set; }
        public string? TutorAccountHolderName { get; set; }
        public string? TutorBankBranch { get; set; }
    }

    public sealed class AdminUpdatePayoutRequest
    {
        [Required, MaxLength(50)]
        public string Status { get; set; } = "Paid";
    }

    public sealed class PayOSChiPayoutResult
    {
        public string ReferenceId { get; set; } = string.Empty;
        public string? BatchId { get; set; }
        public string? PayoutItemId { get; set; }

        public string ApprovalState { get; set; } = "PROCESSING";
        public string TransactionState { get; set; } = "PROCESSING";

        public string? RawResponse { get; set; }
    }
}
