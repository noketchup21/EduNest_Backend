using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("MultipleChoiceQuestionAnswers")]
    public class MultipleChoiceQuestionAnswer
    {
        [Key]
        public int MultipleChoiceQuestionAnswerId { get; set; }

        public int QuestionOptionId { get; set; }      // ← which option selected
        public int SubmissionId { get; set; }

        [Required, MaxLength(500)]
        public string SelectedOption { get; set; }

        // Navigation properties
        [ForeignKey("QuestionOptionId")]
        public virtual QuestionOption QuestionOption { get; set; }

        [ForeignKey("SubmissionId")]
        public virtual Submission Submission { get; set; }
    }
}
