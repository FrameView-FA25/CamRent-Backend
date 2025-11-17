using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public sealed class MockSignatureProvider : IContractSignatureProvider
	{
		private readonly IUnitOfWork _uow;
		public MockSignatureProvider(IUnitOfWork uow) { _uow = uow; }

		public Task<string> CreateEnvelopeAsync(Guid contractId, CancellationToken ct = default)
		{
			// Return a fake sign URL
			return Task.FromResult($"https://example.com/sign/{contractId}");
		}

		public async Task HandleWebhookAsync(string payload, string? signatureHeader, CancellationToken ct = default)
		{
			// Very simple: parse payload for contractId and mark as completed
			// Expect payload: "contractId=<guid>&status=completed"
			var parts = payload.Split('&', StringSplitOptions.RemoveEmptyEntries);
			var dict = parts.Select(p => p.Split('=')).Where(x => x.Length == 2).ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1]));
			if (!dict.TryGetValue("contractId", out var idStr)) return;
			if (!Guid.TryParse(idStr, out var id)) return;
			var contract = await _uow.Repository<Contract>().GetByIdAsync(id);
			if (contract == null) return;
			contract.Status = ContractStatus.Completed;
			await _uow.Repository<Contract>().UpdateAsync(contract);
			await _uow.Complete();
		}
	}
}

