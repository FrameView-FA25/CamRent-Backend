using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static CamRent_Api.Models.AccessoryModel;

namespace CamRent_Api.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccessoriesController : ControllerBase
	{
		private readonly IAccessoryService _accessoryService;
		private readonly IMapper _mapper;
		public AccessoriesController(IAccessoryService accessoryService, IMapper mapper)
		{
			_accessoryService = accessoryService;
			_mapper = mapper;
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> GetAllAccessories([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? q = null, [FromQuery] string? sortBy = "createdAt", [FromQuery] string sortDir = "desc")
		{
			page = Math.Max(1, page);
			pageSize = Math.Clamp(pageSize, 1, 100);
			var accessories = await _accessoryService.GetAllAccessoriesAsync();
			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLowerInvariant();
				accessories = accessories.Where(a => ($"{a.Brand} {a.Model} {a.Variant}").ToLower().Contains(term)).ToList();
			}
			IEnumerable<dynamic> sorted = accessories;
			if (string.Equals(sortBy, "brand", StringComparison.OrdinalIgnoreCase))
				sorted = (sortDir == "asc" ? accessories.OrderBy(a => a.Brand) : accessories.OrderByDescending(a => a.Brand));
			else if (string.Equals(sortBy, "model", StringComparison.OrdinalIgnoreCase))
				sorted = (sortDir == "asc" ? accessories.OrderBy(a => a.Model) : accessories.OrderByDescending(a => a.Model));
			else
				sorted = (sortDir == "asc" ? accessories.OrderBy(a => a.Id) : accessories.OrderByDescending(a => a.Id));

			var total = sorted.Count();
			var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();
			return Ok(new { page, pageSize, total, items });
		}
		[HttpGet("{id}")]
		public async Task<IActionResult> GetAccessoryById(Guid id)
		{
			var accessory = await _accessoryService.GetAccessoryByIdAsync(id);
			if (accessory == null)
			{
				return NotFound();
			}
			return Ok(accessory);
		}
		[HttpGet("GetAccessoriesByUserId")]
		public async Task<IActionResult> GetAccessoriesByUserId()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
					  ?? User.FindFirst("sub")?.Value
					  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
			{
				return Forbid();
			}

			var accessories = await _accessoryService.GetAccessoriesByOwnerIdAsync(Guid.Parse(userId));
			return Ok(accessories);
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> CreateAccessory([FromBody] AccessoryRequest accessoryCreateModel)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
				  ?? User.FindFirst("sub")?.Value
				  ?? User.FindFirst("uid")?.Value;

			if (string.IsNullOrEmpty(userId))
				return Forbid();

			var accessory = _mapper.Map<Accessory>(accessoryCreateModel);
			accessory.OwnerUserId = Guid.Parse(userId);

			var result = await _accessoryService.CreateAccessoryAsync(accessory);
			return result > 0 ? Ok() : BadRequest();
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateAccessory(Guid id, [FromBody] AccessoryRequest accessoryUpdateModel)
		{
			var existingAccessory = await _accessoryService.GetAccessoryByIdAsync(id);
			if (existingAccessory == null)
			{
				return NotFound();
			}
			var accessoryToUpdate = _mapper.Map<Accessory>(accessoryUpdateModel);
			accessoryToUpdate.Id = id;
			var result = await _accessoryService.UpdateAccessoryAsync(accessoryToUpdate);
			return result > 0 ? Ok() : BadRequest();
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteAccessory(Guid id)
		{
			var existingAccessory = await _accessoryService.GetAccessoryByIdAsync(id);
			if (existingAccessory == null)
			{
				return NotFound();
			}
			var result = await _accessoryService.DeleteAccessoryAsync(id);
			return result > 0 ? Ok() : BadRequest();
		}
	}
}


