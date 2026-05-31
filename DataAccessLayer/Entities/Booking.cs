using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Bookings")]
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        public int AvailabilityId { get; set; }
        public int ParentId { get; set; }
        public int StudentId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceAtBooking { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending";
        // Pending / Confirmed / Cancelled / Completed / Rejected

        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey("AvailabilityId")]
        public virtual Availability Availability { get; set; }

        [ForeignKey("ParentId")]
        public virtual Parent Parent { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student Student { get; set; }

        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Homework> Homeworks { get; set; } = new List<Homework>();
    }
}
