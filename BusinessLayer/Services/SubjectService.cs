using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Subject;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Mapster;

namespace BusinessLayer.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;

        public SubjectService(ISubjectRepository subjectRepository)
        {
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<SubjectResponseDTO>> GetAllSubjectsAsync()
        {
            var subjects = await _subjectRepository.GetAllAsync();
            return subjects.Adapt<IEnumerable<SubjectResponseDTO>>();
        }

        public async Task<SubjectResponseDTO?> GetSubjectByIdAsync(int subjectId)
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId);
            if (subject == null) return null;
            return subject.Adapt<SubjectResponseDTO>();
        }

        public async Task<SubjectResponseDTO> CreateSubjectAsync(CreateSubjectDTO dto)
        {
            // Check duplicate name
            var exists = await _subjectRepository.ExistsAsync(s =>
                s.Name.ToLower() == dto.Name.ToLower());

            if (exists)
                throw new InvalidOperationException($"Subject '{dto.Name}' already exists.");

            var subject = dto.Adapt<Subject>();

            await _subjectRepository.AddAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            return subject.Adapt<SubjectResponseDTO>();
        }

        public async Task<SubjectResponseDTO> UpdateSubjectAsync(int subjectId, UpdateSubjectDTO dto)
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId)
                ?? throw new KeyNotFoundException($"Subject with ID {subjectId} not found.");

            // Check duplicate name only if name is changing
            if (!string.IsNullOrWhiteSpace(dto.Name) &&
                dto.Name.ToLower() != subject.Name.ToLower())
            {
                var exists = await _subjectRepository.ExistsAsync(s =>
                    s.Name.ToLower() == dto.Name.ToLower());

                if (exists)
                    throw new InvalidOperationException($"Subject '{dto.Name}' already exists.");

                subject.Name = dto.Name;
            }

            if (!string.IsNullOrWhiteSpace(dto.Description))
                subject.Description = dto.Description;

            if (dto.Objective != null)
                subject.Objective = NormalizeOptionalText(dto.Objective);

            if (dto.LearningGoals != null)
                subject.LearningGoals = NormalizeOptionalText(dto.LearningGoals);

            if (dto.ExpectedResults != null)
                subject.ExpectedResults = NormalizeOptionalText(dto.ExpectedResults);

            if (dto.RequiredTopics != null)
                subject.RequiredTopics = NormalizeOptionalText(dto.RequiredTopics);

            if (dto.CommonDifficulties != null)
                subject.CommonDifficulties = NormalizeOptionalText(dto.CommonDifficulties);

            await _subjectRepository.UpdateAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            return subject.Adapt<SubjectResponseDTO>();
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public async Task<string> DeleteSubjectAsync(int subjectId)
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId)
                ?? throw new KeyNotFoundException($"Subject with ID {subjectId} not found.");

            await _subjectRepository.DeleteAsync(subject);
            await _subjectRepository.SaveChangesAsync();

            return $"Subject '{subject.Name}' deleted successfully.";
        }
    }
}
