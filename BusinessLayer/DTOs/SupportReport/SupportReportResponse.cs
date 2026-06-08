using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.SupportReport
{
    public sealed class SupportReportResponse
    {
        public int SupportReportId { get; set; }

        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int? PayoutId { get; set; }
        public int? BookingId { get; set; }
        public int? LessonId { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public List<SupportReportProofImageResponse> ProofImages { get; set; } = new();
    }

    public sealed class SupportReportProofImageResponse
    {
        public int SupportReportProofImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
