using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Subject;

namespace BusinessLayer.IServices
{
    public interface ISubjectService
    {
        Task<IEnumerable<SubjectResponseDTO>> GetAllSubjectsAsync();
        Task<SubjectResponseDTO?> GetSubjectByIdAsync(int subjectId);
        Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectDTO dto);
        Task<SubjectResponseDTO> UpdateSubjectAsync(int subjectId, UpdateSubjectDTO dto);
        Task<string> DeleteSubjectAsync(int subjectId);
    }
}
