using System.Security.Claims;
using BusinessLayer.DTOs.Material;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/material")]
    public sealed class MaterialController : ControllerBase
    {
        private readonly IMaterialService _materialService;

        public MaterialController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        [HttpGet("availability/{availabilityId:int}")]
        public async Task<ActionResult<List<MaterialSectionResponse>>> GetByAvailability(int availabilityId)
        {
            try
            {
                return Ok(await _materialService.GetByAvailabilityAsync(CurrentUserId(), availabilityId));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPost("availability/{availabilityId:int}/sections")]
        public async Task<ActionResult<MaterialSectionResponse>> CreateSection(
            int availabilityId,
            [FromBody] UpsertMaterialSectionRequest request)
        {
            try
            {
                return Ok(await _materialService.CreateSectionAsync(CurrentUserId(), availabilityId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPut("sections/{sectionId:int}")]
        public async Task<ActionResult<MaterialSectionResponse>> UpdateSection(
            int sectionId,
            [FromBody] UpsertMaterialSectionRequest request)
        {
            try
            {
                return Ok(await _materialService.UpdateSectionAsync(CurrentUserId(), sectionId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpDelete("sections/{sectionId:int}")]
        public async Task<IActionResult> DeleteSection(int sectionId)
        {
            try
            {
                await _materialService.DeleteSectionAsync(CurrentUserId(), sectionId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPost("sections/{sectionId:int}/items")]
        public async Task<ActionResult<MaterialResponse>> CreateItem(
            int sectionId,
            [FromForm] UpsertMaterialItemRequest request)
        {
            try
            {
                return Ok(await _materialService.CreateItemAsync(CurrentUserId(), sectionId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPost("availability/{availabilityId:int}")]
        public async Task<ActionResult<MaterialResponse>> CreateItemForAvailability(
            int availabilityId,
            [FromForm] UpsertMaterialItemRequest request)
        {
            try
            {
                return Ok(await _materialService.CreateItemForAvailabilityAsync(CurrentUserId(), availabilityId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPut("items/{materialId:int}")]
        public async Task<ActionResult<MaterialResponse>> UpdateItem(
            int materialId,
            [FromForm] UpsertMaterialItemRequest request)
        {
            try
            {
                return Ok(await _materialService.UpdateItemAsync(CurrentUserId(), materialId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpPut("{materialId:int}")]
        public async Task<ActionResult<MaterialResponse>> UpdateLegacyItem(
            int materialId,
            [FromForm] UpsertMaterialItemRequest request)
        {
            try
            {
                return Ok(await _materialService.UpdateItemAsync(CurrentUserId(), materialId, request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpDelete("items/{materialId:int}")]
        public async Task<IActionResult> DeleteItem(int materialId)
        {
            try
            {
                await _materialService.DeleteItemAsync(CurrentUserId(), materialId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [Authorize(Roles = "Tutor")]
        [HttpDelete("{materialId:int}")]
        public async Task<IActionResult> DeleteLegacyItem(int materialId)
        {
            try
            {
                await _materialService.DeleteItemAsync(CurrentUserId(), materialId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        private ActionResult HandleException(Exception ex)
        {
            return ex switch
            {
                KeyNotFoundException => NotFound(new { message = ex.Message }),
                UnauthorizedAccessException => Forbid(),
                InvalidOperationException => BadRequest(new { message = ex.Message }),
                ArgumentException => BadRequest(new { message = ex.Message }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Server error." })
            };
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (!int.TryParse(raw, out var userId))
                throw new UnauthorizedAccessException("Invalid token.");

            return userId;
        }
    }
}
