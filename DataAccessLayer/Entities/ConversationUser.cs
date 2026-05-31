using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("ConversationUsers")]
    public class ConversationUser
    {
        // Composite PK configured via Fluent API
        public int ConversationId { get; set; }
        public int UserId { get; set; }

        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
