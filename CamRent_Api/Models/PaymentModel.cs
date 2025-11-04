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
	}
}


