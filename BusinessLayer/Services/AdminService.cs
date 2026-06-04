using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly EduNestDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;

        public AdminService(
            EduNestDbContext db,
            ICloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
        }

        public async Task TrackDownloadAsync(TrackAppMetricRequest request)
        {
            _db.AppMetrics.Add(new AppMetric
            {
                Type = "Download",
                DeviceId = request.DeviceId,
                Platform = request.Platform,
                AppVersion = request.AppVersion,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task TrackInstallAsync(TrackAppMetricRequest request)
        {
            var deviceId = request.DeviceId?.Trim();

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                var alreadyTracked = await _db.AppMetrics.AnyAsync(x =>
                    x.Type == "Install" &&
                    x.DeviceId == deviceId);

                if (alreadyTracked)
                    return;
            }

            _db.AppMetrics.Add(new AppMetric
            {
                Type = "Install",
                DeviceId = deviceId,
                Platform = request.Platform,
                AppVersion = request.AppVersion,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        public async Task<AdminDashboardResponse> GetDashboardAsync()
        {
            var completedLessons = await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Lessons)
                .Where(l => l.Status == "Completed")
                .ToListAsync();

            decimal grossLessonRevenue = 0;

            foreach (var lesson in completedLessons)
            {
                var totalLessonsInBooking = lesson.Booking.Lessons.Count;

                if (totalLessonsInBooking <= 0)
                    totalLessonsInBooking = 1;

                grossLessonRevenue += lesson.Booking.PriceAtBooking / totalLessonsInBooking;
            }

            var platformRevenue = Math.Round(grossLessonRevenue * 0.10m, 2);
            var tutorRevenue = grossLessonRevenue - platformRevenue;

            return new AdminDashboardResponse
            {
                TotalDownloads = await _db.AppMetrics.CountAsync(x => x.Type == "Download"),
                TotalInstalls = await _db.AppMetrics.CountAsync(x => x.Type == "Install"),

                TotalSubjects = await _db.Subjects.CountAsync(),

                TotalTutors = await _db.Tutors.CountAsync(),
                PendingTutors = await _db.Tutors.CountAsync(t =>
                    t.VerificationStatus == "Pending" || (!t.IsVerified && t.VerificationStatus != "Approved")),
                ApprovedTutors = await _db.Tutors.CountAsync(t => t.IsVerified),

                PendingPayouts = await _db.Payouts.CountAsync(p => p.Status == "Pending"),
                PendingPayoutAmount = await _db.Payouts
                    .Where(p => p.Status == "Pending")
                    .SumAsync(p => p.Amount),

                CompletedLessons = completedLessons.Count,
                GrossLessonRevenue = Math.Round(grossLessonRevenue, 2),
                PlatformRevenue = platformRevenue,
                TutorRevenue = Math.Round(tutorRevenue, 2)
            };
        }

        public async Task<List<TutorVerificationResponse>> GetPendingTutorsAsync()
        {
            var tutors = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .Where(t =>
                    t.VerificationStatus == "Pending" ||
                    (!t.IsVerified && t.VerificationStatus != "Approved"))
                .OrderByDescending(t => t.VerificationSubmittedAt)
                .ThenByDescending(t => t.TutorId)
                .ToListAsync();

            return tutors.Select(ToTutorVerificationResponse).ToList();
        }

        public async Task<TutorVerificationResponse> GetTutorVerificationAsync(int tutorId)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            return ToTutorVerificationResponse(tutor);
        }

        public async Task<TutorVerificationResponse> ApproveTutorAsync(int tutorId)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            if (string.IsNullOrWhiteSpace(tutor.NationalIdNumber) ||
                string.IsNullOrWhiteSpace(tutor.CccdFrontPublicId) ||
                string.IsNullOrWhiteSpace(tutor.CccdBackPublicId) ||
                string.IsNullOrWhiteSpace(tutor.CertificatePublicId) ||
                tutor.BankAccount == null)
            {
                throw new InvalidOperationException(
                    "Tutor has not submitted all verification documents and bank information.");
            }

            tutor.IsVerified = true;
            tutor.VerificationStatus = "Approved";
            tutor.VerificationReviewedAt = DateTime.UtcNow;
            tutor.VerificationRejectReason = null;

            await _db.SaveChangesAsync();

            return ToTutorVerificationResponse(tutor);
        }

        public async Task<TutorVerificationResponse> RejectTutorAsync(
            int tutorId,
            string? reason)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            tutor.IsVerified = false;
            tutor.VerificationStatus = "Rejected";
            tutor.VerificationReviewedAt = DateTime.UtcNow;
            tutor.VerificationRejectReason = string.IsNullOrWhiteSpace(reason)
                ? "Rejected by admin."
                : reason.Trim();

            await _db.SaveChangesAsync();

            return ToTutorVerificationResponse(tutor);
        }

        public async Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectDTO request)
        {
            var name = request.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Subject name is required.");

            var exists = await _db.Subjects.AnyAsync(s =>
                s.Name.ToLower() == name.ToLower());

            if (exists)
                throw new InvalidOperationException("Subject already exists.");

            var subject = new Subject
            {
                Name = name,
                Description = request.Description?.Trim() ?? string.Empty
            };

            _db.Subjects.Add(subject);
            await _db.SaveChangesAsync();

            return new SubjectResponseDTO
            {
                SubjectId = subject.SubjectId,
                Name = subject.Name,
                Description = subject.Description
            };
        }

        public async Task<List<PayoutResponse>> GetPayoutsAsync()
        {
            return await _db.Payouts
                .OrderByDescending(p => p.RequestedAt)
                .Select(p => ToPayoutResponse(p))
                .ToListAsync();
        }

        public async Task<AdminPayoutDetailResponse> GetPayoutDetailAsync(int payoutId)
        {
            var payout = await _db.Payouts
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.User)
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.BankAccount)
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId)
                ?? throw new KeyNotFoundException("Payout not found.");

            var bank = payout.Tutor.BankAccount;

            var transferContent = $"PAYOUT{payout.PayoutId}";

            var hasRequiredBankInfo =
                bank != null &&
                !string.IsNullOrWhiteSpace(bank.BankBin) &&
                !string.IsNullOrWhiteSpace(bank.AccountNumber) &&
                !string.IsNullOrWhiteSpace(bank.AccountHolderName);

            var transferQrUrl = hasRequiredBankInfo
                ? BuildVietQrUrl(
                    bankBin: bank!.BankBin!,
                    accountNumber: bank.AccountNumber,
                    amount: payout.Amount,
                    content: transferContent,
                    accountName: bank.AccountHolderName)
                : null;

            var transferQrNote = hasRequiredBankInfo
                ? "Scan this QR to transfer payout money to the tutor."
                : "Enter the tutor bank BIN to enable quick money transfer QR.";

            return new AdminPayoutDetailResponse
            {
                PayoutId = payout.PayoutId,
                TutorId = payout.TutorId,
                TutorUserId = payout.Tutor.UserId,

                TutorName = payout.Tutor.User?.Name ?? $"Tutor #{payout.TutorId}",
                TutorEmail = payout.Tutor.User?.Email ?? string.Empty,

                Amount = payout.Amount,
                Status = payout.Status,
                RequestedAt = payout.RequestedAt,
                PaidAt = payout.PaidAt,

                BankName = bank?.BankName,
                BankBin = bank?.BankBin,
                AccountNumber = bank?.AccountNumber,
                AccountHolderName = bank?.AccountHolderName,
                BranchName = bank?.BranchName,

                TransferContent = transferContent,
                TransferQrUrl = transferQrUrl,
                TransferQrNote = transferQrNote
            };
        }

        public async Task<PayoutResponse> UpdatePayoutStatusAsync(
            int payoutId,
            string status)
        {
            var payout = await _db.Payouts
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId)
                ?? throw new KeyNotFoundException("Payout not found.");

            payout.Status = NormalizePayoutStatus(status);

            if (payout.Status == "Paid")
            {
                payout.PaidAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            return ToPayoutResponse(payout);
        }

        public async Task<List<TutorVerificationResponse>> GetTutorsAsync()
        {
            var tutors = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .OrderByDescending(t => t.VerificationSubmittedAt)
                .ThenByDescending(t => t.TutorId)
                .ToListAsync();

            return tutors.Select(ToTutorVerificationResponse).ToList();
        }

        private TutorVerificationResponse ToTutorVerificationResponse(Tutor tutor)
        {
            return new TutorVerificationResponse
            {
                TutorId = tutor.TutorId,
                UserId = tutor.UserId,

                TutorName = tutor.User?.Name ?? $"Tutor #{tutor.TutorId}",
                Email = tutor.User?.Email ?? string.Empty,

                IsVerified = tutor.IsVerified,
                VerificationStatus = tutor.VerificationStatus,

                NationalIdNumber = tutor.NationalIdNumber,

                CccdFrontImageUrl = string.IsNullOrWhiteSpace(tutor.CccdFrontPublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CccdFrontPublicId),

                CccdBackImageUrl = string.IsNullOrWhiteSpace(tutor.CccdBackPublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CccdBackPublicId),

                CertificateImageUrl = string.IsNullOrWhiteSpace(tutor.CertificatePublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CertificatePublicId),

                BankName = tutor.BankAccount?.BankName,
                AccountNumber = tutor.BankAccount?.AccountNumber,
                AccountHolderName = tutor.BankAccount?.AccountHolderName,
                BranchName = tutor.BankAccount?.BranchName,

                VerificationSubmittedAt = tutor.VerificationSubmittedAt,
                VerificationReviewedAt = tutor.VerificationReviewedAt,
                VerificationRejectReason = tutor.VerificationRejectReason
            };
        }

        private static PayoutResponse ToPayoutResponse(Payout payout)
        {
            return new PayoutResponse
            {
                PayoutId = payout.PayoutId,
                TutorId = payout.TutorId,
                Amount = payout.Amount,
                Status = payout.Status,
                RequestedAt = payout.RequestedAt,
                PaidAt = payout.PaidAt
            };
        }

        private static string NormalizePayoutStatus(string value)
        {
            var status = value.Trim();

            if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return "Pending";

            if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                return "Paid";

            if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                return "Failed";

            throw new InvalidOperationException("Payout status must be Pending, Paid, or Failed.");
        }

        private static string BuildVietQrUrl(
    string bankBin,
    string accountNumber,
    decimal amount,
    string content,
    string accountName)
        {
            var safeAmount = ((long)amount).ToString();
            var safeContent = Uri.EscapeDataString(content);
            var safeAccountName = Uri.EscapeDataString(accountName.ToUpperInvariant());

            return $"https://img.vietqr.io/image/{bankBin}-{accountNumber}-compact2.png" +
                   $"?amount={safeAmount}" +
                   $"&addInfo={safeContent}" +
                   $"&accountName={safeAccountName}";
        }
    }
}
