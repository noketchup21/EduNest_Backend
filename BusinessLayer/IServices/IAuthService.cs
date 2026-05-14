using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.User;

namespace BusinessLayer.IServices
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterUserDTO dto);
        Task<AuthResponseDTO> LoginAsync(LoginUserDTO dto);
        Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO dto);
        Task LogoutAsync(int userId);
        Task<bool> VerifyEmailAsync(VerifyEmailDTO dto);
        Task<string> ResendVerificationCodeAsync(ResendVerificationDTO dto);
    }
}
