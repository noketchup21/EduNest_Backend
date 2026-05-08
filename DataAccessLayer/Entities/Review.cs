using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Review")]
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }

        public int TutorId { get; set; }

        public int BookingId { get; set; }

        public int? ParentId { get; set; }

        [Column(TypeName = "real")]
        public float Rating { get; set; }

        [MaxLength(2000)]
        public string Comment { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }
    }
}
