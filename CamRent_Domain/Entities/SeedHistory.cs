using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Domain.Entities
{
	public class SeedHistory
	{
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Key { get; set; } = default!;
		public DateTime AppliedAtUtc { get; set; } = DateTime.UtcNow;
	}
}
