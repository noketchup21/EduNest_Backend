using BusinessLayer.DTOs.Material;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class MaterialService : IMaterialService
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Confirmed",
            "Completed"
        };

        private const long MaxMaterialFileSize = 10 * 1024 * 1024;
        private const string R2Prefix = "r2://";

        private readonly EduNestDbContext _db;
        private readonly IWebHostEnvironment _environment;
        private readonly IR2StorageService _r2Storage;

        public MaterialService(
            EduNestDbContext db,
            IWebHostEnvironment environment,
            IR2StorageService r2Storage)
        {
            _db = db;
            _environment = environment;
            _r2Storage = r2Storage;
        }

        public async Task<List<MaterialSectionResponse>> GetByAvailabilityAsync(int userId, int availabilityId)
        {
            await EnsureCanViewAvailabilityAsync(userId, availabilityId);

            var sections = await _db.MaterialSections
                .AsNoTracking()
                .Include(s => s.Materials)
                .Where(s => s.AvailabilityId == availabilityId)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();

            var orphanMaterials = await _db.Materials
                .AsNoTracking()
                .Where(m => m.AvailabilityId == availabilityId && m.MaterialSectionId == null)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var responses = sections.Select(ToSectionResponse).ToList();

            if (orphanMaterials.Any())
            {
                responses.Insert(0, new MaterialSectionResponse
                {
                    SectionId = 0,
                    MaterialSectionId = 0,
                    AvailabilityId = availabilityId,
                    Title = "Materials",
                    Description = null,
                    DisplayOrder = 0,
                    CreatedAt = orphanMaterials.Min(m => m.CreatedAt),
                    Items = orphanMaterials.Select(ToMaterialResponse).ToList()
                });
            }

            return responses;
        }

        public async Task<MaterialSectionResponse> CreateSectionAsync(
            int tutorUserId,
            int availabilityId,
            UpsertMaterialSectionRequest request)
        {
            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, availabilityId);
            ValidateSection(request);

            var nextOrder = await _db.MaterialSections
                .Where(s => s.AvailabilityId == availabilityId)
                .Select(s => (int?)s.DisplayOrder)
                .MaxAsync() ?? 0;

            var section = new MaterialSection
            {
                AvailabilityId = availabilityId,
                Title = request.Title.Trim(),
                Description = NormalizeOptional(request.Description),
                DisplayOrder = nextOrder + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.MaterialSections.Add(section);
            await _db.SaveChangesAsync();

            return ToSectionResponse(section);
        }

        public async Task<MaterialSectionResponse> UpdateSectionAsync(
            int tutorUserId,
            int sectionId,
            UpsertMaterialSectionRequest request)
        {
            ValidateSection(request);

            var section = await _db.MaterialSections
                .Include(s => s.Materials)
                .FirstOrDefaultAsync(s => s.MaterialSectionId == sectionId)
                ?? throw new KeyNotFoundException("Material section not found.");

            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, section.AvailabilityId);

            section.Title = request.Title.Trim();
            section.Description = NormalizeOptional(request.Description);
            section.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToSectionResponse(section);
        }

        public async Task DeleteSectionAsync(int tutorUserId, int sectionId)
        {
            var section = await _db.MaterialSections
                .Include(s => s.Materials)
                .FirstOrDefaultAsync(s => s.MaterialSectionId == sectionId)
                ?? throw new KeyNotFoundException("Material section not found.");

            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, section.AvailabilityId);

            _db.MaterialSections.Remove(section);
            await _db.SaveChangesAsync();
        }

        public async Task<MaterialResponse> CreateItemAsync(
            int tutorUserId,
            int sectionId,
            UpsertMaterialItemRequest request)
        {
            var section = sectionId <= 0
                ? await GetOrCreateDefaultSectionAsync(tutorUserId, request.AvailabilityId)
                : await GetTutorSectionAsync(tutorUserId, sectionId);

            return await CreateItemInSectionAsync(tutorUserId, section, request);
        }

        public async Task<MaterialResponse> CreateItemForAvailabilityAsync(
            int tutorUserId,
            int availabilityId,
            UpsertMaterialItemRequest request)
        {
            var section = await GetOrCreateDefaultSectionAsync(tutorUserId, availabilityId);
            return await CreateItemInSectionAsync(tutorUserId, section, request);
        }

        public async Task<MaterialResponse> UpdateItemAsync(
            int tutorUserId,
            int materialId,
            UpsertMaterialItemRequest request)
        {
            var material = await _db.Materials
                .FirstOrDefaultAsync(m => m.MaterialId == materialId)
                ?? throw new KeyNotFoundException("Material not found.");

            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, material.AvailabilityId);
            ValidateMaterial(request, allowExistingFile: true);

            material.Title = request.Title.Trim();
            material.Description = NormalizeOptional(request.Description);

            if (request.SectionId.HasValue &&
                request.SectionId.Value != material.MaterialSectionId)
            {
                var targetSection = request.SectionId.Value <= 0
                    ? await GetOrCreateDefaultSectionAsync(tutorUserId, material.AvailabilityId)
                    : await GetTutorSectionAsync(tutorUserId, request.SectionId.Value);

                if (targetSection.AvailabilityId != material.AvailabilityId)
                    throw new InvalidOperationException("Material can only be moved within the same course.");

                material.MaterialSectionId = targetSection.MaterialSectionId;
            }

            if (request.File != null && request.File.Length > 0)
            {
                var oldObjectKey = R2ObjectKey(material.FileUrl);
                var saved = await SaveFileAsync(request.File, material.AvailabilityId);
                material.FileUrl = saved.FileUrl;
                material.FileName = saved.FileName;
                material.ContentType = saved.ContentType;
                material.FileSize = saved.FileSize;
                material.MaterialType = InferMaterialType(saved.FileName, saved.ContentType, saved.FileUrl);

                if (oldObjectKey != null)
                    await _r2Storage.DeleteObjectAsync(oldObjectKey);
            }
            else
            {
                var link = NormalizeOptional(request.FileUrl) ?? NormalizeOptional(request.LinkUrl);
                if (link != null)
                {
                    material.FileUrl = link;
                    material.FileName = null;
                    material.ContentType = null;
                    material.FileSize = null;
                    material.MaterialType = InferMaterialType(null, null, link);
                }
            }

            material.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToMaterialResponse(material);
        }

        public async Task DeleteItemAsync(int tutorUserId, int materialId)
        {
            var material = await _db.Materials
                .FirstOrDefaultAsync(m => m.MaterialId == materialId)
                ?? throw new KeyNotFoundException("Material not found.");

            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, material.AvailabilityId);

            var objectKey = R2ObjectKey(material.FileUrl);
            if (objectKey != null)
                await _r2Storage.DeleteObjectAsync(objectKey);

            _db.Materials.Remove(material);
            await _db.SaveChangesAsync();
        }

        public async Task<MaterialDownloadResult> GetDownloadAsync(int materialId)
        {
            var material = await _db.Materials
                .FirstOrDefaultAsync(m => m.MaterialId == materialId)
                ?? throw new KeyNotFoundException("Material not found.");

            if (string.IsNullOrWhiteSpace(material.FileUrl))
                throw new KeyNotFoundException("Material file not found.");

            var r2ObjectKey = R2ObjectKey(material.FileUrl);
            if (r2ObjectKey != null)
            {
                return new MaterialDownloadResult(
                    FilePath: null,
                    RedirectUrl: await _r2Storage.CreateDownloadUrlAsync(
                        r2ObjectKey,
                        material.FileName ?? material.Title),
                    FileName: material.FileName ?? material.Title,
                    ContentType: material.ContentType ?? "application/octet-stream");
            }

            if (Uri.TryCreate(material.FileUrl, UriKind.Absolute, out _))
            {
                return new MaterialDownloadResult(
                    FilePath: null,
                    RedirectUrl: material.FileUrl,
                    FileName: material.FileName ?? material.Title,
                    ContentType: material.ContentType ?? "application/octet-stream");
            }

            var relativePath = material.FileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, relativePath);

            if (!File.Exists(filePath))
                throw new KeyNotFoundException("Uploaded file is missing from storage.");

            return new MaterialDownloadResult(
                FilePath: filePath,
                RedirectUrl: null,
                FileName: material.FileName ?? Path.GetFileName(filePath),
                ContentType: material.ContentType ?? "application/octet-stream");
        }

        private async Task<MaterialResponse> CreateItemInSectionAsync(
            int tutorUserId,
            MaterialSection section,
            UpsertMaterialItemRequest request)
        {
            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, section.AvailabilityId);
            ValidateMaterial(request, allowExistingFile: false);

            var material = new Material
            {
                AvailabilityId = section.AvailabilityId,
                MaterialSectionId = section.MaterialSectionId,
                Title = request.Title.Trim(),
                Description = NormalizeOptional(request.Description),
                CreatedAt = DateTime.UtcNow
            };

            if (request.File != null && request.File.Length > 0)
            {
                var saved = await SaveFileAsync(request.File, section.AvailabilityId);
                material.FileUrl = saved.FileUrl;
                material.FileName = saved.FileName;
                material.ContentType = saved.ContentType;
                material.FileSize = saved.FileSize;
                material.MaterialType = InferMaterialType(saved.FileName, saved.ContentType, saved.FileUrl);
            }
            else
            {
                var link = NormalizeOptional(request.FileUrl) ?? NormalizeOptional(request.LinkUrl);
                material.FileUrl = link;
                material.MaterialType = InferMaterialType(null, null, link);
            }

            _db.Materials.Add(material);
            await _db.SaveChangesAsync();

            return ToMaterialResponse(material);
        }

        private async Task<MaterialSection> GetTutorSectionAsync(int tutorUserId, int sectionId)
        {
            var section = await _db.MaterialSections
                .FirstOrDefaultAsync(s => s.MaterialSectionId == sectionId)
                ?? throw new KeyNotFoundException("Material section not found.");

            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, section.AvailabilityId);

            return section;
        }

        private async Task<MaterialSection> GetOrCreateDefaultSectionAsync(
            int tutorUserId,
            int? availabilityId)
        {
            if (availabilityId == null || availabilityId <= 0)
                throw new ArgumentException("AvailabilityId is required.");

            await EnsureTutorOwnsAvailabilityAsync(tutorUserId, availabilityId.Value);

            var section = await _db.MaterialSections
                .Where(s => s.AvailabilityId == availabilityId.Value)
                .OrderBy(s => s.DisplayOrder)
                .FirstOrDefaultAsync();

            if (section != null) return section;

            section = new MaterialSection
            {
                AvailabilityId = availabilityId.Value,
                Title = "Materials",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.MaterialSections.Add(section);
            await _db.SaveChangesAsync();

            return section;
        }

        private async Task EnsureCanViewAvailabilityAsync(int userId, int availabilityId)
        {
            if (await TutorOwnsAvailabilityAsync(userId, availabilityId)) return;

            var enrolled = await _db.Bookings.AnyAsync(b =>
                b.AvailabilityId == availabilityId &&
                !b.IsDeleted &&
                AllowedStatuses.Contains(b.Status) &&
                (
                    b.UserId == userId ||
                    (b.Parent != null && b.Parent.UserId == userId) ||
                    (b.Student != null && b.Student.UserId == userId)
                ));

            if (!enrolled)
                throw new UnauthorizedAccessException("You are not enrolled in this course.");
        }

        private async Task EnsureTutorOwnsAvailabilityAsync(int tutorUserId, int availabilityId)
        {
            if (!await TutorOwnsAvailabilityAsync(tutorUserId, availabilityId))
                throw new UnauthorizedAccessException("You do not own this course.");
        }

        private Task<bool> TutorOwnsAvailabilityAsync(int tutorUserId, int availabilityId)
        {
            return _db.Availabilities.AnyAsync(a =>
                a.AvailabilityId == availabilityId &&
                a.Tutor.UserId == tutorUserId);
        }

        private static void ValidateSection(UpsertMaterialSectionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Section title is required.");
        }

        private static void ValidateMaterial(
            UpsertMaterialItemRequest request,
            bool allowExistingFile)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Material title is required.");

            if (!allowExistingFile &&
                (request.File == null || request.File.Length == 0) &&
                string.IsNullOrWhiteSpace(request.FileUrl) &&
                string.IsNullOrWhiteSpace(request.LinkUrl))
            {
                throw new ArgumentException("Upload a file or provide a link.");
            }
        }

        private async Task<SavedMaterialFile> SaveFileAsync(
            IFormFile file,
            int availabilityId)
        {
            if (file.Length > MaxMaterialFileSize)
                throw new InvalidOperationException("Material file must be 10MB or smaller.");

            var uploaded = await _r2Storage.UploadMaterialAsync(file, availabilityId);
            return new SavedMaterialFile(
                FileUrl: $"{R2Prefix}{uploaded.ObjectKey}",
                FileName: uploaded.FileName,
                ContentType: uploaded.ContentType,
                FileSize: uploaded.FileSize);
        }

        private static MaterialSectionResponse ToSectionResponse(MaterialSection section)
        {
            return new MaterialSectionResponse
            {
                SectionId = section.MaterialSectionId,
                MaterialSectionId = section.MaterialSectionId,
                AvailabilityId = section.AvailabilityId,
                Title = section.Title,
                Description = section.Description,
                DisplayOrder = section.DisplayOrder,
                CreatedAt = section.CreatedAt,
                UpdatedAt = section.UpdatedAt,
                Items = section.Materials
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(ToMaterialResponse)
                    .ToList()
            };
        }

        private static MaterialResponse ToMaterialResponse(Material material)
        {
            var fileUrl = NormalizeMaterialFileUrl(material);

            return new MaterialResponse
            {
                MaterialId = material.MaterialId,
                SectionId = material.MaterialSectionId,
                MaterialSectionId = material.MaterialSectionId,
                AvailabilityId = material.AvailabilityId,
                Title = material.Title,
                Description = material.Description,
                FileUrl = fileUrl,
                FileName = material.FileName,
                ContentType = material.ContentType,
                FileSize = material.FileSize,
                MaterialType = material.MaterialType,
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt
            };
        }

        private static string? NormalizeMaterialFileUrl(Material material)
        {
            var url = material.FileUrl?.Trim();
            if (string.IsNullOrWhiteSpace(url)) return null;

            if (R2ObjectKey(url) != null)
                return $"/api/material/items/{material.MaterialId}/download";

            if (url.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                return $"/api/material/items/{material.MaterialId}/download";

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                uri.AbsolutePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return $"/api/material/items/{material.MaterialId}/download";
            }

            return url;
        }

        private static string? R2ObjectKey(string? value)
        {
            var text = value?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;

            return text.StartsWith(R2Prefix, StringComparison.OrdinalIgnoreCase)
                ? text[R2Prefix.Length..]
                : null;
        }

        private static string? NormalizeOptional(string? value)
        {
            var text = value?.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        private static string InferMaterialType(string? fileName, string? contentType, string? url)
        {
            var content = contentType?.ToLowerInvariant() ?? string.Empty;
            var text = $"{fileName} {url}".ToLowerInvariant();

            if (content.Contains("pdf") || text.Contains(".pdf")) return "Pdf";
            if (content.StartsWith("image/") || text.EndsWith(".png") || text.EndsWith(".jpg") || text.EndsWith(".jpeg") || text.EndsWith(".webp")) return "Image";
            if (content.StartsWith("video/") || text.EndsWith(".mp4") || text.EndsWith(".mov") || text.EndsWith(".avi")) return "Video";
            if (!string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out _)) return "Link";

            return "File";
        }

        private sealed record SavedMaterialFile(
            string FileUrl,
            string FileName,
            string? ContentType,
            long FileSize);
    }
}
