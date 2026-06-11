using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Entities
{
    [Table("MaterialSections")]
    public class MaterialSection
    {
        [Key]
        public int MaterialSectionId { get; set; }

        public int AvailabilityId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("AvailabilityId")]
        public virtual Availability Availability { get; set; } = null!;

        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
