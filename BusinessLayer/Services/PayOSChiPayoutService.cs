using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Payment;
using BusinessLayer.IServices;
using BusinessLayer.Settings;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V1.Payouts.Batch;

namespace BusinessLayer.Services
{
    public sealed class PayOSChiPayoutService : IPayOSChiPayoutService
    {
        private readonly PayOSChiSetting _setting;
        private readonly PayOSClient _payOSClient;

        public PayOSChiPayoutService(IOptions<PayOSChiSetting> setting)
        {
            _setting = setting.Value;

            if (!_setting.Enabled)
            {
                // Do not throw here. Let approve payout decide fallback QR.
                _payOSClient = null!;
                return;
            }

            if (string.IsNullOrWhiteSpace(_setting.ClientId) ||
                string.IsNullOrWhiteSpace(_setting.ApiKey) ||
                string.IsNullOrWhiteSpace(_setting.ChecksumKey))
            {
                throw new InvalidOperationException("PayOSChi configuration is missing.");
            }

            _payOSClient = new PayOSClient(
                _setting.ClientId,
                _setting.ApiKey,
                _setting.ChecksumKey);
        }

        public async Task<PayOSChiPayoutResult> CreateTutorPayoutAsync(
            int payoutRequestId,
            int amount,
            string tutorBankBin,
            string tutorAccountNumber,
            string description)
        {
            if (!_setting.Enabled)
                throw new InvalidOperationException("PayOSChi automatic payout is disabled.");

            if (amount <= 0)
                throw new InvalidOperationException("Payout amount must be greater than 0.");

            if (string.IsNullOrWhiteSpace(tutorBankBin))
                throw new InvalidOperationException("Tutor bank BIN is missing.");

            if (string.IsNullOrWhiteSpace(tutorAccountNumber))
                throw new InvalidOperationException("Tutor bank account number is missing.");

            var referenceId = $"edunest_payout_{payoutRequestId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var payoutRequest = new PayoutBatchRequest
            {
                ReferenceId = referenceId,
                Category = new List<string> { "tutor_payout" },
                ValidateDestination = _setting.ValidateDestination,
                Payouts = new List<PayoutBatchItem>
                {
                    new PayoutBatchItem
                    {
                        ReferenceId = $"{referenceId}_1",
                        Amount = amount,
                        Description = description,
                        ToBin = tutorBankBin.Trim(),
                        ToAccountNumber = tutorAccountNumber.Trim()
                    }
                }
            };

            var response = await _payOSClient.Payouts.Batch.CreateAsync(payoutRequest);

            var raw = JsonSerializer.Serialize(response);

            return new PayOSChiPayoutResult
            {
                ReferenceId = referenceId,
                BatchId = TryGetString(response, "Id") ??
                          TryGetString(response, "id") ??
                          TryGetString(response, "BatchId") ??
                          TryGetString(response, "batchId"),

                PayoutItemId = null,

                ApprovalState = TryGetString(response, "ApprovalState") ??
                                TryGetString(response, "approvalState") ??
                                "PROCESSING",

                TransactionState = TryGetString(response, "TransactionState") ??
                                   TryGetString(response, "transactionState") ??
                                   "PROCESSING",

                RawResponse = raw
            };
        }

        private static string? TryGetString(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);

            if (property == null)
                return null;

            return property.GetValue(obj)?.ToString();
        }
    }
}
