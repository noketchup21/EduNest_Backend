using System.Security.Claims;
using BusinessLayer.DTOs.Profile;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public sealed class ProfileController : ControllerBase
    {
        private readonly EduNestDbContext _db;

        public ProfileController(EduNestDbContext db)
        {
            _db = db;
        }

        [HttpGet("me")]
        public async Task<ActionResult<MyProfileResponse>> GetMe()
        {
            var userId = CurrentUserId();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            Tutor? tutor = null;

            if (user.Role == "Tutor")
            {
                tutor = await _db.Tutors
                    .Include(t => t.BankAccount)
                    .FirstOrDefaultAsync(t => t.UserId == userId);
            }

            return Ok(ToProfileResponse(user, tutor));
        }

        [HttpPut("me")]
        public async Task<ActionResult<MyProfileResponse>> UpdateMe(
            [FromBody] UpdateMyProfileRequest request)
        {
            var userId = CurrentUserId();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Name is required." });

            user.Name = request.Name.Trim();
            user.Phone = string.IsNullOrWhiteSpace(request.Phone)
                ? null
                : request.Phone.Trim();

            Tutor? tutor = null;

            if (user.Role == "Tutor")
            {
                tutor = await _db.Tutors
                    .Include(t => t.BankAccount)
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor != null && request.TutorBio != null)
                {
                    tutor.Bio = request.TutorBio.Trim();
                }
            }

            await _db.SaveChangesAsync();

            return Ok(ToProfileResponse(user, tutor));
        }

        [Authorize(Roles = "Tutor")]
        [HttpPut("tutor-bank-account")]
        public async Task<ActionResult<MyProfileResponse>> UpdateTutorBankAccount(
            [FromBody] UpdateTutorBankAccountRequest request)
        {
            var userId = CurrentUserId();

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            var tutor = await _db.Tutors
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.UserId == userId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            if (string.IsNullOrWhiteSpace(request.BankName))
                return BadRequest(new { message = "Bank name is required." });

            if (string.IsNullOrWhiteSpace(request.AccountNumber))
                return BadRequest(new { message = "Account number is required." });

            if (string.IsNullOrWhiteSpace(request.AccountHolderName))
                return BadRequest(new { message = "Account holder name is required." });

            var bank = tutor.BankAccount;

            if (bank == null)
            {
                bank = new TutorBankAccount
                {
                    TutorId = tutor.TutorId
                };

                _db.Set<TutorBankAccount>().Add(bank);
                tutor.BankAccount = bank;
            }

            bank.BankName = request.BankName.Trim();
            bank.BankBin = string.IsNullOrWhiteSpace(request.BankBin)
                ? null
                : request.BankBin.Trim();
            bank.AccountNumber = request.AccountNumber.Trim();
            bank.AccountHolderName = request.AccountHolderName.Trim();
            bank.BranchName = string.IsNullOrWhiteSpace(request.BranchName)
                ? null
                : request.BranchName.Trim();
            bank.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(ToProfileResponse(user, tutor));
        }



        private static MyProfileResponse ToProfileResponse(User user, Tutor? tutor)
        {
            return new MyProfileResponse
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,

                TutorId = tutor?.TutorId,
                TutorBio = tutor?.Bio,
                IsVerified = tutor?.IsVerified,
                VerificationStatus = tutor?.VerificationStatus,

                BankName = tutor?.BankAccount?.BankName,
                BankBin = tutor?.BankAccount?.BankBin,
                AccountNumber = tutor?.BankAccount?.AccountNumber,
                AccountHolderName = tutor?.BankAccount?.AccountHolderName,
                BranchName = tutor?.BankAccount?.BranchName
            };
        }

        private int CurrentUserId()
        {
            var value =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("nameid") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("sub");

            if (!int.TryParse(value, out var userId))
                throw new UnauthorizedAccessException("Invalid user token.");

            return userId;
        }
    }
}
