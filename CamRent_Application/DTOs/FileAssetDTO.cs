using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.DTOs
{
	public class FileAssetDTO
	{
		public string Url { get; set; } = string.Empty;
		public string ContentType { get; set; } = string.Empty;
		public long? SizeBytes { get; set; }
		public string? Label { get; set; }
	}
}
