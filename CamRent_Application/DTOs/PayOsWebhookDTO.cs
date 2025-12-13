using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class PayOsWebhookDTO
	{
		public class PayOsWebhookResult
		{
			public bool Success { get; set; }
			public Guid? PaymentId { get; set; }
			public Guid? UserId { get; set; }
			public Guid? BookingId { get; set; }
			public decimal Amount { get; set; }
			public string? Purpose { get; set; }
		}
	}
}
