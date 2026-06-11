using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.IServices;
using BusinessLayer.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public sealed class CloudinaryService : ICloudinaryService
    {
        private const long MaxImageSizeBytes = 5 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImageContentTypes = new(
            StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySetting> options)
        {
            var setting = options.Value;

            if (string.IsNullOrWhiteSpace(setting.CloudName) ||
                string.IsNullOrWhiteSpace(setting.ApiKey) ||
                string.IsNullOrWhiteSpace(setting.ApiSecret))
            {
                throw new InvalidOperationException(
                    "Cloudinary settings are missing. Configure Cloudinary:CloudName, ApiKey, and ApiSecret.");
            }

            var account = new Account(
                setting.CloudName,
                setting.ApiKey,
                setting.ApiSecret);

            _cloudinary = new Cloudinary(account)
            {
                Api = { Secure = true }
            };
        }

        public async Task<string> UploadAuthenticatedImageAsync(
            IFormFile file,
            string folder,
            string publicIdPrefix)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("File is required.");

            var contentType = file.ContentType?.Trim();
            if (string.IsNullOrWhiteSpace(contentType) ||
                !AllowedImageContentTypes.Contains(contentType))
            {
                throw new InvalidOperationException(
                    "Only JPG, PNG, and WebP image files are allowed.");
            }

            if (file.Length > MaxImageSizeBytes)
                throw new InvalidOperationException("Image must be smaller than 5MB.");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),

                Folder = folder,
                PublicId = $"{publicIdPrefix}_{Guid.NewGuid():N}",

                // Important:
                // This makes the asset not publicly accessible by normal URL.
                Type = "authenticated",

                Overwrite = false,
                UseFilename = false,
                UniqueFilename = true
            };

            ImageUploadResult result;
            try
            {
                result = await _cloudinary.UploadAsync(uploadParams);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Image upload failed. Please try again later.",
                    ex);
            }

            if (result.Error != null)
                throw new InvalidOperationException(
                    $"Image upload failed: {result.Error.Message}");

            return result.PublicId;
        }

        public string GenerateSignedImageUrl(
            string publicId,
            int width = 800,
            int height = 800)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                return string.Empty;

            return _cloudinary.Api.UrlImgUp
                .Secure(true)
                .Signed(true)
                .Type("authenticated")
                .Transform(
                    new Transformation()
                        .Width(width)
                        .Height(height)
                        .Crop("limit")
                        .Quality("auto")
                        .FetchFormat("auto"))
                .BuildUrl(publicId);
        }

        public async Task DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId)) return;

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image,
                Type = "authenticated"
            };

            await _cloudinary.DestroyAsync(deletionParams);
        }
    }
}
