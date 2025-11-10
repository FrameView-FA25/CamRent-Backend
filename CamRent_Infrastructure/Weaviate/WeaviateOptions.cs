using System;

namespace CamRent_Infrastructure.Weaviate
{
	public class WeaviateOptions
	{
		public string BaseUrl { get; set; } = string.Empty;
		// If provided, will be sent as Authorization: Bearer {ApiKey}
		public string? ApiKey { get; set; }
		// Optional tenant or additional header key if your instance uses different auth
		public string? AdditionalAuthHeaderName { get; set; }
		public string? AdditionalAuthHeaderValue { get; set; }
	}
}


