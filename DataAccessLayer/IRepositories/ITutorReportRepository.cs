using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface ITutorReportRepository
    {
        Task<Booking?> GetBookingForReportAsync(int bookingId, int reporterUserId);

        Task<bool> LessonBelongsToBookingAsync(int lessonId, int bookingId);

        Task AddReportAsync(TutorReport report);

        Task AddProofImageAsync(TutorReportProofImage proofImage);

        Task<TutorReport?> GetReportByIdAsync(int reportId);

        Task<TutorReport?> GetReportForUpdateAsync(int reportId);

        Task<List<TutorReport>> GetReportsByReporterAsync(int reporterUserId);

        Task<List<TutorReport>> GetReportsForAdminAsync(string? status);

        Task<List<TutorReport>> GetReportsForTutorAsync(
    int tutorUserId,
    string? status);

        Task SaveChangesAsync();
    }
}
