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
        private readonly ICloudinaryService _cloudinaryService;

        public LessonService(
            EduNestDbContext db,
            IWalletService walletService,
            ICloudinaryService cloudinaryService)
        {
            _db = db;
            _walletService = walletService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<LessonResponse>> GetMyLessonsAsync(int userId)
        {
            var tutor = await _db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            var query = _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Subject)
                .AsQueryable();

            query = tutor != null
                ? query.Where(l => l.Booking.Availability.TutorId == tutor.TutorId)
                : query.Where(l => l.Booking.UserId == userId);

            var lessons = await query
              .OrderBy(l => l.ScheduleTime)
              .ToListAsync();

            return lessons
                .Select(ToLessonResponse)
                .ToList();
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

            var createdLesson = await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Subject)
                .FirstAsync(l => l.LessonId == lesson.LessonId);

            return ToLessonResponse(createdLesson);
        }

        public async Task<LessonDetailResponse> GetLessonDetailAsync(
            int tutorUserId,
            int lessonId)
        {
            var lesson = await GetTutorLessonAsync(tutorUserId, lessonId);

            var groupLessons = await GetLessonGroupAsync(
                lesson.Booking.AvailabilityId,
                lesson.ScheduleTime);

            return ToLessonDetailResponse(
                lesson,
                lesson.Booking.Availability,
                groupLessons);
        }

        public async Task<LessonDetailResponse> SetMeetingLinkAsync(
            int tutorUserId,
            int lessonId,
            string meetingLink)
        {
            if (string.IsNullOrWhiteSpace(meetingLink))
                throw new InvalidOperationException("Meeting link is required.");

            var lesson = await GetTutorLessonAsync(tutorUserId, lessonId);

            var groupLessons = await GetLessonGroupAsync(
                lesson.Booking.AvailabilityId,
                lesson.ScheduleTime);

            foreach (var item in groupLessons)
            {
                item.MeetingLink = meetingLink.Trim();
            }

            await _db.SaveChangesAsync();

            return ToLessonDetailResponse(
                lesson,
                lesson.Booking.Availability,
                groupLessons);
        }

        public async Task<LessonResponse> MarkAttendanceAsync(
            int tutorUserId,
            int lessonId,
            MarkAttendanceRequest request)
        {
            var lesson = await _db.Lessons
                    .Include(l => l.Booking)
        .ThenInclude(b => b.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Subject)
                .Include(l => l.Attendances)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            await EnsureTutorOwnsLessonAsync(tutorUserId, lesson);

            if (lesson.Booking.Status != "Confirmed")
                throw new InvalidOperationException("Booking is not confirmed.");

            if (DateTime.UtcNow < lesson.ScheduleTime)
                throw new InvalidOperationException("Attendance can only be taken after the lesson has started.");

            if (lesson.Status == "Completed")
                throw new InvalidOperationException("Cannot update attendance after lesson is completed.");


            var normalizedStatus = NormalizeAttendanceStatus(request.Status);
            var attendance = lesson.Attendances.FirstOrDefault();

            if (attendance == null)
            {
                // Resolve the Student record from the booking's UserId
                var student = await _db.Students
                    .FirstOrDefaultAsync(s => s.UserId == lesson.Booking.UserId)
                    ?? throw new InvalidOperationException(
                        $"No student profile found for user #{lesson.Booking.UserId}.");

                attendance = new Attendance
                {
                    LessonId = lesson.LessonId,
                    StudentId = student.StudentId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Attendances.Add(attendance);
            }

            attendance.Status = normalizedStatus;
            attendance.Note = request.Note ?? string.Empty;
            attendance.AttendedAt = normalizedStatus == "Absent"
                ? null
                : DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToLessonResponse(lesson);
        }

        public async Task<LessonResponse> CompleteLessonAsync(
            int tutorUserId,
            int lessonId,
            CompleteLessonRequest request)
        {
            var now = DateTime.UtcNow;

            var lesson = await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            await EnsureTutorOwnsLessonAsync(tutorUserId, lesson);

            if (lesson.Status == "Completed")
                return ToLessonResponse(lesson);

            if (lesson.Booking.Status != "Confirmed")
                throw new InvalidOperationException("Booking is not confirmed.");

            var endTime = lesson.ScheduleTime.AddMinutes(lesson.Duration);

            if (now < lesson.ScheduleTime)
                throw new InvalidOperationException(
                    "Cannot complete this lesson before its scheduled time.");

            if (now < endTime)
                throw new InvalidOperationException(
                    "Lesson cannot be completed before end time.");

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

        public async Task<LessonDetailResponse> CompleteLessonGroupAsync(
            int tutorUserId,
            int lessonId)
        {
            var lesson = await GetTutorLessonAsync(tutorUserId, lessonId);

            if (lesson.Booking.Status != "Confirmed")
                throw new InvalidOperationException("Booking is not confirmed.");

            var endTime = lesson.ScheduleTime.AddMinutes(lesson.Duration);

            if (DateTime.UtcNow < endTime)
                throw new InvalidOperationException("Lesson cannot be completed before end time.");

            var groupLessons = await GetLessonGroupAsync(
                lesson.Booking.AvailabilityId,
                lesson.ScheduleTime);

            foreach (var item in groupLessons)
            {
                if (item.Status == "Completed")
                    continue;

                item.Status = "Completed";

                await _walletService.CreditTutorForLessonAsync(item);
            }

            var bookingIds = groupLessons
                .Select(l => l.BookingId)
                .Distinct()
                .ToList();

            var bookings = await _db.Bookings
                .Include(b => b.Lessons)
                .Where(b => bookingIds.Contains(b.BookingId))
                .ToListAsync();

            foreach (var booking in bookings)
            {
                if (booking.Lessons.Count > 0 &&
                    booking.Lessons.All(l => l.Status == "Completed"))
                {
                    booking.Status = "Completed";
                }
            }

            await _db.SaveChangesAsync();

            return ToLessonDetailResponse(
                lesson,
                lesson.Booking.Availability,
                groupLessons);
        }

        private async Task<Booking> GetTutorBookingAsync(int tutorUserId, int bookingId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            return await _db.Bookings
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Tutor)
                        .ThenInclude(t => t.User)
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Subject)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.Availability.TutorId == tutor.TutorId &&
                    !b.IsDeleted)
                ?? throw new KeyNotFoundException("Booking not found.");
        }

        private async Task<Lesson> GetTutorLessonAsync(int tutorUserId, int lessonId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var lesson = await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Subject)
                .Include(l => l.Attendances)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            if (lesson.Booking.Availability.TutorId != tutor.TutorId)
                throw new UnauthorizedAccessException("This lesson does not belong to the tutor.");

            return lesson;
        }

        private async Task<List<Lesson>> GetLessonGroupAsync(
            int availabilityId,
            DateTime scheduleTime)
        {
            return await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Subject)
                .Include(l => l.Attendances)
                .Where(l =>
                    l.Booking.AvailabilityId == availabilityId &&
                    l.ScheduleTime == scheduleTime &&
                    l.Booking.Status == "Confirmed")
                .OrderBy(l => l.Booking.User!.Name)
                .ToListAsync();
        }

        private async Task EnsureTutorOwnsLessonAsync(int tutorUserId, Lesson lesson)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            if (lesson.Booking.Availability.TutorId != tutor.TutorId)
                throw new UnauthorizedAccessException("This lesson does not belong to the tutor.");
        }

        private async Task<Tutor> GetTutorByUserIdAsync(int userId)
        {
            return await _db.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");
        }

        private static string NormalizeAttendanceStatus(string status)
        {
            var value = status.Trim();

            if (value.Equals("Present", StringComparison.OrdinalIgnoreCase))
                return "Present";

            if (value.Equals("Absent", StringComparison.OrdinalIgnoreCase))
                return "Absent";

            if (value.Equals("Late", StringComparison.OrdinalIgnoreCase))
                return "Late";

            throw new InvalidOperationException("Attendance status must be Present, Absent, or Late.");
        }

        private LessonResponse ToLessonResponse(Lesson lesson)
        {
            var availability = lesson.Booking.Availability;
            var tutor = availability.Tutor;
            var tutorUser = tutor?.User;

            return new LessonResponse
            {
                LessonId = lesson.LessonId,
                BookingId = lesson.BookingId,

                AvailabilityId = availability.AvailabilityId,

                TutorId = availability.TutorId,
                TutorUserId = tutor?.UserId ?? 0,
                TutorName = tutorUser?.Name ?? $"Tutor #{availability.TutorId}",
                TutorAvatarUrl = AvatarUrl(tutorUser),

                SubjectId = availability.SubjectId,
                SubjectName = availability.Subject?.Name,

                ScheduleTime = lesson.ScheduleTime,
                Duration = lesson.Duration,
                Status = lesson.Status,
                MeetingLink = lesson.MeetingLink
            };
        }

        private LessonDetailResponse ToLessonDetailResponse(
            Lesson mainLesson,
            Availability availability,
            List<Lesson> groupLessons)
        {
            var now = DateTime.UtcNow;
            var endTime = mainLesson.ScheduleTime.AddMinutes(mainLesson.Duration);

            var tutor = availability.Tutor;
            var tutorUser = tutor?.User;

            var meetingLink = groupLessons
                .Select(l => l.MeetingLink)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

            return new LessonDetailResponse
            {
                MainLessonId = mainLesson.LessonId,
                AvailabilityId = availability.AvailabilityId,

                TutorId = availability.TutorId,
                TutorUserId = tutor?.UserId ?? 0,
                TutorName = tutorUser?.Name ?? $"Tutor #{availability.TutorId}",
                TutorAvatarUrl = AvatarUrl(tutorUser),

                SubjectId = availability.SubjectId,
                SubjectName = availability.Subject?.Name,

                ScheduleTime = mainLesson.ScheduleTime,
                Duration = mainLesson.Duration,
                EndTime = endTime,
                Status = mainLesson.Status,
                MeetingLink = meetingLink,
                CanTakeAttendance = now >= mainLesson.ScheduleTime,
                CanComplete = now >= endTime,

                Students = groupLessons.Select(l =>
                {
                    var attendance = l.Attendances.FirstOrDefault();

                    return new LessonStudentResponse
                    {
                        LessonId = l.LessonId,
                        BookingId = l.BookingId,
                        UserId = l.Booking.UserId ?? 0,
                        StudentName = l.Booking.User?.Name ?? $"User #{l.Booking.UserId}",
                        AttendanceStatus = attendance?.Status ?? "Not marked",
                        LessonStatus = l.Status
                    };
                }).ToList()
            };
        }

        private string? AvatarUrl(User? user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.AvatarPublicId))
                return null;

            return _cloudinaryService.GenerateSignedImageUrl(
                user.AvatarPublicId,
                300,
                300);
        }
    }
}