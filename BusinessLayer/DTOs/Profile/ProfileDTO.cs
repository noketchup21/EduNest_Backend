using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Profile
{
    public sealed class MyProfileResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public int? TutorId { get; set; }
        public string? TutorBio { get; set; }
        public bool? IsVerified { get; set; }
        public string? VerificationStatus { get; set; }

        public string? BankName { get; set; }
        public string? BankBin { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public string? BranchName { get; set; }
    }

    public sealed class UpdateMyProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Tutor only
        public string? TutorBio { get; set; }
    }

    public sealed class UpdateTutorBankAccountRequest
    {
        [Required]
        public string BankName { get; set; } = string.Empty;

        public string? BankBin { get; set; }

        [Required]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        public string AccountHolderName { get; set; } = string.Empty;

        public string? BranchName { get; set; }
    }

    public sealed class TutorBankAccountResponse
    {
        public int TutorBankAccountId { get; set; }
        public int TutorId { get; set; }

        public string BankName { get; set; } = string.Empty;
        public string? BankBin { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string? BranchName { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
