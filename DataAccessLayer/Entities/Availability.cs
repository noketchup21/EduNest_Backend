using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Availabilities")]
    public class Availability
    {
        [Key]
        public int AvailabilityId { get; set; }

        public int TutorId { get; set; }

        public int? SubjectId { get; set; }

        [Required, MaxLength(20)]
        public string DayOfWeek { get; set; }

        [Required, MaxLength(20)]
        public string Mode { get; set; }
        // Online / Offline

        [MaxLength(500)]
        public string? OfflineAreas { get; set; }

        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public DateTime StartCourseTime { get; set; }
        public DateTime EndCourseTime { get; set; }

        [Required, MaxLength(50)]
        public string Level { get; set; }

        public int Slot { get; set; }                // base slots per month

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerSlot { get; set; }

        [Required, MaxLength(20)]
        public string Status { get; set; } // Active / Inactive / Full

        // Navigation properties
        [ForeignKey("TutorId")]
        public virtual Tutor Tutor { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
