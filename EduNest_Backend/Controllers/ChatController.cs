using System.Security.Claims;
using BusinessLayer.DTOs.Conversation;
using BusinessLayer.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduNest_Backend.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public sealed class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("conversation")]
        public async Task<ActionResult<ConversationResponse>> StartConversation(
            StartConversationRequest request)
        {
            try
            {
                return Ok(await _chatService.StartConversationAsync(
                    CurrentUserId(),
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("conversation")]
        public async Task<ActionResult<List<ConversationResponse>>> GetConversations()
        {
            return Ok(await _chatService.GetMyConversationsAsync(CurrentUserId()));
        }

        [HttpPost("conversation/{conversationId:int}/message")]
        public async Task<ActionResult<MessageResponse>> SendMessage(
            int conversationId,
            SendMessageRequest request)
        {
            try
            {
                return Ok(await _chatService.SendMessageAsync(
                    CurrentUserId(),
                    conversationId,
                    request));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }

        [HttpGet("conversation/{conversationId:int}/message")]
        public async Task<ActionResult<List<MessageResponse>>> GetMessages(
            int conversationId)
        {
            try
            {
                return Ok(await _chatService.GetMessagesAsync(
                    CurrentUserId(),
                    conversationId));
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

            return int.Parse(raw!);
        }
    }
}
