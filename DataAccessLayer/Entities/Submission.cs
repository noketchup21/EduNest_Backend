using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Submissions")]
    public class Submission
    {
        [Key]
        public int SubmissionId { get; set; }

        public int HomeworkId { get; set; }
        public int? StudentId { get; set; }
        public int? UserId { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public double TotalScore { get; set; }
        public DateTime? GradedAt { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }

        // Navigation properties
        [ForeignKey("HomeworkId")]
        public virtual Homework Homework { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public virtual ICollection<MultipleChoiceQuestionAnswer> MultipleChoiceQuestionAnswers { get; set; } = new List<MultipleChoiceQuestionAnswer>();
        public virtual ICollection<EssayAnswer> EssayAnswers { get; set; } = new List<EssayAnswer>();
    }
}
