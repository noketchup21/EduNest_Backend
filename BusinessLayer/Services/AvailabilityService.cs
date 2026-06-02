using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                .Include(a => a.Bookings)
                .Where(a => a.Status == "Active")
                .OrderBy(a => a.StartCourseTime)
                .ToListAsync();

            return data.Select(ToResponse).ToList();
        }

        public async Task<List<AvailabilityResponse>> GetByTutorAsync(int tutorId)
        {
            var data = await _db.Availabilities
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

            if (request.StartCourseTime > request.EndCourseTime)
                throw new InvalidOperationException("Start course time must be before end course time.");

            if (request.StartTime >= request.EndTime)
                throw new InvalidOperationException("Start time must be before end time.");

            var availability = new Availability
            {
                TutorId = tutor.TutorId,
                SubjectId = request.SubjectId,
                DayOfWeek = request.DayOfWeek,
                StartCourseTime = request.StartCourseTime,
                EndCourseTime = request.EndCourseTime,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Mode = request.Mode,
                Slot = request.Slot,
                Level = request.Level,
                PricePerSlot = request.PricePerSlot,
                Status = "Active"
            };

            _db.Availabilities.Add(availability);
            await _db.SaveChangesAsync();

            return ToResponse(availability);
        }

        public async Task<AvailabilityResponse> UpdateAsync(
            int tutorUserId,
            int availabilityId,
            UpdateAvailabilityRequest request)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            var availability = await _db.Availabilities
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a =>
                    a.AvailabilityId == availabilityId &&
                    a.TutorId == tutor.TutorId)
                ?? throw new KeyNotFoundException("Availability not found.");

            if (request.SubjectId.HasValue)
                availability.SubjectId = request.SubjectId;

            if (!string.IsNullOrWhiteSpace(request.DayOfWeek))
                availability.DayOfWeek = request.DayOfWeek;

            if (!string.IsNullOrWhiteSpace(request.Status))
                availability.Status = request.Status;

            if (!string.IsNullOrWhiteSpace(request.Mode))
                availability.Mode = request.Mode;

            if (!string.IsNullOrWhiteSpace(request.Level))
                availability.Level = request.Level;

            if (request.StartCourseTime.HasValue)
                availability.StartCourseTime = request.StartCourseTime.Value;

            if (request.EndCourseTime.HasValue)
                availability.EndCourseTime = request.EndCourseTime.Value;

            if (request.StartTime.HasValue)
                availability.StartTime = request.StartTime.Value;

            if (request.EndTime.HasValue)
                availability.EndTime = request.EndTime.Value;

            if (request.Slot.HasValue)
                availability.Slot = request.Slot.Value;

            if (request.PricePerSlot.HasValue)
                availability.PricePerSlot = request.PricePerSlot.Value;

            if (!string.IsNullOrWhiteSpace(request.Status))
                availability.Status = request.Status;

            if (availability.StartCourseTime > availability.EndCourseTime)
                throw new InvalidOperationException("Start course time must be before end course time.");

            if (availability.StartTime >= availability.EndTime)
                throw new InvalidOperationException("Start time must be before end time.");

            await _db.SaveChangesAsync();

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
            return await _db.Tutors.FirstOrDefaultAsync(t => t.UserId == userId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");
        }

        private static AvailabilityResponse ToResponse(Availability a)
        {
            var booked = a.Bookings?
                .Count(b =>
                    !b.IsDeleted &&
                    (b.Status == "Pending" || b.Status == "Confirmed"))
                ?? 0;

            return new AvailabilityResponse
            {
                AvailabilityId = a.AvailabilityId,
                TutorId = a.TutorId,
                SubjectId = a.SubjectId,
                DayOfWeek = a.DayOfWeek,
                StartCourseTime = a.StartCourseTime,
                EndCourseTime = a.EndCourseTime,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Slot = a.Slot,
                RemainingSlot = Math.Max(0, a.Slot - booked),
                PricePerSlot = a.PricePerSlot,
                Status = a.Status,
                    Mode = a.Mode,
                    Level = a.Level
            };
        }
    }
}
