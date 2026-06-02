using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Conversation;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class ChatService : IChatService
    {
        private readonly EduNestDbContext _db;

        public ChatService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<ConversationResponse> StartConversationAsync(
            int userId,
            StartConversationRequest request)
        {
            if (userId == request.OtherUserId)
                throw new InvalidOperationException("Cannot chat with yourself.");

            var otherExists = await _db.Users
                .AnyAsync(u => u.UserId == request.OtherUserId && !u.IsDeleted);

            if (!otherExists)
                throw new KeyNotFoundException("Other user not found.");

            var existing = await _db.Conversations
                .Include(c => c.ConversationUsers)
                .Where(c =>
                    c.ConversationUsers.Any(cu => cu.UserId == userId) &&
                    c.ConversationUsers.Any(cu => cu.UserId == request.OtherUserId))
                .FirstOrDefaultAsync();

            if (existing != null)
                return ToConversationResponse(existing);

            var conversation = new Conversation
            {
                UserId = userId,
                IsActive = true,
                LastMessageAt = DateTime.UtcNow
            };

            conversation.ConversationUsers.Add(new ConversationUser
            {
                UserId = userId
            });

            conversation.ConversationUsers.Add(new ConversationUser
            {
                UserId = request.OtherUserId
            });

            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            return ToConversationResponse(conversation);
        }

        public async Task<List<ConversationResponse>> GetMyConversationsAsync(int userId)
        {
            var conversations = await _db.Conversations
                .Include(c => c.ConversationUsers)
                .Where(c =>
                    c.ConversationUsers.Any(cu => cu.UserId == userId) &&
                    c.IsActive)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return conversations.Select(ToConversationResponse).ToList();
        }

        public async Task<MessageResponse> SendMessageAsync(
            int userId,
            int conversationId,
            SendMessageRequest request)
        {
            var conversation = await _db.Conversations
                .Include(c => c.ConversationUsers)
                .FirstOrDefaultAsync(c =>
                    c.ConversationId == conversationId &&
                    c.IsActive)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (!conversation.ConversationUsers.Any(cu => cu.UserId == userId))
                throw new UnauthorizedAccessException("You are not in this conversation.");

            var message = new Message
            {
                ConversationId = conversationId,
                UserId = userId,
                Content = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsDeleted = false
            };

            conversation.LastMessageAt = message.CreatedAt;

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return ToMessageResponse(message);
        }

        public async Task<List<MessageResponse>> GetMessagesAsync(
            int userId,
            int conversationId)
        {
            var allowed = await _db.ConversationUsers
                .AnyAsync(cu =>
                    cu.ConversationId == conversationId &&
                    cu.UserId == userId);

            if (!allowed)
                throw new UnauthorizedAccessException("You are not in this conversation.");

            return await _db.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => ToMessageResponse(m))
                .ToListAsync();
        }

        private static ConversationResponse ToConversationResponse(Conversation c) => new()
        {
            ConversationId = c.ConversationId,
            LastMessageAt = c.LastMessageAt,
            IsActive = c.IsActive,
            UserIds = c.ConversationUsers.Select(cu => cu.UserId).ToList()
        };

        private static MessageResponse ToMessageResponse(Message m) => new()
        {
            MessageId = m.MessageId,
            ConversationId = m.ConversationId,
            UserId = m.UserId,
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        };
    }
}
