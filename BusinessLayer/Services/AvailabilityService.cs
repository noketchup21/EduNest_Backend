using BusinessLayer.DTOs.Availability;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class AvailabilityService : IAvailabilityService
    {
        private readonly EduNestDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;

        public AvailabilityService(
            EduNestDbContext db,
            ICloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<AvailabilityResponse>> GetAllAsync()
        {
            var data = await _db.Availabilities
                .Include(a => a.Tutor)
                    .ThenInclude(t => t.User)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
.Where(a =>
    a.Status == "Active" &&
    a.Tutor.IsVerified &&
    a.Tutor.VerificationStatus == "Approved" &&
    !a.Bookings.Any(b =>
        !b.IsDeleted &&
        (b.Status == "Confirmed" ||
         b.Status == "Completed" ||
         b.Payments.Any(p => p.Status == "Paid"))))
                .OrderBy(a => a.Tutor.User.Name)
                .ThenBy(a => a.StartCourseTime)
                .ToListAsync();

            return data.Select(ToResponse).ToList();
        }

        public async Task<List<AvailabilityResponse>> GetByTutorAsync(int tutorId)
        {
            var data = await _db.Availabilities
                .Include(a => a.Tutor)
                    .ThenInclude(t => t.User)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .Where(a =>
                    a.TutorId == tutorId &&
                    a.Status == "Active" &&
                    !a.Bookings.Any(b =>
                        !b.IsDeleted &&
                        (b.Status == "Confirmed" ||
                         b.Status == "Completed" ||
                         b.Payments.Any(p => p.Status == "Paid"))))
                .OrderBy(a => a.StartCourseTime)
                .ToListAsync();

            return data.Select(ToResponse).ToList();
        }

        public async Task<List<AvailabilityResponse>> GetMyAvailabilityAsync(int tutorUserId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var data = await _db.Availabilities
                .Include(a => a.Tutor)
                    .ThenInclude(t => t.User)
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
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == tutorUserId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            var normalizedDays = NormalizeDaysOfWeek(
                request.DaysOfWeek,
                request.DayOfWeek);
            var normalizedDayText = string.Join(",", normalizedDays);

            var startCourseDate = ToUtcDateOnly(request.StartCourseTime);
            var endCourseDate = ToUtcDateOnly(request.EndCourseTime);

            ValidateCourseTime(
                startCourseDate,
                endCourseDate,
                request.StartTime,
                request.EndTime);

            if (!tutor.IsVerified || tutor.VerificationStatus != "Approved")
            {
                throw new InvalidOperationException(
                    "Your tutor profile is waiting for admin approval. You cannot create availability yet.");
            }

            if (request.PricePerSlot < 0)
                throw new InvalidOperationException("Price per lesson cannot be negative.");

            var mode = NormalizeMode(request.Mode);
            var offlineAreas = NormalizeOfflineAreas(mode, request.OfflineAreas);
            var description = NormalizeDescription(request.Description);

            await EnsureNoTutorAvailabilityConflictAsync(
                tutor.TutorId,
                excludeAvailabilityId: null,
                dayOfWeeks: normalizedDays,
                startCourseTime: startCourseDate,
                endCourseTime: endCourseDate,
                startTime: request.StartTime,
                endTime: request.EndTime);

            var slotCount = CalculateSlotCount(
                startCourseDate,
                endCourseDate,
                normalizedDays);

            var availability = new Availability
            {
                TutorId = tutor.TutorId,
                SubjectId = request.SubjectId,
                DayOfWeek = normalizedDayText,
                StartCourseTime = startCourseDate,
                EndCourseTime = endCourseDate,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Mode = mode,
                OfflineAreas = offlineAreas,
                Description = description,
                Level = "General",
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
                    .ThenInclude(t => t.User)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a =>
                    a.AvailabilityId == availabilityId &&
                    a.TutorId == tutor.TutorId)
                ?? throw new KeyNotFoundException("Availability not found.");

            if (request.SubjectId.HasValue)
                availability.SubjectId = request.SubjectId;

            if (request.DaysOfWeek?.Any() == true ||
                !string.IsNullOrWhiteSpace(request.DayOfWeek))
            {
                availability.DayOfWeek = string.Join(
                    ",",
                    NormalizeDaysOfWeek(request.DaysOfWeek, request.DayOfWeek));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
                availability.Status = NormalizeAvailabilityStatus(request.Status);

            if (!string.IsNullOrWhiteSpace(request.Mode))
                availability.Mode = NormalizeMode(request.Mode);

            if (request.OfflineAreas != null)
                availability.OfflineAreas = request.OfflineAreas.Trim();

            if (request.Description != null)
                availability.Description = NormalizeDescription(request.Description);

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
                if (request.PricePerSlot.Value < 0)
                    throw new InvalidOperationException("Price per lesson cannot be negative.");

                availability.PricePerSlot = request.PricePerSlot.Value;
            }

            var normalizedDays = NormalizeDaysOfWeek(null, availability.DayOfWeek);
            availability.DayOfWeek = string.Join(",", normalizedDays);
            availability.OfflineAreas = NormalizeOfflineAreas(
                availability.Mode,
                availability.OfflineAreas);
            availability.StartCourseTime = ToUtcDateOnly(availability.StartCourseTime);
            availability.EndCourseTime = ToUtcDateOnly(availability.EndCourseTime);

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
                    dayOfWeeks: normalizedDays,
                    startCourseTime: availability.StartCourseTime,
                    endCourseTime: availability.EndCourseTime,
                    startTime: availability.StartTime,
                    endTime: availability.EndTime);
            }

            availability.Slot = CalculateSlotCount(
                availability.StartCourseTime,
                availability.EndCourseTime,
                normalizedDays);

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

        public async Task<AvailabilityResponse> SetStatusAsync(
    int tutorUserId,
    int availabilityId,
    string status)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var availability = await _db.Availabilities
                .Include(a => a.Tutor)
                    .ThenInclude(t => t.User)
                .Include(a => a.Subject)
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a =>
                    a.AvailabilityId == availabilityId &&
                    a.TutorId == tutor.TutorId)
                ?? throw new KeyNotFoundException("Availability not found.");

            var normalizedStatus = NormalizeAvailabilityStatus(status);

            if (HasActiveBooking(availability))
            {
                throw new InvalidOperationException(
                    "This course already has booking activity and cannot be enabled or disabled.");
            }

            if (normalizedStatus == "Active")
            {
                await EnsureNoTutorAvailabilityConflictAsync(
                    availability.TutorId,
                    excludeAvailabilityId: availability.AvailabilityId,
                    dayOfWeeks: NormalizeDaysOfWeek(null, availability.DayOfWeek),
                    startCourseTime: availability.StartCourseTime,
                    endCourseTime: availability.EndCourseTime,
                    startTime: availability.StartTime,
                    endTime: availability.EndTime);
            }

            availability.Status = normalizedStatus;

            await _db.SaveChangesAsync();

            return ToResponse(availability);
        }

        private static bool HasActiveBooking(Availability availability)
        {
            return availability.Bookings.Any(b =>
                !b.IsDeleted &&
                b.Status != "Cancelled" &&
                b.Status != "Expired");
        }

        private async Task<Tutor> GetTutorByUserIdAsync(int userId)
        {
            return await _db.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");
        }

        private async Task EnsureNoTutorAvailabilityConflictAsync(
            int tutorId,
            int? excludeAvailabilityId,
            IReadOnlyCollection<string> dayOfWeeks,
            DateTime startCourseTime,
            DateTime endCourseTime,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            var normalizedDays = dayOfWeeks
                .Select(NormalizeDayOfWeek)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingAvailabilities = await _db.Availabilities
                .Where(a =>
                    a.TutorId == tutorId &&
                    a.Status == "Active" &&
                    (!excludeAvailabilityId.HasValue ||
                     a.AvailabilityId != excludeAvailabilityId.Value))
                .ToListAsync();

            var newStartDate = startCourseTime.Date;
            var newEndDate = endCourseTime.Date;

            var conflict = existingAvailabilities.FirstOrDefault(a =>
                NormalizeDaysOfWeek(null, a.DayOfWeek)
                    .Any(day => normalizedDays.Contains(day)) &&
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
                var conflictDays = string.Join(
                    ", ",
                    NormalizeDaysOfWeek(null, conflict.DayOfWeek)
                        .Where(day => normalizedDays.Contains(day)));

                throw new InvalidOperationException(
                    $"You already have availability on {conflictDays} " +
                    $"from {conflict.StartTime:hh\\:mm} to {conflict.EndTime:hh\\:mm} " +
                    $"during this course date range.");
            }
        }

        private AvailabilityResponse ToResponse(Availability a)
        {
            var hasBookings = HasActiveBooking(a);
            var isPubliclyBookable = !HasPaidOrConfirmedBooking(a);

            return new AvailabilityResponse
            {
                AvailabilityId = a.AvailabilityId,
                TutorId = a.TutorId,
                TutorUserId = a.Tutor?.UserId ?? 0,
                TutorName = a.Tutor?.User?.Name ?? $"Tutor #{a.TutorId}",
                TutorAvatarUrl = AvatarUrl(a.Tutor?.User),

                SubjectId = a.SubjectId,
                SubjectName = a.Subject?.Name,

                DayOfWeek = a.DayOfWeek,
                DaysOfWeek = NormalizeDaysOfWeek(null, a.DayOfWeek).ToList(),
                StartCourseTime = a.StartCourseTime,
                EndCourseTime = a.EndCourseTime,
                StartTime = a.StartTime,
                EndTime = a.EndTime,

                Slot = a.Slot,
                RemainingSlot = isPubliclyBookable ? 1 : 0,
                PricePerSlot = a.PricePerSlot,
                TotalCoursePrice = a.PricePerSlot * Math.Max(1, a.Slot),

                Status = a.Status,
                Mode = a.Mode,
                OfflineAreas = a.OfflineAreas,
                Description = a.Description,
                Level = a.Level,

                HasBookings = hasBookings
            };
        }

        private static DateTime ToUtcDateOnly(DateTime value)
        {
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
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
            IReadOnlyCollection<string> dayOfWeeks)
        {
            var targetDays = dayOfWeeks
                .Select(day =>
                {
                    if (!Enum.TryParse<DayOfWeek>(day, true, out var parsed))
                        throw new InvalidOperationException("Invalid day of week.");

                    return parsed;
                })
                .ToHashSet();

            var startDate = startCourseTime.Date;
            var endDate = endCourseTime.Date;

            if (startDate > endDate)
                throw new InvalidOperationException("Start course time must be before end course time.");

            var count = 0;

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (targetDays.Contains(date.DayOfWeek))
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

        private static IReadOnlyList<string> NormalizeDaysOfWeek(
            IEnumerable<string>? daysOfWeek,
            string? fallbackDayOfWeek)
        {
            var values = daysOfWeek?
                .Where(day => !string.IsNullOrWhiteSpace(day))
                .Select(day => day.Trim())
                .ToList() ?? new List<string>();

            if (values.Count == 0 && !string.IsNullOrWhiteSpace(fallbackDayOfWeek))
            {
                values = fallbackDayOfWeek
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }

            if (values.Count == 0)
                throw new InvalidOperationException("Choose at least one day of week.");

            var normalized = values
                .Select(NormalizeDayOfWeek)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(DaySortOrder)
                .ToList();

            return normalized;
        }

        private static int DaySortOrder(string day)
        {
            return Enum.Parse<DayOfWeek>(day) switch
            {
                DayOfWeek.Monday => 1,
                DayOfWeek.Tuesday => 2,
                DayOfWeek.Wednesday => 3,
                DayOfWeek.Thursday => 4,
                DayOfWeek.Friday => 5,
                DayOfWeek.Saturday => 6,
                DayOfWeek.Sunday => 7,
                _ => 8
            };
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

        private static bool HasPaidOrConfirmedBooking(Availability availability)
        {
            return availability.Bookings.Any(b =>
                !b.IsDeleted &&
                (b.Status == "Confirmed" ||
                 b.Status == "Completed" ||
                 b.Payments.Any(p => p.Status == "Paid")));
        }

        private static string NormalizeMode(string value)
        {
            var mode = value.Trim();

            if (mode.Equals("Online", StringComparison.OrdinalIgnoreCase))
                return "Online";

            if (mode.Equals("Offline", StringComparison.OrdinalIgnoreCase))
                return "Offline";

            throw new InvalidOperationException("Mode must be Online or Offline.");
        }

        private static string? NormalizeOfflineAreas(string mode, string? offlineAreas)
        {
            if (!mode.Equals("Offline", StringComparison.OrdinalIgnoreCase))
                return null;

            var areas = offlineAreas?.Trim();

            if (string.IsNullOrWhiteSpace(areas))
            {
                throw new InvalidOperationException(
                    "Offline tutoring areas are required for offline availability.");
            }

            if (areas.Length > 500)
                throw new InvalidOperationException("Offline tutoring areas must be 500 characters or less.");

            return areas;
        }

        private static string? NormalizeDescription(string? description)
        {
            var value = description?.Trim();

            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (value.Length > 1000)
                throw new InvalidOperationException("Description must be 1000 characters or less.");

            return value;
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
