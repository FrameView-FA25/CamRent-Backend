using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IEmbeddingService
	{
		Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
	}
}

