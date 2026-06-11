using BusinessLayer.DTOs.Material;

namespace BusinessLayer.IServices
{
    public interface IMaterialService
    {
        Task<List<MaterialSectionResponse>> GetByAvailabilityAsync(int userId, int availabilityId);

        Task<MaterialSectionResponse> CreateSectionAsync(
            int tutorUserId,
            int availabilityId,
            UpsertMaterialSectionRequest request);

        Task<MaterialSectionResponse> UpdateSectionAsync(
            int tutorUserId,
            int sectionId,
            UpsertMaterialSectionRequest request);

        Task DeleteSectionAsync(int tutorUserId, int sectionId);

        Task<MaterialResponse> CreateItemAsync(
            int tutorUserId,
            int sectionId,
            UpsertMaterialItemRequest request);

        Task<MaterialResponse> CreateItemForAvailabilityAsync(
            int tutorUserId,
            int availabilityId,
            UpsertMaterialItemRequest request);

        Task<MaterialResponse> UpdateItemAsync(
            int tutorUserId,
            int materialId,
            UpsertMaterialItemRequest request);

        Task DeleteItemAsync(int tutorUserId, int materialId);

        Task<MaterialDownloadResult> GetDownloadAsync(int materialId);
    }

    public sealed record MaterialDownloadResult(
        string? FilePath,
        string? RedirectUrl,
        string FileName,
        string ContentType);
}
