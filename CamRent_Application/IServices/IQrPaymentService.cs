using System;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
    public class VietQrPayload
    {
        public Guid PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Content { get; set; } = string.Empty; // Transfer memo e.g., CR-<paymentId>
        public string Payload { get; set; } = string.Empty; // Encoded string used to generate QR
        public byte[] PngImage { get; set; } = Array.Empty<byte>();
        public DateTime ExpiresAt { get; set; }
    }

    public interface IQrPaymentService
    {
        Task<VietQrPayload> GenerateVietQrAsync(Guid paymentId, decimal amount, string? description = null, CancellationToken ct = default);
    }
}


