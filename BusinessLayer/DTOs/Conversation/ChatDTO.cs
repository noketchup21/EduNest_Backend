using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Conversation
{
    public sealed class StartConversationRequest
    {
        [Required]
        public int OtherUserId { get; set; }
    }

    public sealed class SendMessageRequest
    {
        [Required, MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    public sealed class ConversationResponse
    {
        public int ConversationId { get; set; }
        public DateTime LastMessageAt { get; set; }
        public bool IsActive { get; set; }
        public List<int> UserIds { get; set; } = new();
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserRole { get; set; } = string.Empty;
        public string? OtherUserAvatarUrl { get; set; }
    }

    public sealed class MessageResponse
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
