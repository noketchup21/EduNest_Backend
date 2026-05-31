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

        [Required, MaxLength(255)]
        public string BankName { get; set; }

        [Required, MaxLength(100)]
        public string AccountNumber { get; set; }

        [Required, MaxLength(255)]
        public string AccountHolderName { get; set; }

        // Navigation properties
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }
    }
}
