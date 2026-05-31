using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Settings
{
    public class EmailSetting
    {
        public const string SectionName = "EmailSetting";
        public string SenderName { get; set; }

        // Google Apps Script
        public string GoogleScriptUrl { get; set; }
        public string GoogleScriptSecretKey { get; set; }
    }
}
