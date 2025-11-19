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

		public async Task<List<BookingResponseDTO>> GetAllAsync()
		{
			var bookings = await _unitOfWork.Repository<Booking>().ListAsync(include: b => b.Include(b => b.Items));
			foreach (var booking in bookings)
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
			if (booking == null)
			{
				return 0;
			}
			booking.StaffId = staffUserId;
			booking.Status = BookingStatus.Confirmed;
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			return await _unitOfWork.Complete();
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByRenterIdAsync(Guid renterId)
		{
			var bookings = await _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.RenterId == renterId && b.Status != BookingStatus.Draft, include: b => b.Include(b => b.Items));
			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			return results;
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByStaffIdAsync(Guid staffId)
		{
			var bookings = await _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.StaffId == staffId && b.Status != BookingStatus.Draft && b.Status != BookingStatus.PendingApproval,
				include: b => b.Include(b => b.Items)
							.ThenInclude(i => i.Camera)
						.Include(b => b.Items)
							.ThenInclude(i => i.Accessory)
						.Include(b => b.Items)
							.ThenInclude(i => i.Combo));
			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			return results;
		}
		private decimal CalculateDepositForCamera(Camera camera)
		{
			var raw = camera.EstimatedValueVnd * camera.DepositPercent;

			if (camera.DepositCapMinVnd.HasValue)
				raw = Math.Max(raw, camera.DepositCapMinVnd.Value);

			if (camera.DepositCapMaxVnd.HasValue)
				raw = Math.Min(raw, camera.DepositCapMaxVnd.Value);

			return raw;
		}

		private decimal CalculateDepositForAccessory(Accessory acc)
		{
			var raw = acc.EstimatedValueVnd * acc.DepositPercent;

			if (acc.DepositCapMinVnd.HasValue)
				raw = Math.Max(raw, acc.DepositCapMinVnd.Value);

			if (acc.DepositCapMaxVnd.HasValue)
				raw = Math.Min(raw, acc.DepositCapMaxVnd.Value);

			return raw;
		}


		public async Task<int> AddToCart(Guid renterId, Guid id, ItemType type, int quantity)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();
			var bookingItemRepo = _unitOfWork.Repository<BookingItem>();

			// Lấy booking Draft + include Items để check trùng
			var booking = (await bookingRepo.ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft,
				include: q => q.Include(b => b.Items)
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

				await bookingRepo.AddAsync(booking);
			}

			BookingItem? bookingItem = null;

			if (type == ItemType.Camera)
			{
				var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Camera not found");

				// Set branch nếu chưa có
				if (booking.BranchId == null)
				{
					booking.BranchId = camera.BranchId;
				}

				// 🔎 Tìm item đã tồn tại
				bookingItem = booking.Items
					.FirstOrDefault(bi => bi.CameraId == camera.Id && bi.ComboId == null && bi.AccessoryId == null);

				if (bookingItem != null)
				{
					// Đã có -> cộng dồn
					bookingItem.Quantity += quantity;
					bookingItem.UnitPrice = camera.BaseDailyRate;
					bookingItem.DepositAmount = CalculateDepositForCamera(camera);
				}
				else
				{
					// Chưa có -> tạo mới
					bookingItem = new BookingItem
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						CameraId = camera.Id,
						Quantity = quantity,
						UnitPrice = camera.BaseDailyRate,
						DepositAmount = CalculateDepositForCamera(camera)
					};

					booking.Items.Add(bookingItem);
					await bookingItemRepo.AddAsync(bookingItem);
				}
			}
			else if (type == ItemType.Accessory)
			{
				var accessory = await _unitOfWork.Repository<Accessory>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Accessory not found");

				if (booking.BranchId == null)
				{
					booking.BranchId = accessory.BranchId;
				}

				bookingItem = booking.Items
					.FirstOrDefault(bi => bi.AccessoryId == accessory.Id && bi.CameraId == null && bi.ComboId == null);

				if (bookingItem != null)
				{
					bookingItem.Quantity += quantity;
					bookingItem.UnitPrice = accessory.BaseDailyRate;
					bookingItem.DepositAmount = CalculateDepositForAccessory(accessory);
				}
				else
				{
					bookingItem = new BookingItem
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						AccessoryId = accessory.Id,
						Quantity = quantity,
						UnitPrice = accessory.BaseDailyRate,
						DepositAmount = CalculateDepositForAccessory(accessory)
					};

					booking.Items.Add(bookingItem);
					await bookingItemRepo.AddAsync(bookingItem);
				}
			}
			else if (type == ItemType.Combo)
			{
				var combo = await _unitOfWork.Repository<Combo>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Combo not found");

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
						}
					}
					else if (comboItem?.AccessoryId != null)
					{
						var acc = await _unitOfWork.Repository<Accessory>().GetByIdAsync(comboItem.AccessoryId.Value);
						if (acc != null)
						{
							booking.BranchId = acc.BranchId;
						}
					}
				}

				bookingItem = booking.Items
					.FirstOrDefault(bi => bi.ComboId == combo.Id && bi.CameraId == null && bi.AccessoryId == null);

				if (bookingItem != null)
				{
					bookingItem.Quantity += quantity;
					bookingItem.UnitPrice = combo.PriceOverride ?? bookingItem.UnitPrice;
					bookingItem.DepositAmount = combo.DepositOverride ?? bookingItem.DepositAmount;
				}
				else
				{
					bookingItem = new BookingItem
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						ComboId = combo.Id,
						Quantity = quantity,
						UnitPrice = combo.PriceOverride ?? 0m,
						DepositAmount = combo.DepositOverride ?? 0m
					};

					booking.Items.Add(bookingItem);
					await bookingItemRepo.AddAsync(bookingItem);
				}
			}

			return await _unitOfWork.Complete();
		}




		public async Task<int> RemoveFromCart(Guid renterId, Guid id, ItemType type)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();
			var bookingItemRepo = _unitOfWork.Repository<BookingItem>();

			var booking = (await bookingRepo.ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft
			)).FirstOrDefault();

			if (booking == null)
			{
				// Không có cart -> coi như không có gì để xóa
				return 0;
			}

			var items = await bookingItemRepo.ListAsync(
				filter: bi => bi.BookingId == booking.Id &&
					((type == ItemType.Camera && bi.CameraId == id) ||
					 (type == ItemType.Accessory && bi.AccessoryId == id) ||
					 (type == ItemType.Combo && bi.ComboId == id))
			);

			var item = items.FirstOrDefault();
			if (item == null)
			{
				return 0;
			}

			await bookingItemRepo.DeleteAsync(item.Id); // hoặc DeleteAsync(item) tùy repo
			return await _unitOfWork.Complete();
		}


		public async Task<Cart?> GetCartByRenterIdAsync(Guid renterId)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();

			// ✅ Dùng 1 query Include/ThenInclude, KHÔNG đụng booking.Id trước khi check null
			var booking = (await bookingRepo.ListAsync(
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft,
				include: q => q
					.Include(b => b.Items)
						.ThenInclude(i => i.Camera)
					.Include(b => b.Items)
						.ThenInclude(i => i.Accessory)
					.Include(b => b.Items)
						.ThenInclude(i => i.Combo)
			)).FirstOrDefault();

			if (booking == null)
			{
				booking = new Booking
				{
					Id = Guid.NewGuid(),
					RenterId = renterId,
					Status = BookingStatus.Draft,
					CreatedAt = DateTime.UtcNow,
					Items = new List<BookingItem>()
				};

				await bookingRepo.AddAsync(booking);
				await _unitOfWork.Complete();
			}

			var cart = _mapper.Map<Cart>(booking);
			cart.TotalPrice = booking.SnapshotRentalTotal;
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

		public async Task<int> CreateBookingAsync(CreateBookingRequest createBookingRequest, Guid renterId)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();

			var cart = (await bookingRepo.ListAsync(
				include: b => b
					.Include(b => b.Items),
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft
			)).FirstOrDefault();

			if (cart == null)
				throw new InvalidOperationException("No draft booking found.");

			// Map thông tin pickup/return, location,... từ request vào cart
			_mapper.Map(createBookingRequest, cart);

			// ====== TÍNH SNAPSHOT ======
			var days = Math.Max(1, (cart.ReturnAt.Date - cart.PickupAt.Date).Days);

			// 1) Tổng base daily rate (cho 1 ngày)
			var baseDailyTotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
			cart.SnapshotBaseDailyRate = baseDailyTotal;

			// 2) Tổng tiền thuê cho toàn bộ số ngày
			var rentalTotal = baseDailyTotal * days;
			cart.SnapshotRentalTotal = rentalTotal;

			// 3) Tổng tiền cọc
			var depositTotal = cart.Items.Sum(i => i.DepositAmount * i.Quantity);
			cart.SnapshotDepositAmount = depositTotal;

			// 4) % cọc so với tiền thuê (ví dụ: 0.5 = 50%)
			cart.SnapshotDepositPercent = rentalTotal == 0
				? 0
				: depositTotal / rentalTotal;

			// 5) % phí nền tảng – thường lấy từ config
			// Ví dụ bạn có IOptions<PlatformSettings> _platformSettings;
			const decimal platformFeePercent = 0.20m; // 20%
			cart.SnapshotPlatformFeePercent = platformFeePercent;

			// ====== CHUYỂN TRẠNG THÁI ======
			cart.Status = BookingStatus.PendingApproval;

			await bookingRepo.UpdateAsync(cart);
			return await _unitOfWork.Complete();
		}

	}
}
