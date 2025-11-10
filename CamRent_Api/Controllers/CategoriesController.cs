using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
		public async Task<ActionResult<Guid>> Create([FromBody] CreateCategoryRequest request)
		{
			var id = await _categoryService.CreateAsync(request.Name, request.ParentId);
			return Ok(id);
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<ActionResult<object>> List([FromQuery] string? search, [FromQuery] string? sort = "name", [FromQuery] bool desc = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			var (items, total) = await _categoryService.ListAsync(search, sort, desc, page, pageSize);
			return Ok(new { total, items });
		}

		[HttpDelete("{id:guid}")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _categoryService.DeleteAsync(id);
			return NoContent();
		}

		[HttpPost("link/camera")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> LinkCamera([FromBody] LinkRequest request)
		{
			await _categoryService.LinkCameraAsync(request.CategoryId, request.DeviceId);
			return NoContent();
		}

		[HttpPost("link/accessory")]
		[Authorize(Policy = "AdminOnly")]
		public async Task<IActionResult> LinkAccessory([FromBody] LinkRequest request)
		{
			await _categoryService.LinkAccessoryAsync(request.CategoryId, request.DeviceId);
			return NoContent();
		}
	}
}
