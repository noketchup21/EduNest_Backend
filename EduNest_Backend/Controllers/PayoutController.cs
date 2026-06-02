using System.Security.Claims;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/payout")]
    [Authorize]
    public sealed class PayoutController : ControllerBase
    {
        private readonly IPayoutService _payoutService;

        public PayoutController(IPayoutService payoutService)
        {
            _payoutService = payoutService;
        }

        [HttpPost]
        public async Task<ActionResult<PayoutResponse>> RequestPayout(
            RequestPayoutRequest request)
        {
            return Ok(await _payoutService.RequestPayoutAsync(
                CurrentUserId(),
                request));
        }

        [HttpGet("me")]
        public async Task<ActionResult<List<PayoutResponse>>> GetMyPayouts()
        {
            return Ok(await _payoutService.GetPayoutsAsync(CurrentUserId()));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{payoutId:int}")]
        public async Task<ActionResult<PayoutResponse>> UpdatePayout(
            int payoutId,
            AdminUpdatePayoutRequest request)
        {
            return Ok(await _payoutService.AdminUpdatePayoutAsync(
                payoutId,
                request));
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return int.Parse(raw!);
        }
    }
}
