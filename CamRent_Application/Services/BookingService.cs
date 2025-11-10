using AutoMapper;
using CamRent_Application.Common;
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

		public async Task<int> AddToCart(Guid renterId, Guid id, BookingItemType type, int quantity)
		{
			var booking = (await _unitOfWork.Repository<Booking>().ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft
				)).FirstOrDefault();
			if (booking == null)
			{
				booking = new Booking
				{
					Id = Guid.NewGuid(),
					RenterId = renterId,
					Status = BookingStatus.Draft,
					CreatedAt = DateTime.UtcNow
				};
				await _unitOfWork.Repository<Booking>().AddAsync(booking);
			}
			if (type == BookingItemType.Camera)
			{
				var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Camera not found");
				var cameraItem = new BookingItem
				{
					Id = Guid.NewGuid(),
					BookingId = booking!.Id,
					CameraId = camera.Id,
					Quantity = quantity,
					UnitPrice = camera.BaseDailyRate,
					DepositAmount = camera.EstimatedValueVnd * camera.DepositPercent 
				};
				booking.Items.Add(cameraItem);
			}
			if (type == BookingItemType.Accessory)
			{
				var accessory = await _unitOfWork.Repository<Accessory>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Accessory not found");
				var accessoryItem = new BookingItem
				{
					Id = Guid.NewGuid(),
					BookingId = booking!.Id,
					AccessoryId = accessory.Id,
					Quantity = quantity,
					UnitPrice = accessory.BaseDailyRate,
					DepositAmount = accessory.EstimatedValueVnd * accessory.DepositPercent 
				};
				booking.Items.Add(accessoryItem);
			}
			if(type == BookingItemType.Combo)
			{
				var combo = await _unitOfWork.Repository<Combo>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Combo not found");
				var comboItem = new BookingItem
				{
					Id = Guid.NewGuid(),
					BookingId = booking!.Id,
					ComboId = combo.Id,
					Quantity = quantity
				};
				booking.Items.Add(comboItem);
			}
			return await _unitOfWork.Complete();
		}

		public async Task<int> RemoveFromCart(Guid renterId, Guid id, BookingItemType type)
		{
			var booking = _unitOfWork.Repository<Booking>().ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft
				).Result.FirstOrDefault();
			var items = _unitOfWork.Repository<BookingItem>().ListAsync(
				filter: bi => bi.BookingId == booking!.Id &&
				((type == BookingItemType.Camera && bi.CameraId == id) ||
				(type == BookingItemType.Accessory && bi.AccessoryId == id) ||
				(type == BookingItemType.Combo && bi.ComboId == id))
				).Result.FirstOrDefault();
			await _unitOfWork.Repository<BookingItem>().DeleteAsync(items.Id);
			return await _unitOfWork.Complete();
		}

		public async Task<Cart?> GetCartByRenterIdAsync(Guid renterId)
		{
			var booking = (await _unitOfWork.Repository<Booking>().ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft,
				include: b => b.Include(b => b.Items)
			)).FirstOrDefault();
			if(booking == null)
			{
				booking = new Booking
				{
					Id = Guid.NewGuid(),
					RenterId = renterId,
					Status = BookingStatus.Draft,
					CreatedAt = DateTime.UtcNow
				};
				await _unitOfWork.Repository<Booking>().AddAsync(booking);
				await _unitOfWork.Complete();
			}
			var cart = _mapper.Map<Cart>(booking);
			cart.TotalPrice = (double)booking.Items.Sum(i => i.UnitPrice * i.Quantity);
			return cart;
		}

		public Task<List<BookingStatusDTO>> GetBookingStatusesAsync()
		{
			var statuses = Enum.GetValues(typeof(BookingStatus))
				.Cast<BookingStatus>()
				.Select(bs => new BookingStatusDTO
				{
					Status = bs,
					StatusText = bs.GetDisplayName()
				})
				.ToList();
			return Task.FromResult(statuses);
		}
	}
}
