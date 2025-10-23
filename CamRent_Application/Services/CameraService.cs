using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using CamRent_Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Application.Services
{
	public class CameraService : ICameraService
	{
		private readonly IUnitOfWork _unitOfWork;
		public CameraService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public Task AddAsync(Camera camera)
		{
			throw new NotImplementedException();
		}

		public Task DeleteAsync(Guid id)
		{
			throw new NotImplementedException();
		}

		public Task<bool> ExistsAsync(Guid id)
		{
			throw new NotImplementedException();
		}

		public async Task<IReadOnlyList<Camera>> GetAllAsync()
		{
			var result = await _unitOfWork.Repository<Camera>().GetAllAsync();

			return result;
		}

		public Task<Camera?> GetByIdAsync(Guid id)
		{
			throw new NotImplementedException();
		}

		public Task<IEnumerable<Camera>> ListAsync(Expression<Func<Camera, bool>>? filter = null, Func<IQueryable<Camera>, IOrderedQueryable<Camera>>? orderBy = null, Func<IQueryable<Camera>, IIncludableQueryable<Camera, object>>? include = null)
		{
			throw new NotImplementedException();
		}

		public Task UpdateAsync(Camera camera)
		{
			throw new NotImplementedException();
		}
	}
}
