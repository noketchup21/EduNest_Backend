using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Admin
{
    public sealed class AdminDashboardResponse
    {
        public int TotalDownloads { get; set; }
        public int TotalInstalls { get; set; }

        public int TotalSubjects { get; set; }

        public int TotalTutors { get; set; }
        public int PendingTutors { get; set; }
        public int ApprovedTutors { get; set; }

        public int PendingPayouts { get; set; }
        public decimal PendingPayoutAmount { get; set; }

        public int CompletedLessons { get; set; }
        public decimal GrossLessonRevenue { get; set; }
        public decimal PlatformRevenue { get; set; }
        public decimal TutorRevenue { get; set; }
    }

    public sealed class TrackAppMetricRequest
    {
        public string? DeviceId { get; set; }
        public string? Platform { get; set; }
        public string? AppVersion { get; set; }
    }

    public sealed class AdminTutorResponse
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Bio { get; set; }
        public bool IsVerified { get; set; }
    }

    public sealed class UpdatePayoutStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
