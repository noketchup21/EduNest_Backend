using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly EduNestDbContext _db;

        public AdminService(EduNestDbContext db)
        {
            _db = db;
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
                PendingTutors = await _db.Tutors.CountAsync(t => !t.IsVerified),
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

        public async Task<List<AdminTutorResponse>> GetPendingTutorsAsync()
        {
            return await _db.Tutors
                .Include(t => t.User)
                .Where(t => !t.IsVerified)
                .OrderByDescending(t => t.TutorId)
                .Select(t => ToTutorResponse(t))
                .ToListAsync();
        }

        public async Task<AdminTutorResponse> ApproveTutorAsync(int tutorId)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            tutor.IsVerified = true;

            await _db.SaveChangesAsync();

            return ToTutorResponse(tutor);
        }

        public async Task<AdminTutorResponse> RejectTutorAsync(int tutorId)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            tutor.IsVerified = false;

            await _db.SaveChangesAsync();

            return ToTutorResponse(tutor);
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

        public async Task<PayoutResponse> UpdatePayoutStatusAsync(int payoutId, string status)
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

        private static AdminTutorResponse ToTutorResponse(Tutor tutor)
        {
            return new AdminTutorResponse
            {
                TutorId = tutor.TutorId,
                UserId = tutor.UserId,
                TutorName = tutor.User?.Name ?? $"Tutor #{tutor.TutorId}",
                Email = tutor.User?.Email ?? string.Empty,
                Phone = tutor.User?.Phone,
                Bio = tutor.Bio,
                IsVerified = tutor.IsVerified
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
    }
}
