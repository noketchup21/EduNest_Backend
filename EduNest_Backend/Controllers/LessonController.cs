using System.Security.Claims;
using BusinessLayer.DTOs.Attendance;
using BusinessLayer.DTOs.Lesson;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/lesson")]
    [Authorize]
    public sealed class LessonController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<LessonResponse>>> GetMyLessons()
        {
            return Ok(await _lessonService.GetMyLessonsAsync(CurrentUserId()));
        }

        [HttpPost("booking/{bookingId:int}")]
        public async Task<ActionResult<LessonResponse>> AddLesson(
            int bookingId,
            CreateLessonRequest request)
        {
            return Ok(await _lessonService.AddLessonAsync(
                CurrentUserId(),
                bookingId,
                request));
        }

        [HttpPost("{lessonId:int}/attendance")]
        public async Task<ActionResult<LessonResponse>> MarkAttendance(
            int lessonId,
            MarkAttendanceRequest request)
        {
            try
            {
                return Ok(await _lessonService.MarkAttendanceAsync(
                    CurrentUserId(),
                    lessonId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpPost("{lessonId:int}/complete")]
        public async Task<ActionResult<LessonResponse>> CompleteLesson(
            int lessonId,
            CompleteLessonRequest request)
        {
            try
            {
                return Ok(await _lessonService.CompleteLessonAsync(
                    CurrentUserId(),
                    lessonId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize]
        [HttpGet("{lessonId:int}/detail")]
        public async Task<ActionResult<LessonDetailResponse>> GetLessonDetail(int lessonId)
        {
            return Ok(await _lessonService.GetLessonDetailAsync(CurrentUserId(), lessonId));
        }

        [Authorize]
        [HttpPost("{lessonId:int}/meeting-link")]
        public async Task<ActionResult<LessonDetailResponse>> SetMeetingLink(
            int lessonId,
            [FromBody] SetMeetingLinkRequest request)
        {
            return Ok(await _lessonService.SetMeetingLinkAsync(
                CurrentUserId(),
                lessonId,
                request.MeetingLink));
        }

        [Authorize]
        [HttpPost("{lessonId:int}/complete-group")]
        public async Task<ActionResult<LessonDetailResponse>> CompleteLessonGroup(int lessonId)
        {
            try
            {
                return Ok(await _lessonService.CompleteLessonGroupAsync(
                    CurrentUserId(),
                    lessonId));
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

                InvalidOperationException => Conflict(new { message = ex.Message }),

                ArgumentException => BadRequest(new { message = ex.Message }),

                _ => StatusCode(500, new { message = "An unexpected error occurred." })
            };
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return int.Parse(raw!);
        }
    }
}
