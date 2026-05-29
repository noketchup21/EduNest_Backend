using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Tier")]
    public class Tier
    {
        [Key]
        public int TierId { get; set; }

        public int Rate { get; set; }             // commission rate %
        public int CurrentStreak { get; set; }    // streak count

        // Navigation properties
        public virtual ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
    }
}
