using BusinessLayer.DTOs.Homework;

namespace BusinessLayer.IServices
{
    public interface IHomeworkService
    {
        Task<List<HomeworkResponse>> GetByLessonAsync(int userId, int lessonId);
        Task<HomeworkResponse> GetByIdAsync(int userId, int homeworkId);
        Task<HomeworkResponse> CreateForLessonAsync(int tutorUserId, int lessonId, CreateHomeworkRequest request);
        Task<HomeworkResponse> UpdateAsync(int tutorUserId, int homeworkId, UpdateHomeworkRequest request);
        Task DeleteAsync(int tutorUserId, int homeworkId);
        Task<HomeworkSubmissionResponse> SubmitAsync(int userId, int homeworkId, SubmitHomeworkRequest request);
        Task<HomeworkSubmissionResponse> GradeEssaySubmissionAsync(
            int tutorUserId,
            int homeworkId,
            int submissionId,
            GradeEssaySubmissionRequest request);
    }
}
