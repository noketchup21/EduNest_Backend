using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    /// <summary>
    /// Junction table — many students can belong to many classes.
    /// </summary>
    [Table("ClassStudent")]
    public class ClassStudent
    {
        // Composite PK configured via Fluent API in DbContext
        public int StudentId { get; set; }
        public int ClassId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }
    }
}
