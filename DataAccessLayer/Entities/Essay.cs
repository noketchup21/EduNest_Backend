using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Essay")]
    public class Essay
    {
        [Key]
        public int EssayId { get; set; }

        public int HomeworkId { get; set; }

        [Required, MaxLength(1000)]
        public string QuestionText { get; set; }

        public double Points { get; set; }

        // Navigation properties
        [ForeignKey("HomeworkId")]
        public virtual Homework Homework { get; set; }

        public virtual ICollection<EssayAnswer> EssayAnswers { get; set; } = new List<EssayAnswer>();
    }
}
