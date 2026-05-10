using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.User;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Mapster;

namespace BusinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.FindAsync(u => !u.IsDeleted);
            return users.Adapt<IEnumerable<UserResponseDTO>>();
        }

        public async Task<UserResponseDTO?> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.FindOneAsync(u => u.UserId == id && !u.IsDeleted);
            if (user == null) return null;
            return user.Adapt<UserResponseDTO>();
        }

        public async Task<UserResponseDTO> CreateUserAsync(RegisterUserDTO userDto)
        {
            var user = userDto.Adapt<User>();

            // Set default properties
            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;
            user.IsDeleted = false;
            user.Password = BCrypt.Net.BCrypt.HashPassword(userDto.Password);

            var createdUser = await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            return createdUser.Adapt<UserResponseDTO>();
        }

        public async Task UpdateUserAsync(int id, UserUpdateDto userDto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException($"User with ID {id} not found.");

            // Mapster — map non-null properties onto existing entity
            userDto.Adapt(user);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null || user.IsDeleted)
                throw new KeyNotFoundException("User not found.");

            user.IsDeleted = true;
            user.IsActive = false;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }
    }
}
