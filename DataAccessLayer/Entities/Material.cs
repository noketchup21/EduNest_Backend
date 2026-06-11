using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Materials")]
    public class Material
    {
        [Key]
        public int MaterialId { get; set; }

        public int AvailabilityId { get; set; }        // ← links to Availability

        public int? MaterialSectionId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        [MaxLength(255)]
        public string? FileName { get; set; }

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long? FileSize { get; set; }

        [Required, MaxLength(30)]
        public string MaterialType { get; set; } = "File";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("AvailabilityId")]
        public virtual Availability Availability { get; set; } = null!;

        [ForeignKey("MaterialSectionId")]
        public virtual MaterialSection? Section { get; set; }
    }
}
