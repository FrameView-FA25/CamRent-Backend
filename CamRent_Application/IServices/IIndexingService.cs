using System;
using System.Threading.Tasks;

namespace CamRent_Application.IServices
{
	public interface IIndexingService
	{
		void EnqueueUpsert(string @class, Guid id);
		void EnqueueDelete(string @class, Guid id);
	}
}

