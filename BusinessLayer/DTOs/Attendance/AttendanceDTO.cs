using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Attendance
{
    public sealed class MarkAttendanceRequest
    {
        [Required, MaxLength(20)]
        public string Status { get; set; } = "Present";

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
