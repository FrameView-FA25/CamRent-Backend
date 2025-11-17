using CamRent_Application.Common;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.Extensions.Options;
using QRCoder;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.Services
{
    public class QrPaymentService : IQrPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PaymentQrOptions _options;

        public QrPaymentService(IUnitOfWork unitOfWork, IOptions<PaymentQrOptions> options)
        {
            _unitOfWork = unitOfWork;
            _options = options.Value;
        }

        public async Task<VietQrPayload> GenerateVietQrAsync(Guid paymentId, decimal amount, string? description = null, CancellationToken ct = default)
        {
            var payment = await _unitOfWork.Repository<Payment>().GetByIdAsync(paymentId)
                ?? throw new InvalidOperationException("Payment not found");

            var content = (_options.ContentTemplate ?? "CR-{PaymentId}")
                .Replace("{PaymentId}", paymentId.ToString("N"));
            if (!string.IsNullOrWhiteSpace(description))
                content = description;

            // Simplified payload (skeleton). In production, build EMV payload per VietQR spec.
            var payload = BuildSimpleVietQrUri(_options.BankBin, _options.AccountNumber, _options.AccountName, amount, content);

            var png = GeneratePng(payload);

            return new VietQrPayload
            {
                PaymentId = paymentId,
                Amount = amount,
                Content = content,
                Payload = payload,
                PngImage = png,
                ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.ExpiryMinutes))
            };
        }

        private static string BuildSimpleVietQrUri(string bankBin, string accountNumber, string accountName, decimal amount, string content)
        {
            var builder = new StringBuilder();
            builder.Append("vietqr://transfer?");
            builder.Append($"bin={Uri.EscapeDataString(bankBin)}");
            builder.Append($"&account={Uri.EscapeDataString(accountNumber)}");
            builder.Append($"&name={Uri.EscapeDataString(accountName)}");
            builder.Append($"&amount={amount}");
            builder.Append($"&addInfo={Uri.EscapeDataString(content)}");
            builder.Append("&currency=VND");
            return builder.ToString();
        }

        private static byte[] GeneratePng(string text)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
            var pngCode = new PngByteQRCode(data);
            return pngCode.GetGraphic(6);
        }
    }
}


