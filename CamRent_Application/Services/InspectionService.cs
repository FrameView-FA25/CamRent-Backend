using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class InspectionService : IInspectionService
	{
		private readonly IUnitOfWork _unitOfWork;
		public InspectionService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		
	}
}
