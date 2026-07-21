using System;
using System.Collections.Generic;
using System.Linq;
using BusinessLayer.DTOs.Booking;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class BookingService : IBookingService
    {
        private readonly EduNestDbContext _db;
        private static readonly TimeSpan PendingBookingLifetime = TimeSpan.FromMinutes(15);

        public BookingService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<BookingResponse> CreateBookingAsync(
            int userId,
            CreateBookingRequest request)
        {
            await ExpirePendingBookingsAsync();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            var availability = await _db.Availabilities
                .Include(a => a.Bookings)
                    .ThenInclude(b => b.Payments)
                .Include(a => a.Tutor)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a =>
                    a.AvailabilityId == request.AvailabilityId &&
                    a.Status == "Active")
                ?? throw new KeyNotFoundException("Availability not found or inactive.");

            if (availability.Slot <= 0)
                throw new InvalidOperationException("This course has no lessons.");

            var alreadyBooked = availability.Bookings.Any(b =>
                !b.IsDeleted &&
                (b.Status == "Confirmed" ||
                 b.Status == "Completed" ||
                 b.Payments.Any(p => p.Status == "Paid")));

            if (alreadyBooked)
                throw new InvalidOperationException("This course has already been booked.");

            var existingPendingBooking = await _db.Bookings
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Tutor)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b =>
                    b.UserId == user.UserId &&
                    b.AvailabilityId == availability.AvailabilityId &&
                    b.Status == "Pending" &&
                    !b.IsDeleted);

            if (existingPendingBooking != null)
            {
                return ToBookingResponse(
                    existingPendingBooking,
                    existingPendingBooking.Availability ?? availability);
            }

            var totalPrice = availability.PricePerSlot * Math.Max(1, availability.Slot);
            var isFreeCourse = totalPrice == 0;

            var booking = new Booking
            {
                AvailabilityId = availability.AvailabilityId,
                UserId = user.UserId,
                ParentId = null,
                StudentId = null,
                PriceAtBooking = totalPrice,
                Status = isFreeCourse ? "Confirmed" : "Pending",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Bookings.Add(booking);

            if (isFreeCourse)
                EnsureLessonsForBooking(booking, availability);

            await _db.SaveChangesAsync();

            return ToBookingResponse(booking, availability);
        }

        public async Task<List<BookingResponse>> GetMyBookingsAsync(int userId)
        {
            await ExpirePendingBookingsAsync();

            var bookings = await _db.Bookings
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Tutor)
                        .ThenInclude(t => t.User)
                .Where(b => b.UserId == userId && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bookings
                .Where(b => b.Availability != null)
                .Select(b => ToBookingResponse(b, b.Availability!))
                .ToList();
        }

        public async Task<BookingResponse> CancelBookingAsync(int userId, int bookingId)
        {
            await ExpirePendingBookingsAsync();

            var booking = await _db.Bookings
                .Include(b => b.Payments)
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Tutor)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.UserId == userId &&
                    !b.IsDeleted)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (booking.Status != "Pending")
                throw new InvalidOperationException("Only pending bookings can be cancelled.");

            var hasPaidPayment = booking.Payments.Any(p => p.Status == "Paid");

            if (hasPaidPayment)
                throw new InvalidOperationException("This booking has already been paid.");

            booking.Status = "Cancelled";

            foreach (var payment in booking.Payments.Where(p => p.Status == "Pending"))
            {
                payment.Status = "Cancelled";
            }

            await _db.SaveChangesAsync();

            return ToBookingResponse(booking, booking.Availability!);
        }

        public async Task<int> ExpirePendingBookingsAsync()
        {
            var expireBefore = DateTime.UtcNow.Subtract(PendingBookingLifetime);

            var bookings = await _db.Bookings
                .Include(b => b.Payments)
                .Where(b =>
                    b.Status == "Pending" &&
                    b.CreatedAt <= expireBefore &&
                    !b.IsDeleted)
                .ToListAsync();

            var expiredCount = 0;

            foreach (var booking in bookings)
            {
                var hasPaidPayment = booking.Payments.Any(p => p.Status == "Paid");

                if (hasPaidPayment)
                    continue;

                booking.Status = "Expired";
                expiredCount++;

                foreach (var payment in booking.Payments.Where(p => p.Status == "Pending"))
                {
                    payment.Status = "Expired";
                }
            }

            await _db.SaveChangesAsync();

            return expiredCount;
        }

        private void EnsureLessonsForBooking(Booking booking, Availability availability)
        {
            if (_db.Lessons.Any(l => l.BookingId == booking.BookingId))
                return;

            var days = ParseDaysOfWeek(availability.DayOfWeek);
            var vietnamTimeZone = GetVietnamTimeZone();
            var duration = Math.Max(
                30,
                (int)(availability.EndTime - availability.StartTime).TotalMinutes);

            for (var date = availability.StartCourseTime.Date;
                 date <= availability.EndCourseTime.Date;
                 date = date.AddDays(1))
            {
                if (!days.Contains(date.DayOfWeek))
                    continue;

                var localLessonTime = DateTime.SpecifyKind(
                    date.Add(availability.StartTime),
                    DateTimeKind.Unspecified);

                var utcLessonTime = TimeZoneInfo.ConvertTimeToUtc(
                    localLessonTime,
                    vietnamTimeZone);

                _db.Lessons.Add(new Lesson
                {
                    BookingId = booking.BookingId,
                    ScheduleTime = utcLessonTime,
                    Duration = duration,
                    Status = "Scheduled",
                    MeetingLink = string.Empty
                });
            }
        }

        private static BookingResponse ToBookingResponse(
            Booking booking,
            Availability availability)
        {
            return new BookingResponse
            {
                BookingId = booking.BookingId,
                AvailabilityId = booking.AvailabilityId,
                UserId = booking.UserId ?? 0,
                TutorId = availability.TutorId,
                TutorName = availability.Tutor?.User?.Name ?? $"Tutor #{availability.TutorId}",
                SubjectId = availability.SubjectId,
                DayOfWeek = availability.DayOfWeek,
                StartCourseTime = availability.StartCourseTime,
                EndCourseTime = availability.EndCourseTime,
                StartTime = availability.StartTime,
                EndTime = availability.EndTime,
                PriceAtBooking = booking.PriceAtBooking,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            };
        }

        private static HashSet<DayOfWeek> ParseDaysOfWeek(string value)
        {
            var days = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(day => Enum.TryParse<DayOfWeek>(day, true, out var parsed)
                    ? parsed
                    : DayOfWeek.Monday)
                .ToHashSet();

            return days.Count == 0
                ? new HashSet<DayOfWeek> { DayOfWeek.Monday }
                : days;
        }

        private static TimeZoneInfo GetVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
            catch
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
        }
    }
}
