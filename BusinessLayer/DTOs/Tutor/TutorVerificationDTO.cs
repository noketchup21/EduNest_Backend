using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs.Tutor
{
    public sealed class SubmitTutorVerificationRequest
    {
        [Required]
        public string NationalIdNumber { get; set; } = string.Empty;

        [Required]
        public IFormFile CccdFrontImage { get; set; } = null!;

        [Required]
        public IFormFile CccdBackImage { get; set; } = null!;

        [Required]
        public IFormFile CertificateImage { get; set; } = null!;

        [Required]
        public string BankName { get; set; } = string.Empty;

        [Required]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        public string AccountHolderName { get; set; } = string.Empty;

        public string? BranchName { get; set; }

        // Optional, used for VietQR quick payout transfer
        public string? BankBin { get; set; }
    }

    public sealed class TutorVerificationResponse
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }

        public string TutorName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;

        public string? NationalIdNumber { get; set; }

        // Admin/tutor screen receives signed URLs from backend.
        public string? CccdFrontImageUrl { get; set; }
        public string? CccdBackImageUrl { get; set; }
        public string? CertificateImageUrl { get; set; }

        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolderName { get; set; }
        public string? BranchName { get; set; }

        public DateTime? VerificationSubmittedAt { get; set; }
        public DateTime? VerificationReviewedAt { get; set; }
        public string? VerificationRejectReason { get; set; }

        public string? BankBin { get; set; }
    }

    public sealed class RejectTutorRequest
    {
        public string? Reason { get; set; }
    }
}
