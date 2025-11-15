using CamRent_Application.Interfaces;
using CamRent_Domain.Common;
using CamRent_Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Infrastructure.Data
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly CamRentDbContext _context;
		private readonly IServiceProvider _serviceProvider;
		public UnitOfWork(CamRentDbContext context, IServiceProvider serviceProvider)
		{
			_context = context;
			_serviceProvider = serviceProvider;
		}

		public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
		{
			// Resolve concrete GenericRepository<TEntity> from DI
			return _serviceProvider.GetRequiredService<GenericRepository<TEntity>>();
		}

		public async Task<int> Complete()
		{
			try
			{
				return await _context.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				var innerMessage = ex.InnerException?.Message ?? "No inner exception";
				Console.WriteLine($"DbUpdateException: {ex.Message}");
				Console.WriteLine($"Inner Exception: {innerMessage}");
				throw;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unexpected Error: {ex.Message}");
				throw;
			}
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}
