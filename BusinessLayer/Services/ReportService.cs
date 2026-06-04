using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Report;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.Services
{
    public sealed class ReportService : IReportService
    {
        private const int MaxProofImages = 5;
        private const long MaxProofImageSizeBytes = 5 * 1024 * 1024;

        private readonly ITutorReportRepository _reportRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public ReportService(
            ITutorReportRepository reportRepository,
            ICloudinaryService cloudinaryService)
        {
            _reportRepository = reportRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<TutorReportResponse> CreateReportAsync(
            int reporterUserId,
            CreateTutorReportRequest request)
        {
            ValidateCreateRequest(request);

            var booking = await _reportRepository.GetBookingForReportAsync(
                request.BookingId,
                reporterUserId)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (booking.Status == "Cancelled" || booking.Status == "Expired")
            {
                throw new InvalidOperationException(
                    "Cannot report a cancelled or expired booking.");
            }

            if (request.LessonId.HasValue)
            {
                var lessonBelongsToBooking =
                    await _reportRepository.LessonBelongsToBookingAsync(
                        request.LessonId.Value,
                        booking.BookingId);

                if (!lessonBelongsToBooking)
                {
                    throw new InvalidOperationException(
                        "Lesson does not belong to this booking.");
                }
            }

            var report = new TutorReport
            {
                ReporterUserId = reporterUserId,
                TutorId = booking.Availability.TutorId,
                BookingId = booking.BookingId,
                AvailabilityId = booking.AvailabilityId,
                LessonId = request.LessonId,
                Category = request.Category.Trim(),
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddReportAsync(report);
            await _reportRepository.SaveChangesAsync();

            var folder = $"edunest/reports/report-{report.TutorReportId}";

            for (var i = 0; i < request.ProofImages.Count; i++)
            {
                var file = request.ProofImages[i];

                var publicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                    file,
                    folder,
                    $"proof_{i + 1}_{Guid.NewGuid():N}");

                await _reportRepository.AddProofImageAsync(new TutorReportProofImage
                {
                    TutorReportId = report.TutorReportId,
                    PublicId = publicId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _reportRepository.SaveChangesAsync();

            return await AdminGetReportAsync(report.TutorReportId);
        }

        public async Task<List<TutorReportResponse>> GetMyReportsAsync(
            int reporterUserId)
        {
            var reports = await _reportRepository.GetReportsByReporterAsync(
                reporterUserId);

            return reports.Select(ToResponse).ToList();
        }

        public async Task<List<TutorReportResponse>> AdminGetReportsAsync(
            string? status)
        {
            var normalizedStatus = string.IsNullOrWhiteSpace(status)
                ? null
                : NormalizeReportStatus(status);

            var reports = await _reportRepository.GetReportsForAdminAsync(
                normalizedStatus);

            return reports.Select(ToResponse).ToList();
        }

        public async Task<TutorReportResponse> AdminGetReportAsync(int reportId)
        {
            var report = await _reportRepository.GetReportByIdAsync(reportId)
                ?? throw new KeyNotFoundException("Report not found.");

            return ToResponse(report);
        }

        public async Task<TutorReportResponse> AdminUpdateReportStatusAsync(
            int reportId,
            UpdateTutorReportStatusRequest request)
        {
            var report = await _reportRepository.GetReportForUpdateAsync(reportId)
                ?? throw new KeyNotFoundException("Report not found.");

            report.Status = NormalizeReportStatus(request.Status);
            report.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
                ? null
                : request.AdminNote.Trim();
            report.ReviewedAt = DateTime.UtcNow;

            await _reportRepository.SaveChangesAsync();

            return await AdminGetReportAsync(reportId);
        }

        private static void ValidateCreateRequest(CreateTutorReportRequest request)
        {
            if (request.BookingId <= 0)
            {
                throw new InvalidOperationException("Booking is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Category))
            {
                throw new InvalidOperationException("Report category is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new InvalidOperationException("Report title is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new InvalidOperationException("Report description is required.");
            }

            if (request.ProofImages == null || request.ProofImages.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one proof image is required.");
            }

            if (request.ProofImages.Count > MaxProofImages)
            {
                throw new InvalidOperationException(
                    $"Maximum {MaxProofImages} proof images are allowed.");
            }

            foreach (var file in request.ProofImages)
            {
                ValidateProofImage(file);
            }
        }

        private static void ValidateProofImage(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                throw new InvalidOperationException("Invalid proof image.");
            }

            if (file.Length > MaxProofImageSizeBytes)
            {
                throw new InvalidOperationException(
                    "Each proof image must be less than 5MB.");
            }

            var contentType = file.ContentType?.ToLowerInvariant() ?? "";

            if (!contentType.StartsWith("image/"))
            {
                throw new InvalidOperationException(
                    "Only image files are allowed as proof.");
            }
        }

        private TutorReportResponse ToResponse(TutorReport report)
        {
            return new TutorReportResponse
            {
                TutorReportId = report.TutorReportId,

                ReporterUserId = report.ReporterUserId,
                ReporterName = report.ReporterUser?.Name
                    ?? $"User #{report.ReporterUserId}",

                TutorId = report.TutorId,
                TutorUserId = report.Tutor?.UserId ?? 0,
                TutorName = report.Tutor?.User?.Name
                    ?? $"Tutor #{report.TutorId}",

                BookingId = report.BookingId,
                AvailabilityId = report.AvailabilityId,
                LessonId = report.LessonId,

                SubjectName = report.Availability?.Subject?.Name,

                Category = report.Category,
                Title = report.Title,
                Description = report.Description,

                Status = report.Status,
                AdminNote = report.AdminNote,

                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,

                ProofImages = report.ProofImages
                    .Select(img => new TutorReportProofImageResponse
                    {
                        TutorReportProofImageId = img.TutorReportProofImageId,
                        ImageUrl = _cloudinaryService.GenerateSignedImageUrl(
                            img.PublicId)
                    })
                    .ToList()
            };
        }

        private static string NormalizeReportStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new InvalidOperationException("Report status is required.");
            }

            var value = status.Trim();

            if (value.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                return "Pending";

            if (value.Equals("Reviewing", StringComparison.OrdinalIgnoreCase))
                return "Reviewing";

            if (value.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
                return "Resolved";

            if (value.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
                return "Rejected";

            throw new InvalidOperationException(
                "Report status must be Pending, Reviewing, Resolved, or Rejected.");
        }
    }
}
