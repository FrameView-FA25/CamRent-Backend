using CamRent_Application.Interfaces;
using CamRent_Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamRent_Domain.Interfaces
{
	public interface IUnitOfWork : IDisposable
	{
		IGenericRepository<T> Repository<T>() where T : BaseEntity;
		Task<int> Complete();
	}
}
