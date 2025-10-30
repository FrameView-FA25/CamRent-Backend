using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using System.Linq;
using System.Linq.Expressions;

namespace CamRent_Application.Services
{
	public class BookingService : IBookingService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAvailabilityService _availabilityService;
		private readonly IPricingService _pricingService;
		public BookingService(IUnitOfWork unitOfWork, IAvailabilityService availabilityService, IPricingService pricingService)
		{
			_unitOfWork = unitOfWork;
			_availabilityService = availabilityService;
			_pricingService = pricingService;
		}

		public async Task<Booking?> GetByIdAsync(Guid bookingId)
		{
			return await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
		}

		public async Task<Guid> CreateDraftAsync(Guid renterId, DateTime pickupAt, DateTime returnAt)
		{
			var booking = new Booking
			{
				Id = Guid.NewGuid(),
				Type = BookingType.Rental,
				RenterId = renterId,
				PickupAt = pickupAt,
				ReturnAt = returnAt,
				Status = BookingStatus.Draft,
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};
			await _unitOfWork.Repository<Booking>().AddAsync(booking);
			await _unitOfWork.Complete();
			return booking.Id;
		}

		public async Task AddItemAsync(Guid bookingId, Guid? cameraId, Guid? accessoryId, int quantity, decimal unitPrice, decimal depositAmount)
		{
			if ((cameraId.HasValue && accessoryId.HasValue) || (!cameraId.HasValue && !accessoryId.HasValue))
				throw new ArgumentException("Provide exactly one of cameraId or accessoryId");
			if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");

			if (cameraId.HasValue)
			{
				var ok = await _availabilityService.IsCameraAvailableAsync(cameraId.Value, booking.PickupAt, booking.ReturnAt);
				if (!ok) throw new InvalidOperationException("Camera is not available in the selected period");
				if (unitPrice <= 0 || depositAmount <= 0)
				{
					int days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));
					(var unit, var deposit, _) = await _pricingService.GetCameraPricingAsync(cameraId.Value, days);
					unitPrice = unit;
					depositAmount = deposit;
				}
			}
			if (accessoryId.HasValue)
			{
				var ok = await _availabilityService.IsAccessoryAvailableAsync(accessoryId.Value, booking.PickupAt, booking.ReturnAt);
				if (!ok) throw new InvalidOperationException("Accessory is not available in the selected period");
				if (unitPrice <= 0 || depositAmount <= 0)
				{
					int days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));
					(var unit, var deposit, _) = await _pricingService.GetAccessoryPricingAsync(accessoryId.Value, days);
					unitPrice = unit;
					depositAmount = deposit;
				}
			}

			var item = new BookingItem
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				CameraId = cameraId,
				AccessoryId = accessoryId,
				Quantity = quantity,
				UnitPrice = unitPrice,
				DepositAmount = depositAmount,
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};
			await _unitOfWork.Repository<BookingItem>().AddAsync(item);

			await UpdateSnapshotTotalsAsync(bookingId);
			await _unitOfWork.Complete();
		}

		public async Task RemoveItemAsync(Guid bookingItemId)
		{
			await _unitOfWork.Repository<BookingItem>().DeleteAsync(bookingItemId);
			await _unitOfWork.Complete();
		}

		public async Task UpdateTimesAsync(Guid bookingId, DateTime pickupAt, DateTime returnAt)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			booking.PickupAt = pickupAt;
			booking.ReturnAt = returnAt;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			await UpdateSnapshotTotalsAsync(bookingId);
			await _unitOfWork.Complete();
		}

		public async Task SubmitForApprovalAsync(Guid bookingId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			booking.Status = BookingStatus.PendingApproval;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			await _unitOfWork.Complete();
		}

		public async Task ApproveAsync(Guid bookingId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			booking.Status = BookingStatus.Confirmed;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			await _unitOfWork.Complete();
		}

		public async Task CancelAsync(Guid bookingId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			booking.Status = BookingStatus.Cancelled;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			await _unitOfWork.Complete();
		}

		public async Task<int> ProcessStatusesAsync(DateTime nowUtc)
		{
			int updated = 0;
			// Load bookings that may require transition
			var candidates = await _unitOfWork.Repository<Booking>().ListAsync(
				b => b.Status == BookingStatus.Confirmed
					|| b.Status == BookingStatus.InUse
					|| b.Status == BookingStatus.Returned
			);
			foreach (var b in candidates)
			{
				var original = b.Status;
				if ((b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InUse) && nowUtc > b.ReturnAt)
				{
					b.Status = BookingStatus.Overdue;
				}
				if (b.Status == BookingStatus.Returned)
				{
					b.Status = BookingStatus.Completed;
				}
				if (b.Status != original)
				{
					await _unitOfWork.Repository<Booking>().UpdateAsync(b);
					updated++;
				}
			}
			if (updated > 0) await _unitOfWork.Complete();
			return updated;
		}

		private async Task UpdateSnapshotTotalsAsync(Guid bookingId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var items = await _unitOfWork.Repository<BookingItem>().ListAsync(bi => bi.BookingId == bookingId);
			int days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));
			decimal total = items.Sum(i => i.UnitPrice * i.Quantity * days);
			decimal deposit = items.Sum(i => i.DepositAmount * i.Quantity);
			booking.SnapshotRentalTotal = total;
			booking.SnapshotDepositAmount = deposit;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
		}
	}
}
