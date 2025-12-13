using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public static class ContractModel
	{
		public class SignContractRequest
		{
			public string SignatureBase64 { get; set; } = default!;
		}

		public class CreateContractResponse
		{
			public Guid ContractId { get; set; }
		}
	}
}
