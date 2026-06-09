using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Payouts")]
    public class Payout
    {
        [Key]
        public int PayoutId { get; set; }

        public int TutorId { get; set; }

        public int? WalletTransactionId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";
        // Pending / Processing / Paid / ManualQrRequired / Rejected / Failed

        [Required, MaxLength(50)]
        public string PayoutMethod { get; set; } = "ManualQr";
        // ManualQr / PayOSChi

        [MaxLength(150)]
        public string? PayOSChiReferenceId { get; set; }

        [MaxLength(150)]
        public string? PayOSChiBatchId { get; set; }

        [MaxLength(150)]
        public string? PayOSChiPayoutItemId { get; set; }

        [MaxLength(100)]
        public string? PayOSChiApprovalState { get; set; }

        [MaxLength(100)]
        public string? PayOSChiTransactionState { get; set; }

        [MaxLength(1000)]
        public string? PayOSChiFailureReason { get; set; }

        [ForeignKey(nameof(TutorId))]
        public virtual Tutor Tutor { get; set; } = null!;

        [ForeignKey(nameof(WalletTransactionId))]
        public virtual WalletTransaction? WalletTransaction { get; set; }
    }
}
