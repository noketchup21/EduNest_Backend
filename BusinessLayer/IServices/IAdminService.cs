using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.DTOs.Tutor;

namespace BusinessLayer.IServices
{
    public interface IAdminService
    {
        Task TrackDownloadAsync(TrackAppMetricRequest request);
        Task TrackInstallAsync(TrackAppMetricRequest request);

        Task<AdminDashboardResponse> GetDashboardAsync();

        Task<List<TutorVerificationResponse>> GetPendingTutorsAsync();
        Task<TutorVerificationResponse> GetTutorVerificationAsync(int tutorId);
        Task<TutorVerificationResponse> ApproveTutorAsync(int tutorId);
        Task<TutorVerificationResponse> RejectTutorAsync(int tutorId, string? reason);

        Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectDTO request);

        Task<List<PayoutResponse>> GetPayoutsAsync();
        Task<AdminPayoutDetailResponse> GetPayoutDetailAsync(int payoutId);
        Task<PayoutResponse> UpdatePayoutStatusAsync(int payoutId, string status);

        Task<List<TutorVerificationResponse>> GetTutorsAsync();

        Task<TutorVerificationResponse> UpdateTutorAccountStatusAsync(
    int tutorId,
    bool isActive);
    }
}
