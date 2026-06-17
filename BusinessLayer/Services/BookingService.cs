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

            if (availability.PricePerSlot <= 0)
                throw new InvalidOperationException("Invalid course price.");

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

            var booking = new Booking
            {
                AvailabilityId = availability.AvailabilityId,
                UserId = user.UserId,
                ParentId = null,
                StudentId = null,
                PriceAtBooking = totalPrice,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Bookings.Add(booking);
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
                PriceAtBooking = booking.PriceAtBooking,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
