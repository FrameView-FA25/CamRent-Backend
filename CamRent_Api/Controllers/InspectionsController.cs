using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class InspectionsController : ControllerBase
	{
		private readonly IInspectionService _inspectionService;
		public InspectionsController(IInspectionService inspectionService)
		{
			_inspectionService = inspectionService;
		}

		public class CreateInspectionRequest
		{
			public Guid BookingId { get; set; }
			public InspectionType Type { get; set; }
			public Guid? PerformedByUserId { get; set; }
			public Guid? BranchId { get; set; }
			public string? Notes { get; set; }
			public List<Item> Items { get; set; } = new();
			public class Item { public string Section { get; set; } = string.Empty; public string Label { get; set; } = string.Empty; public string? Value { get; set; } public bool? Passed { get; set; } public string? Notes { get; set; } }
		}

		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateInspectionRequest request)
		{
			var id = await _inspectionService.CreateInspectionAsync(
				request.BookingId,
				request.Type,
				request.PerformedByUserId,
				request.BranchId,
				request.Notes,
				request.Items.Select(i => (i.Section, i.Label, i.Value, i.Passed, i.Notes))
			);
			return Ok(id);
		}
	}
}
