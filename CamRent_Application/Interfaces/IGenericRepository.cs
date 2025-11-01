using CamRent_Domain.Common;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace CamRent_Application.Interfaces
{
	public interface IGenericRepository<T> where T : BaseEntity
	{
		Task<T?> GetByIdAsync(Guid id);
		Task<List<T>> GetAllAsync();

		Task AddAsync(T entity);

		Task UpdateAsync(T entity);
		Task DeleteAsync(Guid id);

		Task<bool> ExistsAsync(Guid id);

		Task<IEnumerable<T>> ListAsync(
			Expression<Func<T, bool>>? filter = null,
			Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
			Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null
		);
	}
}
