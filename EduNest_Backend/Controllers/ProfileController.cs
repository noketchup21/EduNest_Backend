using System.Security.Claims;
using BusinessLayer.DTOs.Profile;
using BusinessLayer.IServices;
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
        private readonly ICloudinaryService _cloudinaryService;

        public ProfileController(EduNestDbContext db, ICloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
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

        [Authorize]
        [HttpPut("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AvatarResponse>> UploadAvatar(
    [FromForm] IFormFile avatar)
        {
            var userId = CurrentUserId();

            if (avatar == null || avatar.Length == 0)
            {
                return BadRequest(new { message = "Avatar image is required." });
            }

            if (!avatar.ContentType.StartsWith("image/"))
            {
                return BadRequest(new { message = "Only image files are allowed." });
            }

            if (avatar.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { message = "Avatar must be less than 5MB." });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var oldPublicId = user.AvatarPublicId;

            var newPublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                avatar,
                "edunest/avatars",
                $"user_{userId}_{Guid.NewGuid():N}");

            user.AvatarPublicId = newPublicId;

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(oldPublicId))
            {
                await _cloudinaryService.DeleteImageAsync(oldPublicId);
            }

            return Ok(new AvatarResponse
            {
                AvatarUrl = _cloudinaryService.GenerateSignedImageUrl(newPublicId, 300, 300)
            });
        }

        [Authorize]
        [HttpDelete("avatar")]
        public async Task<ActionResult> DeleteAvatar()
        {
            var userId = CurrentUserId();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var oldPublicId = user.AvatarPublicId;

            user.AvatarPublicId = null;

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(oldPublicId))
            {
                await _cloudinaryService.DeleteImageAsync(oldPublicId);
            }

            return NoContent();
        }



        private MyProfileResponse ToProfileResponse(User user, Tutor? tutor)
        {
            return new MyProfileResponse
            {
                UserId = user.UserId,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                AvatarUrl = string.IsNullOrWhiteSpace(user.AvatarPublicId)
        ? null
        : _cloudinaryService.GenerateSignedImageUrl(
            user.AvatarPublicId,
            300,
            300),

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
