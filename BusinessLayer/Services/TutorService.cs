using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Tutor;
using BusinessLayer.IServices;
using DataAccessLayer.IRepositories;
using Mapster;

namespace BusinessLayer.Services
{
    public class TutorService : ITutorService
    {
        private readonly ITutorRepository _tutorRepository;
        private readonly IUserRepository _userRepository;

        public TutorService(
            ITutorRepository tutorRepository,
            IUserRepository userRepository)
        {
            _tutorRepository = tutorRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<TutorResponseDTO>> GetAllTutorsAsync()
        {
            var tutors = await _tutorRepository.FindAsync(t =>
                !t.User.IsDeleted);

            var result = new List<TutorResponseDTO>();
            foreach (var tutor in tutors)
            {
                var user = await _userRepository.GetByIdAsync(tutor.UserId);
                if (user == null) continue;

                var dto = tutor.Adapt<TutorResponseDTO>();
                dto.Name = user.Name;
                dto.Email = user.Email;
                dto.Phone = user.Phone;
                result.Add(dto);
            }

            return result;
        }

        public async Task<TutorResponseDTO?> GetTutorByIdAsync(int tutorId)
        {
            var tutor = await _tutorRepository.FindOneAsync(t =>
                t.TutorId == tutorId);

            if (tutor == null) return null;

            var user = await _userRepository.GetByIdAsync(tutor.UserId);
            if (user == null || user.IsDeleted) return null;

            var dto = tutor.Adapt<TutorResponseDTO>();
            dto.Name = user.Name;
            dto.Email = user.Email;
            dto.Phone = user.Phone;
            return dto;
        }

        public async Task<TutorResponseDTO?> GetTutorByUserIdAsync(int userId)
        {
            var tutor = await _tutorRepository.FindOneAsync(t =>
                t.UserId == userId);

            if (tutor == null) return null;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.IsDeleted) return null;

            var dto = tutor.Adapt<TutorResponseDTO>();
            dto.Name = user.Name;
            dto.Email = user.Email;
            dto.Phone = user.Phone;
            return dto;
        }

        public async Task<TutorResponseDTO> UpdateTutorAsync(int userId, UpdateTutorDTO dto)
        {
            // 1. Get user
            var user = await _userRepository.FindOneAsync(u =>
                u.UserId == userId && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // 2. Get tutor profile
            var tutor = await _tutorRepository.FindOneAsync(t =>
                t.UserId == userId);

            if (tutor == null)
                throw new KeyNotFoundException("Tutor profile not found.");

            // 3. Update user fields
            if (!string.IsNullOrWhiteSpace(dto.Name))
                user.Name = dto.Name;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                user.Phone = dto.Phone;

            // 4. Update tutor fields
            if (!string.IsNullOrWhiteSpace(dto.Bio))
                tutor.Bio = dto.Bio;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            await _tutorRepository.UpdateAsync(tutor);
            await _tutorRepository.SaveChangesAsync();

            var response = tutor.Adapt<TutorResponseDTO>();
            response.Name = user.Name;
            response.Email = user.Email;
            response.Phone = user.Phone;
            return response;
        }

        public async Task DeleteTutorAsync(int userId)
        {
            // 1. Get user
            var user = await _userRepository.FindOneAsync(u =>
                u.UserId == userId && !u.IsDeleted);

            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // 2. Soft delete user
            user.IsDeleted = true;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

    }
}
