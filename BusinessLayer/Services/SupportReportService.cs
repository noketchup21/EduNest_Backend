using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.SupportReport;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class SupportReportService : ISupportReportService
    {
        private const int MaxProofImages = 5;
        private const long MaxProofImageSizeBytes = 5 * 1024 * 1024;

        private readonly EduNestDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;

        public SupportReportService(
            EduNestDbContext db,
            ICloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<SupportReportResponse> CreateAsync(
            int userId,
            string role,
            CreateSupportReportRequest request)
        {
            ValidateCreateRequest(request);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId)
                ?? throw new KeyNotFoundException("User not found.");

            var report = new SupportReport
            {
                UserId = userId,
                Role = string.IsNullOrWhiteSpace(role) ? user.Role : role,
                Category = request.Category.Trim(),
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                PayoutId = request.PayoutId,
                BookingId = request.BookingId,
                LessonId = request.LessonId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await _db.SupportReports.AddAsync(report);
            await _db.SaveChangesAsync();

            if (request.ProofImages != null && request.ProofImages.Count > 0)
            {
                var folder = $"edunest/support-reports/report-{report.SupportReportId}";

                for (var i = 0; i < request.ProofImages.Count; i++)
                {
                    var file = request.ProofImages[i];

                    var publicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                        file,
                        folder,
                        $"proof_{i + 1}_{Guid.NewGuid():N}");

                    await _db.SupportReportProofImages.AddAsync(
                        new SupportReportProofImage
                        {
                            SupportReportId = report.SupportReportId,
                            PublicId = publicId,
                            CreatedAt = DateTime.UtcNow
                        });
                }

                await _db.SaveChangesAsync();
            }

            return await AdminGetDetailAsync(report.SupportReportId);
        }

        public async Task<List<SupportReportResponse>> GetMineAsync(int userId)
        {
            var reports = await BaseQuery()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(ToResponse).ToList();
        }

        public async Task<List<SupportReportResponse>> AdminGetAllAsync(
            string? role,
            string? status)
        {
            var query = BaseQuery();

            if (!string.IsNullOrWhiteSpace(role))
            {
                var roleValue = role.Trim().ToLower();
                query = query.Where(r => r.Role.ToLower() == roleValue);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var normalizedStatus = NormalizeStatus(status);
                query = query.Where(r => r.Status == normalizedStatus);
            }

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reports.Select(ToResponse).ToList();
        }

        public async Task<SupportReportResponse> AdminGetDetailAsync(
            int supportReportId)
        {
            var report = await BaseQuery()
                .FirstOrDefaultAsync(r => r.SupportReportId == supportReportId)
                ?? throw new KeyNotFoundException("Support report not found.");

            return ToResponse(report);
        }

        public async Task<SupportReportResponse> AdminUpdateStatusAsync(
            int supportReportId,
            UpdateSupportReportStatusRequest request)
        {
            var report = await _db.SupportReports
                .FirstOrDefaultAsync(r => r.SupportReportId == supportReportId)
                ?? throw new KeyNotFoundException("Support report not found.");

            report.Status = NormalizeStatus(request.Status);
            report.AdminNote = string.IsNullOrWhiteSpace(request.AdminNote)
                ? null
                : request.AdminNote.Trim();
            report.ReviewedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return await AdminGetDetailAsync(supportReportId);
        }

        private IQueryable<SupportReport> BaseQuery()
        {
            return _db.SupportReports
                .Include(r => r.User)
                .Include(r => r.ProofImages);
        }

        private SupportReportResponse ToResponse(SupportReport report)
        {
            return new SupportReportResponse
            {
                SupportReportId = report.SupportReportId,

                UserId = report.UserId,
                UserName = report.User?.Name ?? $"User #{report.UserId}",
                UserEmail = report.User?.Email ?? string.Empty,
                Role = report.Role,

                Category = report.Category,
                Title = report.Title,
                Description = report.Description,

                PayoutId = report.PayoutId,
                BookingId = report.BookingId,
                LessonId = report.LessonId,

                Status = report.Status,
                AdminNote = report.AdminNote,

                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,

                ProofImages = report.ProofImages
                    .Select(img => new SupportReportProofImageResponse
                    {
                        SupportReportProofImageId = img.SupportReportProofImageId,
                        ImageUrl = _cloudinaryService.GenerateSignedImageUrl(img.PublicId)
                    })
                    .ToList()
            };
        }

        private static void ValidateCreateRequest(CreateSupportReportRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Category))
            {
                throw new InvalidOperationException("Category is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                throw new InvalidOperationException("Title is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Description))
            {
                throw new InvalidOperationException("Description is required.");
            }

            if (request.ProofImages == null) return;

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

        private static string NormalizeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new InvalidOperationException("Status is required.");
            }

            var value = status.Trim();

            if (value.Equals("Pending", StringComparison.OrdinalIgnoreCase))
            {
                return "Pending";
            }

            if (value.Equals("Reviewing", StringComparison.OrdinalIgnoreCase))
            {
                return "Reviewing";
            }

            if (value.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            {
                return "Resolved";
            }

            if (value.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return "Rejected";
            }

            throw new InvalidOperationException(
                "Status must be Pending, Reviewing, Resolved, or Rejected.");
        }
    }
}
