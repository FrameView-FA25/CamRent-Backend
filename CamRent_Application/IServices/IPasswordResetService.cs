using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IPasswordResetService
	{
		Task RequestResetAsync(string email, string? continueUrl = null, CancellationToken ct = default);
		Task<bool> ResetAsync(string email, string token, string newPassword, CancellationToken ct = default);
	}
}

