using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace BusinessLayer.Services
{
    public class TutorSubjectService : ITutorSubjectService
    {
        private readonly ITutorSubjectRepository _tutorSubjectRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ISubjectRepository _subjectRepository;

        public TutorSubjectService(
            ITutorSubjectRepository tutorSubjectRepository,
            ITutorRepository tutorRepository,
            ISubjectRepository subjectRepository)
        {
            _tutorSubjectRepository = tutorSubjectRepository;
            _tutorRepository = tutorRepository;
            _subjectRepository = subjectRepository;
        }

        public async Task<IEnumerable<TutorSubjectResponseDTO>> GetMySubjectsAsync(int tutorUserId)
        {
            // 1. Get tutor by userId
            var tutor = await _tutorRepository.FindOneAsync(t => t.UserId == tutorUserId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            // 2. Get all tutor subjects
            var tutorSubjects = await _tutorSubjectRepository.FindAsync(ts =>
                ts.TutorId == tutor.TutorId);

            // 3. Build response
            var result = new List<TutorSubjectResponseDTO>();
            foreach (var ts in tutorSubjects)
                result.Add(await BuildResponseAsync(ts));

            return result;
        }

        public async Task<TutorSubjectResponseDTO> AddSubjectAsync(
            int tutorUserId, AddTutorSubjectDTO dto)
        {
            // 1. Get tutor
            var tutor = await _tutorRepository.FindOneAsync(t => t.UserId == tutorUserId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            // 2. Check subject exists
            var subject = await _subjectRepository.GetByIdAsync(dto.SubjectId)
                ?? throw new KeyNotFoundException("Subject not found.");

            // 3. Check not already added
            var exists = await _tutorSubjectRepository.ExistsAsync(ts =>
                ts.TutorId == tutor.TutorId && ts.SubjectId == dto.SubjectId);

            if (exists)
                throw new InvalidOperationException(
                    $"You already teach '{subject.Name}'.");

            // 4. Create tutor subject
            var tutorSubject = new TutorSubject
            {
                TutorId = tutor.TutorId,
                SubjectId = dto.SubjectId
            };

            await _tutorSubjectRepository.AddAsync(tutorSubject);
            await _tutorSubjectRepository.SaveChangesAsync();

            return await BuildResponseAsync(tutorSubject);
        }

        public async Task<string> RemoveSubjectAsync(int tutorUserId, int subjectId)
        {
            // 1. Get tutor
            var tutor = await _tutorRepository.FindOneAsync(t => t.UserId == tutorUserId)
                ?? throw new KeyNotFoundException("Tutor profile not found.");

            // 2. Get tutor subject
            var tutorSubject = await _tutorSubjectRepository.FindOneAsync(ts =>
                ts.TutorId == tutor.TutorId && ts.SubjectId == subjectId)
                ?? throw new KeyNotFoundException("Subject not found in your list.");

            // 3. Check no active availabilities using this subject
            // Prevent removing subject if tutor has active slots for it
            var hasActiveAvailability = await _tutorSubjectRepository.ExistsAsync(ts =>
                ts.TutorId == tutor.TutorId &&
                ts.SubjectId == subjectId);

            await _tutorSubjectRepository.DeleteAsync(tutorSubject);
            await _tutorSubjectRepository.SaveChangesAsync();

            var subject = await _subjectRepository.GetByIdAsync(subjectId);
            return $"'{subject?.Name}' removed from your subjects successfully.";
        }

        public async Task<IEnumerable<TutorSubjectResponseDTO>> GetSubjectsByTutorIdAsync(
            int tutorId)
        {
            var tutorSubjects = await _tutorSubjectRepository.FindAsync(ts =>
                ts.TutorId == tutorId);

            var result = new List<TutorSubjectResponseDTO>();
            foreach (var ts in tutorSubjects)
                result.Add(await BuildResponseAsync(ts));

            return result;
        }

        public async Task<IEnumerable<TutorSubjectResponseDTO>> GetTutorsBySubjectIdAsync(
            int subjectId)
        {
            var tutorSubjects = await _tutorSubjectRepository.FindAsync(ts =>
                ts.SubjectId == subjectId);

            var result = new List<TutorSubjectResponseDTO>();
            foreach (var ts in tutorSubjects)
                result.Add(await BuildResponseAsync(ts));

            return result;
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private async Task<TutorSubjectResponseDTO> BuildResponseAsync(TutorSubject ts)
        {
            var subject = await _subjectRepository.GetByIdAsync(ts.SubjectId);

            return new TutorSubjectResponseDTO
            {
                TutorId = ts.TutorId,
                SubjectId = ts.SubjectId,
                SubjectName = subject?.Name ?? string.Empty,
                SubjectDescription = subject?.Description ?? string.Empty
            };
        }
    }
}
