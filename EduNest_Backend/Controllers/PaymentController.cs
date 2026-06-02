using System.Security.Claims;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public sealed class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize]
        [HttpPost("booking/{bookingId:int}/payos")]
        public async Task<ActionResult<CreatePaymentResponse>> CreatePayment(
            int bookingId)
        {
            return Ok(await _paymentService.CreatePayOsPaymentAsync(
                CurrentUserId(),
                bookingId));
        }

        [AllowAnonymous]
        [HttpPost("payos/webhook")]
        public async Task<IActionResult> PayOsWebhook(
            [FromBody] PayOsWebhookRequest request)
        {
            await _paymentService.HandlePayOsWebhookAsync(request);
            return Ok();
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return int.Parse(raw!);
        }
    }
}
