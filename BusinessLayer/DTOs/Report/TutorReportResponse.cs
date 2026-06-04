using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Report
{
    public sealed class TutorReportResponse
    {
        public int TutorReportId { get; set; }

        public int ReporterUserId { get; set; }
        public string ReporterName { get; set; } = string.Empty;

        public int TutorId { get; set; }
        public int TutorUserId { get; set; }
        public string TutorName { get; set; } = string.Empty;

        public int BookingId { get; set; }
        public int AvailabilityId { get; set; }
        public int? LessonId { get; set; }

        public string? SubjectName { get; set; }

        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public List<TutorReportProofImageResponse> ProofImages { get; set; } = new();
    }

    public sealed class TutorReportProofImageResponse
    {
        public int TutorReportProofImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
