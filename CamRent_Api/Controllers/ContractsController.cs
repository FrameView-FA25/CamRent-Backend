using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities; // để lấy ContractSignerRole, Contract nếu cần
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Net.Mime;
using System.Security.Claims;
using System.Threading.Tasks;
using static CamRent_Api.Models.ContractModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ContractsController : ControllerBase
	{
		private readonly IContractService _contractService;
		private readonly IContractTemplateService _templateService;

		public ContractsController(
			IContractService contractService,
			IContractTemplateService templateService)
		{
			_contractService = contractService;
			_templateService = templateService;
		}

		#region Helpers

		private Guid? GetCurrentUserId()
		{
			var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
					 ?? User.FindFirstValue("sub"); // tuỳ bạn map

			return Guid.TryParse(id, out var guid) ? guid : (Guid?)null;
		}



		#endregion

		// 1) Tạo hợp đồng booking
		[HttpPost("booking/{bookingId:guid}")]
		[Authorize(/* Policy = "StaffOrManager" */)]
		[SwaggerOperation(
			Summary = "Tạo hợp đồng booking",
			Description = "Tạo hợp đồng điện tử cho booking giữa CamRent và renter."
		)]
		[ProducesResponseType(typeof(CreateBookingContractResponse), 200)]
		public async Task<IActionResult> CreateBookingContract([FromRoute] Guid bookingId)
		{
			var userId = GetCurrentUserId();
			if (userId == null)
				return Unauthorized();

			var contract = await _contractService.CreateBookingContractAsync(bookingId, userId.Value);

			var response = new CreateBookingContractResponse
			{
				ContractId = contract.Id
			};

			return Ok(response);
		}
		[HttpPost("verification/{verificationId:guid}")]
		[Authorize(/* Policy = "StaffOrManager" */)]
		public async Task<IActionResult> CreateVerificationContract([FromRoute] Guid verificationId)
		{
			var userId = GetCurrentUserId();
			if (userId == null)
				return Unauthorized();

			var contract = await _contractService.CreateVerificationContractAsync(verificationId, userId.Value);

			return Ok(new
			{
				contract.Id,
				contract.Type,
				contract.Status
			});
		}

		[HttpGet("{contractId:guid}/preview")]
		[Authorize] // tuỳ bạn, có thể cho renter/owner/staff xem
		[SwaggerOperation(
		Summary = "Xem trước hợp đồng (PDF nháp, chưa cần chữ ký)",
		Description = "Generate file PDF hợp đồng từ dữ liệu Contract + Booking hiện tại, không yêu cầu đã ký."
		)]
		public async Task<IActionResult> PreviewContract([FromRoute] Guid contractId)
		{
			// 1. Load Contract kèm navigation
			//    Ở đây mình minh hoạ kiểu generic, bạn có thể đổi sang repo chuyên biệt.
			var contract = await _contractService.GetByIdAsync(contractId);
			if (contract == null)
				return NotFound(new { message = "Contract not found" });

			// Nếu GetByIdAsync không Include Booking/Renter/Branch/Signatures
			// thì bạn nên tạo 1 method custom, ví dụ:
			// var contract = await _contractRepository.GetWithDetailsAsync(contractId);

			byte[] pdfBytes;

			switch (contract.Type)
			{
				case ContractType.Booking:
					pdfBytes = await _templateService.RenderBookingContractAsync(contract);
					break;

				case ContractType.Verification:
					pdfBytes = await _templateService.RenderVerificationContractAsync(contract);
					break;

				default:
					return BadRequest(new { message = "Unsupported contract type for preview" });
			}

			var fileName = $"contract_preview_{contractId}.pdf";
			return File(pdfBytes, MediaTypeNames.Application.Pdf, fileName);
		}


		// 2) Ký hợp đồng (nhận base64 chữ ký)
		[HttpPost("{contractId:guid}/sign")]
		[AllowAnonymous] // hoặc [Authorize] nếu bạn muốn bắt buộc login
		[SwaggerOperation(
			Summary = "Ký hợp đồng điện tử",
			Description = "Nhận chữ ký dạng ảnh base64, lưu Cloudinary, cập nhật trạng thái hợp đồng."
		)]
		public async Task<IActionResult> SignContract(
			[FromRoute] Guid contractId,
			[FromBody] SignContractRequest request)
		{
			var userId = GetCurrentUserId(); // có thể null nếu AllowAnonymous
			var role = User.FindAll(ClaimTypes.Role).Select(r => r.Value).FirstOrDefault();
			ContractSignerRole contractSignerRole = ContractSignerRole.Platform; 
			if (role == UserRole.Owner.ToString())
			{
				contractSignerRole = ContractSignerRole.Owner;
			}
			else if (role == UserRole.Renter.ToString())
			{
				contractSignerRole = ContractSignerRole.Renter;
			}

			var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
			var ua = Request.Headers["User-Agent"].ToString();

			var contract = await _contractService.SignContractAsync(
				contractId,
				contractSignerRole,
				request.SignatureBase64,
				userId,
				ip,
				ua);

			return Ok(new
			{
				contract.Id,
				contract.Status,
				contract.SignedAt
			});
		}

		// 3) Tải file PDF hợp đồng
		[HttpGet("{contractId:guid}/file")]
		[Authorize] // tuỳ quyền
		[SwaggerOperation(
			Summary = "Tải PDF hợp đồng",
			Description = "Trả về file PDF của hợp đồng nếu đã generate."
		)]
		public async Task<IActionResult> DownloadContractFile([FromRoute] Guid contractId)
		{
			var pdfBytes = await _contractService.DownloadContractPdfAsync(contractId);
			if (pdfBytes == null)
				return NotFound(new
				{
					message = "Contract PDF not found or not generated yet."
				});

			var fileName = $"contract_{contractId}.pdf";
			return File(pdfBytes, MediaTypeNames.Application.Pdf, fileName);
		}
	}
}
