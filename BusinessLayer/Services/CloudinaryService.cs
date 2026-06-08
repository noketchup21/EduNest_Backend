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
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySetting> options)
        {
            var setting = options.Value;

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

            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidOperationException("Only image files are allowed.");

            if (file.Length > 5 * 1024 * 1024)
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

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException(result.Error.Message);

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
