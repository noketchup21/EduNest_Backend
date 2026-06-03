using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public sealed class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [AllowAnonymous]
        [HttpPost("app/download")]
        public async Task<ActionResult> TrackDownload([FromBody] TrackAppMetricRequest request)
        {
            await _adminService.TrackDownloadAsync(request);
            return Ok(new { message = "Download tracked" });
        }

        [AllowAnonymous]
        [HttpPost("app/install")]
        public async Task<ActionResult> TrackInstall([FromBody] TrackAppMetricRequest request)
        {
            await _adminService.TrackInstallAsync(request);
            return Ok(new { message = "Install tracked" });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardResponse>> GetDashboard()
        {
            return Ok(await _adminService.GetDashboardAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("tutor/pending")]
        public async Task<ActionResult<List<AdminTutorResponse>>> GetPendingTutors()
        {
            return Ok(await _adminService.GetPendingTutorsAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("tutor/{tutorId:int}/approve")]
        public async Task<ActionResult<AdminTutorResponse>> ApproveTutor(int tutorId)
        {
            return Ok(await _adminService.ApproveTutorAsync(tutorId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("tutor/{tutorId:int}/reject")]
        public async Task<ActionResult<AdminTutorResponse>> RejectTutor(int tutorId)
        {
            return Ok(await _adminService.RejectTutorAsync(tutorId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("subject")]
        public async Task<ActionResult<SubjectResponseDTO>> CreateSubject(
            [FromBody] CreateSubjectDTO request)
        {
            return Ok(await _adminService.CreateSubjectAsync(request));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("payout")]
        public async Task<ActionResult<List<PayoutResponse>>> GetPayouts()
        {
            return Ok(await _adminService.GetPayoutsAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("payout/{payoutId:int}")]
        public async Task<ActionResult<PayoutResponse>> UpdatePayoutStatus(
            int payoutId,
            [FromBody] AdminUpdatePayoutRequest request)
        {
            return Ok(await _adminService.UpdatePayoutStatusAsync(
                payoutId,
                request.Status));
        }
    }
}
