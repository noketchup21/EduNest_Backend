using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Student")]
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        public int? ParentId { get; set; }

        public int UserId { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Grade { get; set; }

        [MaxLength(255)]
        public string School { get; set; }

        // Navigation properties
        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();
        public virtual ICollection<Submission> Submissions { get; set; } = new List<Submission>();
        public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
