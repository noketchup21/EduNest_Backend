using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.IServices
{
    public interface IMeetingLinkService
    {
        Task<string> CreateMeetingLinkAsync(
    string title,
    DateTime startUtc,
    DateTime endUtc);
    }
}
