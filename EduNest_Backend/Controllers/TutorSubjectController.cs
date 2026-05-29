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
    public class TutorSubjectController : ControllerBase
    {
        private readonly ITutorSubjectService _tutorSubjectService;

        public TutorSubjectController(ITutorSubjectService tutorSubjectService)
        {
            _tutorSubjectService = tutorSubjectService;
        }

        // GET /api/tutorsubject/my-subjects
        [HttpGet("my-subjects")]
        [Authorize(Roles = "Tutor")]
        public async Task<ActionResult<IEnumerable<TutorSubjectResponseDTO>>> GetMySubjectsAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var subjects = await _tutorSubjectService.GetMySubjectsAsync(userId);
                return Ok(subjects);
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

        // GET /api/tutorsubject/tutor/{tutorId}
        [HttpGet("tutor/{tutorId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TutorSubjectResponseDTO>>> GetSubjectsByTutorAsync(
            int tutorId)
        {
            try
            {
                if (tutorId <= 0)
                    return BadRequest(new { message = "Invalid tutor ID." });

                var subjects = await _tutorSubjectService.GetSubjectsByTutorIdAsync(tutorId);
                if (!subjects.Any())
                    return NotFound(new { message = "No subjects found for this tutor." });

                return Ok(subjects);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/tutorsubject/subject/{subjectId}
        [HttpGet("subject/{subjectId}")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<TutorSubjectResponseDTO>>> GetTutorsBySubjectAsync(
            int subjectId)
        {
            try
            {
                if (subjectId <= 0)
                    return BadRequest(new { message = "Invalid subject ID." });

                var tutors = await _tutorSubjectService.GetTutorsBySubjectIdAsync(subjectId);
                if (!tutors.Any())
                    return NotFound(new { message = "No tutors found for this subject." });

                return Ok(tutors);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // POST /api/tutorsubject
        [HttpPost]
        [Authorize(Roles = "Tutor")]
        public async Task<ActionResult<TutorSubjectResponseDTO>> AddSubjectAsync(
            [FromBody] AddTutorSubjectDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var userId = GetCurrentUserId();
                var result = await _tutorSubjectService.AddSubjectAsync(userId, dto);
                return StatusCode(201, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE /api/tutorsubject/{subjectId}
        [HttpDelete("{subjectId}")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> RemoveSubjectAsync(int subjectId)
        {
            try
            {
                if (subjectId <= 0)
                    return BadRequest(new { message = "Invalid subject ID." });

                var userId = GetCurrentUserId();
                var message = await _tutorSubjectService.RemoveSubjectAsync(userId, subjectId);
                return Ok(new { message });
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
