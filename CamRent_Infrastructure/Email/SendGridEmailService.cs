using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CamRent_Application.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CamRent_Infrastructure.Email
{
	public sealed class SendGridEmailService : IEmailService
	{
		private readonly EmailOptions _opts;
		private readonly HttpClient _http;
		private readonly ILogger<SendGridEmailService> _logger;

		public SendGridEmailService(IOptions<EmailOptions> options, HttpClient http, ILogger<SendGridEmailService> logger)
		{
			_opts = options.Value;
			_http = http;
			_logger = logger;
		}

		public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
		{
			// Với SendGrid HTTP API, Password chứa API key
			var apiKey = _opts.Password;
			if (string.IsNullOrWhiteSpace(apiKey))
			{
				_logger.LogWarning("SendGrid API key is missing, skip sending email to {To}.", to);
				return;
			}

			var fromEmail = string.IsNullOrWhiteSpace(_opts.From) ? _opts.User : _opts.From;
			if (string.IsNullOrWhiteSpace(fromEmail))
			{
				_logger.LogWarning("SendGrid from email is missing, skip sending email to {To}.", to);
				return;
			}

			var payload = new
			{
				personalizations = new[]
				{
					new
					{
						to = new[]
						{
							new { email = to }
						}
					}
				},
				from = new
				{
					email = fromEmail,
					name = _opts.FromName
				},
				subject,
				content = new[]
				{
					new
					{
						type = "text/html",
						value = htmlBody
					}
				}
			};

			var json = JsonSerializer.Serialize(payload);
			using var request = new HttpRequestMessage(HttpMethod.Post, "v3/mail/send")
			{
				Content = new StringContent(json, Encoding.UTF8, "application/json")
			};
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

			try
			{
				var response = await _http.SendAsync(request, ct);
				if (!response.IsSuccessStatusCode)
				{
					var body = await response.Content.ReadAsStringAsync(ct);
					_logger.LogError("SendGrid send failed to {To}. Status {Status}: {Body}", to, response.StatusCode, body);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "SendGrid send failed to {To}", to);
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


