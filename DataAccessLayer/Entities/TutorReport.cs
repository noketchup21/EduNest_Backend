using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("TutorReports")]
    public class TutorReport
    {
        [Key]
        public int TutorReportId { get; set; }

        public int ReporterUserId { get; set; }
        public int TutorId { get; set; }
        public int BookingId { get; set; }
        public int AvailabilityId { get; set; }
        public int? LessonId { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending";
        // Pending, Reviewing, Resolved, Rejected

        [MaxLength(2000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }

        [ForeignKey(nameof(ReporterUserId))]
        public User ReporterUser { get; set; } = null!;

        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; } = null!;

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; } = null!;

        [ForeignKey(nameof(AvailabilityId))]
        public Availability Availability { get; set; } = null!;

        [ForeignKey(nameof(LessonId))]
        public Lesson? Lesson { get; set; }

        public ICollection<TutorReportProofImage> ProofImages { get; set; } =
            new List<TutorReportProofImage>();
    }
}
