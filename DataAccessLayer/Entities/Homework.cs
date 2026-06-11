using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Homeworks")]
    public class Homework
    {
        [Key]
        public int HomeworkId { get; set; }

        public int BookingId { get; set; }             // ← links to Booking
        public int? LessonId { get; set; }

        [Required, MaxLength(30)]
        public string Type { get; set; } = "MultipleChoice";

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(500)]
        public string Url { get; set; }

        public DateTime DueDate { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        [ForeignKey("LessonId")]
        public virtual Lesson? Lesson { get; set; }

        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public virtual ICollection<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; } = new List<MultipleChoiceQuestion>();
        public virtual ICollection<Essay> Essays { get; set; } = new List<Essay>();
    }
}
