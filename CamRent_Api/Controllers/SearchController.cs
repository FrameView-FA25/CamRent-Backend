using CamRent_Application.IServices;
using CamRent_Infrastructure.Weaviate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class SearchController : ControllerBase
	{
		private readonly IEmbeddingService _embed;
		private readonly IWeaviateClient _client;
		public SearchController(IEmbeddingService embed, IWeaviateClient client)
		{
			_embed = embed;
			_client = client;
		}

		public sealed class SearchResponseItem
		{
			public Guid Id { get; set; }
			public string Class { get; set; } = string.Empty;
			public string? Name { get; set; }
			public string? Description { get; set; }
			public float? Score { get; set; }
		}

		[HttpGet]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Tìm kiếm thiết bị (AI + keyword)", Description = "Hỗn hợp search semantic/hybrid cho camera, accessory, combo. Hỗ trợ type, mode, phân trang. Quyền: Công khai")]
		public async Task<ActionResult<object>> Get([FromQuery] string q, [FromQuery] string type = "all", [FromQuery] string mode = "hybrid",
			[FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			if (string.IsNullOrWhiteSpace(q)) return BadRequest("q is required");
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 100);

			float[]? vec = null;
			if (!string.Equals(mode, "keyword", StringComparison.OrdinalIgnoreCase))
			{
				vec = await _embed.EmbedAsync(q, HttpContext.RequestAborted);
			}

			var classes = new List<string>();
			if (string.Equals(type, "all", StringComparison.OrdinalIgnoreCase)) classes.AddRange(new[] { "Camera", "Accessory", "Combo" });
			else if (string.Equals(type, "camera", StringComparison.OrdinalIgnoreCase)) classes.Add("Camera");
			else if (string.Equals(type, "accessory", StringComparison.OrdinalIgnoreCase)) classes.Add("Accessory");
			else if (string.Equals(type, "combo", StringComparison.OrdinalIgnoreCase)) classes.Add("Combo");
			else return BadRequest("type must be all|camera|accessory|combo");

			var items = new List<SearchResponseItem>();
			foreach (var cls in classes)
			{
				CamRent_Infrastructure.Weaviate.WeaviateQueryResult res;
				if (string.Equals(mode, "semantic", StringComparison.OrdinalIgnoreCase))
					res = await _client.NearVectorAsync(cls, vec!, 20, HttpContext.RequestAborted);
				else if (string.Equals(mode, "keyword", StringComparison.OrdinalIgnoreCase))
					res = await _client.HybridAsync(cls, q, null, 20, 0.0f, HttpContext.RequestAborted);
				else
					res = await _client.HybridAsync(cls, q, vec, 20, 0.5f, HttpContext.RequestAborted);

				IEnumerable<WeaviateObject> list = cls switch
				{
					"Camera" => res.Data?.Get?.Camera ?? new List<WeaviateObject>(),
					"Accessory" => res.Data?.Get?.Accessory ?? new List<WeaviateObject>(),
					"Combo" => res.Data?.Get?.Combo ?? new List<WeaviateObject>(),
					_ => new List<WeaviateObject>()
				};
				foreach (var x in list)
				{
					if (Guid.TryParse(x._Additional.Id, out var id))
					{
						items.Add(new SearchResponseItem
						{
							Id = id,
							Class = cls,
							Name = x.Name,
							Description = x.Description,
							Score = x._Additional.Distance.HasValue ? 1 - x._Additional.Distance.Value : null
						});
					}
				}
			}

			var ordered = items.OrderByDescending(i => i.Score ?? 0).ToList();
			var total = ordered.Count;
			var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(new { page, pageSize, total, items = paged });
		}
	}
}

