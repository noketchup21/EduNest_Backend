using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public sealed class TutorReportRepository : ITutorReportRepository
    {
        private readonly EduNestDbContext _db;

        public TutorReportRepository(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<Booking?> GetBookingForReportAsync(
            int bookingId,
            int reporterUserId)
        {
            return await _db.Bookings
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Subject)
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Tutor)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.UserId == reporterUserId &&
                    !b.IsDeleted);
        }

        public async Task<bool> LessonBelongsToBookingAsync(
            int lessonId,
            int bookingId)
        {
            return await _db.Lessons.AnyAsync(l =>
                l.LessonId == lessonId &&
                l.BookingId == bookingId);
        }

        public async Task AddReportAsync(TutorReport report)
        {
            await _db.TutorReports.AddAsync(report);
        }

        public async Task AddProofImageAsync(TutorReportProofImage proofImage)
        {
            await _db.TutorReportProofImages.AddAsync(proofImage);
        }

        public async Task<TutorReport?> GetReportByIdAsync(int reportId)
        {
            return await BaseReportQuery()
                .FirstOrDefaultAsync(r => r.TutorReportId == reportId);
        }

        public async Task<TutorReport?> GetReportForUpdateAsync(int reportId)
        {
            return await _db.TutorReports
                .FirstOrDefaultAsync(r => r.TutorReportId == reportId);
        }

        public async Task<List<TutorReport>> GetReportsByReporterAsync(
            int reporterUserId)
        {
            return await BaseReportQuery()
                .Where(r => r.ReporterUserId == reporterUserId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TutorReport>> GetReportsForAdminAsync(
            string? status)
        {
            var query = BaseReportQuery();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TutorReport>> GetReportsForTutorAsync(
    int tutorUserId,
    string? status)
        {
            var query = BaseReportQuery()
                .Where(r => r.Tutor.UserId == tutorUserId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        private IQueryable<TutorReport> BaseReportQuery()
        {
            return _db.TutorReports
                .Include(r => r.ReporterUser)
                .Include(r => r.Tutor)
                    .ThenInclude(t => t.User)
                .Include(r => r.Availability)
                    .ThenInclude(a => a.Subject)
                .Include(r => r.ProofImages);
        }
    }
}
