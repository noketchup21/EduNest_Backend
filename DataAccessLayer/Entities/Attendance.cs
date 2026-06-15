using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Attendances")]
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }          // proper PK

        public int LessonId { get; set; }
        public int? StudentId { get; set; }
        public int? UserId { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; }             // Present / Absent / Late

        public DateTime? AttendedAt { get; set; }      // nullable — null if absent

        [MaxLength(500)]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("LessonId")]
        public virtual Lesson Lesson { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}

