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
    public class ParentController : ControllerBase
    {
        private readonly IParentService _parentService;

        public ParentController(IParentService parentService)
        {
            _parentService = parentService;
        }

        // GET /api/parent
        [HttpGet]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ParentResponseDTO>>> GetAllParentsAsync()
        {
            try
            {
                var parents = await _parentService.GetAllParentsAsync();
                if (!parents.Any())
                    return NotFound(new { message = "No parents found." });
                return Ok(parents);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/parent/{parentId}
        [HttpGet("{parentId}")]
        //[Authorize(Roles = "Admin")]
        public async Task<ActionResult<ParentResponseDTO>> GetParentByIdAsync(int parentId)
        {
            try
            {
                if (parentId <= 0)
                    return BadRequest(new { message = "Invalid parent ID." });

                var parent = await _parentService.GetParentByIdAsync(parentId);
                if (parent == null)
                    return NotFound(new { message = $"Parent with ID {parentId} not found." });

                return Ok(parent);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/parent/profile
        [HttpGet("profile")]
        [Authorize(Roles = "Parent")]
        public async Task<ActionResult<ParentResponseDTO>> GetMyProfileAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                var parent = await _parentService.GetParentByUserIdAsync(userId);

                if (parent == null)
                    return NotFound(new { message = "Parent profile not found." });

                return Ok(parent);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT /api/parent/profile
        [HttpPut("profile")]
        [Authorize(Roles = "Parent")]
        public async Task<ActionResult<ParentResponseDTO>> UpdateProfileAsync(
            [FromBody] UpdateParentDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var userId = GetCurrentUserId();
                var updated = await _parentService.UpdateParentAsync(userId, dto);
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

        // DELETE /api/parent/profile
        [HttpDelete("profile")]
        [Authorize(Roles = "Parent")]
        public async Task<IActionResult> DeleteProfileAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                await _parentService.DeleteParentAsync(userId);
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

        // GET /api/parent/children
        [HttpGet("children")]
        [Authorize(Roles = "Parent")]
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
        [Authorize(Roles = "Parent")]
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
        [Authorize(Roles = "Parent")]
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
