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

        private readonly EduNestDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public MaterialService(EduNestDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        public async Task<List<MaterialSectionResponse>> GetByAvailabilityAsync(int userId, int availabilityId)
        {
            await EnsureCanViewAvailabilityAsync(userId, availabilityId);

            var sections = await _db.MaterialSections
                .Include(s => s.Materials)
                .Where(s => s.AvailabilityId == availabilityId)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.CreatedAt)
                .ToListAsync();

            var orphanMaterials = await _db.Materials
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
                var saved = await SaveFileAsync(request.File);
                material.FileUrl = saved.FileUrl;
                material.FileName = saved.FileName;
                material.ContentType = saved.ContentType;
                material.FileSize = saved.FileSize;
                material.MaterialType = InferMaterialType(saved.FileName, saved.ContentType, saved.FileUrl);
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

            _db.Materials.Remove(material);
            await _db.SaveChangesAsync();
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
                var saved = await SaveFileAsync(request.File);
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

        private async Task<SavedMaterialFile> SaveFileAsync(IFormFile file)
        {
            if (file.Length > 50 * 1024 * 1024)
                throw new InvalidOperationException("Material file must be smaller than 50MB.");

            var uploadsRoot = Path.Combine(
                _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"),
                "uploads",
                "materials");

            Directory.CreateDirectory(uploadsRoot);

            var extension = Path.GetExtension(file.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
            var fileName = $"{Guid.NewGuid():N}{safeExtension}";
            var path = Path.Combine(uploadsRoot, fileName);

            await using (var stream = File.Create(path))
            {
                await file.CopyToAsync(stream);
            }

            return new SavedMaterialFile(
                FileUrl: $"/uploads/materials/{fileName}",
                FileName: file.FileName,
                ContentType: file.ContentType,
                FileSize: file.Length);
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
            return new MaterialResponse
            {
                MaterialId = material.MaterialId,
                SectionId = material.MaterialSectionId,
                MaterialSectionId = material.MaterialSectionId,
                AvailabilityId = material.AvailabilityId,
                Title = material.Title,
                Description = material.Description,
                FileUrl = material.FileUrl,
                FileName = material.FileName,
                ContentType = material.ContentType,
                FileSize = material.FileSize,
                MaterialType = material.MaterialType,
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt
            };
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
