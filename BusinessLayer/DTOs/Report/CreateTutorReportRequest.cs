using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BusinessLayer.DTOs.Report
{
    public sealed class CreateTutorReportRequest
    {
        [Required]
        public int BookingId { get; set; }

        public int? LessonId { get; set; }

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public List<IFormFile> ProofImages { get; set; } = new();
    }
}
