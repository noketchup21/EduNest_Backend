using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("MultipleChoiceQuestion")]
    public class MultipleChoiceQuestion
    {
        [Key]
        public int MultipleChoiceQuestionId { get; set; }

        public int HomeworkId { get; set; }

        [Required, MaxLength(1000)]
        public string QuestionText { get; set; }

        public double Point { get; set; }

        // Navigation properties
        [ForeignKey("HomeworkId")]
        public virtual Homework Homework { get; set; }

        public virtual ICollection<QuestionOption> QuestionOptions { get; set; } = new List<QuestionOption>();
        public virtual ICollection<MultipleChoiceQuestionAnswer> Answers { get; set; } = new List<MultipleChoiceQuestionAnswer>();
    }
}
