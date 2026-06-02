using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Wallet
{
    public sealed class WalletResponse
    {
        public int WalletId { get; set; }
        public int TutorId { get; set; }
        public decimal Balance { get; set; }
        public decimal PendingBalance { get; set; }
    }

    public sealed class WalletTransactionResponse
    {
        public int WalletTransactionId { get; set; }
        public int WalletId { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
