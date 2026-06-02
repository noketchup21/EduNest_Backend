using System.Security.Claims;
using BusinessLayer.DTOs.Booking;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/booking")]
    [Authorize]
    public sealed class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost]
        public async Task<ActionResult<BookingResponse>> CreateBooking(
            [FromBody] CreateBookingRequest request)
        {
            return Ok(await _bookingService.CreateBookingAsync(CurrentUserId(), request));
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<BookingResponse>>> GetMyBookings()
        {
            return Ok(await _bookingService.GetMyBookingsAsync(CurrentUserId()));
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(raw, out var userId))
                throw new UnauthorizedAccessException();

            return userId;
        }
    }
}
