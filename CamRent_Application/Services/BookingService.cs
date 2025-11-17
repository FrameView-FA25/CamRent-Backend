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
			foreach(var booking in bookings)
			{
				booking.Items = (await _unitOfWork.Repository<BookingItem>().ListAsync(filter: b => b.BookingId == booking.Id, include: b => b.Include(b => b.Camera).Include(b => b.Accessory).Include(b => b.Combo))).ToList();
			}
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

		public async Task<int> AddToCart(Guid renterId, Guid id, ItemType type, int quantity)
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

			BookingItem? bookingItem = null;

			if (type == ItemType.Camera)
			{
				var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Camera not found");

				// Gắn chi nhánh cho booking nếu chưa có (book với sàn/owner qua chi nhánh camera)
				if (booking.BranchId == null)
				{
					booking.BranchId = camera.BranchId;
					await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
				}

				bookingItem = new BookingItem
				{
					Id = Guid.NewGuid(),
					BookingId = booking.Id,
					CameraId = camera.Id,
					Quantity = quantity,
					UnitPrice = camera.BaseDailyRate,
					DepositAmount = camera.EstimatedValueVnd * camera.DepositPercent
				};
			}
			if (type == ItemType.Accessory)
			{
				var accessory = await _unitOfWork.Repository<Accessory>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Accessory not found");

				if (booking.BranchId == null)
				{
					booking.BranchId = accessory.BranchId;
					await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
				}

				bookingItem = new BookingItem
				{
					Id = Guid.NewGuid(),
					BookingId = booking.Id,
					AccessoryId = accessory.Id,
					Quantity = quantity,
					UnitPrice = accessory.BaseDailyRate,
					DepositAmount = accessory.EstimatedValueVnd * accessory.DepositPercent
				};
			}
			if (type == ItemType.Combo)
			{
				var combo = await _unitOfWork.Repository<Combo>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Combo not found");

				// Với combo, có thể chọn chi nhánh từ item đầu tiên thuộc combo (nếu booking chưa có Branch)
				if (booking.BranchId == null)
				{
					var comboItem = (await _unitOfWork.Repository<ComboItem>()
						.ListAsync(ci => ci.ComboId == combo.Id))
						.FirstOrDefault();
					if (comboItem?.CameraId != null)
					{
						var cam = await _unitOfWork.Repository<Camera>().GetByIdAsync(comboItem.CameraId.Value);
						if (cam != null)
						{
							booking.BranchId = cam.BranchId;
							await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
						}
					}
					else if (comboItem?.AccessoryId != null)
					{
						var acc = await _unitOfWork.Repository<Accessory>().GetByIdAsync(comboItem.AccessoryId.Value);
						if (acc != null)
						{
							booking.BranchId = acc.BranchId;
							await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
						}
					}
				}

				bookingItem = new BookingItem
				{
					Id = Guid.NewGuid(),
					BookingId = booking.Id,
					ComboId = combo.Id,
					Quantity = quantity
				};
			}

			// Phòng trường hợp enum có thêm value mới mà chưa xử lý
			if (bookingItem != null)
			{
				await _unitOfWork.Repository<BookingItem>().AddAsync(bookingItem);
			}
			return await _unitOfWork.Complete();
		}


		public async Task<int> RemoveFromCart(Guid renterId, Guid id, ItemType type)
		{
			var booking = _unitOfWork.Repository<Booking>().ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft
				).Result.FirstOrDefault();
			var items = _unitOfWork.Repository<BookingItem>().ListAsync(
				filter: bi => bi.BookingId == booking!.Id &&
				((type == ItemType.Camera && bi.CameraId == id) ||
				(type == ItemType.Accessory && bi.AccessoryId == id) ||
				(type == ItemType.Combo && bi.ComboId == id))
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
			booking.Items = (await _unitOfWork.Repository<BookingItem>().ListAsync(filter: b => b.BookingId == booking.Id, include: b => b.Include(b => b.Camera).Include(b => b.Accessory).Include(b => b.Combo))).ToList();
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
