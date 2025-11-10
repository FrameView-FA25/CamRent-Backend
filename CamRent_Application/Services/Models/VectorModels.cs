using System;
using System.Collections.Generic;

namespace CamRent_Application.Services.Models
{
	public sealed class VectorUpsertItem
	{
		public Guid Id { get; set; }
		public string Class { get; set; } = string.Empty;
		public Dictionary<string, object> Properties { get; set; } = new();
		public float[]? Vector { get; set; }
	}

	public sealed class VectorSearchResult
	{
		public Guid Id { get; set; }
		public string Class { get; set; } = string.Empty;
		public string? Name { get; set; }
		public string? Description { get; set; }
		public float? Score { get; set; } // 1 - distance if needed
	}
}


