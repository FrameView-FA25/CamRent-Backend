using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.InspectionModel;

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
