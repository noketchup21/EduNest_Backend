using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("SupportReports")]
    public class SupportReport
    {
        [Key]
        public int SupportReportId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string Role { get; set; } = string.Empty; // Tutor, Learner, Parent

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;
        // MissingPayment, SlowPayout, LessonIssue, BookingIssue, AppBug, AccountIssue, Other

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int? PayoutId { get; set; }
        public int? BookingId { get; set; }
        public int? LessonId { get; set; }

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";
        // Pending, Reviewing, Resolved, Rejected

        [MaxLength(2000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        public ICollection<SupportReportProofImage> ProofImages { get; set; } =
            new List<SupportReportProofImage>();
    }
}
