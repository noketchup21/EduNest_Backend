using System.Security.Claims;
using BusinessLayer.DTOs.Parent;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent")]
    public class ParentController : ControllerBase
    {
        private readonly IParentService _parentService;

        public ParentController(IParentService parentService)
        {
            _parentService = parentService;
        }
        // GET /api/parent/children
        [HttpGet("children")]
        public async Task<ActionResult<IEnumerable<ChildResponseDTO>>> GetMyChildrenAsync()
        {
            try
            {
                var parentUserId = GetCurrentUserId();
                var children = await _parentService.GetMyChildrenAsync(parentUserId);
                return Ok(children);
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

        // POST /api/parent/children/link
        [HttpPost("children/link")]
        public async Task<IActionResult> LinkChildAsync([FromBody] LinkChildDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ChildEmail))
                    return BadRequest(new { message = "Child email is required." });

                var parentUserId = GetCurrentUserId();
                var message = await _parentService.LinkChildAsync(parentUserId, dto);
                return Ok(new { message });
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

        // DELETE /api/parent/children/{studentId}/unlink
        [HttpDelete("children/{studentId}/unlink")]
        public async Task<IActionResult> UnlinkChildAsync(int studentId)
        {
            try
            {
                if (studentId <= 0)
                    return BadRequest(new { message = "Invalid student ID." });

                var parentUserId = GetCurrentUserId();
                var message = await _parentService.UnlinkChildAsync(parentUserId, studentId);
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
