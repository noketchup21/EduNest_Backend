using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    public class AppMetric
    {
        [Key]
        public int AppMetricId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = string.Empty;
        // Download, Install

        [MaxLength(100)]
        public string? DeviceId { get; set; }

        [MaxLength(50)]
        public string? Platform { get; set; }

        [MaxLength(50)]
        public string? AppVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
