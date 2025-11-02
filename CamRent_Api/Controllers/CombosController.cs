using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;
using static CamRent_Api.Models.ComboModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CombosController : ControllerBase
	{
		private readonly IComboService _comboService;
		public CombosController(IComboService comboService)
		{
			_comboService = comboService;
		}

		
		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateComboRequest request)
		{
			var id = await _comboService.CreateAsync(request.Name, request.Description, request.PriceOverride);
			return Ok(id);
		}

		[HttpGet("{id:guid}")]
		public async Task<ActionResult<object>> Get(Guid id)
		{
			var combo = await _comboService.GetAsync(id);
			return Ok(combo);
		}

		
		[HttpPost("{id:guid}/items")]
		public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest request)
		{
			await _comboService.AddItemAsync(id, request.CameraId, request.AccessoryId, request.Quantity);
			return NoContent();
		}

		[HttpDelete("items/{itemId:guid}")]
		public async Task<IActionResult> RemoveItem(Guid itemId)
		{
			await _comboService.RemoveItemAsync(itemId);
			return NoContent();
		}
	}
}
