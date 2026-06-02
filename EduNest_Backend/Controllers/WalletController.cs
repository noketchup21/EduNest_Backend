using System.Security.Claims;
using BusinessLayer.DTOs.Wallet;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    [Authorize]
    public sealed class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;

        public WalletController(IWalletService walletService)
        {
            _walletService = walletService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<WalletResponse>> GetWallet()
        {
            return Ok(await _walletService.GetTutorWalletAsync(CurrentUserId()));
        }

        [HttpGet("transaction")]
        public async Task<ActionResult<List<WalletTransactionResponse>>> GetTransactions()
        {
            return Ok(await _walletService.GetTutorWalletTransactionsAsync(CurrentUserId()));
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return int.Parse(raw!);
        }
    }
}
