using CamRent_Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class PaymentModel
	{
		public class CreateAuthorizationRequest
		{
			[Required]
			public Guid BookingId { get; set; }
			[Required]
			public PaymentType Mode { get; set; }
			[Required]
			public PaymentMethod Method { get; set; }   // PayOs / Wallet
		}

		public class AddLineRequest
		{
			[Required]
			[StringLength(50)]
			public string Type { get; set; } = string.Empty;
			[Range(0.01, double.MaxValue)]
			public decimal Amount { get; set; }
		}

		public class CaptureRequest
		{
			[Range(0.01, double.MaxValue)]
			public decimal Amount { get; set; }
		}

		public class RefundRequest
		{
			[Range(0.01, double.MaxValue)]
			public decimal Amount { get; set; }
		}

		public class InitPayOsRequest
		{
			[Url]
			public string ReturnUrl { get; set; } = string.Empty;
			[Url]
			public string CancelUrl { get; set; } = string.Empty;
		}
	}
}


