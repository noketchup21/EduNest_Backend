using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Wallet;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class WalletService : IWalletService
    {
        private readonly EduNestDbContext _db;

        public WalletService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<WalletResponse> GetTutorWalletAsync(int tutorUserId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);
            var wallet = await EnsureWalletAsync(tutor.TutorId);

            return ToWalletResponse(wallet);
        }

        public async Task<List<WalletTransactionResponse>> GetTutorWalletTransactionsAsync(int tutorUserId)
        {
            var tutor = await GetTutorByUserIdAsync(tutorUserId);
            var wallet = await EnsureWalletAsync(tutor.TutorId);

            return await _db.WalletTransactions
                .Where(t => t.WalletId == wallet.WalletId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => ToWalletTransactionResponse(t))
                .ToListAsync();
        }

        public async Task CreditTutorForLessonAsync(Lesson lesson)
        {
            var availability = lesson.Booking.Availability;

            var wallet = await EnsureWalletAsync(availability.TutorId);

            var alreadyCredited = await _db.WalletTransactions.AnyAsync(t =>
                t.WalletId == wallet.WalletId &&
                t.Type == "Credit" &&
                t.Description != null &&
                t.Description.Contains($"Lesson #{lesson.LessonId}"));

            if (alreadyCredited)
                return;

            var totalLessons = await _db.Lessons.CountAsync(l =>
                l.BookingId == lesson.BookingId);

            if (totalLessons <= 0)
                totalLessons = 1;

            var grossLessonAmount = Math.Round(
                lesson.Booking.PriceAtBooking / totalLessons,
                2);

            var platformFee = Math.Round(grossLessonAmount * 0.10m, 2);

            var tutorAmount = grossLessonAmount - platformFee;

            wallet.Balance += tutorAmount;

            _db.WalletTransactions.Add(new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Type = "Credit",
                Amount = tutorAmount,
                Description =
                    $"Lesson #{lesson.LessonId} completed. " +
                    $"Gross: {grossLessonAmount}, " +
                    $"Platform fee 10%: {platformFee}, " +
                    $"Tutor receives 90%: {tutorAmount}",
                CreatedAt = DateTime.UtcNow
            });
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

        private static WalletResponse ToWalletResponse(Wallet w) => new()
        {
            WalletId = w.WalletId,
            TutorId = w.TutorId,
            Balance = w.Balance,
            PendingBalance = w.PendingBalance
        };

        private static WalletTransactionResponse ToWalletTransactionResponse(WalletTransaction t) => new()
        {
            WalletTransactionId = t.WalletTransactionId,
            WalletId = t.WalletId,
            Type = t.Type,
            Amount = t.Amount,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        };
    }
}
