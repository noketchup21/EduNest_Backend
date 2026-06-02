using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Attendance;
using BusinessLayer.DTOs.Lesson;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class LessonService : ILessonService
    {
        private readonly EduNestDbContext _db;
        private readonly IWalletService _walletService;

        public LessonService(EduNestDbContext db, IWalletService walletService)
        {
            _db = db;
            _walletService = walletService;
        }

        public async Task<List<LessonResponse>> GetMyLessonsAsync(int userId)
        {
            var tutor = await _db.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);

            var query = _db.Lessons
                .Include(l => l.Booking)
                .ThenInclude(b => b.Availability)
                .AsQueryable();

            query = tutor != null
                ? query.Where(l => l.Booking.Availability.TutorId == tutor.TutorId)
                : query.Where(l => l.Booking.UserId == userId);

            return await query
                .OrderBy(l => l.ScheduleTime)
                .Select(l => ToLessonResponse(l))
                .ToListAsync();
        }

        public async Task<LessonResponse> AddLessonAsync(
            int tutorUserId,
            int bookingId,
            CreateLessonRequest request)
        {
            var booking = await GetTutorBookingAsync(tutorUserId, bookingId);

            if (booking.Status != "Confirmed")
                throw new InvalidOperationException("Booking must be confirmed first.");

            var lesson = new Lesson
            {
                BookingId = booking.BookingId,
                ScheduleTime = DateTime.SpecifyKind(request.ScheduleTime, DateTimeKind.Utc),
                Duration = request.Duration,
                MeetingLink = request.MeetingLink ?? string.Empty,
                Status = "Scheduled"
            };

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();

            return ToLessonResponse(lesson);
        }

        public async Task<LessonResponse> MarkAttendanceAsync(
            int tutorUserId,
            int lessonId,
            MarkAttendanceRequest request)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Booking)
                .ThenInclude(b => b.Availability)
                .Include(l => l.Attendances)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            await EnsureTutorOwnsLessonAsync(tutorUserId, lesson);

            var attendance = lesson.Attendances.FirstOrDefault();

            if (attendance == null)
            {
                attendance = new Attendance
                {
                    LessonId = lesson.LessonId,
                    StudentId = lesson.Booking.StudentId ?? 0,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Attendances.Add(attendance);
            }

            attendance.Status = request.Status;
            attendance.Note = request.Note ?? string.Empty;
            attendance.AttendedAt = request.Status == "Absent" ? null : DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToLessonResponse(lesson);
        }

        public async Task<LessonResponse> CompleteLessonAsync(
            int tutorUserId,
            int lessonId,
            CompleteLessonRequest request)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Booking)
                .ThenInclude(b => b.Availability)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            await EnsureTutorOwnsLessonAsync(tutorUserId, lesson);

            if (lesson.Status == "Completed")
                return ToLessonResponse(lesson);

            if (lesson.Booking.Status != "Confirmed")
                throw new InvalidOperationException("Booking is not confirmed.");

            lesson.Status = "Completed";

            await _walletService.CreditTutorForLessonAsync(lesson);

            var allLessons = await _db.Lessons
                .Where(l => l.BookingId == lesson.BookingId)
                .ToListAsync();

            if (allLessons.Count > 0 && allLessons.All(l => l.Status == "Completed"))
                lesson.Booking.Status = "Completed";

            await _db.SaveChangesAsync();

            return ToLessonResponse(lesson);
        }

        private async Task<Booking> GetTutorBookingAsync(int tutorUserId, int bookingId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            return await _db.Bookings
                .Include(b => b.Availability)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.Availability.TutorId == tutor.TutorId &&
                    !b.IsDeleted)
                ?? throw new KeyNotFoundException("Booking not found.");
        }

        private async Task EnsureTutorOwnsLessonAsync(int tutorUserId, Lesson lesson)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            if (lesson.Booking.Availability.TutorId != tutor.TutorId)
                throw new UnauthorizedAccessException("This lesson does not belong to the tutor.");
        }

        private async Task<Tutor> GetTutorByUserIdAsync(int userId)
            => await _db.Tutors.FirstOrDefaultAsync(t => t.UserId == userId)
               ?? throw new KeyNotFoundException("Tutor profile not found.");

        private static LessonResponse ToLessonResponse(Lesson l) => new()
        {
            LessonId = l.LessonId,
            BookingId = l.BookingId,
            ScheduleTime = l.ScheduleTime,
            Duration = l.Duration,
            Status = l.Status,
            MeetingLink = l.MeetingLink
        };
    }
}
