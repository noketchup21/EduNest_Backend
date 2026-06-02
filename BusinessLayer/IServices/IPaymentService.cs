using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Payment;

namespace BusinessLayer.IServices
{
    public interface IPaymentService
    {
        Task<CreatePaymentResponse> CreatePayOsPaymentAsync(int userId, int bookingId);
        Task HandlePayOsWebhookAsync(PayOsWebhookRequest request);
        Task<CreatePaymentResponse> SyncPayOsPaymentAsync(int userId, int bookingId);
        Task<string> DebugPayOsStatusAsync(long orderCode);
    }
}
