using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Settings
{
    public class PayOSChiSetting
    {
        public const string SectionName = "PayOSChi";

        // Tài khoản chi - used for tutor payout
        public bool Enabled { get; set; } = true;

        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;

        public bool ValidateDestination { get; set; } = true;
        public bool FallbackToQrWhenFailed { get; set; } = true;

        public string DefaultDescriptionPrefix { get; set; } = "EDUNEST PAYOUT";
    }
}
