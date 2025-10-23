using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;

namespace CamRent_Application.IServices
{
	public interface ICameraService
	{
		Task<Camera?> GetByIdAsync(Guid id);
		Task<IReadOnlyList<Camera>> GetAllAsync();
		Task AddAsync(Camera camera);
		Task UpdateAsync(Camera camera);
		Task DeleteAsync(Guid id);
		Task<bool> ExistsAsync(Guid id);

		Task<IEnumerable<Camera>> ListAsync(
			Expression<Func<Camera, bool>>? filter = null,
			Func<IQueryable<Camera>, IOrderedQueryable<Camera>>? orderBy = null,
			Func<IQueryable<Camera>, IIncludableQueryable<Camera, object>>? include = null);
	}
}
