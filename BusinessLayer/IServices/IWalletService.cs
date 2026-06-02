using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Wallet;
using DataAccessLayer.Entities;

namespace BusinessLayer.IServices
{
    public interface IWalletService
    {
        Task<WalletResponse> GetTutorWalletAsync(int tutorUserId);
        Task<List<WalletTransactionResponse>> GetTutorWalletTransactionsAsync(int tutorUserId);
        Task CreditTutorForLessonAsync(Lesson lesson);
    }
}
