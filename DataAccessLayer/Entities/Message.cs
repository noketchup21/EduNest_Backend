using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Message")]
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        public int ConversationId { get; set; }
        public int UserId { get; set; }                // who sent it

        [Required, MaxLength(2000)]
        public string Content { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
