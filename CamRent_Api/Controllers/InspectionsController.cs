using CamRent_Application.IServices;
using CamRent_Domain.Common;
using Microsoft.AspNetCore.Authorization;
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

		
	}
}
