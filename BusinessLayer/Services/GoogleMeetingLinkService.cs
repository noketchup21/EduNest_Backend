using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.IServices;

namespace BusinessLayer.Services
{
    public sealed class GoogleMeetLinkService : IMeetingLinkService
    {
        public Task<string> CreateMeetingLinkAsync(
            string title,
            DateTime startUtc,
            DateTime endUtc)
        {
            // TEMP MVP:
            // Replace this with real Google Calendar API / Google Meet API implementation.
            // Do not fake this for production.
            throw new InvalidOperationException(
                "Google Meet API is not configured yet. Add Google Calendar API credentials or allow tutor to paste a meeting link.");
        }
    }
}
