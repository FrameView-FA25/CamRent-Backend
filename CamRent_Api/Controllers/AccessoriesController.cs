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
		public async Task<IActionResult> GetAllAccessories()
		{
			var accessories = await _accessoryService.GetAllAccessoriesAsync();
			return Ok(accessories);
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


