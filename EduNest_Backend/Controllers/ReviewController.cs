using System.Security.Claims;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/review")]
    public sealed class ReviewController : ControllerBase
    {
        private readonly ITutorEngagementService _engagementService;

        public ReviewController(ITutorEngagementService engagementService)
        {
            _engagementService = engagementService;
        }

        [AllowAnonymous]
        [HttpGet("tutor/{tutorId:int}")]
        public async Task<ActionResult<List<TutorReviewResponse>>> GetTutorReviews(
            int tutorId)
        {
            return Ok(await _engagementService.GetTutorReviewsAsync(tutorId));
        }

        [Authorize(Roles = "Parent,Student")]
        [HttpGet("me")]
        public async Task<ActionResult<List<TutorReviewResponse>>> GetMyReviews()
        {
            return Ok(await _engagementService.GetMyReviewsAsync(CurrentUserId()));
        }

        [Authorize(Roles = "Parent,Student")]
        [HttpPost]
        public async Task<ActionResult<TutorReviewResponse>> Create(
            [FromBody] CreateTutorReviewRequest request)
        {
            return Ok(await _engagementService.CreateTutorReviewAsync(
                CurrentUserId(),
                request));
        }

        private int CurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("nameid") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("sub");

            if (!int.TryParse(raw, out var userId))
                throw new UnauthorizedAccessException("Invalid user token.");

            return userId;
        }
    }
}
