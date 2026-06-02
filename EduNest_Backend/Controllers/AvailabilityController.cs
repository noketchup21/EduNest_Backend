using System.Security.Claims;
using BusinessLayer.DTOs.Availability;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/availability")]
    public sealed class AvailabilityController : ControllerBase
    {
        private readonly IAvailabilityService _availabilityService;

        public AvailabilityController(IAvailabilityService availabilityService)
        {
            _availabilityService = availabilityService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AvailabilityResponse>>> GetAll()
        {
            return Ok(await _availabilityService.GetAllAsync());
        }

        [HttpGet("tutor/{tutorId:int}")]
        public async Task<ActionResult<List<AvailabilityResponse>>> GetByTutor(int tutorId)
        {
            return Ok(await _availabilityService.GetByTutorAsync(tutorId));
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<List<AvailabilityResponse>>> GetMyAvailability()
        {
            return Ok(await _availabilityService.GetMyAvailabilityAsync(CurrentUserId()));
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<AvailabilityResponse>> Create(
            [FromBody] CreateAvailabilityRequest request)
        {
            return Ok(await _availabilityService.CreateAsync(CurrentUserId(), request));
        }

        [Authorize]
        [HttpPut("{availabilityId:int}")]
        public async Task<ActionResult<AvailabilityResponse>> Update(
            int availabilityId,
            [FromBody] UpdateAvailabilityRequest request)
        {
            return Ok(await _availabilityService.UpdateAsync(
                CurrentUserId(),
                availabilityId,
                request));
        }

        [Authorize]
        [HttpDelete("{availabilityId:int}")]
        public async Task<IActionResult> Delete(int availabilityId)
        {
            await _availabilityService.DeleteAsync(CurrentUserId(), availabilityId);
            return NoContent();
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            if (!int.TryParse(raw, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");

            return userId;
        }
    }
}
