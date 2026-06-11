using Microsoft.AspNetCore.Http;

namespace BusinessLayer.IServices
{
    public interface IR2StorageService
    {
        Task<R2UploadResult> UploadMaterialAsync(
            IFormFile file,
            int availabilityId,
            CancellationToken cancellationToken = default);

        Task<string> CreateDownloadUrlAsync(
            string objectKey,
            string? downloadFileName = null);

        Task DeleteObjectAsync(string objectKey);
    }

    public sealed record R2UploadResult(
        string ObjectKey,
        string FileName,
        string? ContentType,
        long FileSize);
}
