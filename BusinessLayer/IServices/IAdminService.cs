using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Admin;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.DTOs.Subject;

namespace BusinessLayer.IServices
{
    public interface IAdminService
    {
        Task TrackDownloadAsync(TrackAppMetricRequest request);
        Task TrackInstallAsync(TrackAppMetricRequest request);

        Task<AdminDashboardResponse> GetDashboardAsync();

        Task<List<AdminTutorResponse>> GetPendingTutorsAsync();
        Task<AdminTutorResponse> ApproveTutorAsync(int tutorId);
        Task<AdminTutorResponse> RejectTutorAsync(int tutorId);

        Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectDTO request);

        Task<List<PayoutResponse>> GetPayoutsAsync();
        Task<PayoutResponse> UpdatePayoutStatusAsync(int payoutId, string status);
    }
}
