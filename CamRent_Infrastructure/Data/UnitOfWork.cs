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
			return _serviceProvider.GetRequiredService<IGenericRepository<TEntity>>();
		}

		public async Task<int> Complete()
		{
			try
			{
				return await _context.SaveChangesAsync();
			}
			catch (DbUpdateException ex)
			{
				// Kiểm tra lỗi nội bộ từ database
				var innerMessage = ex.InnerException?.Message ?? "No inner exception";
				Console.WriteLine($"DbUpdateException: {ex.Message}");
				Console.WriteLine($"Inner Exception: {innerMessage}");

				// Nếu cần, có thể log lỗi vào file hoặc hệ thống giám sát
				throw; // Ném lại lỗi để debug dễ hơn
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
