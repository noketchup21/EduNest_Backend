using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLayer.IServices;
using BusinessLayer.Settings;
using BusinessLayer.Templates;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSetting _emailSettings;
        private readonly HttpClient _httpClient;

        public EmailService(IOptions<EmailSetting> emailSettings)
        {
            _emailSettings = emailSettings.Value;
            _httpClient = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = true
            });
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_emailSettings.GoogleScriptUrl))
                throw new InvalidOperationException("Google Script URL is not configured.");

            if (string.IsNullOrWhiteSpace(_emailSettings.GoogleScriptSecretKey))
                throw new InvalidOperationException("Google Script secret key is not configured.");

            var payload = new
            {
                secretKey = _emailSettings.GoogleScriptSecretKey,
                to = toEmail,
                subject = subject,
                body = body,
                senderName = _emailSettings.SenderName
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_emailSettings.GoogleScriptUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse response
            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
            if (!result.GetProperty("success").GetBoolean())
            {
                var error = result.TryGetProperty("error", out var e)
                    ? e.GetString()
                    : "Unknown error";
                throw new Exception($"Email sending failed: {error}");
            }
        }

        public async Task SendVerificationCodeAsync(string toEmail, string name, string code)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentNullException(nameof(toEmail), "Email address cannot be null.");

            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code), "Verification code cannot be null.");

            // Load template and replace placeholders
            var body = EmailTemplateReader.GetTemplate("VerificationEmailTemplate.html")
                .Replace("{{NAME}}", name ?? "User")
                .Replace("{{CODE}}", code);

            await SendEmailAsync(toEmail, "EduNest - Email Verification Code", body);
        }
    }
}
