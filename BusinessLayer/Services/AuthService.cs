using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.User;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Mapster;
using Microsoft.Extensions.Configuration;

namespace BusinessLayer.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IStudentRepository _studentRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly IParentRepository _parentRepository;

        public AuthService(IUserRepository userRepository, ITokenService tokenService, IConfiguration configuration, IEmailService emailService, IStudentRepository studentRepository, ITutorRepository tutorRepository, IParentRepository parentRepository)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _configuration = configuration;
            _emailService = emailService;
            _studentRepository = studentRepository;
            _tutorRepository = tutorRepository;
            _parentRepository = parentRepository;
        }
        public async Task<string> RegisterAsync(RegisterUserDTO dto)
        {
            // 1. Check if email already exists
            var existingUser = await _userRepository.FindOneAsync(u => u.Email == dto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email is already registered.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new ArgumentException("Email is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("Name is required.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Password is required.");

            // 2. Map DTO → Entity
            var user = dto.Adapt<User>();
            user.Email = dto.Email;
            user.Name = dto.Name;
            user.Role = dto.Role ?? "User";
            user.Phone = dto.Phone;
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = false;
            user.IsDeleted = false;
            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 3. Generate email verification token
            user.EmailVerificationToken = GenerateVerificationCode();
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(10);

            // 4. Save user
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            await CreateRoleProfileAsync(user, dto);

            // 5. Send verification email
            await _emailService.SendVerificationCodeAsync(user.Email, user.Name, user.EmailVerificationToken);

            return "Registration successful. Please check your email for the 6-digit verification code.";
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailDTO dto)
        {
            // 1. Find user by email
            var user = await _userRepository.FindOneAsync(u =>
                u.Email == dto.Email && !u.IsDeleted);

            if (user == null)
                throw new InvalidOperationException("User not found.");

            if (user.IsActive)
                throw new InvalidOperationException("Email is already verified.");

            // 2. Check code matches
            if (user.EmailVerificationToken != dto.Code)
                throw new InvalidOperationException("Invalid verification code.");

            // 3. Check expiry
            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
                throw new InvalidOperationException("Verification code has expired. Please request a new one.");

            // 4. Mark as verified
            user.IsActive = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public async Task<string> ResendVerificationCodeAsync(ResendVerificationDTO dto)
        {
            var user = await _userRepository.FindOneAsync(u =>
                u.Email == dto.Email && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            if (user.IsActive)
                throw new InvalidOperationException("Email is already verified.");

            // Generate new code
            user.EmailVerificationToken = GenerateVerificationCode();
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddMinutes(10);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            await _emailService.SendVerificationCodeAsync(user.Email, user.Name, user.EmailVerificationToken);

            return "A new verification code has been sent to your email.";
        }

        public async Task<AuthResponseDTO> LoginAsync(LoginUserDTO dto)
        {
            // 1. Find user by email
            var user = await _userRepository.FindOneAsync(u =>
                u.Email == dto.Email && !u.IsDeleted);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // 2. Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid email or password.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is disabled.");

            // 3. Generate tokens
            var claims = GetClaims(user);
            var accessToken = _tokenService.GenerateAccessToken(claims);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // 4. Save refresh token to DB
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(30),
                User = user.Adapt<UserResponseDTO>()
            };
        }

        public async Task<AuthResponseDTO> RefreshTokenAsync(RefreshTokenRequestDTO dto)
        {
            // 1. Validate the expired access token
            var principal = _tokenService.GetPrincipalFromExpiredToken(dto.AccessToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdClaim == null)
                throw new UnauthorizedAccessException("Invalid access token.");

            // 2. Find user
            var user = await _userRepository.GetByIdAsync(int.Parse(userIdClaim));
            if (user == null || user.IsDeleted)
                throw new UnauthorizedAccessException("User not found.");

            // 3. Validate refresh token
            if (user.RefreshToken != dto.RefreshToken)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token has expired. Please login again.");

            // 4. Generate new tokens
            var claims = GetClaims(user);
            var newAccessToken = _tokenService.GenerateAccessToken(claims);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // 5. Save new refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiry = DateTime.UtcNow.AddMinutes(30),
                User = user.Adapt<UserResponseDTO>()
            };
        }

        public async Task RevokeTokenAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException($"User {userId} not found.");

            // Clear refresh token — forces re-login
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        //Helpers-------------------------------

        private static string GenerateVerificationCode()
        {
            // Cryptographically random 6-digit code
            var randomNumber = new byte[4];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            var value = Math.Abs(BitConverter.ToInt32(randomNumber, 0)) % 1000000;
            return value.ToString("D6"); // always 6 digits e.g. "047832"
        }
        private static IEnumerable<Claim> GetClaims(User user) =>
        [
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role)
        ];
        private async Task CreateRoleProfileAsync(User user, RegisterUserDTO dto)
        {
            switch (user.Role)
            {
                case "Student":
                    var student = new Student
                    {
                        UserId = user.UserId,
                        ParentId = null,
                        Grade = 0,
                        School = dto.School ?? string.Empty
                    };
                    await _studentRepository.AddAsync(student);
                    await _studentRepository.SaveChangesAsync();
                    break;

                case "Tutor":
                    var tutor = new Tutor
                    {
                        UserId = user.UserId,
                        Bio = dto.Bio ?? string.Empty,
                        Revenue = 0,
                        Rating = 0,
                        IsVerified = false  // Tutors need admin approval
                    };
                    await _tutorRepository.AddAsync(tutor);
                    await _tutorRepository.SaveChangesAsync();
                    break;

                case "Parent":
                    var parent = new Parent
                    {
                        UserId = user.UserId,
                        Address = dto.Address ?? string.Empty
                    };
                    await _parentRepository.AddAsync(parent);
                    await _parentRepository.SaveChangesAsync();
                    break;
            }
        }
    }
}
