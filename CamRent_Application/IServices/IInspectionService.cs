using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.IServices
{
	public interface IInspectionService
	{
		Task<Guid> CreateInspectionAsync(Guid bookingId, InspectionType type, Guid? performedByUserId, Guid? branchId, string? notes, IEnumerable<(string section, string label, string? value, bool? passed, string? notes)> items);
	}
}
