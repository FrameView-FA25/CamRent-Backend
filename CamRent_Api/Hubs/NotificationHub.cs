using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CamRent_Api.Hubs
{
	// Hub dùng để đẩy thông báo realtime cho client (Renter, Owner, Staff, Manager, Admin)
	[Authorize]
	public class NotificationHub : Hub
	{
		public override async Task OnConnectedAsync()
		{
			var user = Context.User;
			if (user != null)
			{
				// Thêm connection vào group theo từng role để broadcast theo role
				var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Distinct();
				foreach (var role in roles)
				{
					await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
				}
			}

			await base.OnConnectedAsync();
		}
	}
}


