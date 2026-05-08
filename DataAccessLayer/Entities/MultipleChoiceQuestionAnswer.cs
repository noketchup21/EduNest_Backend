using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("MultipleChoiceQuestionAnswer")]
    public class MultipleChoiceQuestionAnswer
    {
        [Key]
        public int MultipleChoiceQuestionsAnswerId { get; set; }

        public int MultipleChoiceQuestionId { get; set; }

        public int SubmissionId { get; set; }

        [Required, MaxLength(1)]
        public string SelectedOption { get; set; }

        public bool IsCorrect { get; set; }

        // Navigation properties
        [ForeignKey("MultipleChoiceQuestionId")]
        public virtual MultipleChoiceQuestion MultipleChoiceQuestion { get; set; }

        [ForeignKey("SubmissionId")]
        public virtual Submission Submission { get; set; }
    }
}
