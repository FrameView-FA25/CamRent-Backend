using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CamRent_Application.DTOs;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore.Query;

namespace CamRent_Application.IServices
{
	public interface ICameraService
	{
		Task<CameraResponseDTO?> GetByIdAsync(Guid id);
		Task<List<CameraResponseDTO>> GetAllAsync();
		Task<int> AddAsync(Camera camera);
		Task<int> UpdateAsync(Camera camera);
		Task<int> DeleteAsync(Guid id);

		Task<IEnumerable<CameraResponseDTO>> ListAsync();
	}
}
