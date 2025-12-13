using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Api.Models.CategoryModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class CategoriesController : ControllerBase
	{
		private readonly ICategoryService _categoryService;
		public CategoriesController(ICategoryService categoryService)
		{
			_categoryService = categoryService;
		}

		[HttpPost]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Tạo danh mục", Description = "Tạo mới một category (tùy chọn parent) cho thiết bị. Quyền: Admin")]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateCategoryRequest request)
		{
			var id = await _categoryService.CreateAsync(request.Name, request.ParentId);
			return Ok(id);
		}

		[HttpGet]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Danh sách danh mục", Description = "Trả về danh sách category, hỗ trợ tìm kiếm, sắp xếp, phân trang. Quyền: Công khai")]
		public async Task<ActionResult<object>> List([FromQuery] string? search, [FromQuery] string? sort = "name", [FromQuery] bool desc = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			var (items, total) = await _categoryService.ListAsync(search, sort, desc, page, pageSize);
			return Ok(new { total, items });
		}

		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Xóa danh mục", Description = "Xóa category theo id (chỉ Admin). Cần đảm bảo không còn thiết bị liên kết.")]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _categoryService.DeleteAsync(id);
			return NoContent();
		}

		[HttpPost("link/camera")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Gắn danh mục cho camera", Description = "Liên kết một camera vào category cụ thể. Quyền: Admin")]
		public async Task<IActionResult> LinkCamera([FromBody] LinkRequest request)
		{
			await _categoryService.LinkCameraAsync(request.CategoryId, request.DeviceId);
			return NoContent();
		}

		[HttpPost("link/accessory")]
		[Authorize(Policy = "AdminOnly")]
		[SwaggerOperation(Summary = "Gắn danh mục cho phụ kiện", Description = "Liên kết một accessory vào category cụ thể. Quyền: Admin")]
		public async Task<IActionResult> LinkAccessory([FromBody] LinkRequest request)
		{
			await _categoryService.LinkAccessoryAsync(request.CategoryId, request.DeviceId);
			return NoContent();
		}
	}
}
