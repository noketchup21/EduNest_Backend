using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public class TutorService : ITutorService
    {
        private readonly ITutorRepository _tutorRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly EduNestDbContext _db;

        public TutorService(
            ITutorRepository tutorRepository,
            IUserRepository userRepository,
            ICloudinaryService cloudinaryService,
            EduNestDbContext db)
        {
            _tutorRepository = tutorRepository;
            _userRepository = userRepository;
            _cloudinaryService = cloudinaryService;
            _db = db;
        }

        public async Task<IEnumerable<TutorResponseDTO>> GetAllTutorsAsync()
        {
            var tutors = await _tutorRepository.FindAsync(t =>
                !t.User.IsDeleted);

            var result = new List<TutorResponseDTO>();
            foreach (var tutor in tutors)
            {
                var user = await _userRepository.GetByIdAsync(tutor.UserId);
                if (user == null) continue;

                var dto = tutor.Adapt<TutorResponseDTO>();
                dto.Name = user.Name;
                dto.Email = user.Email;
                dto.Phone = user.Phone;
                result.Add(dto);
            }

            return result;
        }

        public async Task<TutorResponseDTO?> GetTutorByIdAsync(int tutorId)
        {
            var tutor = await _tutorRepository.FindOneAsync(t =>
                t.TutorId == tutorId);

            if (tutor == null) return null;

            var user = await _userRepository.GetByIdAsync(tutor.UserId);
            if (user == null || user.IsDeleted) return null;

            var dto = tutor.Adapt<TutorResponseDTO>();
            dto.Name = user.Name;
            dto.Email = user.Email;
            dto.Phone = user.Phone;
            return dto;
        }

        public async Task<TutorResponseDTO?> GetTutorByUserIdAsync(int userId)
        {
            var tutor = await _tutorRepository.FindOneAsync(t =>
                t.UserId == userId);

            if (tutor == null) return null;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted) return null;

            var dto = tutor.Adapt<TutorResponseDTO>();
            dto.Name = user.Name;
            dto.Email = user.Email;
            dto.Phone = user.Phone;
            return dto;
        }

        public async Task<TutorResponseDTO> UpdateTutorAsync(int userId, UpdateTutorDTO dto)
        {
            // 1. Get user
            var user = await _userRepository.FindOneAsync(u =>
                u.UserId == userId && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // 2. Get tutor profile
            var tutor = await _tutorRepository.FindOneAsync(t =>
                t.UserId == userId);

            if (tutor == null)
                throw new KeyNotFoundException("Tutor profile not found.");

            // 3. Update user fields
            if (!string.IsNullOrWhiteSpace(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                user.Phone = dto.Phone;

            // 4. Update tutor fields
            if (!string.IsNullOrWhiteSpace(dto.Bio))
                tutor.Bio = dto.Bio;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            await _tutorRepository.UpdateAsync(tutor);
            await _tutorRepository.SaveChangesAsync();

            var response = tutor.Adapt<TutorResponseDTO>();
            response.Name = user.Name;
            response.Email = user.Email;
            response.Phone = user.Phone;
            return response;
        }

        public async Task DeleteTutorAsync(int userId)
        {
            // 1. Get user
            var user = await _userRepository.FindOneAsync(u =>
                u.UserId == userId && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // 2. Soft delete user
            user.IsDeleted = true;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<TutorVerificationResponse> GetMyVerificationAsync(int tutorUserId)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.UserId == tutorUserId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            return ToTutorVerificationResponse(tutor);
        }

        public async Task<TutorVerificationResponse> SubmitTutorVerificationAsync(
            int tutorUserId,
            SubmitTutorVerificationRequest request)
        {
            var tutor = await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.UserId == tutorUserId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            if (tutor.IsVerified && tutor.VerificationStatus == "Approved")
            {
                throw new InvalidOperationException(
                    "Your tutor profile has already been approved.");
            }

            var folder = $"edunest/tutor-verification/tutor-{tutor.TutorId}";

            var cccdFrontPublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                request.CccdFrontImage,
                folder,
                "cccd_front");

            var cccdBackPublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                request.CccdBackImage,
                folder,
                "cccd_back");

            var certificatePublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                request.CertificateImage,
                folder,
                "certificate");

            tutor.NationalIdNumber = request.NationalIdNumber.Trim();

            tutor.CccdFrontPublicId = cccdFrontPublicId;
            tutor.CccdBackPublicId = cccdBackPublicId;
            tutor.CertificatePublicId = certificatePublicId;

            tutor.IsVerified = false;
            tutor.VerificationStatus = "Pending";
            tutor.VerificationSubmittedAt = DateTime.UtcNow;
            tutor.VerificationReviewedAt = null;
            tutor.VerificationRejectReason = null;

            var bankAccount = tutor.BankAccount;

            if (bankAccount == null)
            {
                bankAccount = new TutorBankAccount
                {
                    TutorId = tutor.TutorId
                };

                _db.Set<TutorBankAccount>().Add(bankAccount);
                tutor.BankAccount = bankAccount;
            }

            bankAccount.BankName = request.BankName.Trim();
            bankAccount.AccountNumber = request.AccountNumber.Trim();
            bankAccount.AccountHolderName = request.AccountHolderName.Trim();
            bankAccount.BranchName = request.BranchName?.Trim();
            bankAccount.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToTutorVerificationResponse(tutor);
        }

        private TutorVerificationResponse ToTutorVerificationResponse(Tutor tutor)
        {
            return new TutorVerificationResponse
            {
                TutorId = tutor.TutorId,
                UserId = tutor.UserId,

                TutorName = tutor.User?.Name ?? $"Tutor #{tutor.TutorId}",
                Email = tutor.User?.Email ?? string.Empty,

                IsVerified = tutor.IsVerified,
                VerificationStatus = tutor.VerificationStatus,

                NationalIdNumber = tutor.NationalIdNumber,

                CccdFrontImageUrl = string.IsNullOrWhiteSpace(tutor.CccdFrontPublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CccdFrontPublicId),

                CccdBackImageUrl = string.IsNullOrWhiteSpace(tutor.CccdBackPublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CccdBackPublicId),

                CertificateImageUrl = string.IsNullOrWhiteSpace(tutor.CertificatePublicId)
                    ? null
                    : _cloudinaryService.GenerateSignedImageUrl(tutor.CertificatePublicId),

                BankName = tutor.BankAccount?.BankName,
                AccountNumber = tutor.BankAccount?.AccountNumber,
                AccountHolderName = tutor.BankAccount?.AccountHolderName,
                BranchName = tutor.BankAccount?.BranchName,

                VerificationSubmittedAt = tutor.VerificationSubmittedAt,
                VerificationReviewedAt = tutor.VerificationReviewedAt,
                VerificationRejectReason = tutor.VerificationRejectReason
            };
        }

    }
}
