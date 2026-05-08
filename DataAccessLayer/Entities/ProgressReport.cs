using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("ProgressReport")]
    public class ProgressReport
    {
        [Key]
        public int ReportId { get; set; }

        public int LessonId { get; set; }

        public int TutorId { get; set; }

        public int StudentId { get; set; }

        [MaxLength(2000)]
        public string Comments { get; set; }

        public TimeSpan CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("LessonId")]
        public virtual Lesson Lesson { get; set; }

        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }
    }

}
