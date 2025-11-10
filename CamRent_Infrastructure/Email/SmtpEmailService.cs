using System.Net;
using System.Net.Mail;
using System.Text;
using CamRent_Application.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CamRent_Infrastructure.Email
{
	public sealed class SmtpEmailService : IEmailService
	{
		private readonly EmailOptions _opts;
		private readonly ILogger<SmtpEmailService> _logger;

		public SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
		{
			_opts = options.Value;
			_logger = logger;
		}

		public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
		{
			using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
			{
				EnableSsl = _opts.EnableSsl,
				Credentials = new NetworkCredential(_opts.User, _opts.Password)
			};
			var msg = new MailMessage
			{
				From = new MailAddress(_opts.From, _opts.FromName, Encoding.UTF8),
				Subject = subject,
				Body = htmlBody,
				IsBodyHtml = true,
				BodyEncoding = Encoding.UTF8,
				SubjectEncoding = Encoding.UTF8
			};
			msg.To.Add(new MailAddress(to));
			try
			{
				await client.SendMailAsync(msg);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Email send failed to {To}", to);
			}
		}

		public Task SendPaymentCapturedAsync(string to, string bookingCode, decimal amount, CancellationToken ct = default)
		{
			var html = $"<p>Thanh toán thành công cho đơn thuê {bookingCode}.</p><p>Số tiền: {amount:N0} VND</p>";
			return SendAsync(to, $"Thanh toán thành công - {bookingCode}", html, ct);
		}

		public Task SendPaymentRefundedAsync(string to, string bookingCode, decimal amount, CancellationToken ct = default)
		{
			var html = $"<p>Hoàn tiền cho đơn thuê {bookingCode}.</p><p>Số tiền: {amount:N0} VND</p>";
			return SendAsync(to, $"Hoàn tiền - {bookingCode}", html, ct);
		}
	}
}


