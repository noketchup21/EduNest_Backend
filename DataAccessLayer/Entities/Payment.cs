using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int BookingId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending / Success / Failed / Refunded

        [Required, MaxLength(50)]
        public string Provider { get; set; } = "PayOS"; // PayOS / VietQR

        public long ProviderOrderCode { get; set; }

        [MaxLength(100)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? CheckoutUrl { get; set; }

        [MaxLength(4000)]
        public string? QrCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }
    }
}
