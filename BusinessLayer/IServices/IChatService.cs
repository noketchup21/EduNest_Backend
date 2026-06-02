using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Conversation;

namespace BusinessLayer.IServices
{
    public interface IChatService
    {
        Task<ConversationResponse> StartConversationAsync(int userId, StartConversationRequest request);
        Task<List<ConversationResponse>> GetMyConversationsAsync(int userId);
        Task<MessageResponse> SendMessageAsync(int userId, int conversationId, SendMessageRequest request);
        Task<List<MessageResponse>> GetMessagesAsync(int userId, int conversationId);
    }
}
