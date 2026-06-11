using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BusinessLayer.IServices;
using BusinessLayer.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public sealed class R2StorageService : IR2StorageService
    {
        private readonly CloudflareR2Setting _setting;
        private readonly IAmazonS3 _s3;

        public R2StorageService(IOptions<CloudflareR2Setting> options)
        {
            _setting = options.Value;
            ValidateSettings(_setting);

            var credentials = new BasicAWSCredentials(
                _setting.AccessKeyId,
                _setting.SecretAccessKey);

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{_setting.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                AuthenticationRegion = "auto",
                RegionEndpoint = RegionEndpoint.USEast1
            };

            _s3 = new AmazonS3Client(credentials, config);
        }

        public async Task<R2UploadResult> UploadMaterialAsync(
            IFormFile file,
            int availabilityId,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is required.");

            var extension = Path.GetExtension(file.FileName);
            var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension;
            var key = $"classes/{availabilityId}/materials/{Guid.NewGuid():N}{safeExtension}";

            await using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _setting.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                AutoCloseStream = false
            };

            await _s3.PutObjectAsync(request, cancellationToken);

            return new R2UploadResult(
                ObjectKey: key,
                FileName: file.FileName,
                ContentType: file.ContentType,
                FileSize: file.Length);
        }

        public Task<string> CreateDownloadUrlAsync(
            string objectKey,
            string? downloadFileName = null)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
                throw new InvalidOperationException("Object key is required.");

            var publicUrl = PublicUrl(objectKey);
            if (!string.IsNullOrWhiteSpace(publicUrl))
                return Task.FromResult(publicUrl);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _setting.BucketName,
                Key = objectKey,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(
                    Math.Max(1, _setting.PresignedUrlExpirationMinutes))
            };

            if (!string.IsNullOrWhiteSpace(downloadFileName))
            {
                request.ResponseHeaderOverrides.ContentDisposition =
                    $"attachment; filename=\"{downloadFileName.Replace("\"", string.Empty)}\"";
            }

            return Task.FromResult(_s3.GetPreSignedURL(request));
        }

        public async Task DeleteObjectAsync(string objectKey)
        {
            if (string.IsNullOrWhiteSpace(objectKey)) return;

            await _s3.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _setting.BucketName,
                Key = objectKey
            });
        }

        private string? PublicUrl(string objectKey)
        {
            var baseUrl = _setting.PublicBaseUrl?.Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;

            return $"{baseUrl}/{Uri.EscapeDataString(objectKey).Replace("%2F", "/")}";
        }

        private static void ValidateSettings(CloudflareR2Setting setting)
        {
            if (string.IsNullOrWhiteSpace(setting.AccountId) ||
                string.IsNullOrWhiteSpace(setting.AccessKeyId) ||
                string.IsNullOrWhiteSpace(setting.SecretAccessKey) ||
                string.IsNullOrWhiteSpace(setting.BucketName))
            {
                throw new InvalidOperationException(
                    "Cloudflare R2 settings are missing. Configure CloudflareR2:AccountId, AccessKeyId, SecretAccessKey, and BucketName.");
            }
        }
    }
}
