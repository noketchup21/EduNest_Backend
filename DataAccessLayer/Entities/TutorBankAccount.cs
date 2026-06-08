using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("TutorBankAccounts")]
    public class TutorBankAccount
    {
        [Key]
        public int TutorBankAccountId { get; set; }

        public int TutorId { get; set; }

        [Required, MaxLength(100)]
        public string BankName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string AccountHolderName { get; set; } = string.Empty;

        [Required]
        public string BankBin { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? BranchName { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(TutorId))]
        public Tutor Tutor { get; set; } = null!;
    }
}
