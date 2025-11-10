using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;

namespace CamRent_Application.Services
{
	public class InspectionService : IInspectionService
	{
		private readonly IUnitOfWork _unitOfWork;
		public InspectionService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CreateInspectionAsync(Guid bookingId, InspectionType type, Guid? performedByUserId, Guid? branchId, string? notes, IEnumerable<(string section, string label, string? value, bool? passed, string? notes)> items)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var inspection = new Inspection
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				Type = type,
				Notes = notes ?? string.Empty,
				PerformedAt = DateTime.UtcNow,
				PerformedByUserId = performedByUserId,
				BranchId = branchId,
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};
			await _unitOfWork.Repository<Inspection>().AddAsync(inspection);

			foreach (var it in items)
			{
				var item = new InspectionItem
				{
					Id = Guid.NewGuid(),
					InspectionId = inspection.Id,
					Section = it.section,
					Label = it.label,
					Value = it.value,
					Passed = it.passed,
					Notes = it.notes
				};
				await _unitOfWork.Repository<InspectionItem>().AddAsync(item);
			}

			// status transitions
			if (type == InspectionType.Pre && booking.Status == BookingStatus.Confirmed)
			{
				booking.Status = BookingStatus.InUse;
				await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			}
			else if (type == InspectionType.Post && (booking.Status == BookingStatus.InUse || booking.Status == BookingStatus.Overdue))
			{
				booking.Status = BookingStatus.Returned;
				await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			}

			await _unitOfWork.Complete();
			return inspection.Id;
		}
	}
}
