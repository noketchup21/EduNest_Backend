using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using BusinessLayer.Settings;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly EduNestDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IR2StorageService _r2StorageService;
        private readonly IAdminTutorRepository _adminTutorRepository;
        private readonly IPayOSChiPayoutService _payOSChiPayoutService;
        private readonly PayOSChiSetting _payOSChiSetting;

        public AdminService(
            EduNestDbContext db,
            ICloudinaryService cloudinaryService,
            IR2StorageService r2StorageService,
            IAdminTutorRepository adminTutorRepository,
            IPayOSChiPayoutService payOSChiPayoutService,
            IOptions<PayOSChiSetting> payOSChiSetting)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
            _r2StorageService = r2StorageService;
            _adminTutorRepository = adminTutorRepository;
            _payOSChiPayoutService = payOSChiPayoutService;
            _payOSChiSetting = payOSChiSetting.Value;
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

            var platformRevenue = Math.Round(grossLessonRevenue * 0.20m, 2);
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

                PendingPayouts = await _db.Payouts.CountAsync(p =>
                    p.Status == "Pending" ||
                    p.Status == "Processing" ||
                    p.Status == "ManualQrRequired"),

                PendingPayoutAmount = await _db.Payouts
    .Where(p =>
        p.Status == "Pending" ||
        p.Status == "Processing" ||
        p.Status == "ManualQrRequired")
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

            return (await Task.WhenAll(tutors.Select(ToTutorVerificationResponseAsync)))
                .ToList();
        }

        public async Task<TutorVerificationResponse> GetTutorVerificationAsync(int tutorId)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            return await ToTutorVerificationResponseAsync(tutor);
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

            return await ToTutorVerificationResponseAsync(tutor);
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

            return await ToTutorVerificationResponseAsync(tutor);
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
            var payouts = await _db.Payouts
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.BankAccount)
                .OrderByDescending(p => p.RequestedAt)
                .ToListAsync();

            return payouts.Select(ToPayoutResponse).ToList();
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

            var transferContent = $"EDUNEST PAYOUT {payout.PayoutId}";

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
                TransferQrNote = transferQrNote,

                PayoutMethod = payout.PayoutMethod,

                ApprovedAt = payout.ApprovedAt,

                PayOSChiReferenceId = payout.PayOSChiReferenceId,
                PayOSChiBatchId = payout.PayOSChiBatchId,
                PayOSChiPayoutItemId = payout.PayOSChiPayoutItemId,
                PayOSChiApprovalState = payout.PayOSChiApprovalState,
                PayOSChiTransactionState = payout.PayOSChiTransactionState,
                PayOSChiFailureReason = payout.PayOSChiFailureReason,
            };
        }

        public async Task<PayoutResponse> UpdatePayoutStatusAsync(
            int payoutId,
            string status)
        {
            var normalizedStatus = NormalizeFinalPayoutStatus(status);

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var payout = await _db.Payouts
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.Wallet)
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.BankAccount)
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId)
                ?? throw new KeyNotFoundException("Payout not found.");

            if (payout.Status != "Pending" &&
                payout.Status != "Processing" &&
                payout.Status != "ManualQrRequired")
            {
                throw new InvalidOperationException(
                    $"This payout is already {payout.Status} and cannot be updated again.");
            }

            var wallet = payout.Tutor.Wallet
                ?? throw new InvalidOperationException("Tutor wallet not found.");

            wallet.PendingBalance = Math.Max(0, wallet.PendingBalance - payout.Amount);

            if (normalizedStatus == "Paid")
            {
                payout.Status = "Paid";
                payout.PayoutMethod = "ManualQr";
                payout.PaidAt = DateTime.UtcNow;

                _db.WalletTransactions.Add(new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    Type = "PayoutPaid",
                    Amount = payout.Amount,
                    Description = $"Payout #{payout.PayoutId} was paid by admin.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            else if (normalizedStatus == "Failed")
            {
                payout.Status = "Failed";
                payout.PaidAt = null;

                wallet.Balance += payout.Amount;

                _db.WalletTransactions.Add(new WalletTransaction
                {
                    WalletId = wallet.WalletId,
                    Type = "Refund",
                    Amount = payout.Amount,
                    Description = $"Payout #{payout.PayoutId} failed. Amount returned to tutor wallet.",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

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

            return (await Task.WhenAll(tutors.Select(ToTutorVerificationResponseAsync)))
                .ToList();
        }

        public async Task<TutorVerificationResponse> UpdateTutorAccountStatusAsync(
    int tutorId,
    bool isActive)
        {
            var tutor = await _adminTutorRepository.GetTutorWithUserAndBankAsync(tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            if (tutor.User == null)
            {
                throw new KeyNotFoundException("Tutor user account not found.");
            }

            tutor.User.IsActive = isActive;

            await _adminTutorRepository.SaveChangesAsync();

            return await ToTutorVerificationResponseAsync(tutor);
        }

        public async Task<PayoutResponse> ApprovePayoutWithPayOSChiAsync(int payoutId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var payout = await _db.Payouts
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.User)
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.Wallet)
                .Include(p => p.Tutor)
                    .ThenInclude(t => t.BankAccount)
                .FirstOrDefaultAsync(p => p.PayoutId == payoutId)
                ?? throw new KeyNotFoundException("Payout not found.");

            if (payout.Status != "Pending")
            {
                throw new InvalidOperationException(
                    $"Only pending payout can be approved. Current status: {payout.Status}");
            }

            var wallet = payout.Tutor.Wallet
                ?? throw new InvalidOperationException("Tutor wallet not found.");

            var bank = payout.Tutor.BankAccount;

            if (bank == null)
                throw new InvalidOperationException("Tutor has not added bank information.");

            if (string.IsNullOrWhiteSpace(bank.BankBin))
                throw new InvalidOperationException("Tutor bank BIN is missing.");

            if (string.IsNullOrWhiteSpace(bank.AccountNumber))
                throw new InvalidOperationException("Tutor account number is missing.");

            if (payout.Amount <= 0)
                throw new InvalidOperationException("Invalid payout amount.");

            payout.Status = "Processing";
            payout.PayoutMethod = "PayOSChi";
            payout.ApprovedAt = DateTime.UtcNow;
            payout.PayOSChiFailureReason = null;

            await _db.SaveChangesAsync();

            try
            {
                var result = await _payOSChiPayoutService.CreateTutorPayoutAsync(
                    payoutRequestId: payout.PayoutId,
                    amount: Convert.ToInt32(payout.Amount),
                    tutorBankBin: bank.BankBin,
                    tutorAccountNumber: bank.AccountNumber,
                    description: BuildPayOSChiDescription(payout.PayoutId));

                payout.PayOSChiReferenceId = result.ReferenceId;
                payout.PayOSChiBatchId = result.BatchId;
                payout.PayOSChiPayoutItemId = result.PayoutItemId;
                payout.PayOSChiApprovalState = result.ApprovalState;
                payout.PayOSChiTransactionState = result.TransactionState;

                if (IsPayOSSuccess(result.ApprovalState) ||
                    IsPayOSSuccess(result.TransactionState))
                {
                    wallet.PendingBalance = Math.Max(0, wallet.PendingBalance - payout.Amount);

                    payout.Status = "Paid";
                    payout.PaidAt = DateTime.UtcNow;

                    _db.WalletTransactions.Add(new WalletTransaction
                    {
                        WalletId = wallet.WalletId,
                        Type = "PayoutPaid",
                        Amount = payout.Amount,
                        Description = $"Payout #{payout.PayoutId} was paid automatically by payOS Chi.",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    payout.Status = "Processing";
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToPayoutResponse(payout);
            }
            catch (Exception ex)
            {
                if (!_payOSChiSetting.FallbackToQrWhenFailed)
                    throw;

                payout.Status = "ManualQrRequired";
                payout.PayoutMethod = "ManualQr";
                payout.PayOSChiFailureReason = BuildPayOSChiFailureReason(ex);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ToPayoutResponse(payout);
            }
        }

        private string BuildPayOSChiDescription(int payoutId)
        {
            var prefix = string.IsNullOrWhiteSpace(_payOSChiSetting.DefaultDescriptionPrefix)
                ? "EDUNEST PAYOUT"
                : _payOSChiSetting.DefaultDescriptionPrefix.Trim();

            return $"{prefix} {payoutId}";
        }

        private static string BuildPayOSChiFailureReason(Exception ex)
        {
            var parts = new List<string>
            {
                $"{ex.GetType().Name}: {ex.Message}"
            };

            foreach (var propertyName in new[]
                     {
                         "StatusCode",
                         "Code",
                         "ErrorCode",
                         "ResponseCode",
                         "ResponseBody",
                         "RawResponse",
                         "Details"
                     })
            {
                var property = ex.GetType().GetProperty(propertyName);
                var value = property?.GetValue(ex);

                if (value == null)
                    continue;

                var text = value.ToString();

                if (!string.IsNullOrWhiteSpace(text))
                    parts.Add($"{propertyName}: {text}");
            }

            if (ex.InnerException != null)
            {
                parts.Add(
                    $"Inner {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }

            var reason = string.Join(" | ", parts);

            return reason.Length <= 1000
                ? reason
                : reason[..1000];
        }

        private static bool IsPayOSSuccess(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Equals("SUCCEEDED", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("PAID", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<TutorVerificationResponse> ToTutorVerificationResponseAsync(Tutor tutor)
        {
            return new TutorVerificationResponse
            {
                TutorId = tutor.TutorId,
                UserId = tutor.UserId,

                TutorName = tutor.User?.Name ?? $"Tutor #{tutor.TutorId}",
                Email = tutor.User?.Email ?? string.Empty,

                IsActive = tutor.User?.IsActive == true,

                IsVerified = tutor.IsVerified,
                VerificationStatus = tutor.VerificationStatus,

                NationalIdNumber = tutor.NationalIdNumber,

                CccdFrontImageUrl = string.IsNullOrWhiteSpace(tutor.CccdFrontPublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CccdFrontPublicId),

                CccdBackImageUrl = string.IsNullOrWhiteSpace(tutor.CccdBackPublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CccdBackPublicId),

                CertificateImageUrl = GenerateCertificateImageUrls(tutor.CertificatePublicId)
                    .FirstOrDefault(),
                CertificateImageUrls = GenerateCertificateImageUrls(tutor.CertificatePublicId),
                TranscriptDocumentUrl = string.IsNullOrWhiteSpace(tutor.TranscriptDocumentObjectKey)
                    ? null
                    : await _r2StorageService.CreateDownloadUrlAsync(
                        tutor.TranscriptDocumentObjectKey),

                BankName = tutor.BankAccount?.BankName,
                BankBin = tutor.BankAccount?.BankBin,
                AccountNumber = tutor.BankAccount?.AccountNumber,
                AccountHolderName = tutor.BankAccount?.AccountHolderName,
                BranchName = tutor.BankAccount?.BranchName,

                VerificationSubmittedAt = tutor.VerificationSubmittedAt,
                VerificationReviewedAt = tutor.VerificationReviewedAt,
                VerificationRejectReason = tutor.VerificationRejectReason,
            };
        }

        private List<string> GenerateCertificateImageUrls(string? value)
        {
            return ParseCertificatePublicIds(value)
                .Select(publicId => _cloudinaryService.GenerateSignedImageUrl(publicId))
                .ToList();
        }

        private static List<string> ParseCertificatePublicIds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            var trimmed = value.Trim();

            if (!trimmed.StartsWith("["))
            {
                return new List<string> { trimmed };
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(trimmed)?
                    .Where(publicId => !string.IsNullOrWhiteSpace(publicId))
                    .Select(publicId => publicId.Trim())
                    .ToList() ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string> { trimmed };
            }
        }

        private static PayoutResponse ToPayoutResponse(Payout payout)
        {
            var bank = payout.Tutor?.BankAccount;

            return new PayoutResponse
            {
                PayoutId = payout.PayoutId,
                TutorId = payout.TutorId,
                Amount = payout.Amount,
                Status = payout.Status,

                PayoutMethod = payout.PayoutMethod,

                RequestedAt = payout.RequestedAt,
                ApprovedAt = payout.ApprovedAt,
                PaidAt = payout.PaidAt,

                PayOSChiReferenceId = payout.PayOSChiReferenceId,
                PayOSChiBatchId = payout.PayOSChiBatchId,
                PayOSChiPayoutItemId = payout.PayOSChiPayoutItemId,
                PayOSChiApprovalState = payout.PayOSChiApprovalState,
                PayOSChiTransactionState = payout.PayOSChiTransactionState,
                PayOSChiFailureReason = payout.PayOSChiFailureReason,

                TutorBankName = bank?.BankName,
                TutorBankBin = bank?.BankBin,
                TutorAccountNumber = bank?.AccountNumber,
                TutorAccountHolderName = bank?.AccountHolderName,
                TutorBankBranch = bank?.BranchName
            };
        }

        private static string NormalizeFinalPayoutStatus(string value)
        {
            var status = value.Trim();

            if (status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
                return "Paid";

            if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                return "Failed";

            throw new InvalidOperationException("Payout status must be Paid or Failed.");
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
