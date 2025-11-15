using System.ComponentModel.DataAnnotations;

namespace CamRent_Api.Models
{
	public class PaymentModel
	{
		public class CreateAuthorizationRequest
		{
			[Required]
			public Guid BookingId { get; set; }
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
			[Range(0.01, double.MaxValue)]
			public decimal Amount { get; set; }
			[StringLength(100)]
			public string? Description { get; set; }
			[Url]
			public string ReturnUrl { get; set; } = string.Empty;
			[Url]
			public string CancelUrl { get; set; } = string.Empty;
		}
	}
}


