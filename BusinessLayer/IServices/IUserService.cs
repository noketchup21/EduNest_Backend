using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.User;
using DataAccessLayer.Entities;

namespace BusinessLayer.IServices
{
    public interface IUserService
    {
        Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync();
        Task<UserResponseDTO?> GetUserByIdAsync(int id);
        Task<UserResponseDTO> CreateUserAsync(RegisterUserDTO dto);
        Task UpdateUserAsync(int id, UserUpdateDto userUpdateDto);
        Task DeleteUserAsync(int id);
    }
}
