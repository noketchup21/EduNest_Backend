using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Report;

namespace BusinessLayer.IServices
{
    public interface IReportService
    {
        Task<TutorReportResponse> CreateReportAsync(
            int reporterUserId,
            CreateTutorReportRequest request);

        Task<List<TutorReportResponse>> GetMyReportsAsync(int reporterUserId);

        Task<List<TutorReportResponse>> AdminGetReportsAsync(string? status);

        Task<TutorReportResponse> AdminGetReportAsync(int reportId);

        Task<TutorReportResponse> AdminUpdateReportStatusAsync(
            int reportId,
            UpdateTutorReportStatusRequest request);

        Task<List<TutorReportResponse>> TutorGetReportsAsync(
    int tutorUserId,
    string? status);
    }
}
