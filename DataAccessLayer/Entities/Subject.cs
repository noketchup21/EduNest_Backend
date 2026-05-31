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
    [Table("Subjects")]
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<TutorSubject> TutorSubjects { get; set; } = new List<TutorSubject>();
        public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    }
}
