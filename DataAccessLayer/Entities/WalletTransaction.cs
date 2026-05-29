using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("WalletTransaction")]
    public class WalletTransaction
    {
        [Key]
        public int WalletTransactionId { get; set; }

        public int WalletId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; }               // Credit / Debit

        [MaxLength(500)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("WalletId")]
        public virtual Wallet Wallet { get; set; }

        public virtual Payout Payout { get; set; }     // 1:1 optional
    }
}
