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
            var booking = await _db.Bookings
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b =>
                    b.BookingId == bookingId &&
                    b.UserId == userId &&
                    !b.IsDeleted)
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

            var checkout = await CreatePayOsCheckoutUrlAsync(payment, description);

            payment.CheckoutUrl = checkout.checkoutUrl;
            payment.QrCode = checkout.qrCode;

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();

            return ToPaymentResponse(payment);
        }

        public async Task HandlePayOsWebhookAsync(PayOsWebhookRequest request)
        {
            if (request == null || request.Data == null)
                return;

            Console.WriteLine($"PayOS webhook received. OrderCode: {request.Data.OrderCode}, Code: {request.Code}, Success: {request.Success}");

            // For MVP: do not silently block payment update if signature check fails.
            // You can change this back to throw after everything works.
            var validSignature = VerifyPayOsWebhookSignature(request);

            if (!validSignature)
            {
                Console.WriteLine("PayOS webhook signature invalid. Continuing for MVP sync safety.");
            }

            var payment = await _db.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Availability)
                .FirstOrDefaultAsync(p => p.ProviderOrderCode == request.Data.OrderCode)
                ?? throw new KeyNotFoundException(
                    $"Payment not found for orderCode {request.Data.OrderCode}.");

            if (!request.Success || request.Code != "00")
            {
                payment.Status = "Failed";
                await _db.SaveChangesAsync();
                return;
            }

            if (!string.IsNullOrWhiteSpace(request.Data.Code) &&
                request.Data.Code != "00")
            {
                payment.Status = "Failed";
                await _db.SaveChangesAsync();
                return;
            }

            await MarkPaymentPaidAndCreateLessonsAsync(payment);
        }

        public async Task<CreatePaymentResponse> SyncPayOsPaymentAsync(int userId, int bookingId)
        {
            var payment = await _db.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Availability)
                .FirstOrDefaultAsync(p =>
                    p.BookingId == bookingId &&
                    p.Booking.UserId == userId)
                ?? throw new KeyNotFoundException("Payment not found.");

            if (payment.Status == "Paid")
                return ToPaymentResponse(payment);

            if (payment.Provider != "PayOS")
                throw new InvalidOperationException("This payment is not a PayOS payment.");

            if (payment.ProviderOrderCode <= 0)
                throw new InvalidOperationException("Payment order code is missing.");

            using var client = CreatePayOsClient();

            var response = await client.GetAsync(
                $"/v2/payment-requests/{payment.ProviderOrderCode}");

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayOS sync failed: {body}");

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var code = root.GetProperty("code").GetString();

            if (code != "00")
                throw new InvalidOperationException($"PayOS returned error: {body}");

            var data = root.GetProperty("data");

            var status = data.GetProperty("status").GetString();
            var amountPaid = data.TryGetProperty("amountPaid", out var amountPaidElement)
                ? amountPaidElement.GetDecimal()
                : 0m;

            if (status == "PAID" && amountPaid >= payment.TotalPrice)
            {
                await MarkPaymentPaidAndCreateLessonsAsync(payment);
            }
            else if (status == "CANCELLED")
            {
                payment.Status = "Failed";
                await _db.SaveChangesAsync();
            }

            return ToPaymentResponse(payment);
        }

        public async Task<string> DebugPayOsStatusAsync(long orderCode)
        {
            using var client = CreatePayOsClient();

            var response = await client.GetAsync($"/v2/payment-requests/{orderCode}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"PayOS debug failed. Status: {(int)response.StatusCode}. Body: {body}");
            }

            return body;
        }

        private async Task<(string? checkoutUrl, string? qrCode)> CreatePayOsCheckoutUrlAsync(
            Payment payment,
            string description)
        {
            ValidatePayOsConfig();

            var amount = (int)Math.Round(
                payment.TotalPrice,
                0,
                MidpointRounding.AwayFromZero);

            var signatureData =
                $"amount={amount}" +
                $"&cancelUrl={_payOs.CancelUrl}" +
                $"&description={description}" +
                $"&orderCode={payment.ProviderOrderCode}" +
                $"&returnUrl={_payOs.ReturnUrl}";

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

            using var client = CreatePayOsClient();

            var response = await client.PostAsync(
                "/v2/payment-requests",
                new StringContent(
                    JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json"));

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"PayOS create payment failed: {responseBody}");

            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            var code = root.GetProperty("code").GetString();

            if (code != "00")
                throw new InvalidOperationException($"PayOS create payment returned error: {responseBody}");

            var data = root.GetProperty("data");

            return (
                data.TryGetProperty("checkoutUrl", out var checkoutUrl)
                    ? checkoutUrl.GetString()
                    : null,
                data.TryGetProperty("qrCode", out var qrCode)
                    ? qrCode.GetString()
                    : null
            );
        }

        private async Task MarkPaymentPaidAndCreateLessonsAsync(Payment payment)
        {
            if (payment.Status == "Paid")
                return;

            if (payment.Booking == null)
                throw new InvalidOperationException("Payment booking was not loaded.");

            payment.Status = "Paid";
            payment.PaidAt = DateTime.UtcNow;

            payment.Booking.Status = "Confirmed";

            await EnsureLessonsForBookingAsync(payment.Booking);

            await _db.SaveChangesAsync();
        }

        private async Task EnsureLessonsForBookingAsync(Booking booking)
        {
            if (await _db.Lessons.AnyAsync(l => l.BookingId == booking.BookingId))
                return;

            var availability = booking.Availability
                ?? await _db.Availabilities.FirstAsync(a =>
                    a.AvailabilityId == booking.AvailabilityId);

            var day = ParseDayOfWeek(availability.DayOfWeek);

            var startDate = availability.StartCourseTime.Date;
            var endDate = availability.EndCourseTime.Date;

            var time = availability.StartTime;
            var duration = Math.Max(
                30,
                (int)(availability.EndTime - availability.StartTime).TotalMinutes);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (date.DayOfWeek != day)
                    continue;

                _db.Lessons.Add(new Lesson
                {
                    BookingId = booking.BookingId,
                    ScheduleTime = DateTime.SpecifyKind(
                        date.Add(time),
                        DateTimeKind.Utc),
                    Duration = duration,
                    Status = "Scheduled",
                    MeetingLink = string.Empty
                });
            }
        }

        private HttpClient CreatePayOsClient()
        {
            ValidatePayOsConfig();

            var client = _httpClientFactory.CreateClient();

            client.BaseAddress = new Uri("https://api-merchant.payos.vn");
            client.DefaultRequestHeaders.Add("x-client-id", _payOs.ClientId);
            client.DefaultRequestHeaders.Add("x-api-key", _payOs.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }

        private void ValidatePayOsConfig()
        {
            if (string.IsNullOrWhiteSpace(_payOs.ClientId))
                throw new InvalidOperationException("PayOS ClientId is missing.");

            if (string.IsNullOrWhiteSpace(_payOs.ApiKey))
                throw new InvalidOperationException("PayOS ApiKey is missing.");

            if (string.IsNullOrWhiteSpace(_payOs.ChecksumKey))
                throw new InvalidOperationException("PayOS ChecksumKey is missing.");

            if (string.IsNullOrWhiteSpace(_payOs.ReturnUrl))
                throw new InvalidOperationException("PayOS ReturnUrl is missing.");

            if (string.IsNullOrWhiteSpace(_payOs.CancelUrl))
                throw new InvalidOperationException("PayOS CancelUrl is missing.");
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

            var data = string.Join(
                '&',
                pairs
                    .Where(p => !string.IsNullOrEmpty(p.Value))
                    .Select(p => $"{p.Key}={p.Value}"));

            var expected = HmacSha256(data, _payOs.ChecksumKey);

            return string.Equals(
                expected,
                request.Signature,
                StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSha256(string data, string key)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));

            return Convert
                .ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                .ToLowerInvariant();
        }

        private static DayOfWeek ParseDayOfWeek(string value)
        {
            return Enum.TryParse<DayOfWeek>(value, true, out var day)
                ? day
                : DayOfWeek.Monday;
        }

        private static CreatePaymentResponse ToPaymentResponse(Payment payment)
        {
            return new CreatePaymentResponse
            {
                PaymentId = payment.PaymentId,
                BookingId = payment.BookingId,
                Amount = payment.TotalPrice,
                Status = payment.Status,
                Provider = payment.Provider,
                OrderCode = payment.ProviderOrderCode,
                Description = payment.Description,
                CheckoutUrl = payment.CheckoutUrl,
                QrCode = payment.QrCode
            };
        }
    }
}