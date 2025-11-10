using AutoMapper;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using static CamRent_Application.DTOs.BookingDTO;

namespace CamRent_Application.Services
{
	public class BookingService : IBookingService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAvailabilityService _availabilityService;
		private readonly IPricingService _pricingService;
		private readonly IMapper _mapper;
		public BookingService(IUnitOfWork unitOfWork, IAvailabilityService availabilityService, IPricingService pricingService, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_availabilityService = availabilityService;
			_pricingService = pricingService;
			_mapper = mapper;
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

		public async Task AddItemAsync(Guid bookingId, Guid? cameraId, Guid? accessoryId, Guid? comboId, int quantity, decimal unitPrice, decimal depositAmount)
		{
			int provided = (cameraId.HasValue ? 1 : 0) + (accessoryId.HasValue ? 1 : 0) + (comboId.HasValue ? 1 : 0);
			if (provided != 1)
				throw new ArgumentException("Provide exactly one of cameraId, accessoryId, or comboId");
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

			if (comboId.HasValue)
			{
				int days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));
				var combo = await _unitOfWork.Repository<Combo>().GetByIdAsync(comboId.Value)
					?? throw new InvalidOperationException("Combo not found");
				var comboItems = await _unitOfWork.Repository<ComboItem>().ListAsync(ci => ci.ComboId == comboId.Value);
				decimal unitSum = 0;
				decimal depositSum = 0;
				foreach (var ci in comboItems)
				{
					if (ci.CameraId.HasValue)
					{
						(var unit, var deposit, _) = await _pricingService.GetCameraPricingAsync(ci.CameraId.Value, days);
						unitSum += unit * ci.Quantity;
						depositSum += deposit * ci.Quantity;
					}
					else if (ci.AccessoryId.HasValue)
					{
						(var unit, var deposit, _) = await _pricingService.GetAccessoryPricingAsync(ci.AccessoryId.Value, days);
						unitSum += unit * ci.Quantity;
						depositSum += deposit * ci.Quantity;
					}
				}
				if (unitPrice <= 0)
				{
					unitPrice = combo.PriceOverride ?? unitSum;
				}
				if (depositAmount <= 0)
				{
					depositAmount = depositSum;
				}
			}

			var item = new BookingItem
			{
				Id = Guid.NewGuid(),
				BookingId = bookingId,
				CameraId = cameraId,
				AccessoryId = accessoryId,
				ComboId = comboId,
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

		public async Task FinalizeAsync(Guid bookingId, decimal ownerShareRatio, Guid platformUserId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			var quote = await _pricingService.QuoteBookingAsync(bookingId, null, ownerShareRatio);

			// Determine owner from first item (MVP assumption: single-owner booking)
			var firstItem = (await _unitOfWork.Repository<BookingItem>().ListAsync(bi => bi.BookingId == bookingId)).FirstOrDefault()
				?? throw new InvalidOperationException("Booking has no items");
			Guid? ownerUserId = null;
			if (firstItem.CameraId.HasValue)
			{
				var cam = await _unitOfWork.Repository<Camera>().GetByIdAsync(firstItem.CameraId.Value);
				ownerUserId = cam?.OwnerUserId;
			}
			else if (firstItem.AccessoryId.HasValue)
			{
				var acc = await _unitOfWork.Repository<Accessory>().GetByIdAsync(firstItem.AccessoryId.Value);
				ownerUserId = acc?.OwnerUserId;
			}
			else if (firstItem.ComboId.HasValue)
			{
				var comboItems = await _unitOfWork.Repository<ComboItem>().ListAsync(ci => ci.ComboId == firstItem.ComboId.Value);
				var firstCam = comboItems.FirstOrDefault(ci => ci.CameraId.HasValue);
				if (firstCam != null)
				{
					var cam = await _unitOfWork.Repository<Camera>().GetByIdAsync(firstCam.CameraId!.Value);
					ownerUserId = cam?.OwnerUserId;
				}
				else
				{
					var firstAcc = comboItems.FirstOrDefault(ci => ci.AccessoryId.HasValue);
					if (firstAcc != null)
					{
						var acc = await _unitOfWork.Repository<Accessory>().GetByIdAsync(firstAcc.AccessoryId!.Value);
						ownerUserId = acc?.OwnerUserId;
					}
				}
			}
			if (!ownerUserId.HasValue) throw new InvalidOperationException("Cannot determine owner for payout");

			// Ensure wallets
			var ownerWallet = (await _unitOfWork.Repository<Wallet>().ListAsync(w => w.OwnerUserId == ownerUserId.Value)).FirstOrDefault();
			if (ownerWallet == null)
			{
				ownerWallet = new Wallet { Id = Guid.NewGuid(), OwnerUserId = ownerUserId.Value, Balance = 0, CreatedAt = DateTime.UtcNow };
				await _unitOfWork.Repository<Wallet>().AddAsync(ownerWallet);
			}
			Wallet? platformWallet = null;
			if (platformUserId != Guid.Empty)
			{
				platformWallet = (await _unitOfWork.Repository<Wallet>().ListAsync(w => w.OwnerUserId == platformUserId)).FirstOrDefault();
				if (platformWallet == null)
				{
					platformWallet = new Wallet { Id = Guid.NewGuid(), OwnerUserId = platformUserId, Balance = 0, CreatedAt = DateTime.UtcNow };
					await _unitOfWork.Repository<Wallet>().AddAsync(platformWallet);
				}
			}

			// Transactions
			var ownerTx = new Transaction
			{
				Id = Guid.NewGuid(),
				WalletId = ownerWallet.Id,
				Type = "payout_owner",
				Amount = quote.OwnerShare,
				Currency = "VND",
				Reference = $"Booking:{bookingId}",
				CreatedAt = DateTime.UtcNow
			};
			await _unitOfWork.Repository<Transaction>().AddAsync(ownerTx);
			ownerWallet.Balance += quote.OwnerShare;
			await _unitOfWork.Repository<Wallet>().UpdateAsync(ownerWallet);

			if (platformWallet != null && quote.PlatformNet > 0)
			{
				var platformTx = new Transaction
				{
					Id = Guid.NewGuid(),
					WalletId = platformWallet.Id,
					Type = "platform_net",
					Amount = quote.PlatformNet,
					Currency = "VND",
					Reference = $"Booking:{bookingId}",
					CreatedAt = DateTime.UtcNow
				};
				await _unitOfWork.Repository<Transaction>().AddAsync(platformTx);
				platformWallet.Balance += quote.PlatformNet;
				await _unitOfWork.Repository<Wallet>().UpdateAsync(platformWallet);
			}

			await _unitOfWork.Complete();
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

		public async Task<List<BookingResponseDTO>> GetAllAsync()
		{
			var bookings = await _unitOfWork.Repository<Booking>().ListAsync(include: b => b.Include(b => b.Items));
			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			return results;
		}

		public async Task<int> AssignStaffToBookingsAsync(Guid bookingId, Guid staffUserId)
		{
			var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId)
				?? throw new InvalidOperationException("Booking not found");
			if(booking == null)
			{
				return 0;
			}
			booking.StaffId = staffUserId;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			return await _unitOfWork.Complete();
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByRenterIdAsync(Guid renterId)
		{
			var bookings = await  _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.RenterId == renterId && b.Status != BookingStatus.Draft, include: b => b.Include(b => b.Items));
			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			return results;
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByStaffIdAsync(Guid staffId)
		{
			var bookings = await _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.StaffId == staffId && b.Status != BookingStatus.PendingApproval, include: b => b.Include(b => b.Items));
			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			return results;
		}
	}
}
