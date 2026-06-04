using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Availability
{
    public sealed class AvailabilityResponse
    {
        public int AvailabilityId { get; set; }
        public int TutorId { get; set; }
        public int? SubjectId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public DateTime StartCourseTime { get; set; }
        public DateTime EndCourseTime { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Slot { get; set; }
        public int RemainingSlot { get; set; }
        public decimal PricePerSlot { get; set; }
        public decimal TotalCoursePrice { get; set; }   
        public string Status { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int TutorUserId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string? SubjectName { get; set; }
        public bool HasBookings { get; set; }
    }

    public sealed class CreateAvailabilityRequest
    {
        public int? SubjectId { get; set; }

        [Required]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required]
        public DateTime StartCourseTime { get; set; }

        [Required]
        public DateTime EndCourseTime { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }
        [Required]
        public string Mode { get; set; } = string.Empty;

        [Required]
        public string Level { get; set; } = string.Empty;

        public int Slot { get; set; }

        [Range(1000, double.MaxValue)]
        public decimal PricePerSlot { get; set; }
    }

    public sealed class UpdateAvailabilityRequest
    {
        public int? SubjectId { get; set; }
        public string? DayOfWeek { get; set; }
        public DateTime? StartCourseTime { get; set; }
        public DateTime? EndCourseTime { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public decimal? PricePerSlot { get; set; }
        public string? Status { get; set; }
        public string? Mode { get; set; }
        public string? Level { get; set; }
    }

    public sealed class UpdateAvailabilityStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
