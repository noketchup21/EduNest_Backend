using System.Security.Claims;
using BusinessLayer.DTOs.Homework;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/homework")]
    public sealed class HomeworkController : ControllerBase
    {
        private readonly IHomeworkService _homeworkService;

        public HomeworkController(IHomeworkService homeworkService)
        {
            _homeworkService = homeworkService;
        }

        [HttpGet("lesson/{lessonId:int}")]
        public async Task<ActionResult<List<HomeworkResponse>>> GetByLesson(int lessonId)
        {
            try
            {
                return Ok(await _homeworkService.GetByLessonAsync(CurrentUserId(), lessonId));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("{homeworkId:int}")]
        public async Task<ActionResult<HomeworkResponse>> GetById(int homeworkId)
        {
            try
            {
                return Ok(await _homeworkService.GetByIdAsync(CurrentUserId(), homeworkId));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPost("lesson/{lessonId:int}")]
        public async Task<ActionResult<HomeworkResponse>> CreateForLesson(
            int lessonId,
            [FromBody] CreateHomeworkRequest request)
        {
            try
            {
                return Ok(await _homeworkService.CreateForLessonAsync(
                    CurrentUserId(),
                    lessonId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPut("{homeworkId:int}")]
        public async Task<ActionResult<HomeworkResponse>> Update(
            int homeworkId,
            [FromBody] UpdateHomeworkRequest request)
        {
            try
            {
                return Ok(await _homeworkService.UpdateAsync(
                    CurrentUserId(),
                    homeworkId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpDelete("{homeworkId:int}")]
        public async Task<IActionResult> Delete(int homeworkId)
        {
            try
            {
                await _homeworkService.DeleteAsync(CurrentUserId(), homeworkId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost("{homeworkId:int}/submit")]
        public async Task<ActionResult<HomeworkSubmissionResponse>> Submit(
            int homeworkId,
            [FromBody] SubmitHomeworkRequest request)
        {
            try
            {
                return Ok(await _homeworkService.SubmitAsync(
                    CurrentUserId(),
                    homeworkId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPatch("{homeworkId:int}/submissions/{submissionId:int}/grade")]
        public async Task<ActionResult<HomeworkSubmissionResponse>> GradeEssaySubmission(
            int homeworkId,
            int submissionId,
            [FromBody] GradeEssaySubmissionRequest request)
        {
            try
            {
                return Ok(await _homeworkService.GradeEssaySubmissionAsync(
                    CurrentUserId(),
                    homeworkId,
                    submissionId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private ActionResult HandleException(Exception ex)
        {
            return ex switch
            {
                KeyNotFoundException => NotFound(new { message = ex.Message }),
                UnauthorizedAccessException => Forbid(),
                InvalidOperationException => BadRequest(new { message = ex.Message }),
                ArgumentException => BadRequest(new { message = ex.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Server error." })
            };
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
