using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// A class taught by a tutor on a specific subject.
    /// Students are enrolled via the ClassStudent junction table.
    /// </summary>
    [Table("Class")]
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        public int TutorId { get; set; }

        public int SubjectId { get; set; }

        public int TotalStudent { get; set; }

        // Navigation properties
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }

        public virtual ICollection<ClassStudent> ClassStudents { get; set; } = new List<ClassStudent>();
        public virtual ICollection<Homework> Homeworks { get; set; } = new List<Homework>();
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
