using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CamRent_Api.Auth
{
	public class HeaderAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public const string SchemeName = "HeaderAuth";

		public HeaderAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
			: base(options, logger, encoder, clock) { }

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			var userId = Request.Headers["X-User-Id"].FirstOrDefault();
			var roles = Request.Headers["X-Roles"].FirstOrDefault();
			if (string.IsNullOrWhiteSpace(userId))
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, userId)
			};
			if (!string.IsNullOrWhiteSpace(roles))
			{
				foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
				{
					claims.Add(new Claim(ClaimTypes.Role, role));
				}
			}
			var identity = new ClaimsIdentity(claims, SchemeName);
			var principal = new ClaimsPrincipal(identity);
			var ticket = new AuthenticationTicket(principal, SchemeName);
			return Task.FromResult(AuthenticateResult.Success(ticket));
		}
	}
}
