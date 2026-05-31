using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Conversations")]
    public class Conversation
    {
        [Key]
        public int ConversationId { get; set; }

        public int UserId { get; set; }                // ← creator of conversation

        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<ConversationUser> ConversationUsers { get; set; } = new List<ConversationUser>();
    }
}
