using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Lesson
{
    public sealed class LessonResponse
    {
        public int LessonId { get; set; }
        public int BookingId { get; set; }
        public DateTime ScheduleTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MeetingLink { get; set; }
    }

    public sealed class CreateLessonRequest
    {
        [Required]
        public DateTime ScheduleTime { get; set; }

        [Range(1, 480)]
        public int Duration { get; set; } = 90;

        public string? MeetingLink { get; set; }
    }

    public sealed class CompleteLessonRequest
    {
        public string? Note { get; set; }
    }
}
