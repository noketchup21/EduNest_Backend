using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Tutor
{
    public class TutorResponseDTO
    {
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Bio { get; set; }
        public decimal Revenue { get; set; }
        public double Rating { get; set; }
        public bool IsVerified { get; set; }
    }

    public class UpdateTutorDTO
    {
        public string? Bio { get; set; }
        public string? Phone { get; set; }
        public string? Name { get; set; }
    }

    public sealed class UpdateTutorBankAccountRequest
    {
        [Required]
        public string BankName { get; set; } = string.Empty;

        // Optional. Used for VietQR quick payout transfer.
        public string? BankBin { get; set; }

        [Required]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        public string AccountHolderName { get; set; } = string.Empty;

        public string? BranchName { get; set; }
    }
}
