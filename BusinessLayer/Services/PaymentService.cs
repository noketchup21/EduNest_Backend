using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.IServices;
using BusinessLayer.Settings;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BusinessLayer.Services
{
    public sealed class PaymentService : IPaymentService
    {
        private readonly EduNestDbContext _db;
        private readonly PayOSSetting _payOs;
        private readonly IHttpClientFactory _httpClientFactory;

        public PaymentService(
            EduNestDbContext db,
            IOptions<PayOSSetting> payOs,
            IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _payOs = payOs.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<CreatePaymentResponse> CreatePayOsPaymentAsync(int userId, int bookingId)
        {
            var booking = await _db.Bookings.Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.UserId == userId && !b.IsDeleted)
                ?? throw new KeyNotFoundException("Booking not found.");

            if (booking.Status is "Confirmed" or "Completed")
                throw new InvalidOperationException("Booking already paid.");

            var existing = booking.Payments
                .Where(p => p.Status == "Pending")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (existing != null)
                return ToPaymentResponse(existing);

            var orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var description = $"EDU{booking.BookingId}";

            var payment = new Payment
            {
                BookingId = booking.BookingId,
                TotalPrice = booking.PriceAtBooking,
                Status = "Pending",
                Provider = "PayOS",
                ProviderOrderCode = orderCode,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            try
            {
                var checkout = await CreatePayOsCheckoutUrlAsync(payment, description);
                payment.CheckoutUrl = checkout.checkoutUrl;
                payment.QrCode = checkout.qrCode;
            }
            catch
            {
                payment.Provider = "VietQR";
                payment.QrCode = BuildVietQrQuickLink(payment.TotalPrice, description);
                payment.CheckoutUrl = payment.QrCode;
            }

            await _db.SaveChangesAsync();

            return ToPaymentResponse(payment);
        }

        public async Task HandlePayOsWebhookAsync(PayOsWebhookRequest request)
        {
            if (request.Data == null)
                return;

            if (!VerifyPayOsWebhookSignature(request))
                return;

            var payment = await _db.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Availability)
                .FirstOrDefaultAsync(p => p.ProviderOrderCode == request.Data.OrderCode)
                ?? throw new KeyNotFoundException("Payment not found.");

            if (payment.Status == "Success")
                return;

            if (!request.Success || request.Code != "00")
            {
                payment.Status = "Failed";
                await _db.SaveChangesAsync();
                return;
            }

            payment.Status = "Success";
            payment.PaidAt = DateTime.UtcNow;
            payment.Booking.Status = "Confirmed";

            await EnsureLessonsForBookingAsync(payment.Booking);
            await _db.SaveChangesAsync();
        }

        private async Task<(string? checkoutUrl, string? qrCode)> CreatePayOsCheckoutUrlAsync(
            Payment payment,
            string description)
        {
            if (string.IsNullOrWhiteSpace(_payOs.ClientId) ||
                string.IsNullOrWhiteSpace(_payOs.ApiKey) ||
                string.IsNullOrWhiteSpace(_payOs.ChecksumKey))
            {
                throw new InvalidOperationException("PayOS is not configured.");
            }

            var amount = (int)Math.Round(payment.TotalPrice, 0, MidpointRounding.AwayFromZero);

            var signatureData =
                $"amount={amount}&cancelUrl={_payOs.CancelUrl}&description={description}&orderCode={payment.ProviderOrderCode}&returnUrl={_payOs.ReturnUrl}";

            var signature = HmacSha256(signatureData, _payOs.ChecksumKey);

            var body = new
            {
                orderCode = payment.ProviderOrderCode,
                amount,
                description,
                cancelUrl = _payOs.CancelUrl,
                returnUrl = _payOs.ReturnUrl,
                signature
            };

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://api-merchant.payos.vn");
            client.DefaultRequestHeaders.Add("x-client-id", _payOs.ClientId);
            client.DefaultRequestHeaders.Add("x-api-key", _payOs.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(
                "/v2/payment-requests",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

            response.EnsureSuccessStatusCode();

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var data = json.RootElement.GetProperty("data");

            return (
                data.TryGetProperty("checkoutUrl", out var c) ? c.GetString() : null,
                data.TryGetProperty("qrCode", out var q) ? q.GetString() : null
            );
        }

        private bool VerifyPayOsWebhookSignature(PayOsWebhookRequest request)
        {
            if (string.IsNullOrWhiteSpace(_payOs.ChecksumKey) || request.Data == null)
                return false;

            var d = request.Data;

            var pairs = new SortedDictionary<string, string?>
            {
                ["accountNumber"] = d.AccountNumber,
                ["amount"] = d.Amount.ToString(CultureInfo.InvariantCulture),
                ["code"] = d.Code,
                ["currency"] = d.Currency,
                ["desc"] = d.Desc,
                ["description"] = d.Description,
                ["orderCode"] = d.OrderCode.ToString(CultureInfo.InvariantCulture),
                ["paymentLinkId"] = d.PaymentLinkId,
                ["reference"] = d.Reference,
                ["transactionDateTime"] = d.TransactionDateTime
            };

            var data = string.Join('&',
                pairs.Where(p => !string.IsNullOrEmpty(p.Value))
                     .Select(p => $"{p.Key}={p.Value}"));

            var expected = HmacSha256(data, _payOs.ChecksumKey);

            return string.Equals(expected, request.Signature, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
        }

        private string BuildVietQrQuickLink(decimal amount, string description)
        {
            if (string.IsNullOrWhiteSpace(_payOs.BankBin) ||
                string.IsNullOrWhiteSpace(_payOs.BankAccountNo))
            {
                return null!;
            }

            var intAmount = ((int)Math.Round(amount, 0, MidpointRounding.AwayFromZero))
                .ToString(CultureInfo.InvariantCulture);

            var accountName = Uri.EscapeDataString(_payOs.BankAccountName ?? string.Empty);

            return $"https://img.vietqr.io/image/{_payOs.BankBin}-{_payOs.BankAccountNo}-compact2.png?amount={intAmount}&addInfo={Uri.EscapeDataString(description)}&accountName={accountName}";
        }

        private async Task EnsureLessonsForBookingAsync(Booking booking)
        {
            if (await _db.Lessons.AnyAsync(l => l.BookingId == booking.BookingId))
                return;

            var availability = booking.Availability
                ?? await _db.Availabilities.FirstAsync(a => a.AvailabilityId == booking.AvailabilityId);

            var day = ParseDayOfWeek(availability.DayOfWeek);
            var startDate = availability.StartCourseTime.Date;
            var endDate = availability.EndCourseTime.Date;
            var time = availability.StartTime;
            var duration = Math.Max(30, (int)(availability.EndTime - availability.StartTime).TotalMinutes);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != day)
                    continue;

                _db.Lessons.Add(new Lesson
                {
                    BookingId = booking.BookingId,
                    ScheduleTime = DateTime.SpecifyKind(date.Add(time), DateTimeKind.Utc),
                    Duration = duration,
                    Status = "Scheduled",
                    MeetingLink = string.Empty
                });
            }
        }

        private static DayOfWeek ParseDayOfWeek(string value)
            => Enum.TryParse<DayOfWeek>(value, true, out var d) ? d : DayOfWeek.Monday;

        private static CreatePaymentResponse ToPaymentResponse(Payment p) => new()
        {
            PaymentId = p.PaymentId,
            BookingId = p.BookingId,
            Amount = p.TotalPrice,
            Status = p.Status,
            Provider = p.Provider,
            OrderCode = p.ProviderOrderCode,
            Description = p.Description,
            CheckoutUrl = p.CheckoutUrl,
            QrCode = p.QrCode
        };
    }
}
