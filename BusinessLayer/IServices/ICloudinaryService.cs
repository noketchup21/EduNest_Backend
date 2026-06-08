using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.IServices
{
    public interface ICloudinaryService
    {
        Task<string> UploadAuthenticatedImageAsync(
            IFormFile file,
            string folder,
            string publicIdPrefix);

        string GenerateSignedImageUrl(
            string publicId,
            int width = 800,
            int height = 800);
        Task DeleteImageAsync(string publicId);
    }
}
