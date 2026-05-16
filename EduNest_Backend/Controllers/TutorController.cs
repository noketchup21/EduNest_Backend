using System.Security.Claims;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly ITutorService _tutorService;

        public TutorController(ITutorService tutorService)
        {
            _tutorService = tutorService;
        }

        // GET /api/tutor
        [HttpGet]
        [AllowAnonymous] // anyone can browse tutors
        public async Task<ActionResult<IEnumerable<TutorResponseDTO>>> GetAllTutorsAsync()
        {
            try
            {
                var tutors = await _tutorService.GetAllTutorsAsync();
                if (!tutors.Any())
                    return NotFound(new { message = "No tutors found." });
                return Ok(tutors);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/tutor/{id}
        [HttpGet("{tutorId}")]
        [AllowAnonymous]
        public async Task<ActionResult<TutorResponseDTO>> GetTutorByIdAsync(int tutorId)
        {
            try
            {
                if (tutorId <= 0)
                    return BadRequest(new { message = "Invalid tutor ID." });

                var tutor = await _tutorService.GetTutorByIdAsync(tutorId);
                if (tutor == null)
                    return NotFound(new { message = $"Tutor with ID {tutorId} not found." });

                return Ok(tutor);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/tutor/profile
        [HttpGet("profile")]
        [Authorize(Roles = "Tutor")]
        public async Task<ActionResult<TutorResponseDTO>> GetMyProfileAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tutor = await _tutorService.GetTutorByUserIdAsync(userId);

                if (tutor == null)
                    return NotFound(new { message = "Tutor profile not found." });

                return Ok(tutor);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT /api/tutor/profile
        [HttpPut("profile")]
        [Authorize(Roles = "Tutor")]
        public async Task<ActionResult<TutorResponseDTO>> UpdateProfileAsync(
            [FromBody] UpdateTutorDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var userId = GetCurrentUserId();
                var updated = await _tutorService.UpdateTutorAsync(userId, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE /api/tutor/profile
        [HttpDelete("profile")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> DeleteProfileAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _tutorService.DeleteTutorAsync(userId);
                return Ok(new { message = "Account deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Invalid token.");
            return int.Parse(userIdClaim);
        }
    }
}
