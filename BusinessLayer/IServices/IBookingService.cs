using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Booking;

namespace BusinessLayer.IServices
{
    public interface IBookingService
    {
        Task<BookingResponse> CreateBookingAsync(int userId, CreateBookingRequest request);
        Task<List<BookingResponse>> GetMyBookingsAsync(int userId);
    }
}
