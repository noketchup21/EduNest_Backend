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

        Task<LessonResponse> AddLessonAsync(
            int tutorUserId,
            int bookingId,
            CreateLessonRequest request);

        Task<LessonDetailResponse> GetLessonDetailAsync(
            int tutorUserId,
            int lessonId);

        Task<LessonDetailResponse> SetMeetingLinkAsync(
            int tutorUserId,
            int lessonId,
            string meetingLink);

        Task<LessonResponse> MarkAttendanceAsync(
            int tutorUserId,
            int lessonId,
            MarkAttendanceRequest request);

        Task<LessonResponse> CompleteLessonAsync(
            int tutorUserId,
            int lessonId,
            CompleteLessonRequest request);

        Task<LessonDetailResponse> CompleteLessonGroupAsync(
            int tutorUserId,
            int lessonId);
    }
}
