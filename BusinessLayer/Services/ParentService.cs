using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Parent;
using BusinessLayer.IServices;
using DataAccessLayer.IRepositories;

namespace BusinessLayer.Services
{
    public class ParentService : IParentService
    {
        private readonly IParentRepository _parentRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IUserRepository _userRepository;

        public ParentService(
            IParentRepository parentRepository,
            IStudentRepository studentRepository,
            IUserRepository userRepository)
        {
            _parentRepository = parentRepository;
            _studentRepository = studentRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<ParentResponseDTO>> GetAllParentsAsync()
        {
            var parents = await _parentRepository.GetAllAsync();
            var result = new List<ParentResponseDTO>();

            foreach (var parent in parents)
                result.Add(await BuildParentResponseAsync(parent));

            return result;
        }

        public async Task<ParentResponseDTO?> GetParentByIdAsync(int parentId)
        {
            var parent = await _parentRepository.FindOneAsync(p =>
                p.ParentId == parentId);

            if (parent == null) return null;
            return await BuildParentResponseAsync(parent);
        }

        public async Task<ParentResponseDTO?> GetParentByUserIdAsync(int userId)
        {
            var parent = await _parentRepository.FindOneAsync(p =>
                p.UserId == userId);

            if (parent == null) return null;
            return await BuildParentResponseAsync(parent);
        }

        public async Task<ParentResponseDTO> UpdateParentAsync(int userId, UpdateParentDTO dto)
        {
            var user = await _userRepository.FindOneAsync(u =>
                u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            var parent = await _parentRepository.FindOneAsync(p =>
                p.UserId == userId)
                ?? throw new KeyNotFoundException("Parent profile not found.");

            if (!string.IsNullOrWhiteSpace(dto.Name)) user.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Phone)) user.Phone = dto.Phone;
            if (!string.IsNullOrWhiteSpace(dto.Address)) parent.Address = dto.Address;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
            await _parentRepository.UpdateAsync(parent);
            await _parentRepository.SaveChangesAsync();

            return await BuildParentResponseAsync(parent);
        }

        public async Task DeleteParentAsync(int userId)
        {
            var user = await _userRepository.FindOneAsync(u =>
                u.UserId == userId && !u.IsDeleted)
                ?? throw new KeyNotFoundException("User not found.");

            user.IsDeleted = true;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<string> LinkChildAsync(int parentUserId, LinkChildDTO dto)
        {
            // 1. Get parent record by userId
            var parent = await _parentRepository.FindOneAsync(p =>
                p.UserId == parentUserId);

            if (parent == null)
                throw new KeyNotFoundException("Parent profile not found.");

            // 2. Find child user by email
            var childUser = await _userRepository.FindOneAsync(u =>
                u.Email == dto.ChildEmail && !u.IsDeleted);

            if (childUser == null)
                throw new KeyNotFoundException("No account found with that email.");

            // 3. Make sure the account is a Student
            if (childUser.Role != "Student")
                throw new InvalidOperationException("This account is not a Student.");

            // 4. Get student record
            var student = await _studentRepository.FindOneAsync(s =>
                s.UserId == childUser.UserId);

            if (student == null)
                throw new KeyNotFoundException("Student profile not found.");

            // 5. Check if already linked to this parent
            if (student.ParentId == parent.ParentId)
                throw new InvalidOperationException("This student is already linked to your account.");

            // 6. Check if already linked to another parent
            if (student.ParentId != null)
                throw new InvalidOperationException("This student is already linked to another parent.");

            // 7. Link child to parent
            student.ParentId = parent.ParentId;
            await _studentRepository.UpdateAsync(student);
            await _studentRepository.SaveChangesAsync();

            return $"Successfully linked {childUser.Name} to your account.";
        }

        public async Task<string> UnlinkChildAsync(int parentUserId, int studentId)
        {
            // 1. Get parent record
            var parent = await _parentRepository.FindOneAsync(p =>
                p.UserId == parentUserId);

            if (parent == null)
                throw new KeyNotFoundException("Parent profile not found.");

            // 2. Get student record
            var student = await _studentRepository.FindOneAsync(s =>
                s.StudentId == studentId && s.ParentId == parent.ParentId);

            if (student == null)
                throw new KeyNotFoundException("Student not found or not linked to your account.");

            // 3. Unlink
            student.ParentId = null;
            await _studentRepository.UpdateAsync(student);
            await _studentRepository.SaveChangesAsync();

            return "Successfully unlinked student from your account.";
        }

        public async Task<IEnumerable<ChildResponseDTO>> GetMyChildrenAsync(int parentUserId)
        {
            // 1. Get parent record
            var parent = await _parentRepository.FindOneAsync(p =>
                p.UserId == parentUserId);

            if (parent == null)
                throw new KeyNotFoundException("Parent profile not found.");

            // 2. Get all students linked to this parent
            var students = await _studentRepository.FindAsync(s =>
                s.ParentId == parent.ParentId);

            if (!students.Any())
                return Enumerable.Empty<ChildResponseDTO>();

            // 3. Build response with user details
            var result = new List<ChildResponseDTO>();
            foreach (var student in students)
            {
                var user = await _userRepository.GetByIdAsync(student.UserId);
                if (user == null) continue;

                result.Add(new ChildResponseDTO
                {
                    StudentId = student.StudentId,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    Grade = student.Grade,
                    School = student.School
                });
            }

            return result;
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private async Task<ParentResponseDTO> BuildParentResponseAsync(
            DataAccessLayer.Entities.Parent parent)
        {
            var user = await _userRepository.GetByIdAsync(parent.UserId);
            var children = await GetMyChildrenAsync(parent.UserId);

            return new ParentResponseDTO
            {
                ParentId = parent.ParentId,
                UserId = parent.UserId,
                Address = parent.Address,
                Name = user?.Name ?? string.Empty,
                Email = user?.Email ?? string.Empty,
                Phone = user?.Phone ?? string.Empty,
                Children = children
            };
        }
    }
}
