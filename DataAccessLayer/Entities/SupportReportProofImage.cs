using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("SupportReportProofImages")]
    public class SupportReportProofImage
    {
        [Key]
        public int SupportReportProofImageId { get; set; }

        public int SupportReportId { get; set; }

        [Required, MaxLength(500)]
        public string PublicId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SupportReportId))]
        public SupportReport SupportReport { get; set; } = null!;
    }
}
