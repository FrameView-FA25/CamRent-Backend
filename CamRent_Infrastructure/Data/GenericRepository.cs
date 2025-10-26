using CamRent_Application.Interfaces;
using CamRent_Domain.Common;
using CamRent_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace CamRent_Infrastructure.Data
{
	public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
	{
		protected readonly CamRentDbContext _context;

		public GenericRepository(CamRentDbContext context)
		{
			_context = context;
		}

		public async Task<T?> GetByIdAsync(Guid id)
			=> await _context.Set<T>().FindAsync(id);

		public async Task<List<T>> GetAllAsync()
			=> await _context.Set<T>().ToListAsync();

		public async Task AddAsync(T entity)
			=> await _context.Set<T>().AddAsync(entity);

		public Task UpdateAsync(T entity)
		{
			_context.Set<T>().Update(entity);
			return Task.CompletedTask;
		}

		public async Task DeleteAsync(Guid id)
		{
			var entity = await GetByIdAsync(id);
			if (entity != null) _context.Set<T>().Remove(entity);
		}


		public async Task<bool> ExistsAsync(Guid id)
			=> await _context.Set<T>().AnyAsync(e => e.Id == id);

		public async Task<IEnumerable<T>> ListAsync(
			Expression<Func<T, bool>>? filter = null,
			Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
			Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
		{
			IQueryable<T> query = _context.Set<T>();

			if (filter != null)
				query = query.Where(filter);

			if (include != null)
				query = include(query);

			if (orderBy != null)
				return await orderBy(query).ToListAsync();

			return await query.ToListAsync();
		}
	}
}
