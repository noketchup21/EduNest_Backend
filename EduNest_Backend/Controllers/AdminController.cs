using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.DTOs.Tutor;
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
        public async Task<ActionResult> TrackDownload(
            [FromBody] TrackAppMetricRequest request)
        {
            await _adminService.TrackDownloadAsync(request);
            return Ok(new { message = "Download tracked" });
        }

        [AllowAnonymous]
        [HttpPost("app/install")]
        public async Task<ActionResult> TrackInstall(
            [FromBody] TrackAppMetricRequest request)
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
        public async Task<ActionResult<List<TutorVerificationResponse>>> GetPendingTutors()
        {
            return Ok(await _adminService.GetPendingTutorsAsync());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("tutor/{tutorId:int}/verification")]
        public async Task<ActionResult<TutorVerificationResponse>> GetTutorVerification(
            int tutorId)
        {
            return Ok(await _adminService.GetTutorVerificationAsync(tutorId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("tutor/{tutorId:int}/approve")]
        public async Task<ActionResult<TutorVerificationResponse>> ApproveTutor(
            int tutorId)
        {
            return Ok(await _adminService.ApproveTutorAsync(tutorId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("tutor/{tutorId:int}/reject")]
        public async Task<ActionResult<TutorVerificationResponse>> RejectTutor(
            int tutorId,
            [FromBody] RejectTutorRequest request)
        {
            return Ok(await _adminService.RejectTutorAsync(
                tutorId,
                request.Reason));
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
        [HttpGet("payout/{payoutId:int}")]
        public async Task<ActionResult<AdminPayoutDetailResponse>> GetPayoutDetail(
            int payoutId)
        {
            return Ok(await _adminService.GetPayoutDetailAsync(payoutId));
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

        [Authorize(Roles = "Admin")]
        [HttpGet("tutor")]
        public async Task<ActionResult<List<TutorVerificationResponse>>> GetTutors()
        {
            return Ok(await _adminService.GetTutorsAsync());
        }
    }
}
