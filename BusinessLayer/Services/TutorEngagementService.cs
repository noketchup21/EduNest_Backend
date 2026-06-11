using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class TutorEngagementService : ITutorEngagementService
    {
        private readonly EduNestDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;

        public TutorEngagementService(
            EduNestDbContext db,
            ICloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<List<FavoriteTutorResponse>> GetFavoriteTutorsAsync(
            int userId)
        {
            var user = await GetLearnerUserAsync(userId);
            var parentId = await ParentIdForLearnerAsync(user);

            var favorites = await FavoriteTutorQuery()
                .Where(f =>
                    f.UserId == user.UserId ||
                    (parentId != null && f.ParentId == parentId))
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return favorites.Select(ToFavoriteTutorResponse).ToList();
        }

        public async Task<FavoriteTutorResponse> SaveFavoriteTutorAsync(
            int userId,
            int tutorId)
        {
            var user = await GetLearnerUserAsync(userId);
            var tutor = await GetActiveTutorAsync(tutorId);
            var parentId = await ParentIdForLearnerAsync(user);

            var existing = await FavoriteTutorQuery()
                .FirstOrDefaultAsync(f =>
                    f.TutorId == tutor.TutorId &&
                    (f.UserId == user.UserId ||
                     (parentId != null && f.ParentId == parentId)));

            if (existing != null)
            {
                if (existing.UserId == null)
                    existing.UserId = user.UserId;

                await _db.SaveChangesAsync();
                return ToFavoriteTutorResponse(existing);
            }

            var favorite = new FavoriteTutor
            {
                TutorId = tutor.TutorId,
                UserId = user.UserId,
                ParentId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            _db.FavoriteTutors.Add(favorite);
            await _db.SaveChangesAsync();

            favorite.Tutor = tutor;
            favorite.User = user;

            return ToFavoriteTutorResponse(favorite);
        }

        public async Task UnsaveFavoriteTutorAsync(int userId, int tutorId)
        {
            var user = await GetLearnerUserAsync(userId);
            var parentId = await ParentIdForLearnerAsync(user);

            var favorites = await _db.FavoriteTutors
                .Where(f =>
                    f.TutorId == tutorId &&
                    (f.UserId == user.UserId ||
                     (parentId != null && f.ParentId == parentId)))
                .ToListAsync();

            if (favorites.Count == 0)
                return;

            _db.FavoriteTutors.RemoveRange(favorites);
            await _db.SaveChangesAsync();
        }

        public async Task<List<TutorReviewResponse>> GetTutorReviewsAsync(
            int tutorId)
        {
            _ = await GetActiveTutorAsync(tutorId);

            var reviews = await ReviewQuery()
                .Where(r => r.TutorId == tutorId)
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            return reviews.Select(ToTutorReviewResponse).ToList();
        }

        public async Task<List<TutorReviewResponse>> GetMyReviewsAsync(int userId)
        {
            var user = await GetLearnerUserAsync(userId);
            var parentId = await ParentIdForLearnerAsync(user);

            var reviews = await ReviewQuery()
                .Where(r =>
                    r.UserId == user.UserId ||
                    (parentId != null && r.ParentId == parentId))
                .OrderByDescending(r => r.UploadedAt)
                .ToListAsync();

            return reviews.Select(ToTutorReviewResponse).ToList();
        }

        public async Task<TutorReviewResponse> CreateTutorReviewAsync(
            int userId,
            CreateTutorReviewRequest request)
        {
            if (request.Rating < 1 || request.Rating > 5)
                throw new InvalidOperationException("Rating must be between 1 and 5.");

            var user = await GetLearnerUserAsync(userId);
            var parentId = await ParentIdForLearnerAsync(user);

            var booking = await _db.Bookings
                .Include(b => b.Payments)
                .Include(b => b.User)
                .Include(b => b.Parent)
                    .ThenInclude(p => p.User)
                .Include(b => b.Student)
                    .ThenInclude(s => s.User)
                .Include(b => b.Availability)
                    .ThenInclude(a => a.Tutor)
                        .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == request.BookingId &&
                    !b.IsDeleted)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (!UserOwnsBooking(user.UserId, parentId, booking))
                throw new UnauthorizedAccessException("This booking does not belong to you.");

            if (booking.Availability.TutorId != request.TutorId)
                throw new InvalidOperationException("Booking does not belong to this tutor.");

            if (!CanReviewBooking(booking))
                throw new InvalidOperationException(
                    "You can review a tutor after booking payment is completed.");

            var alreadyReviewed = await _db.Reviews.AnyAsync(r =>
                r.BookingId == booking.BookingId &&
                (r.UserId == user.UserId ||
                 (parentId != null && r.ParentId == parentId)));

            if (alreadyReviewed)
                throw new InvalidOperationException("This booking has already been reviewed.");

            var review = new Review
            {
                BookingId = booking.BookingId,
                TutorId = request.TutorId,
                UserId = user.UserId,
                ParentId = parentId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim() ?? string.Empty,
                UploadedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            await RecalculateTutorRatingAsync(request.TutorId);

            review.User = user;
            review.Parent = parentId == null
                ? null
                : await _db.Parents
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.ParentId == parentId);

            return ToTutorReviewResponse(review);
        }

        private IQueryable<FavoriteTutor> FavoriteTutorQuery()
        {
            return _db.FavoriteTutors
                .Include(f => f.User)
                .Include(f => f.Parent)
                    .ThenInclude(p => p!.User)
                .Include(f => f.Tutor)
                    .ThenInclude(t => t.User);
        }

        private IQueryable<Review> ReviewQuery()
        {
            return _db.Reviews
                .Include(r => r.User)
                .Include(r => r.Parent)
                    .ThenInclude(p => p!.User)
                .Include(r => r.Booking)
                    .ThenInclude(b => b.User)
                .Include(r => r.Tutor)
                    .ThenInclude(t => t.User);
        }

        private async Task<User> GetLearnerUserAsync(int userId)
        {
            var user = await _db.Users
                .Include(u => u.Parent)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            var role = user.Role.Trim();

            if (!role.Equals("Parent", StringComparison.OrdinalIgnoreCase) &&
                !role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(
                    "Only parent or student accounts can use this feature.");
            }

            return user;
        }

        private async Task<Tutor> GetActiveTutorAsync(int tutorId)
        {
            return await _db.Tutors
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.TutorId == tutorId &&
                    !t.User.IsDeleted &&
                    t.User.IsActive)
                ?? throw new KeyNotFoundException("Tutor not found.");
        }

        private async Task<int?> ParentIdForLearnerAsync(User user)
        {
            if (user.Parent != null)
                return user.Parent.ParentId;

            if (user.Student != null)
                return user.Student.ParentId;

            if (user.Role.Equals("Parent", StringComparison.OrdinalIgnoreCase))
            {
                var parent = await _db.Parents
                    .FirstOrDefaultAsync(p => p.UserId == user.UserId);

                if (parent != null)
                    return parent.ParentId;
            }

            return null;
        }

        private static bool UserOwnsBooking(
            int userId,
            int? parentId,
            Booking booking)
        {
            return booking.UserId == userId ||
                booking.Parent?.UserId == userId ||
                booking.Student?.UserId == userId ||
                (parentId != null && booking.ParentId == parentId);
        }

        private static bool CanReviewBooking(Booking booking)
        {
            var status = booking.Status.Trim();

            if (status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Paid", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return booking.Payments.Any(p =>
                p.Status.Equals("Paid", StringComparison.OrdinalIgnoreCase));
        }

        private async Task RecalculateTutorRatingAsync(int tutorId)
        {
            var tutor = await _db.Tutors.FirstOrDefaultAsync(t => t.TutorId == tutorId)
                ?? throw new KeyNotFoundException("Tutor not found.");

            var average = await _db.Reviews
                .Where(r => r.TutorId == tutorId)
                .AverageAsync(r => (double?)r.Rating);

            tutor.Rating = Math.Round(average ?? 0, 2);
            await _db.SaveChangesAsync();
        }

        private FavoriteTutorResponse ToFavoriteTutorResponse(FavoriteTutor favorite)
        {
            var tutor = favorite.Tutor;
            var tutorUser = tutor.User;

            return new FavoriteTutorResponse
            {
                FavoriteTutorId = favorite.FavoriteId,
                TutorId = favorite.TutorId,
                UserId = tutor.UserId,
                Name = tutorUser?.Name ?? $"Tutor #{favorite.TutorId}",
                Email = tutorUser?.Email ?? string.Empty,
                Phone = tutorUser?.Phone,
                Bio = tutor.Bio,
                Rating = tutor.Rating,
                IsVerified = tutor.IsVerified,
                AvatarUrl = AvatarUrl(tutorUser)
            };
        }

        private TutorReviewResponse ToTutorReviewResponse(Review review)
        {
            var reviewerName =
                review.User?.Name ??
                review.Parent?.User?.Name ??
                review.Booking?.User?.Name ??
                "Learner";

            return new TutorReviewResponse
            {
                ReviewId = review.ReviewId,
                BookingId = review.BookingId,
                TutorId = review.TutorId,
                UserId = review.UserId ?? review.Parent?.UserId ?? 0,
                Rating = (int)Math.Round(review.Rating),
                Comment = review.Comment ?? string.Empty,
                CreatedAt = review.UploadedAt,
                ReviewerName = reviewerName
            };
        }

        private string? AvatarUrl(User? user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.AvatarPublicId))
                return null;

            return _cloudinaryService.GenerateSignedImageUrl(
                user.AvatarPublicId,
                300,
                300);
        }
    }
}
