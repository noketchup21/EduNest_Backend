using System.Security.Claims;
using BusinessLayer.DTOs.User;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDTO>> RegisterAsync(
            [FromBody] RegisterUserDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var result = await _authService.RegisterAsync(dto);

                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred during registration.",
                    error = ex.Message
                });
            }
        }

        // POST /api/auth/verify-email
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                await _authService.VerifyEmailAsync(dto);
                return Ok(new { message = "Email verified successfully. You can now login." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // POST /api/auth/resend-code
        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCodeAsync([FromBody] ResendVerificationDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest(new { message = "Email is required." });

                var message = await _authService.ResendVerificationCodeAsync(dto);
                return Ok(new { message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDTO>> LoginAsync(
            [FromBody] LoginUserDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var result = await _authService.LoginAsync(dto);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred during login.",
                    error = ex.Message
                });
            }
        }

        // POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponseDTO>> RefreshTokenAsync(
            [FromBody] RefreshTokenRequestDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid request body." });

                var result = await _authService.RefreshTokenAsync(dto);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "An error occurred while refreshing token.",
                    error = ex.Message
                });
            }
        }

        // POST: api/authlogout
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> LogoutAsync()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Invalid token." });

                await _authService.LogoutAsync(int.Parse(userIdClaim));

                return Ok(new { message = "Logged out successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }
    }
}
