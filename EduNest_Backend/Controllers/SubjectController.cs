using BusinessLayer.DTOs.Subject;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubjectController : ControllerBase
    {
        private readonly ISubjectService _subjectService;

        public SubjectController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        // GET /api/subject
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SubjectResponseDTO>>> GetAllAsync()
        {
            try
            {
                var subjects = await _subjectService.GetAllSubjectsAsync();
                if (!subjects.Any())
                    return NotFound(new { message = "No subjects found." });
                return Ok(subjects);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/subject/{id}
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<SubjectResponseDTO>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid subject ID." });

                var subject = await _subjectService.GetSubjectByIdAsync(id);
                if (subject == null)
                    return NotFound(new { message = $"Subject with ID {id} not found." });

                return Ok(subject);
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // POST /api/subject
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubjectResponseDTO>> CreateAsync(
            [FromBody] CreateSubjectDTO dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest(new { message = "Subject name is required." });

                var created = await _subjectService.CreateSubjectAsync(dto);
                return StatusCode(201, created);
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

        // PUT /api/subject/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubjectResponseDTO>> UpdateAsync(
            int id, [FromBody] UpdateSubjectDTO dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid subject ID." });

                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var updated = await _subjectService.UpdateSubjectAsync(id, dto);
                return Ok(updated);
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

        // DELETE /api/subject/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid subject ID." });

                var message = await _subjectService.DeleteSubjectAsync(id);
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
    }
}
