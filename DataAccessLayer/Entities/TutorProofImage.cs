using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Entities
{
    [Table("TutorReportProofImages")]
    public class TutorReportProofImage
    {
        [Key]
        public int TutorReportProofImageId { get; set; }

        public int TutorReportId { get; set; }

        [Required, MaxLength(500)]
        public string PublicId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(TutorReportId))]
        public TutorReport TutorReport { get; set; } = null!;
    }
}
