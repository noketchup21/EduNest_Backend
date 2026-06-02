using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Booking;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{

    public sealed class BookingService : IBookingService
    {
        private readonly EduNestDbContext _db;

        public BookingService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<BookingResponse> CreateBookingAsync(int userId, CreateBookingRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            var availability = await _db.Availabilities
                .Include(a => a.Bookings)
                .FirstOrDefaultAsync(a => a.AvailabilityId == request.AvailabilityId && a.Status == "Active")
                ?? throw new KeyNotFoundException("Availability not found or inactive.");

            var activeBookings = await _db.Bookings.CountAsync(b =>
                b.AvailabilityId == availability.AvailabilityId &&
                !b.IsDeleted &&
                (b.Status == "Pending" || b.Status == "Confirmed"));

            if (availability.Slot > 0 && activeBookings >= availability.Slot)
                throw new InvalidOperationException("This course is already full.");

            var price = availability.PricePerSlot * Math.Max(1, availability.Slot);

            var booking = new Booking
            {
                AvailabilityId = availability.AvailabilityId,
                UserId = user.UserId,
                ParentId = null,
                StudentId = null,
                PriceAtBooking = price,
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
            var bookings = await _db.Bookings
                .Include(b => b.Availability)
                .Where(b => b.UserId == userId && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return bookings.Select(b => ToBookingResponse(b, b.Availability)).ToList();
        }

        private static BookingResponse ToBookingResponse(Booking b, Availability a) => new()
        {
            BookingId = b.BookingId,
            AvailabilityId = b.AvailabilityId,
            UserId = b.UserId ?? 0,
            TutorId = a.TutorId,
            SubjectId = a.SubjectId,
            PriceAtBooking = b.PriceAtBooking,
            Status = b.Status,
            CreatedAt = b.CreatedAt
        };
    }
}
