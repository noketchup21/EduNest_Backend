using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Tutor;

namespace BusinessLayer.IServices
{
    public interface ITutorSubjectService
    {
        // Tutor manages their own subjects
        Task<IEnumerable<TutorSubjectResponseDTO>> GetMySubjectsAsync(int tutorUserId);
        Task<TutorSubjectResponseDTO> AddSubjectAsync(int tutorUserId, AddTutorSubjectDTO dto);
        Task<string> RemoveSubjectAsync(int tutorUserId, int subjectId);

        // Public — anyone can browse
        Task<IEnumerable<TutorSubjectResponseDTO>> GetSubjectsByTutorIdAsync(int tutorId);
        Task<IEnumerable<TutorSubjectResponseDTO>> GetTutorsBySubjectIdAsync(int subjectId);
    }
}
