using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Homework")]
    public class Homework
    {
        [Key]
        public int HomeworkId { get; set; }

        public int ClassId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string HomeworkType { get; set; }

        public DateTime? DueDate { get; set; }

        // Navigation properties
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public virtual ICollection<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; } = new List<MultipleChoiceQuestion>();
        public virtual ICollection<Essay> Essays { get; set; } = new List<Essay>();
    }
}
