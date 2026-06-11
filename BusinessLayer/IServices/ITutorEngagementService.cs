using BusinessLayer.DTOs.Tutor;

namespace BusinessLayer.IServices
{
    public interface ITutorEngagementService
    {
        Task<List<FavoriteTutorResponse>> GetFavoriteTutorsAsync(int userId);
        Task<FavoriteTutorResponse> SaveFavoriteTutorAsync(int userId, int tutorId);
        Task UnsaveFavoriteTutorAsync(int userId, int tutorId);
        Task<List<TutorReviewResponse>> GetTutorReviewsAsync(int tutorId);
        Task<List<TutorReviewResponse>> GetMyReviewsAsync(int userId);
        Task<TutorReviewResponse> CreateTutorReviewAsync(
            int userId,
            CreateTutorReviewRequest request);
    }
}
