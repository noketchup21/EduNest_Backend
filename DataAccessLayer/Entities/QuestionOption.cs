using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("QuestionOption")]
    public class QuestionOption
    {
        [Key]
        public int QuestionOptionId { get; set; }

        public int MultipleChoiceQuestionId { get; set; }

        public bool IsCorrect { get; set; } = false;

        [Required, MaxLength(500)]
        public string Content { get; set; }

        // Navigation properties
        [ForeignKey("MultipleChoiceQuestionId")]
        public virtual MultipleChoiceQuestion MultipleChoiceQuestion { get; set; }

        public virtual ICollection<MultipleChoiceQuestionAnswer> Answers { get; set; } = new List<MultipleChoiceQuestionAnswer>();
    }
}
