using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Booking
{
    public sealed class CreateBookingRequest
    {
        [Required]
        public int AvailabilityId { get; set; }

        // Optional note from mobile app. No parent/student required in MVP flow.
        public string? Note { get; set; }
    }

    public sealed class BookingResponse
    {
        public int BookingId { get; set; }
        public int AvailabilityId { get; set; }
        public int UserId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public int? SubjectId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public DateTime StartCourseTime { get; set; }
        public DateTime EndCourseTime { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal PriceAtBooking { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
