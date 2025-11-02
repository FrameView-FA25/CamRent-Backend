using CamRent_Application.IServices;
using Microsoft.AspNetCore.Mvc;

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

		public class CreateCategoryRequest { public string Name { get; set; } = string.Empty; public Guid? ParentId { get; set; } }
		[HttpPost]
		public async Task<ActionResult<Guid>> Create([FromBody] CreateCategoryRequest request)
		{
			var id = await _categoryService.CreateAsync(request.Name, request.ParentId);
			return Ok(id);
		}

		[HttpGet]
		public async Task<ActionResult<object>> List()
		{
			var list = await _categoryService.ListAsync();
			return Ok(list);
		}

		[HttpDelete("{id:guid}")]
		public async Task<IActionResult> Delete(Guid id)
		{
			await _categoryService.DeleteAsync(id);
			return NoContent();
		}

		public class LinkRequest { public Guid CategoryId { get; set; } public Guid DeviceId { get; set; } }
		[HttpPost("link/camera")]
		public async Task<IActionResult> LinkCamera([FromBody] LinkRequest request)
		{
			await _categoryService.LinkCameraAsync(request.CategoryId, request.DeviceId);
			return NoContent();
		}

		[HttpPost("link/accessory")]
		public async Task<IActionResult> LinkAccessory([FromBody] LinkRequest request)
		{
			await _categoryService.LinkAccessoryAsync(request.CategoryId, request.DeviceId);
			return NoContent();
		}
	}
}
