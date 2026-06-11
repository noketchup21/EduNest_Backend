using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs.Material
{
    public sealed class UpsertMaterialSectionRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public sealed class UpsertMaterialItemRequest
    {
        public int? AvailabilityId { get; set; }
        public int? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FileUrl { get; set; }
        public string? LinkUrl { get; set; }
        public IFormFile? File { get; set; }
    }

    public sealed class MaterialSectionResponse
    {
        public int SectionId { get; set; }
        public int MaterialSectionId { get; set; }
        public int AvailabilityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<MaterialResponse> Items { get; set; } = new();
    }

    public sealed class MaterialResponse
    {
        public int MaterialId { get; set; }
        public int? SectionId { get; set; }
        public int? MaterialSectionId { get; set; }
        public int AvailabilityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? FileUrl { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public long? FileSize { get; set; }
        public string MaterialType { get; set; } = "File";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
