using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Report
{
    public sealed class UpdateTutorReportStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? AdminNote { get; set; }
    }
}
