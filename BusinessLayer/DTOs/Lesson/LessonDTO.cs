using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Lesson
{
    public sealed class LessonResponse
    {
        public int LessonId { get; set; }
        public int BookingId { get; set; }

        public int AvailabilityId { get; set; }
        public int AvailabilitySlot { get; set; }

        public int TutorId { get; set; }
        public int TutorUserId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string? TutorAvatarUrl { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }

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

    public sealed class SetMeetingLinkRequest
    {
        [Required]
        public string MeetingLink { get; set; } = string.Empty;
    }

    public sealed class LessonDetailResponse
    {
        public int MainLessonId { get; set; }
        public int AvailabilityId { get; set; }
        public int AvailabilitySlot { get; set; }

        public int TutorId { get; set; }
        public int TutorUserId { get; set; }
        public string TutorName { get; set; } = string.Empty;
        public string? TutorAvatarUrl { get; set; }
        public string StudentName { get; set; } = string.Empty;

        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }

        public DateTime ScheduleTime { get; set; }
        public int Duration { get; set; }
        public DateTime EndTime { get; set; }

        public string Status { get; set; } = string.Empty;
        public string MeetingLink { get; set; } = string.Empty;

        public bool CanTakeAttendance { get; set; }
        public bool CanComplete { get; set; }

        public List<LessonStudentResponse> Students { get; set; } = new();
    }

    public sealed class LessonStudentResponse
    {
        public int LessonId { get; set; }
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string AttendanceStatus { get; set; } = string.Empty;
        public string LessonStatus { get; set; } = string.Empty;
    }
}
