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
            return Ok(await _chatService.StartConversationAsync(
                CurrentUserId(),
                request));
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
            return Ok(await _chatService.SendMessageAsync(
                CurrentUserId(),
                conversationId,
                request));
        }

        [HttpGet("conversation/{conversationId:int}/message")]
        public async Task<ActionResult<List<MessageResponse>>> GetMessages(
            int conversationId)
        {
            return Ok(await _chatService.GetMessagesAsync(
                CurrentUserId(),
                conversationId));
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            return int.Parse(raw!);
        }
    }
}
