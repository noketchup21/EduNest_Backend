using BusinessLayer.DTOs.Availability;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class AvailabilityService : IAvailabilityService
    {
        private readonly EduNestDbContext _db;

        public AvailabilityService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<List<AvailabilityResponse>> GetAllAsync()
        {
            var data = await _db.Availabilities
                .Include(a => a.Tutor)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .Where(a => a.Status == "Active")
                .OrderBy(a => a.StartCourseTime)
                .ToListAsync();

            return data.Select(ToResponse).ToList();
        }

        public async Task<List<AvailabilityResponse>> GetByTutorAsync(int tutorId)
        {
            var data = await _db.Availabilities
                .Include(a => a.Tutor)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .Where(a => a.TutorId == tutorId && a.Status == "Active")
                .OrderBy(a => a.StartCourseTime)
                .ToListAsync();

            return data.Select(ToResponse).ToList();
        }

        public async Task<List<AvailabilityResponse>> GetMyAvailabilityAsync(int tutorUserId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var data = await _db.Availabilities
                .Include(a => a.Tutor)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .Where(a => a.TutorId == tutor.TutorId)
                .OrderByDescending(a => a.StartCourseTime)
                .ToListAsync();

            return data.Select(ToResponse).ToList();
        }

        public async Task<AvailabilityResponse> CreateAsync(
            int tutorUserId,
            CreateAvailabilityRequest request)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            ValidateCourseTime(
                request.StartCourseTime,
                request.EndCourseTime,
                request.StartTime,
                request.EndTime);

            if (request.PricePerSlot <= 0)
                throw new InvalidOperationException("Price per lesson must be greater than 0.");

            var normalizedDay = NormalizeDayOfWeek(request.DayOfWeek);

            await EnsureNoTutorAvailabilityConflictAsync(
                tutor.TutorId,
                excludeAvailabilityId: null,
                dayOfWeek: normalizedDay,
                startCourseTime: request.StartCourseTime,
                endCourseTime: request.EndCourseTime,
                startTime: request.StartTime,
                endTime: request.EndTime);

            var slotCount = CalculateSlotCount(
                request.StartCourseTime,
                request.EndCourseTime,
                normalizedDay);

            var availability = new Availability
            {
                TutorId = tutor.TutorId,
                SubjectId = request.SubjectId,
                DayOfWeek = normalizedDay,
                StartCourseTime = request.StartCourseTime,
                EndCourseTime = request.EndCourseTime,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Mode = request.Mode,
                Level = request.Level,
                Slot = slotCount,
                PricePerSlot = request.PricePerSlot,
                Status = "Active"
            };

            _db.Availabilities.Add(availability);
            await _db.SaveChangesAsync();

            availability.Tutor = tutor;

            if (availability.SubjectId.HasValue)
            {
                availability.Subject = await _db.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectId == availability.SubjectId.Value);
            }

            return ToResponse(availability);
        }

        public async Task<AvailabilityResponse> UpdateAsync(
            int tutorUserId,
            int availabilityId,
            UpdateAvailabilityRequest request)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var availability = await _db.Availabilities
                .Include(a => a.Tutor)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a =>
                    a.AvailabilityId == availabilityId &&
                    a.TutorId == tutor.TutorId)
                ?? throw new KeyNotFoundException("Availability not found.");

            if (request.SubjectId.HasValue)
                availability.SubjectId = request.SubjectId;

            if (!string.IsNullOrWhiteSpace(request.DayOfWeek))
                availability.DayOfWeek = NormalizeDayOfWeek(request.DayOfWeek);

            if (!string.IsNullOrWhiteSpace(request.Status))
                availability.Status = NormalizeAvailabilityStatus(request.Status);

            if (!string.IsNullOrWhiteSpace(request.Mode))
                availability.Mode = request.Mode.Trim();

            if (!string.IsNullOrWhiteSpace(request.Level))
                availability.Level = request.Level.Trim();

            if (request.StartCourseTime.HasValue)
                availability.StartCourseTime = request.StartCourseTime.Value;

            if (request.EndCourseTime.HasValue)
                availability.EndCourseTime = request.EndCourseTime.Value;

            if (request.StartTime.HasValue)
                availability.StartTime = request.StartTime.Value;

            if (request.EndTime.HasValue)
                availability.EndTime = request.EndTime.Value;

            if (request.PricePerSlot.HasValue)
            {
                if (request.PricePerSlot.Value <= 0)
                    throw new InvalidOperationException("Price per lesson must be greater than 0.");

                availability.PricePerSlot = request.PricePerSlot.Value;
            }

            availability.DayOfWeek = NormalizeDayOfWeek(availability.DayOfWeek);

            ValidateCourseTime(
                availability.StartCourseTime,
                availability.EndCourseTime,
                availability.StartTime,
                availability.EndTime);

            if (availability.Status == "Active")
            {
                await EnsureNoTutorAvailabilityConflictAsync(
                    availability.TutorId,
                    excludeAvailabilityId: availability.AvailabilityId,
                    dayOfWeek: availability.DayOfWeek,
                    startCourseTime: availability.StartCourseTime,
                    endCourseTime: availability.EndCourseTime,
                    startTime: availability.StartTime,
                    endTime: availability.EndTime);
            }

            var slotCount = CalculateSlotCount(
                availability.StartCourseTime,
                availability.EndCourseTime,
                availability.DayOfWeek);

            availability.Slot = slotCount;

            await _db.SaveChangesAsync();

            if (availability.SubjectId.HasValue)
            {
                availability.Subject = await _db.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectId == availability.SubjectId.Value);
            }

            return ToResponse(availability);
        }

        public async Task DeleteAsync(int tutorUserId, int availabilityId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var availability = await _db.Availabilities
                .FirstOrDefaultAsync(a =>
                    a.AvailabilityId == availabilityId &&
                    a.TutorId == tutor.TutorId)
                ?? throw new KeyNotFoundException("Availability not found.");

            availability.Status = "Inactive";

            await _db.SaveChangesAsync();
        }

        private async Task<Tutor> GetTutorByUserIdAsync(int userId)
        {
            return await _db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");
        }

        private async Task EnsureNoTutorAvailabilityConflictAsync(
            int tutorId,
            int? excludeAvailabilityId,
            string dayOfWeek,
            DateTime startCourseTime,
            DateTime endCourseTime,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            var normalizedDay = NormalizeDayOfWeek(dayOfWeek);

            var existingAvailabilities = await _db.Availabilities
                .Where(a =>
                    a.TutorId == tutorId &&
                    a.Status == "Active" &&
                    a.DayOfWeek == normalizedDay &&
                    (!excludeAvailabilityId.HasValue ||
                     a.AvailabilityId != excludeAvailabilityId.Value))
                .ToListAsync();

            var newStartDate = startCourseTime.Date;
            var newEndDate = endCourseTime.Date;

            var conflict = existingAvailabilities.FirstOrDefault(a =>
                DateRangesOverlap(
                    newStartDate,
                    newEndDate,
                    a.StartCourseTime.Date,
                    a.EndCourseTime.Date) &&
                TimeRangesOverlap(
                    startTime,
                    endTime,
                    a.StartTime,
                    a.EndTime));

            if (conflict != null)
            {
                throw new InvalidOperationException(
                    $"You already have availability on {normalizedDay} " +
                    $"from {conflict.StartTime:hh\\:mm} to {conflict.EndTime:hh\\:mm} " +
                    $"during this course date range.");
            }
        }

        private static AvailabilityResponse ToResponse(Availability a)
        {
            return new AvailabilityResponse
            {
                AvailabilityId = a.AvailabilityId,
                TutorId = a.TutorId,
                TutorUserId = a.Tutor?.UserId ?? 0,
                SubjectId = a.SubjectId,

                // Keep these only if your AvailabilityResponse DTO has them.
                //SubjectName = a.Subject?.Name,
                //TotalCoursePrice = a.PricePerSlot * Math.Max(1, a.Slot),

                DayOfWeek = a.DayOfWeek,
                StartCourseTime = a.StartCourseTime,
                EndCourseTime = a.EndCourseTime,
                StartTime = a.StartTime,
                EndTime = a.EndTime,

                // Slot means number of lessons, not capacity.
                Slot = a.Slot,
                //RemainingSlot = a.Slot,

                PricePerSlot = a.PricePerSlot,
                Status = a.Status,
                Mode = a.Mode,
                Level = a.Level
            };
        }

        private static void ValidateCourseTime(
            DateTime startCourseTime,
            DateTime endCourseTime,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            if (startCourseTime.Date > endCourseTime.Date)
                throw new InvalidOperationException("Start course time must be before end course time.");

            if (startTime >= endTime)
                throw new InvalidOperationException("Start time must be before end time.");
        }

        private static int CalculateSlotCount(
            DateTime startCourseTime,
            DateTime endCourseTime,
            string dayOfWeek)
        {
            if (!Enum.TryParse<DayOfWeek>(dayOfWeek, true, out var targetDay))
                throw new InvalidOperationException("Invalid day of week.");

            var startDate = startCourseTime.Date;
            var endDate = endCourseTime.Date;

            if (startDate > endDate)
                throw new InvalidOperationException("Start course time must be before end course time.");

            var count = 0;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek == targetDay)
                    count++;
            }

            if (count <= 0)
                throw new InvalidOperationException("No lesson found in this date range for selected day.");

            return count;
        }

        private static bool DateRangesOverlap(
            DateTime startA,
            DateTime endA,
            DateTime startB,
            DateTime endB)
        {
            return startA <= endB && startB <= endA;
        }

        private static bool TimeRangesOverlap(
            TimeSpan startA,
            TimeSpan endA,
            TimeSpan startB,
            TimeSpan endB)
        {
            // Allows 18:00-20:00 and 20:00-22:00.
            // Blocks 18:00-20:00 and 19:00-21:00.
            return startA < endB && startB < endA;
        }

        private static string NormalizeDayOfWeek(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Day of week is required.");

            if (!Enum.TryParse<DayOfWeek>(value.Trim(), true, out var day))
                throw new InvalidOperationException("Invalid day of week.");

            return day.ToString();
        }

        private static string NormalizeAvailabilityStatus(string value)
        {
            var status = value.Trim();

            if (status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                return "Active";

            if (status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                return "Inactive";

            throw new InvalidOperationException("Availability status must be Active or Inactive.");
        }
    }
}