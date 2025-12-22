using AutoMapper;
using CamRent_Application.Common;
using CamRent_Application.DTOs;
using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Linq;
using System.Linq.Expressions;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.WalletDTO;

namespace CamRent_Application.Services
{
	public class BookingService : IBookingService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IPricingService _pricingService;
		private readonly IWalletService _walletService;
		private readonly IMapper _mapper;
		public BookingService(IUnitOfWork unitOfWork,  IPricingService pricingService, IMapper mapper, IWalletService walletService)
		{
			_unitOfWork = unitOfWork;
			_pricingService = pricingService;
			_mapper = mapper;
			_walletService = walletService;
		}

		public async Task<BookingResponseDTO?> GetByIdAsync(Guid? bookingId)
		{
			if (!bookingId.HasValue)
			{
				return null;
			}
			
			var booking =  (await _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.Id == bookingId.Value, 
				include: b => b.Include(b => b.Items)
							.ThenInclude(i => i.Camera)
						.Include(b => b.Items)
							.ThenInclude(i => i.Accessory)
						.Include(b => b.Items)
							.ThenInclude(i => i.Combo)
						.Include(b => b.Contracts)
							.ThenInclude(c => c.Signatures)
						.Include(b => b.Payments).ThenInclude(p => p.Lines)
						.Include(b => b.Renter))).FirstOrDefault();
			var result = _mapper.Map<BookingResponseDTO>(booking);
			result.Payments = result.Payments?
			.Where(p => p != null && p.Status == PaymentStatus.Captured)
			.ToList() ?? new();

			return result;
		}

		public async Task<List<BookingResponseDTO>> GetAllAsync()
		{
			var bookings = (await _unitOfWork.Repository<Booking>().ListAsync(include: b => b.Include(b => b.Items))).ToList();
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
			await _unitOfWork.Repository<Booking>().UpdateAsync(booking);
			return await _unitOfWork.Complete();
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByRenterIdAsync(Guid renterId)
		{
			var bookings = await _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.RenterId == renterId && b.Status != BookingStatus.Draft, include: b => b
						.Include(b => b.Items)
							.ThenInclude(i => i.Camera)
						.Include(b => b.Items)
							.ThenInclude(i => i.Accessory)
						.Include(b => b.Items)
							.ThenInclude(i => i.Combo)
						.Include(b => b.Payments)
						.Include(b => b.Contracts));
			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);

			foreach (var booking in results)
			{
				foreach (var item in booking.Items )
				{
					if (item.ItemType == ItemType.Camera.ToString())
					{
						var media = (await _unitOfWork.Repository<FileAsset>()
							.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == item.ItemId)).ToList();
						item.Media = _mapper.Map<List<FileAssetDTO>>(media);
					}
					else if (item.ItemType == ItemType.Accessory.ToString())
					{
						var media = (await _unitOfWork.Repository<FileAsset>()
							.ListAsync(f => f.OwnerType == FileOwnerType.Accessory && f.OwnerId == item.ItemId)).ToList();
						item.Media = _mapper.Map<List<FileAssetDTO>>(media);
					}
				}	
			}
			return results;
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByStaffIdAsync(Guid staffId)
		{
			var bookings = (await _unitOfWork.Repository<Booking>().ListAsync(filter: b => b.StaffId == staffId && b.Status != BookingStatus.Draft,
				include: b => b.Include(b => b.Items)
							.ThenInclude(i => i.Camera)
						.Include(b => b.Items)
							.ThenInclude(i => i.Accessory)
						.Include(b => b.Items)
							.ThenInclude(i => i.Combo)
						.Include(b => b.Renter)
						.Include(b => b.Payments)
						.Include(b => b.Contracts))).ToList();

			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			foreach (var booking in results)
			{
				foreach (var item in booking.Items)
				{
					if (item.ItemType == ItemType.Camera.ToString())
					{
						var media = (await _unitOfWork.Repository<FileAsset>()
							.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == item.ItemId)).ToList();
						item.Media = _mapper.Map<List<FileAssetDTO>>(media);
					}
					else if (item.ItemType == ItemType.Accessory.ToString())
					{
						var media = (await _unitOfWork.Repository<FileAsset>()
							.ListAsync(f => f.OwnerType == FileOwnerType.Accessory && f.OwnerId == item.ItemId)).ToList();
						item.Media = _mapper.Map<List<FileAssetDTO>>(media);
					}
				}
			}
			return results;
		}

		public async Task<List<BookingResponseDTO>> GetBookingsByBranchManagerIdAsync(Guid managerId)
		{
			var branch = await _unitOfWork.Repository<Branch>().FirstOrDefaultAsync(b => b.ManagerId == managerId);
			if (branch == null) return new List<BookingResponseDTO>();

			var bookings = (await _unitOfWork.Repository<Booking>().ListAsync(
				filter: b => b.BranchId == branch.Id && b.Status != BookingStatus.Draft,
				include: b => b
					.Include(b => b.Staff)
					.Include(b => b.Items)
						.ThenInclude(i => i.Camera)
					.Include(b => b.Items)
						.ThenInclude(i => i.Accessory)
					.Include(b => b.Items)
						.ThenInclude(i => i.Combo)
					.Include(b => b.Renter)
					.Include(b => b.Payments)
					.Include(b => b.Contracts))).ToList();

			var results = _mapper.Map<List<BookingResponseDTO>>(bookings);
			foreach (var booking in results)
			{
				foreach (var item in booking.Items)
				{
					if (item.ItemType == ItemType.Camera.ToString())
					{
						var media = (await _unitOfWork.Repository<FileAsset>()
							.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == item.ItemId)).ToList();
						item.Media = _mapper.Map<List<FileAssetDTO>>(media);
					}
					else if (item.ItemType == ItemType.Accessory.ToString())
					{
						var media = (await _unitOfWork.Repository<FileAsset>()
							.ListAsync(f => f.OwnerType == FileOwnerType.Accessory && f.OwnerId == item.ItemId)).ToList();
						item.Media = _mapper.Map<List<FileAssetDTO>>(media);
					}
				}
			}
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


		public async Task<(bool Success, string? Message)> AddToCart(Guid renterId, Guid id, ItemType type)
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
					CreatedAt = DateTime.UtcNow,
					Items = new List<BookingItem>()
				};

				await bookingRepo.AddAsync(booking);
			}

			BookingItem? bookingItem = null;

			if (type == ItemType.Camera)
			{
				var camera = await _unitOfWork.Repository<Camera>().GetByIdAsync(id)
					?? throw new InvalidOperationException("Camera not found");

				if (booking.BranchId == null)
				{
					booking.BranchId = camera.BranchId;
				}

				// Tìm item đã tồn tại
				bookingItem = booking.Items
					.FirstOrDefault(bi => bi.CameraId == camera.Id && bi.ComboId == null && bi.AccessoryId == null);

				if (bookingItem != null)
				{
					// Nếu đã tồn tại, trả message theo yêu cầu
					return (false, "Thiết bị đã có trong giỏ hàng rồi");
				}
				else
				{
					bookingItem = new BookingItem
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						CameraId = camera.Id,
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
					return (false, "Thiết bị đã có trong giỏ hàng rồi");
				}
				else
				{
					bookingItem = new BookingItem
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						AccessoryId = accessory.Id,
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
					return (false, "Thiết bị đã có trong giỏ hàng rồi");
				}
				else
				{
					bookingItem = new BookingItem
					{
						Id = Guid.NewGuid(),
						BookingId = booking.Id,
						ComboId = combo.Id,
						UnitPrice = combo.PriceOverride ?? 0m,
						DepositAmount = combo.DepositOverride ?? 0m
					};

					booking.Items.Add(bookingItem);
					await bookingItemRepo.AddAsync(bookingItem);
				}
			}

			var saved = await _unitOfWork.Complete();
			if (saved > 0)
			{
				return (true, null);
			}
			return (false, "Thêm vào giỏ hàng thất bại");
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
			foreach (var item in cart.Items)
			{
				if(item.ItemType == ItemType.Camera.ToString() )
				{
					var media = (await _unitOfWork.Repository<FileAsset>()
						.ListAsync(f => f.OwnerType == FileOwnerType.Camera && f.OwnerId == item.ItemId)).ToList();
					item.Media = _mapper.Map<List<FileAssetDTO>>(media);
				}
				else if(item.ItemType == ItemType.Accessory.ToString() )
				{
					var media = (await _unitOfWork.Repository<FileAsset>()
						.ListAsync(f => f.OwnerType == FileOwnerType.Accessory && f.OwnerId == item.ItemId)).ToList();
					item.Media = _mapper.Map<List<FileAssetDTO>>(media);
				}
			}
			// fill UnavailableRanges cho từng item trong cart
			foreach (var itemDto in cart.Items)
			{
				if (itemDto.ItemId == null) continue;

				var type = Enum.Parse<ItemType>(itemDto.ItemType);
				var ranges = await GetUnavailableRangesForItemAsync(itemDto.ItemId.Value, type);

				itemDto.UnavailableRanges = ranges;
			}
			return cart;
		}

		public async Task<List<BookingItemUnavailableRangeDTO>> GetUnavailableRangesForItemAsync(
			Guid itemId,
			ItemType type,
			CancellationToken cancellationToken = default)
		{
			var bookingItemRepo = _unitOfWork.Repository<BookingItem>();

			// filter theo loại item
			Expression<Func<BookingItem, bool>> filter = bi =>
				bi.Booking != null &&
				bi.Booking.Status != BookingStatus.Draft &&
				bi.Booking.Status != BookingStatus.Cancelled &&
				bi.Booking.Status != BookingStatus.Completed &&
				(
					(type == ItemType.Camera && bi.CameraId == itemId) ||
					(type == ItemType.Accessory && bi.AccessoryId == itemId) ||
					(type == ItemType.Combo && bi.ComboId == itemId)
				);

			var items = await bookingItemRepo.ListAsync(
				filter: filter,
				include: q => q.Include(bi => bi.Booking)
			);

			var result = items
				.Select(bi => new BookingItemUnavailableRangeDTO
				{
					BookingId = bi.Booking!.Id,
					StartUtc = bi.Booking.PickupAt,   // đang lưu UTC
					EndUtc = bi.Booking.ReturnAt,
					Status = bi.Booking.Status.ToString()
				})
				.ToList();

			return result;
		}

		/// <summary>
		/// Danh sách renter đã từng thuê ít nhất một thiết bị của owner.
		/// </summary>
		public async Task<List<OwnerRenterSummaryDTO>> GetOwnerRentersAsync(Guid ownerUserId)
		{
			// Thiết bị của owner
			var cameras = await _unitOfWork.Repository<Camera>().ListAsync(c => c.OwnerUserId == ownerUserId);
			var accessories = await _unitOfWork.Repository<Accessory>().ListAsync(a => a.OwnerUserId == ownerUserId);

			var cameraIds = cameras.Select(c => c.Id).ToHashSet();
			var accessoryIds = accessories.Select(a => a.Id).ToHashSet();

			if (!cameraIds.Any() && !accessoryIds.Any())
				return new List<OwnerRenterSummaryDTO>();

			var validStatuses = new[]
			{
				BookingStatus.Confirmed,
				BookingStatus.PickedUp,
				BookingStatus.Returned,
				BookingStatus.Completed,
				BookingStatus.Overdue
			};

			// BookingItems liên quan tới thiết bị của owner, kèm Booking + Renter
			var bookingItems = await _unitOfWork.Repository<BookingItem>()
				.ListAsync(
					bi =>
						bi.Booking != null &&
						bi.Booking.RenterId != null &&
						validStatuses.Contains(bi.Booking.Status) &&
						(
							(bi.CameraId.HasValue && cameraIds.Contains(bi.CameraId.Value)) ||
							(bi.AccessoryId.HasValue && accessoryIds.Contains(bi.AccessoryId.Value))
						),
					include: q => q
						.Include(bi => bi.Booking)!.ThenInclude(b => b.Renter)
				);

			var groups = bookingItems
				.Where(bi => bi.Booking != null && bi.Booking.Renter != null && bi.Booking.RenterId != null)
				.GroupBy(bi => new
				{
					RenterId = bi.Booking!.RenterId!.Value,
					RenterName = bi.Booking!.Renter!.FullName,
					RenterEmail = bi.Booking!.Renter!.Email
				});

			var result = groups
				.Select(g =>
				{
					var distinctBookings = g
						.Where(bi => bi.Booking != null)
						.Select(bi => bi.Booking!.Id)
						.Distinct()
						.ToList();

					var lastPickup = g
						.Where(bi => bi.Booking != null)
						.Max(bi => (DateTime?)bi.Booking!.PickupAt);

					return new OwnerRenterSummaryDTO
					{
						RenterId = g.Key.RenterId,
						RenterName = g.Key.RenterName ?? string.Empty,
						Email = g.Key.RenterEmail,
						TotalBookings = distinctBookings.Count,
						LastPickupAt = lastPickup
					};
				})
				.OrderByDescending(x => x.LastPickupAt)
				.ToList();

			return result;
		}

		/// <summary>
		/// Lịch sử booking giữa một owner và một renter cụ thể
		/// (chỉ bao gồm các item thuộc owner trong mỗi booking).
		/// </summary>
		public async Task<List<OwnerRenterBookingDTO>> GetOwnerRenterBookingsAsync(Guid ownerUserId, Guid renterId)
		{
			// Thiết bị của owner
			var cameras = await _unitOfWork.Repository<Camera>().ListAsync(c => c.OwnerUserId == ownerUserId);
			var accessories = await _unitOfWork.Repository<Accessory>().ListAsync(a => a.OwnerUserId == ownerUserId);

			var cameraIds = cameras.Select(c => c.Id).ToHashSet();
			var accessoryIds = accessories.Select(a => a.Id).ToHashSet();

			if (!cameraIds.Any() && !accessoryIds.Any())
				return new List<OwnerRenterBookingDTO>();

			var validStatuses = new[]
			{
				BookingStatus.Confirmed,
				BookingStatus.PickedUp,
				BookingStatus.Returned,
				BookingStatus.Completed,
				BookingStatus.Overdue
			};

			// BookingItems liên quan tới thiết bị của owner + booking của renter này
			var bookingItems = await _unitOfWork.Repository<BookingItem>()
				.ListAsync(
					bi =>
						bi.Booking != null &&
						bi.Booking.RenterId == renterId &&
						validStatuses.Contains(bi.Booking.Status) &&
						(
							(bi.CameraId.HasValue && cameraIds.Contains(bi.CameraId.Value)) ||
							(bi.AccessoryId.HasValue && accessoryIds.Contains(bi.AccessoryId.Value))
						),
					include: q => q
						.Include(bi => bi.Booking)!
						.ThenInclude(b => b.Renter)
						.Include(bi => bi.Camera)
						.Include(bi => bi.Accessory)
				);

			var groups = bookingItems
				.Where(bi => bi.Booking != null)
				.GroupBy(bi => bi.Booking!);

			var result = new List<OwnerRenterBookingDTO>();

			foreach (var g in groups)
			{
				var booking = g.Key;

				var dto = new OwnerRenterBookingDTO
				{
					BookingId = booking.Id,
					PickupAt = booking.PickupAt,
					ReturnAt = booking.ReturnAt,
					Status = booking.Status,
					StatusText = booking.Status.GetDisplayName()
				};

				foreach (var item in g)
				{
					if (item.CameraId.HasValue && cameraIds.Contains(item.CameraId.Value))
					{
						var cam = item.Camera ?? cameras.FirstOrDefault(c => c.Id == item.CameraId.Value);
						var name = cam != null ? $"{cam.Brand} {cam.Model}" : "Camera";

						dto.Items.Add(new OwnerRenterBookingItemDTO
						{
							ItemId = item.CameraId.Value,
							ItemName = name,
							ItemType = "camera",
							UnitPrice = item.UnitPrice
						});
					}
					else if (item.AccessoryId.HasValue && accessoryIds.Contains(item.AccessoryId.Value))
					{
						var acc = item.Accessory ?? accessories.FirstOrDefault(a => a.Id == item.AccessoryId.Value);
						var name = acc != null ? $"{acc.Brand} {acc.Model}" : "Accessory";

						dto.Items.Add(new OwnerRenterBookingItemDTO
						{
							ItemId = item.AccessoryId.Value,
							ItemName = name,
							ItemType = "accessory",
							UnitPrice = item.UnitPrice
						});
					}
				}

				// Chỉ add nếu còn ít nhất 1 item thuộc owner
				if (dto.Items.Any())
					result.Add(dto);
			}

			return result
				.OrderByDescending(b => b.PickupAt)
				.ToList();
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

		public async Task<Guid> CreateBookingAsync(CreateBookingRequest createBookingRequest, Guid renterId)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();

			var cart = (await bookingRepo.ListAsync(
				include: b => b
					.Include(b => b.Items),
				filter: b => b.RenterId == renterId && b.Status == BookingStatus.Draft
			)).FirstOrDefault();

			if (cart == null)
				throw new InvalidOperationException("No draft booking found.");

			await EnsureBookingItemsAvailableOrThrowAsync(cart, createBookingRequest.PickupAt, createBookingRequest.ReturnAt);

			// Map thông tin pickup/return, location,... từ request vào cart
			_mapper.Map(createBookingRequest, cart);

			// ====== TÍNH SNAPSHOT ======
			var days = Math.Max(1, (cart.ReturnAt.Date - cart.PickupAt.Date).Days);

			// 1) Tổng base daily rate (cho 1 ngày)
			var baseDailyTotal = cart.Items.Sum(i => i.UnitPrice);
			cart.SnapshotBaseDailyRate = baseDailyTotal;

			// 2) Tổng tiền thuê cho toàn bộ số ngày
			var rentalTotal = baseDailyTotal * days;
			cart.SnapshotRentalTotal = rentalTotal;

			// 3) Tổng tiền cọc
			var depositTotal = cart.Items.Sum(i => i.DepositAmount);
			cart.SnapshotDepositAmount = depositTotal;

			// 5) % phí nền tảng – thường lấy từ config
			// Ví dụ bạn có IOptions<PlatformSettings> _platformSettings;	
			const decimal platformFeePercent = 0.10m; // 10%
			var setting = await _unitOfWork.Repository<MoneyFlatformSetting>()
			.FirstOrDefaultAsync(s => s.IsActive);
			if (setting != null)
			{
				cart.SnapshotPlatformFeePercent = setting.PlatformFeePercent;
			}
			else
			{
				cart.SnapshotPlatformFeePercent = platformFeePercent;
			}



			// ====== CHUYỂN TRẠNG THÁI ======
			cart.CreatedAt = DateTime.UtcNow;

			await bookingRepo.UpdateAsync(cart);
			await _unitOfWork.Complete();
			return cart.Id;
		}

		// Kiểm tra 1 BookingItem có conflict hay không (true = rảnh)
		public async Task<bool> IsBookingItemAvailableAsync(
			BookingItem item,
			DateTime start,
			DateTime end,
			CancellationToken cancellationToken = default)
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			if (start >= end) throw new ArgumentException("start must be earlier than end", nameof(start));

			// Kiểm tra xem item có id hợp lệ hay không
			var hasCamera = item.CameraId != Guid.Empty && item.CameraId != null;
			var hasAccessory = item.AccessoryId != Guid.Empty && item.AccessoryId != null;
			var hasCombo = item.ComboId != Guid.Empty && item.ComboId != null;

			if (!hasCamera && !hasAccessory && !hasCombo)
				throw new ArgumentException("BookingItem must reference CameraId, AccessoryId or ComboId.");

			// Predicate: match item (camera OR accessory OR combo) && booking is active && time overlap
			Expression<Func<BookingItem, bool>> predicate = bi =>
				(
					(hasCamera && bi.CameraId == item.CameraId) ||
					(hasAccessory && bi.AccessoryId == item.AccessoryId) ||
					(hasCombo && bi.ComboId == item.ComboId)
				)
				&& bi.Booking != null
				&& bi.Booking.Status != BookingStatus.Cancelled
				&& bi.Booking.Status != BookingStatus.Draft
				&& bi.Booking.Status != BookingStatus.Completed
				&& bi.Booking.PickupAt < end
				&& bi.Booking.ReturnAt > start;

			// Nếu repository có overload AnyAsync với CancellationToken, truyền vào
			var conflictExists = await _unitOfWork.Repository<BookingItem>()
				.AnyAsync(predicate, cancellationToken);

			return !conflictExists;
		}

		// Kiểm tra toàn bộ items trong cart, ném nếu có conflict
		public async Task EnsureBookingItemsAvailableOrThrowAsync(
			Booking cart,
			DateTime start,
			DateTime end,
			CancellationToken cancellationToken = default)
		{
			if (cart == null) throw new ArgumentNullException(nameof(cart));
			if (cart.Items == null || !cart.Items.Any()) return; // không có item thì ok
			if (start >= end) throw new ArgumentException("start must be earlier than end", nameof(start));

			foreach (var item in cart.Items)
			{
				var available = await IsBookingItemAvailableAsync(item, start, end, cancellationToken);
				if (!available)
				{
					// Bạn có thể trả thông tin chi tiết hơn (ví dụ item name, type) tuỳ model
					throw new InvalidOperationException($"Item (id: {item.Id}) is not available between {start:O} and {end:O}.");
				}
			}
		}
		private async Task SettleBooking(Booking booking)
		{
			// ============ 1. Chuẩn bị dữ liệu ============

			if (!booking.RenterId.HasValue)
				throw new InvalidOperationException("Booking missing renter.");

			// Số ngày thuê (ít nhất 1 ngày)
			var rentalDays = (booking.ReturnAt.Date - booking.PickupAt.Date).TotalDays;
			if (rentalDays < 1) rentalDays = 1;

			// Đảm bảo Items có dữ liệu; nếu không, load từ DB
			var items = booking.Items;
			if (items == null || !items.Any())
			{
				items = (await _unitOfWork.Repository<BookingItem>().ListAsync(i => i.BookingId == booking.Id)).ToList();
			}

			// ============ 2. Tính rental cho từng item + group theo Owner ============

			// Mỗi phần tử: (ownerId, itemRental)
			var ownerItemRentals = new List<(Guid ownerId, decimal itemRental)>();

			foreach (var item in items)
			{
				Guid? ownerId = null;

				if (item.Camera != null)
				{
					ownerId = item.Camera.OwnerUserId;
				}
				else if (item.Accessory != null)
				{
					ownerId = item.Accessory.OwnerUserId;
				}
				else
				{
					// Nếu navigation chưa load, thử lấy từ DB
					if (item.CameraId.HasValue)
					{
						var cam = await _unitOfWork.Repository<Camera>().GetByIdAsync(item.CameraId.Value);
						ownerId = cam?.OwnerUserId;
					}
					else if (item.AccessoryId.HasValue)
					{
						var acc = await _unitOfWork.Repository<Accessory>().GetByIdAsync(item.AccessoryId.Value);
						ownerId = acc?.OwnerUserId;
					}
				}

				if (!ownerId.HasValue)
					continue; // Item không xác định được owner -> bỏ qua hoặc log

				var itemRental = item.UnitPrice * (decimal)rentalDays;
				if (itemRental <= 0)
					continue;

				ownerItemRentals.Add((ownerId.Value, itemRental));
			}

			if (!ownerItemRentals.Any())
				throw new InvalidOperationException("Cannot determine owner rentals for booking items.");

			var itemsTotalRental = ownerItemRentals.Sum(x => x.itemRental);
			if (itemsTotalRental <= 0)
				throw new InvalidOperationException("Total item rental is zero.");

			// ============ 3. Chia tiền thuê (SnapshotRentalTotal) theo owner ============

			var rentalTotal = booking.SnapshotRentalTotal;               // A = tổng tiền thuê của booking
			var feePercent = booking.SnapshotPlatformFeePercent;         // p = % hoa hồng

			var platformFeeTotal = decimal.Round(rentalTotal * feePercent, 0);
			if (platformFeeTotal < 0) platformFeeTotal = 0;

			// Group theo owner
			var groups = ownerItemRentals
				.GroupBy(x => x.ownerId)
				.ToList();

			// Tính payout cho từng owner
			var ownerPayouts = new Dictionary<Guid, decimal>();

			foreach (var g in groups)
			{
				var ownerId = g.Key;
				var ownerGroupRental = g.Sum(x => x.itemRental);

				// Tỷ lệ rental của owner này trên tổng rental items
				var ratio = ownerGroupRental / itemsTotalRental;

				// Doanh thu (gross) của owner từ booking này
				var ownerGrossShare = rentalTotal * ratio;

				// Payout net sau khi trừ fee nền tảng
				var ownerNetPayout = ownerGrossShare * (1 - feePercent);
				if (ownerNetPayout <= 0) continue;

				if (ownerPayouts.ContainsKey(ownerId))
					ownerPayouts[ownerId] += ownerNetPayout;
				else
					ownerPayouts[ownerId] = ownerNetPayout;
			}

			// Credit ví cho từng owner
			foreach (var kv in ownerPayouts)
			{
				var ownerId = kv.Key;
				var amount = decimal.Round(kv.Value, 0); // làm tròn VND

				if (amount <= 0) continue;

				var txReq = new WalletTransactionRequest
				{
					Amount = amount,
					Type = "booking_payout",
					PaymentId = null,
					BookingId = booking.Id,
					Description = $"Payout tiền thuê booking {booking.BookingCode ?? booking.Id.ToString()}"
				};

				await _walletService.CreditAsync(ownerId, txReq);
			}

			// ============ 4. Hoa hồng branch manager / admin ============

			// Mặc định trả hoa hồng = tổng fee nền tảng
			if (platformFeeTotal > 0)
			{
				Guid? commissionReceiverId = null;

				// 1) Ưu tiên Manager của Branch (nếu đã include)
				if (booking.Branch != null && booking.Branch.ManagerId.HasValue)
				{
					commissionReceiverId = booking.Branch.ManagerId.Value;
				}
				// 2) Nếu Branch chưa include nhưng có BranchId -> load từ DB
				else if (booking.BranchId.HasValue)
				{
					var branch = await _unitOfWork.Repository<Branch>().GetByIdAsync(booking.BranchId.Value);
					if (branch != null && branch.ManagerId.HasValue)
					{
						commissionReceiverId = branch.ManagerId.Value;
					}
				}

				// 3) Nếu vẫn chưa có -> tìm 1 user Admin
				if (!commissionReceiverId.HasValue)
				{
					var users = await _unitOfWork.Repository<User>()
						.ListAsync(include: u => u.Include(u => u.Roles));

					var adminUser = users.FirstOrDefault(u => u.Roles.Any(r => r.Role == UserRole.Admin));
					if (adminUser != null)
					{
						commissionReceiverId = adminUser.Id;
					}
				}

				// 4) Nếu vẫn không tìm ra ai thì thôi, không ghi commission (hoặc bạn có thể throw exception tuỳ business)
				if (!commissionReceiverId.HasValue)
				{
					// Option A: bỏ qua hoa hồng
					// return await _unitOfWork.Complete();

					// Option B: ném lỗi để biết là cấu hình sai
					throw new InvalidOperationException("Cannot determine commission receiver (no branch manager or admin).");
				}

				var commissionReq = new WalletTransactionRequest
				{
					Amount = platformFeeTotal,
					Type = "booking_commission",
					PaymentId = null,
					BookingId = booking.Id,
					Description = $"Hoa hồng booking {booking.BookingCode ?? booking.Id.ToString()}"
				};

				await _walletService.CreditAsync(commissionReceiverId.Value, commissionReq);
			}

			// Đánh dấu đã settle để không chạy lại lần nữa
			booking.IsSettled = true;
			booking.SettledAt = DateTime.UtcNow;
		}
		// Added: update booking status implementation
		private async Task OnCancelled(Booking booking)
		{
			var setting = await _unitOfWork.Repository<MoneyFlatformSetting>()
				.FirstOrDefaultAsync(m => m.IsActive);

			var cancelTime = setting?.CancelTime ?? TimeSpan.Zero;

			// booking.PickupAt nên là UTC (nếu đang local thì ToUniversalTime)
			var nowUtc = DateTime.UtcNow;
			var pickupUtc = booking.PickupAt.Kind == DateTimeKind.Utc
				? booking.PickupAt
				: booking.PickupAt.ToUniversalTime();

			var latestCancelUtc = pickupUtc - cancelTime;

			if (nowUtc > latestCancelUtc)
				throw new InvalidOperationException($"Chỉ được hủy trước {cancelTime.TotalHours:0} giờ so với thời điểm nhận hàng.");

			// Lấy tất cả payment đã captured của booking
			var payments = await _unitOfWork.Repository<Payment>()
				.ListAsync(p => p.BookingId == booking.Id && p.Status == PaymentStatus.Captured);

			var refundAmount = payments.Sum(p => p.CapturedAmount);
			if (refundAmount <= 0) return;

			// Credit ví renter
			if (!booking.RenterId.HasValue)
				throw new InvalidOperationException("Booking missing renter.");

			await _walletService.CreditAsync(booking.RenterId.Value, new WalletTransactionRequest
			{
				Amount = refundAmount,
				Type = "booking_refund",
				BookingId = booking.Id,
				PaymentId = null,
				Description = $"Hoàn tiền {booking.BookingCode ?? booking.Id.ToString()}"
			});

			// Update payment status để audit (tuỳ enum bạn có)
			foreach (var p in payments.ToList())
			{
				p.Status = PaymentStatus.Refunded;          // nếu có
				p.RefundedAmount = p.CapturedAmount;       // nếu có
				await _unitOfWork.Repository<Payment>().UpdateAsync(p);
			}
		}
		private async Task OnCompleted(Booking booking)
		{
			if (!booking.IsSettled)
				await SettleBooking(booking);
		}
		private static void ValidateTransition(BookingStatus from, BookingStatus to)
		{
			// Draft -> Confirmed/Cancelled
			// Confirmed -> PickedUp/Cancelled
			// PickedUp -> Returned/Overdue
			// Returned -> Completed
			// Completed: terminal
			// Cancelled: terminal

			var ok = (from, to) switch
			{
				(BookingStatus.Draft, BookingStatus.Confirmed) => true,
				(BookingStatus.Draft, BookingStatus.Cancelled) => true,

				(BookingStatus.Confirmed, BookingStatus.PickedUp) => true,
				(BookingStatus.Confirmed, BookingStatus.Cancelled) => true,

				(BookingStatus.PickedUp, BookingStatus.Returned) => true,
				(BookingStatus.PickedUp, BookingStatus.Overdue) => true,

				(BookingStatus.Returned, BookingStatus.Completed) => true,

				// cho phép set Overdue -> Returned (nếu trả muộn)
				(BookingStatus.Overdue, BookingStatus.Returned) => true,

				_ => false
			};

			if (!ok)
				throw new InvalidOperationException($"Invalid status transition: {from} -> {to}");
		}
		public async Task<int> UpdateBookingStatusAsync(Guid bookingId, BookingStatus newStatus)
		{
			var bookingRepo = _unitOfWork.Repository<Booking>();

			// ✅ Nên load đủ data cần cho các handler (Items/Branch/Payments...)
			var booking = await bookingRepo.FirstOrDefaultAsync(b => b.Id == bookingId);
			if (booking == null) return 0;

			var oldStatus = booking.Status;

			// (optional) validate chuyển trạng thái hợp lệ
			ValidateTransition(oldStatus, newStatus);

			// Apply status
			booking.Status = newStatus;

			// Dispatch theo status
			switch (newStatus)
			{

				case BookingStatus.Completed:
					await OnCompleted(booking); // settle booking
					break;

				case BookingStatus.Cancelled:
					await OnCancelled(booking); // refund về ví theo CancelTime
					break;

			}
			await bookingRepo.UpdateAsync(booking);
			return await _unitOfWork.Complete();
		}

		public async Task<BookingQrDTO?> GenerateBookingQrForRenterAsync(Guid bookingId, Guid renterId, CancellationToken ct = default)
		{
			// Chỉ cho phép renter lấy QR của chính booking của mình
			var booking = await _unitOfWork.Repository<Booking>()
				.FirstOrDefaultAsync(b => b.Id == bookingId && b.RenterId == renterId);

			if (booking == null)
				return null;

			var payload = $"booking:{bookingId:N}";
			var png = GenerateQrPng(payload);

			return new BookingQrDTO
			{
				BookingId = bookingId,
				Payload = payload,
				PngImage = png
			};
		}

		private static byte[] GenerateQrPng(string text)
		{
			using var generator = new QRCodeGenerator();
			using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
			var pngCode = new PngByteQRCode(data);
			return pngCode.GetGraphic(6);
		}
	}
}
