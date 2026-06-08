using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.SupportReport;

namespace BusinessLayer.IServices
{
    public interface ISupportReportService
    {
        Task<SupportReportResponse> CreateAsync(
            int userId,
            string role,
            CreateSupportReportRequest request);

        Task<List<SupportReportResponse>> GetMineAsync(int userId);

        Task<List<SupportReportResponse>> AdminGetAllAsync(
            string? role,
            string? status);

        Task<SupportReportResponse> AdminGetDetailAsync(int supportReportId);

        Task<SupportReportResponse> AdminUpdateStatusAsync(
            int supportReportId,
            UpdateSupportReportStatusRequest request);
    }
}
