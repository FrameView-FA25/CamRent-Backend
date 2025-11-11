using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IContractSignatureProvider
	{
		Task<string> CreateEnvelopeAsync(Guid contractId, CancellationToken ct = default); // returns signUrl
		Task HandleWebhookAsync(string payload, string? signatureHeader, CancellationToken ct = default);
	}
}

