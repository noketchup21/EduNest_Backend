using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Attendance;
using BusinessLayer.DTOs.Lesson;

namespace BusinessLayer.IServices
{
    public interface ILessonService
    {
        Task<List<LessonResponse>> GetMyLessonsAsync(int userId);
        Task<LessonResponse> AddLessonAsync(int tutorUserId, int bookingId, CreateLessonRequest request);
        Task<LessonResponse> MarkAttendanceAsync(int tutorUserId, int lessonId, MarkAttendanceRequest request);
        Task<LessonResponse> CompleteLessonAsync(int tutorUserId, int lessonId, CompleteLessonRequest request);
    }
}
