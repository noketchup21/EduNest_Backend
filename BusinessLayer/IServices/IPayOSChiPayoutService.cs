using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Payment;

namespace BusinessLayer.IServices
{
    public interface IPayOSChiPayoutService
    {
        Task<PayOSChiPayoutResult> CreateTutorPayoutAsync(
            int payoutRequestId,
            int amount,
            string tutorBankBin,
            string tutorAccountNumber,
            string description);
    }
}