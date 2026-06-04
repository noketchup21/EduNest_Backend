using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Tutor;

namespace BusinessLayer.IServices
{
    public interface ITutorService
    {
        Task<IEnumerable<TutorResponseDTO>> GetAllTutorsAsync();
        Task<TutorResponseDTO?> GetTutorByIdAsync(int tutorId);
        Task<TutorResponseDTO?> GetTutorByUserIdAsync(int userId);
        Task<TutorResponseDTO> UpdateTutorAsync(int userId, UpdateTutorDTO dto);
        Task DeleteTutorAsync(int userId);
        Task<TutorVerificationResponse> GetMyVerificationAsync(int tutorUserId);
        Task<TutorVerificationResponse> SubmitTutorVerificationAsync(int tutorUserId,SubmitTutorVerificationRequest request);
        Task<TutorBankAccountResponse> UpdateBankAccountAsync(
    int tutorUserId,
    UpdateTutorBankAccountRequest request);
    }
}
