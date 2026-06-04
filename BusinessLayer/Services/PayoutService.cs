using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class PayoutService : IPayoutService
    {
        private readonly EduNestDbContext _db;
        private const decimal MinimumPayoutAmount = 10000m;

        public PayoutService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<PayoutResponse> RequestPayoutAsync(
            int tutorUserId,
            RequestPayoutRequest request)
        {
            if (request.Amount < MinimumPayoutAmount)
            {
                throw new InvalidOperationException(
                    "Minimum payout amount is 10,000 VND.");
            }

            var tutor = await GetTutorByUserIdAsync(tutorUserId);
            var wallet = await EnsureWalletAsync(tutor.TutorId);

            if (wallet.Balance < request.Amount)
            {
                throw new InvalidOperationException("Not enough wallet balance.");
            }

            await using var transaction = await _db.Database.BeginTransactionAsync();

            wallet.Balance -= request.Amount;
            wallet.PendingBalance += request.Amount;

            var tx = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Type = "Debit",
                Amount = request.Amount,
                Description = "Payout request - pending admin bank transfer",
                CreatedAt = DateTime.UtcNow
            };

            _db.WalletTransactions.Add(tx);
            await _db.SaveChangesAsync();

            var payout = new Payout
            {
                TutorId = tutor.TutorId,
                WalletTransactionId = tx.WalletTransactionId,
                Amount = request.Amount,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            _db.Payouts.Add(payout);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return ToPayoutResponse(payout);
        }

        public async Task<List<PayoutResponse>> GetPayoutsAsync(int tutorUserId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);

            return await _db.Payouts
                .Where(p => p.TutorId == tutor.TutorId)
                .OrderByDescending(p => p.RequestedAt)
                .Select(p => ToPayoutResponse(p))
                .ToListAsync();
        }

        public async Task<PayoutResponse> AdminUpdatePayoutAsync(
            int payoutId,
            AdminUpdatePayoutRequest request)
        {
            var payout = await _db.Payouts
                .Include(p => p.Tutor)
                .ThenInclude(t => t.Wallet)
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId)
                ?? throw new KeyNotFoundException("Payout not found.");

            if (payout.Status != "Pending")
                throw new InvalidOperationException("Only pending payout can be updated.");

            if (request.Status != "Paid" && request.Status != "Failed")
                throw new InvalidOperationException("Status must be Paid or Failed.");

            var wallet = payout.Tutor.Wallet ?? await EnsureWalletAsync(payout.TutorId);

            wallet.PendingBalance = Math.Max(0, wallet.PendingBalance - payout.Amount);

            if (request.Status == "Failed")
                wallet.Balance += payout.Amount;

            payout.Status = request.Status;
            payout.PaidAt = request.Status == "Paid" ? DateTime.UtcNow : null;

            await _db.SaveChangesAsync();

            return ToPayoutResponse(payout);
        }

        private async Task<Tutor> GetTutorByUserIdAsync(int userId)
            => await _db.Tutors.FirstOrDefaultAsync(t => t.UserId == userId)
               ?? throw new KeyNotFoundException("Tutor profile not found.");

        private async Task<Wallet> EnsureWalletAsync(int tutorId)
        {
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.TutorId == tutorId);

            if (wallet != null)
                return wallet;

            wallet = new Wallet
            {
                TutorId = tutorId,
                Balance = 0,
                PendingBalance = 0
            };

            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();

            return wallet;
        }

        private static PayoutResponse ToPayoutResponse(Payout p) => new()
        {
            PayoutId = p.PayoutId,
            TutorId = p.TutorId,
            Amount = p.Amount,
            Status = p.Status,
            RequestedAt = p.RequestedAt,
            PaidAt = p.PaidAt
        };
    }
}
