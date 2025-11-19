using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Tạo combo thiết bị", Description = "Tạo combo thiết bị/phụ kiện với tên, mô tả và giá override. Quyền: BranchManager hoặc Admin")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateComboRequest request)
		{
			var id = await _comboService.CreateAsync(request.Name, request.Description, request.PriceOverride);
			return Ok(id);
		}

		[HttpGet("{id:guid}")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Chi tiết combo", Description = "Trả về thông tin combo và các item bên trong. Quyền: Công khai")]
		public async Task<ActionResult<object>> Get(Guid id)
		{
			var combo = await _comboService.GetAsync(id);
			return Ok(combo);
		}

		
		[HttpPost("{id:guid}/items")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Thêm item vào combo", Description = "Thêm một camera hoặc accessory vào combo đã tồn tại. Quyền: BranchManager hoặc Admin")]
		public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest request)
		{
			await _comboService.AddItemAsync(id, request.CameraId, request.AccessoryId);
			return NoContent();
		}

		[HttpDelete("items/{itemId:guid}")]
		[Authorize(Policy = "BranchManager")]
		[SwaggerOperation(Summary = "Xóa item khỏi combo", Description = "Xóa một item khỏi combo dựa trên combo item id. Quyền: BranchManager hoặc Admin")]
		public async Task<IActionResult> RemoveItem(Guid itemId)
		{
			await _comboService.RemoveItemAsync(itemId);
			return NoContent();
		}
	}
}
