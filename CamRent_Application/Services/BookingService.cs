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
		public BookingService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
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

			await EnsureAvailabilityAsync(booking.PickupAt, booking.ReturnAt, cameraId, accessoryId);

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

		private async Task EnsureAvailabilityAsync(DateTime pickupAt, DateTime returnAt, Guid? cameraId, Guid? accessoryId)
		{
			// get overlapping bookings with blocking statuses
			var overlapping = await _unitOfWork.Repository<Booking>().ListAsync(
				b => b.Status != BookingStatus.Cancelled
					&& b.Status != BookingStatus.Completed
					&& b.PickupAt < returnAt
					&& b.ReturnAt > pickupAt
			);
			if (!overlapping.Any()) return;
			var overlappingIds = overlapping.Select(b => b.Id).ToList();

			if (cameraId.HasValue)
			{
				var conflicts = await _unitOfWork.Repository<BookingItem>().ListAsync(
					bi => bi.CameraId == cameraId && overlappingIds.Contains(bi.BookingId)
				);
				if (conflicts.Any()) throw new InvalidOperationException("Camera is not available in the selected period");
			}
			if (accessoryId.HasValue)
			{
				var conflicts = await _unitOfWork.Repository<BookingItem>().ListAsync(
					bi => bi.AccessoryId == accessoryId && overlappingIds.Contains(bi.BookingId)
				);
				if (conflicts.Any()) throw new InvalidOperationException("Accessory is not available in the selected period");
			}
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
