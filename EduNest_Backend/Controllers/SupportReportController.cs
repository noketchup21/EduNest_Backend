using System.Security.Claims;
using BusinessLayer.DTOs.SupportReport;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/support-report")]
    [Authorize]
    public sealed class SupportReportController : ControllerBase
    {
        private readonly ISupportReportService _supportReportService;

        public SupportReportController(ISupportReportService supportReportService)
        {
            _supportReportService = supportReportService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<SupportReportResponse>> Create(
            [FromForm] CreateSupportReportRequest request)
        {
            try
            {
                return Ok(await _supportReportService.CreateAsync(
                    CurrentUserId(),
                    CurrentRole(),
                    request));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<SupportReportResponse>>> GetMine()
        {
            return Ok(await _supportReportService.GetMineAsync(CurrentUserId()));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<List<SupportReportResponse>>> AdminGetAll(
            [FromQuery] string? role,
            [FromQuery] string? status)
        {
            return Ok(await _supportReportService.AdminGetAllAsync(role, status));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{supportReportId:int}")]
        public async Task<ActionResult<SupportReportResponse>> AdminGetDetail(
            int supportReportId)
        {
            return Ok(await _supportReportService.AdminGetDetailAsync(supportReportId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/{supportReportId:int}/status")]
        public async Task<ActionResult<SupportReportResponse>> AdminUpdateStatus(
            int supportReportId,
            [FromBody] UpdateSupportReportStatusRequest request)
        {
            return Ok(await _supportReportService.AdminUpdateStatusAsync(
                supportReportId,
                request));
        }

        private int CurrentUserId()
        {
            var value =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("nameid") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("sub");

            return int.Parse(value!);
        }

        private string CurrentRole()
        {
            return User.FindFirstValue(ClaimTypes.Role) ??
                   User.FindFirstValue("role") ??
                   "User";
        }
    }
}
