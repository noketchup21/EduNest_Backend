using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Material")]
    public class Material
    {
        [Key]
        public int MaterialId { get; set; }

        public int AvailabilityId { get; set; }        // ← links to Availability

        [Required, MaxLength(255)]
        public string Title { get; set; }

        [MaxLength(2000)]
        public string Description { get; set; }

        [MaxLength(500)]
        public string FileUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("AvailabilityId")]
        public virtual Availability Availability { get; set; }
    }
}
