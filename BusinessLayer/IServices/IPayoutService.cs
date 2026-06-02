using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Payment;

namespace BusinessLayer.IServices
{
    public interface IPayoutService
    {
        Task<PayoutResponse> RequestPayoutAsync(int tutorUserId, RequestPayoutRequest request);
        Task<List<PayoutResponse>> GetPayoutsAsync(int tutorUserId);
        Task<PayoutResponse> AdminUpdatePayoutAsync(int payoutId, AdminUpdatePayoutRequest request);
    }
}
