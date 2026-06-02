using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Settings
{
    public class PayOSSetting
    {
        public const string SectionName = "PayOS";
        public string ClientId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ChecksumKey { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = "https://localhost/payment/success";
        public string CancelUrl { get; set; } = "https://localhost/payment/cancel";
        public string BankBin { get; set; } = string.Empty;
        public string BankAccountNo { get; set; } = string.Empty;
        public string BankAccountName { get; set; } = string.Empty;
    }
}
