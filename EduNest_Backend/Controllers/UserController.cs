using BusinessLayer.DTOs.User;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

    public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET /api/user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDTO>>> GetAllUserAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                if (users == null || !users.Any())
                    return NotFound(new { message = "No users found." });

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // GET /api/user/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDTO>> GetByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid user ID." });

                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found." });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // PUT /api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UserUpdateDto dto)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid user ID." });

                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                await _userService.UpdateUserAsync(id, dto);
                return NoContent(); // 204
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // DELETE /api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Invalid user ID." });

                await _userService.DeleteUserAsync(id);
                return NoContent(); // 204
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

    }
}
