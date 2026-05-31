using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("TutorSubjects")]
    public class TutorSubject
    {
        // Composite PK configured via Fluent API in DbContext
        public int SubjectId { get; set; }
        public int TutorId { get; set; }

        // Navigation properties
        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }
 
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }
    }
}
