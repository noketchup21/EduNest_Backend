using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Lesson")]
    public class Lesson
    {
        [Key]
        public int LessonId { get; set; }

        public int BookingId { get; set; }

        public DateTime ScheduleTime { get; set; }

        /// <summary>Duration in minutes.</summary>
        public int Duration { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; }         // Scheduled / Completed / Cancelled / TutorAbsent / StudentAbsent

        [MaxLength(500)]
        public string MeetingLink { get; set; }

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();
    }
}
