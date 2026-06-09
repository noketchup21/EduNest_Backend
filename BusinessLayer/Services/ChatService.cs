using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private const string RestrictedChatWarning =
            "For your safety, keep communication and payment inside EduNest.";

        private static readonly Regex EmailPattern = new(
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex LinkPattern = new(
            @"\b((https?:\/\/|www\.)\S+|[A-Z0-9-]+\.(com|vn|net|org|io|me|app|edu|info)\b\S*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex PhonePattern = new(
            @"(?<!\d)(?:\+?84|0)(?:[\s.\-()]?\d){8,10}(?!\d)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex LongNumberPattern = new(
            @"(?<!\d)(?:\d[\s.\-]*){8,20}(?!\d)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex RestrictedKeywordPattern = new(
            @"\b(zalo|facebook|fb|messenger|m\.me|telegram|whatsapp|gmail|email|e-mail|qr|vietqr|bank|banking|stk|so\s*tai\s*khoan|số\s*tài\s*khoản|tai\s*khoan\s*ngan\s*hang|tài\s*khoản\s*ngân\s*hàng|chuyen\s*khoan|chuyển\s*khoản|ngan\s*hang|ngân\s*hàng|momo|vietcombank|vcb|techcombank|tcb|mbbank|mb\s*bank|acb|bidv|vietinbank|vpbank|tpbank)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly EduNestDbContext _db;
        private readonly ICloudinaryService _cloudinaryService;

        public ChatService(EduNestDbContext db, ICloudinaryService cloudinaryService)
        {
            _db = db;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ConversationResponse> StartConversationAsync(
            int userId,
            StartConversationRequest request)
        {
            User? otherUser = null;

            if (request.OtherUserId.HasValue && request.OtherUserId.Value > 0)
            {
                otherUser = await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == request.OtherUserId.Value &&
                        !u.IsDeleted &&
                        u.IsActive);
            }
            else if (!string.IsNullOrWhiteSpace(request.OtherUserEmail))
            {
                var email = request.OtherUserEmail.Trim().ToLower();

                otherUser = await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.Email.ToLower() == email &&
                        !u.IsDeleted &&
                        u.IsActive);
            }

            if (otherUser == null)
                throw new KeyNotFoundException("User not found or inactive.");

            if (userId == otherUser.UserId)
                throw new InvalidOperationException("Cannot chat with yourself.");

            var existing = await _db.Conversations
                .Include(c => c.ConversationUsers)
                    .ThenInclude(cu => cu.User)
                .Where(c =>
                    c.ConversationUsers.Any(cu => cu.UserId == userId) &&
                    c.ConversationUsers.Any(cu => cu.UserId == otherUser.UserId))
                .FirstOrDefaultAsync();

            if (existing != null)
                return ToConversationResponse(existing, userId);

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
                UserId = otherUser.UserId
            });

            _db.Conversations.Add(conversation);
            await _db.SaveChangesAsync();

            var fullConversation = await _db.Conversations
                .Include(c => c.ConversationUsers)
                    .ThenInclude(cu => cu.User)
                .FirstAsync(c => c.ConversationId == conversation.ConversationId);

            return ToConversationResponse(fullConversation, userId);
        }

        public async Task<List<ConversationResponse>> GetMyConversationsAsync(int userId)
        {
            var conversations = await _db.Conversations
                .Include(c => c.ConversationUsers)
                    .ThenInclude(cu => cu.User)
                .Where(c =>
                    c.ConversationUsers.Any(cu => cu.UserId == userId) &&
                    c.IsActive)
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            return conversations
                .Select(c => ToConversationResponse(c, userId))
                .ToList();
        }

        public async Task<MessageResponse> SendMessageAsync(
            int userId,
            int conversationId,
            SendMessageRequest request)
        {
            var conversation = await _db.Conversations
                .Include(c => c.ConversationUsers)
                    .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(c =>
                    c.ConversationId == conversationId &&
                    c.IsActive)
                ?? throw new KeyNotFoundException("Conversation not found.");

            if (!conversation.ConversationUsers.Any(cu => cu.UserId == userId))
                throw new UnauthorizedAccessException("You are not in this conversation.");

            var content = request.Content.Trim();

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Message content is required.");

            if (await ShouldApplyRestrictedChatAsync(conversation) &&
                ContainsRestrictedContent(content))
            {
                throw new InvalidOperationException(RestrictedChatWarning);
            }

            var message = new Message
            {
                ConversationId = conversationId,
                UserId = userId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                IsDeleted = false
            };

            conversation.LastMessageAt = message.CreatedAt;

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            message.User = conversation.ConversationUsers
                .First(cu => cu.UserId == userId)
                .User;

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

            var messages = await _db.Messages
                .Include(m => m.User)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return messages.Select(ToMessageResponse).ToList();
        }

        private async Task<bool> ShouldApplyRestrictedChatAsync(Conversation conversation)
        {
            var participantIds = conversation.ConversationUsers
                .Select(cu => cu.UserId)
                .Distinct()
                .ToList();

            if (participantIds.Count != 2)
                return false;

            var tutor = await _db.Tutors
                .FirstOrDefaultAsync(t => participantIds.Contains(t.UserId));

            if (tutor == null)
                return false;

            var learnerUserId = participantIds.FirstOrDefault(id => id != tutor.UserId);

            if (learnerUserId <= 0)
                return false;

            var hasBookedTutor = await _db.Bookings
                .AnyAsync(b =>
                    b.UserId == learnerUserId &&
                    b.Status != "Cancelled" &&
                    b.Status != "Expired" &&
                    b.Status != "Rejected" &&
                    !b.IsDeleted &&
                    b.Availability.TutorId == tutor.TutorId);

            return !hasBookedTutor;
        }

        private static bool ContainsRestrictedContent(string content)
        {
            if (EmailPattern.IsMatch(content) ||
                LinkPattern.IsMatch(content) ||
                PhonePattern.IsMatch(content) ||
                RestrictedKeywordPattern.IsMatch(content))
            {
                return true;
            }

            if (!LongNumberPattern.IsMatch(content))
                return false;

            return RestrictedKeywordPattern.IsMatch(content) ||
                   ContainsBankNumberHint(content);
        }

        private static bool ContainsBankNumberHint(string content)
        {
            var normalized = content.ToLowerInvariant();

            return normalized.Contains("account") ||
                   normalized.Contains("bank") ||
                   normalized.Contains("stk") ||
                   normalized.Contains("tai khoan") ||
                   normalized.Contains("tài khoản") ||
                   normalized.Contains("ngan hang") ||
                   normalized.Contains("ngân hàng") ||
                   normalized.Contains("chuyen khoan") ||
                   normalized.Contains("chuyển khoản");
        }

        private async Task<Conversation?> GetConversationWithUsersAsync(int conversationId)
        {
            return await _db.Conversations
                .Include(c => c.ConversationUsers)
                    .ThenInclude(cu => cu.User)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }

        private ConversationResponse ToConversationResponse(
            Conversation conversation,
            int currentUserId)
        {
            var otherUser = conversation.ConversationUsers
                .Select(cu => cu.User)
                .FirstOrDefault(u => u.UserId != currentUserId);

            return new ConversationResponse
            {
                ConversationId = conversation.ConversationId,
                LastMessageAt = conversation.LastMessageAt,
                IsActive = conversation.IsActive,

                UserIds = conversation.ConversationUsers
                    .Select(cu => cu.UserId)
                    .ToList(),

                OtherUserId = otherUser?.UserId ?? 0,
                OtherUserName = otherUser?.Name ?? "User",
                OtherUserRole = otherUser?.Role ?? "",
                OtherUserAvatarUrl = AvatarUrl(otherUser)
            };
        }

        private MessageResponse ToMessageResponse(Message m) => new()
        {
            MessageId = m.MessageId,
            ConversationId = m.ConversationId,
            UserId = m.UserId,
            UserName = m.User?.Name ?? "User",
            UserRole = m.User?.Role ?? "",
            UserAvatarUrl = AvatarUrl(m.User),
            Content = m.Content,
            IsRead = m.IsRead,
            CreatedAt = m.CreatedAt
        };

        private string? AvatarUrl(User? user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.AvatarPublicId))
                return null;

            return _cloudinaryService.GenerateSignedImageUrl(
                user.AvatarPublicId,
                300,
                300);
        }
    }
}
