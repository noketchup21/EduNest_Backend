using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Availability;

namespace BusinessLayer.IServices
{
    public interface IAvailabilityService
    {
        Task<List<AvailabilityResponse>> GetAllAsync();
        Task<List<AvailabilityResponse>> GetByTutorAsync(int tutorId);
        Task<List<AvailabilityResponse>> GetMyAvailabilityAsync(int tutorUserId);
        Task<AvailabilityResponse> CreateAsync(int tutorUserId, CreateAvailabilityRequest request);
        Task<AvailabilityResponse> UpdateAsync(int tutorUserId, int availabilityId, UpdateAvailabilityRequest request);
        Task DeleteAsync(int tutorUserId, int availabilityId);
    }
}
