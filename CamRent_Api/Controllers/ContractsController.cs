using CamRent_Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static CamRent_Api.Models.ContractModel;

namespace CamRent_Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ContractsController : ControllerBase
	{
    	private readonly IContractService _contractService;
    	private readonly IContractTemplateService _templateService;
    	public ContractsController(IContractService contractService, IContractTemplateService templateService)
		{
			_contractService = contractService;
			_templateService = templateService;
		}
	}
}
