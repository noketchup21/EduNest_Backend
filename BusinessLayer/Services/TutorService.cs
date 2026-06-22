using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Profile;
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
        private const int MaxCertificateImages = 3;
        private const long MaxTranscriptDocumentBytes = 2 * 1024 * 1024;
        private static readonly HashSet<string> AllowedTranscriptExtensions = new(
            StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv"
        };

        private readonly ITutorRepository _tutorRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IR2StorageService _r2StorageService;
        private readonly EduNestDbContext _db;

        public TutorService(
            ITutorRepository tutorRepository,
            IUserRepository userRepository,
            ICloudinaryService cloudinaryService,
            IR2StorageService r2StorageService,
            EduNestDbContext db)
        {
            _tutorRepository = tutorRepository;
            _userRepository = userRepository;
            _cloudinaryService = cloudinaryService;
            _r2StorageService = r2StorageService;
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

            return await ToTutorVerificationResponseAsync(tutor);
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

            ValidateTranscriptDocument(request.TranscriptDocument);

            var folder = $"edunest/tutor-verification/tutor-{tutor.TutorId}";

            var cccdFrontPublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                request.CccdFrontImage,
                folder,
                "cccd_front");

            var cccdBackPublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                request.CccdBackImage,
                folder,
                "cccd_back");

            var certificateImages = GetCertificateImages(request);
            var certificatePublicIds = new List<string>();

            for (var i = 0; i < certificateImages.Count; i++)
            {
                var certificatePublicId = await _cloudinaryService.UploadAuthenticatedImageAsync(
                    certificateImages[i],
                    folder,
                    $"certificate_{i + 1}");

                certificatePublicIds.Add(certificatePublicId);
            }

            if (request.TranscriptDocument is { Length: > 0 } transcriptDocument)
            {
                var upload = await _r2StorageService.UploadTutorDocumentAsync(
                    transcriptDocument,
                    tutor.TutorId);
                tutor.TranscriptDocumentObjectKey = upload.ObjectKey;
            }

            tutor.NationalIdNumber = request.NationalIdNumber.Trim();

            tutor.CccdFrontPublicId = cccdFrontPublicId;
            tutor.CccdBackPublicId = cccdBackPublicId;
            tutor.CertificatePublicId = SerializeCertificatePublicIds(certificatePublicIds);

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

            tutor.BankAccount.BankName = request.BankName.Trim();

            if (string.IsNullOrWhiteSpace(request.BankBin))
            {
                throw new InvalidOperationException("Bank BIN is required.");
            }

            tutor.BankAccount.BankBin = request.BankBin.Trim();

            tutor.BankAccount.AccountNumber = request.AccountNumber.Trim();
            tutor.BankAccount.AccountHolderName = request.AccountHolderName.Trim();

            tutor.BankAccount.BranchName = string.IsNullOrWhiteSpace(request.BranchName)
                ? null
                : request.BranchName.Trim();

            tutor.BankAccount.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return await ToTutorVerificationResponseAsync(tutor);
        }

        public async Task<TeachingPreparationGuideResponse> GenerateTeachingPreparationGuideAsync(
            int tutorUserId,
            GenerateTeachingPreparationGuideRequest request)
        {
            if (request.SubjectId <= 0)
                throw new InvalidOperationException("A base subject is required.");

            if (string.IsNullOrWhiteSpace(request.Technology))
                throw new InvalidOperationException("Technology or teaching focus is required.");

            var tutorExists = await _db.Tutors.AnyAsync(t => t.UserId == tutorUserId);
            if (!tutorExists)
                throw new KeyNotFoundException("Tutor profile not found.");

            var subject = await _db.Subjects
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SubjectId == request.SubjectId)
                ?? throw new KeyNotFoundException("Base subject not found.");

            var technology = request.Technology.Trim();
            var studentLevel = string.IsNullOrWhiteSpace(request.StudentLevel)
                ? "Beginner"
                : request.StudentLevel.Trim();
            var lessonFocus = request.LessonFocus?.Trim() ?? string.Empty;
            var requiredTopics = ToGuideItems(subject.RequiredTopics);
            var learningGoals = ToGuideItems(subject.LearningGoals);
            var expectedResults = ToGuideItems(subject.ExpectedResults);
            var difficulties = ToGuideItems(subject.CommonDifficulties);

            if (requiredTopics.Count == 0)
            {
                requiredTopics.Add($"Review the core concepts in {subject.Name} before adapting them to {technology}.");
            }

            if (learningGoals.Count == 0 && !string.IsNullOrWhiteSpace(subject.Description))
            {
                learningGoals.Add(subject.Description.Trim());
            }

            var knowledgeItems = requiredTopics
                .Select(topic => $"Understand {topic} well enough to explain it using {technology}.")
                .ToList();

            if (!string.IsNullOrWhiteSpace(lessonFocus))
            {
                knowledgeItems.Insert(
                    0,
                    $"Be ready to connect {lessonFocus} to the base subject objectives.");
            }

            var exampleItems = requiredTopics
                .Take(5)
                .Select(topic => $"Prepare one small {technology} example for {topic}.")
                .ToList();

            if (difficulties.Count == 0)
            {
                difficulties.Add("Identify terminology that may be unfamiliar to a new learner and prepare a simple explanation.");
                difficulties.Add("Prepare a short check-in question that reveals whether the learner understands the concept.");
            }

            var checklistItems = new List<string>
            {
                $"Read the admin objective and goals for {subject.Name} before preparing your materials.",
                $"Test every {technology} example yourself before the lesson.",
                "Prepare one guided practice activity and one independent exercise.",
                "Decide how you will check that the learner reached the expected result."
            };

            return new TeachingPreparationGuideResponse
            {
                SubjectId = subject.SubjectId,
                SubjectName = subject.Name,
                Technology = technology,
                StudentLevel = studentLevel,
                LessonFocus = lessonFocus,
                Objective = subject.Objective?.Trim() ?? string.Empty,
                Sections = new List<TeachingPreparationGuideSection>
                {
                    new()
                    {
                        Title = "Admin learning goals",
                        Items = learningGoals.Count == 0
                            ? new List<string> { "No learning goals have been added yet. Use the subject description as your starting point." }
                            : learningGoals
                    },
                    new()
                    {
                        Title = "What you should understand before teaching",
                        Items = knowledgeItems
                    },
                    new()
                    {
                        Title = "Examples and practice to prepare",
                        Items = exampleItems
                    },
                    new()
                    {
                        Title = "Expected learner results",
                        Items = expectedResults.Count == 0
                            ? new List<string> { "Use the admin objective to decide what the learner should be able to do after the lesson." }
                            : expectedResults
                    },
                    new()
                    {
                        Title = "Common misunderstandings to anticipate",
                        Items = difficulties
                    },
                    new()
                    {
                        Title = "Preparation checklist",
                        Items = checklistItems
                    }
                }
            };
        }

        private static List<string> ToGuideItems(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();

            return value
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim().TrimStart('-', '•', '*').Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private async Task<TutorVerificationResponse> ToTutorVerificationResponseAsync(Tutor tutor)
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

                CertificateImageUrl = GenerateCertificateImageUrls(tutor.CertificatePublicId)
                    .FirstOrDefault(),
                CertificateImageUrls = GenerateCertificateImageUrls(tutor.CertificatePublicId),
                TranscriptDocumentUrl = string.IsNullOrWhiteSpace(tutor.TranscriptDocumentObjectKey)
                    ? null
                    : await _r2StorageService.CreateDownloadUrlAsync(
                        tutor.TranscriptDocumentObjectKey),

                BankName = tutor.BankAccount?.BankName,
                AccountNumber = tutor.BankAccount?.AccountNumber,
                AccountHolderName = tutor.BankAccount?.AccountHolderName,
                BranchName = tutor.BankAccount?.BranchName,
                BankBin = tutor.BankAccount?.BankBin,
                VerificationSubmittedAt = tutor.VerificationSubmittedAt,
                VerificationReviewedAt = tutor.VerificationReviewedAt,
                VerificationRejectReason = tutor.VerificationRejectReason
            };
        }

        private static List<Microsoft.AspNetCore.Http.IFormFile> GetCertificateImages(
            SubmitTutorVerificationRequest request)
        {
            var images = request.CertificateImages
                .Where(file => file != null && file.Length > 0)
                .ToList();

            if (images.Count == 0 &&
                request.CertificateImage != null &&
                request.CertificateImage.Length > 0)
            {
                images.Add(request.CertificateImage);
            }

            images = images
                .Take(MaxCertificateImages + 1)
                .ToList();

            if (images.Count == 0)
            {
                throw new InvalidOperationException("At least one certificate image is required.");
            }

            if (images.Count > MaxCertificateImages)
            {
                throw new InvalidOperationException("Maximum 3 certificate images are allowed.");
            }

            return images;
        }

        private static void ValidateTranscriptDocument(
            Microsoft.AspNetCore.Http.IFormFile? file)
        {
            if (file == null) return;

            if (file.Length == 0)
            {
                throw new InvalidOperationException("Transcript document cannot be empty.");
            }

            if (file.Length > MaxTranscriptDocumentBytes)
            {
                throw new InvalidOperationException("Transcript document must be 2 MB or smaller.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedTranscriptExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Transcript document must be a PDF, Word, Excel, PowerPoint, text, or CSV file.");
            }
        }

        private static string SerializeCertificatePublicIds(List<string> publicIds)
        {
            return publicIds.Count == 1
                ? publicIds[0]
                : JsonSerializer.Serialize(publicIds);
        }

        private List<string> GenerateCertificateImageUrls(string? value)
        {
            return ParseCertificatePublicIds(value)
                .Select(publicId => _cloudinaryService.GenerateSignedImageUrl(publicId))
                .ToList();
        }

        private static List<string> ParseCertificatePublicIds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            var trimmed = value.Trim();

            if (!trimmed.StartsWith("["))
            {
                return new List<string> { trimmed };
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(trimmed)?
                    .Where(publicId => !string.IsNullOrWhiteSpace(publicId))
                    .Select(publicId => publicId.Trim())
                    .ToList() ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string> { trimmed };
            }
        }

    }
}
