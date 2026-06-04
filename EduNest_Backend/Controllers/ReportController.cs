using System.Security.Claims;
using BusinessLayer.DTOs.Report;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/report")]
    [Authorize]
    public sealed class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<TutorReportResponse>> CreateReport(
            [FromForm] CreateTutorReportRequest request)
        {
            try
            {
                return Ok(await _reportService.CreateReportAsync(
                    CurrentUserId(),
                    request));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<TutorReportResponse>>> GetMyReports()
        {
            return Ok(await _reportService.GetMyReportsAsync(CurrentUserId()));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<List<TutorReportResponse>>> AdminGetReports(
            [FromQuery] string? status)
        {
            return Ok(await _reportService.AdminGetReportsAsync(status));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{reportId:int}")]
        public async Task<ActionResult<TutorReportResponse>> AdminGetReport(int reportId)
        {
            return Ok(await _reportService.AdminGetReportAsync(reportId));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("admin/{reportId:int}/status")]
        public async Task<ActionResult<TutorReportResponse>> AdminUpdateReportStatus(
            int reportId,
            [FromBody] UpdateTutorReportStatusRequest request)
        {
            try
            {
                return Ok(await _reportService.AdminUpdateReportStatusAsync(
                    reportId,
                    request));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
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
    }
}
