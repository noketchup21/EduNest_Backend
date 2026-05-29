using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("EssayAnswer")]
    public class EssayAnswer
    {
        [Key]
        public int EssayAnswerId { get; set; }

        public int SubmissionId { get; set; }
        public int EssayId { get; set; }

        [Required, MaxLength(5000)]
        public string AnswerText { get; set; }

        public double Score { get; set; }

        [MaxLength(2000)]
        public string Feedback { get; set; }

        // Navigation properties
        [ForeignKey("SubmissionId")]
        public virtual Submission Submission { get; set; }

        [ForeignKey("EssayId")]
        public virtual Essay Essay { get; set; }
    }
}
