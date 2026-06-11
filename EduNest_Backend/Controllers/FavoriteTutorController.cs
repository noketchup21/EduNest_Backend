using System.Security.Claims;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/favorite-tutor")]
    [Authorize(Roles = "Parent,Student")]
    public sealed class FavoriteTutorController : ControllerBase
    {
        private readonly ITutorEngagementService _engagementService;

        public FavoriteTutorController(ITutorEngagementService engagementService)
        {
            _engagementService = engagementService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<FavoriteTutorResponse>>> GetMine()
        {
            return Ok(await _engagementService.GetFavoriteTutorsAsync(CurrentUserId()));
        }

        [HttpPost("{tutorId:int}")]
        public async Task<ActionResult<FavoriteTutorResponse>> Save(int tutorId)
        {
            return Ok(await _engagementService.SaveFavoriteTutorAsync(
                CurrentUserId(),
                tutorId));
        }

        [HttpPost]
        public async Task<ActionResult<FavoriteTutorResponse>> SaveFromBody(
            [FromBody] SaveFavoriteTutorRequest request)
        {
            return Ok(await _engagementService.SaveFavoriteTutorAsync(
                CurrentUserId(),
                request.TutorId));
        }

        [HttpDelete("{tutorId:int}")]
        public async Task<IActionResult> Unsave(int tutorId)
        {
            await _engagementService.UnsaveFavoriteTutorAsync(
                CurrentUserId(),
                tutorId);

            return NoContent();
        }

        [HttpDelete]
        public async Task<IActionResult> UnsaveFromBody(
            [FromBody] SaveFavoriteTutorRequest request)
        {
            await _engagementService.UnsaveFavoriteTutorAsync(
                CurrentUserId(),
                request.TutorId);

            return NoContent();
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

    public sealed class SaveFavoriteTutorRequest
    {
        public int TutorId { get; set; }
    }
}
