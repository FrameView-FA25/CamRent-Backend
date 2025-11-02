using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class ContractModel
	{
		public class CreateContractRequest { [Required] public Guid BookingId { get; set; } [Required] public Guid TemplateId { get; set; } }
		public class SignContractRequest { [Url] public string? SignedFileUrl { get; set; } }
	}
}
