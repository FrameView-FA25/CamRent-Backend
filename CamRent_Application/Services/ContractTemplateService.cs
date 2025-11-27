using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static CamRent_Application.DTOs.BookingDTO;

namespace CamRent_Application.Services
{
	public class ContractTemplateService : IContractTemplateService
	{
		public IMapper _mapper;
		public ContractTemplateService(IMapper mapper)
		{
			_mapper = mapper;
		}
		public async Task<byte[]> RenderBookingContractAsync(Contract contract)
		{
			// Nếu bạn có Booking Include sẵn thông tin thì không cần fetch lại
			var booking = contract.Booking!;
			var renter = booking.Renter!;
			var branch = booking.Branch!;
			var items = _mapper.Map<List<BookingItemDTO>>(booking.Items);

			return await Task.Run(() =>
			{
				var document = Document.Create(container =>
				{
					container.Page(page =>
					{
						page.Margin(40);
						page.Size(PageSizes.A4);
						page.DefaultTextStyle(x => x.FontSize(11));

						page.Header().Column(col =>
						{
							col.Item().Text("HỢP ĐỒNG THUÊ THIẾT BỊ CAMRENT")
								.FontSize(18).Bold().AlignCenter();

							col.Item().Text($"Mã Hợp Đồng: {contract.Id}")
								.FontSize(10).AlignCenter();
						});

						page.Content().Column(col =>
						{
							col.Spacing(10);

							// ---- Thông tin renter ----
							col.Item().Text("1. Thông tin Bên thuê (Renter)").Bold();
							col.Item().Text($"• Họ và tên: {renter.FullName}");
							col.Item().Text($"• Số điện thoại: {renter.Phone}");
							col.Item().Text($"• Email: {renter.Email}");

							// ---- Thông tin chi nhánh ----
							col.Item().Text("2. Thông tin Chi nhánh CamRent").Bold();
							col.Item().Text($"• Tên chi nhánh: {branch.Name}");
							col.Item().Text($"• Địa chỉ: {branch.Address}");

							// ---- Thông tin Booking ----
							col.Item().Text("3. Thông tin Booking").Bold();
							col.Item().Text($"• Ngày thuê: {booking.PickupAt:dd/MM/yyyy}");
							col.Item().Text($"• Ngày trả: {booking.ReturnAt:dd/MM/yyyy}");
							if (booking.Location != null)
								col.Item().Text($"• Địa chỉ giao hàng: {booking.Location.Province + "," + booking.Location.District}");

							// ---- Danh sách thiết bị ----
							col.Item().Text("4. Thiết bị thuê").Bold();

							col.Item().Table(table =>
							{
								table.ColumnsDefinition(columns =>
								{
									columns.ConstantColumn(25);
									columns.RelativeColumn();
									columns.RelativeColumn();
									columns.RelativeColumn();
								});

								// Header
								table.Header(header =>
								{
									header.Cell().Text("#").Bold();
									header.Cell().Text("Thiết bị").Bold();
									header.Cell().Text("Giá thuê").Bold();
									header.Cell().Text("Đặt cọc").Bold();
								});

								int index = 1;
								foreach (var item in items)
								{
									table.Cell().Text(index++.ToString());
									table.Cell().Text(item.ItemName);
									table.Cell().Text($"{item.UnitPrice:N0} đ");
									table.Cell().Text($"{item.DepositAmount:N0} đ");
								}
							});

							// ---- Tính tiền ----
							var totalRental = booking.SnapshotRentalTotal ;
							var totalDeposit = booking.SnapshotDepositAmount;
							var final = totalRental + totalDeposit;
							var depositBooking = booking.SnapshotRentalTotal * booking.SnapshotPlatformFeePercent;

							col.Item().Text("5. Tổng tiền thanh toán").Bold();
							col.Item().Text($"• Tổng giá thuê: {totalRental:N0} đ");
							col.Item().Text($"• Tổng đặt cọc của những thiết bị: {totalDeposit:N0} đ");
							col.Item().Text($"• Tổng phải trả: {final:N0} đ");
							col.Item().Text($"• Đặt cọc đơn hàng: {totalDeposit:N0} đ");

							// ---- Điều khoản ----
							col.Item().Text("6. Điều khoản chung").Bold();

							col.Item().Text(
								"• Hàng sẽ được giao trước 12 giờ của ngày thuê nếu chọn giao hàng.\n" +
								"• Trả hàng trước 16h của ngày trả.\n" +
								"• Bên thuê cam kết giữ gìn và trả lại thiết bị đúng thời gian.\n" +
								"• Mọi hư hỏng sẽ được bồi thường theo giá trị thiết bị."
							);

							// ---- chữ ký ----
							col.Item().Row(row =>
							{
								row.RelativeItem().Column(c =>
								{
									c.Item().Text("BÊN THUÊ (Renter)").Bold();

									var sig = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Renter)?.SignatureAsset?.Url;

									if (!string.IsNullOrEmpty(sig))
										c.Item().Image(sig, ImageScaling.FitWidth);
									else
										c.Item().Text("\n(Ký và ghi rõ họ tên)\n");
								});

								row.RelativeItem().Column(c =>
								{
									c.Item().Text("ĐẠI DIỆN CAMRENT").Bold();

									var sig = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Platform)?.SignatureAsset?.Url;

									if (!string.IsNullOrEmpty(sig))
										c.Item().Image(sig, ImageScaling.FitWidth);
									else
										c.Item().Text("\n(Ký và ghi rõ họ tên)\n");
								});
							});
						});

						page.Footer()
							.AlignCenter()
							.Text($"Ngày in: {DateTime.Now:dd/MM/yyyy}");
					});
				});

				return document.GeneratePdf();
			});
		}

		// --- Template Verification tương tự ---
		public async Task<byte[]> RenderVerificationContractAsync(Contract contract)
		{
			// Giả sử bạn có navigation:
			// contract.Owner: User (Owner thiết bị)
			// contract.Branch: Branch (nếu cần gắn với chi nhánh quản lý)
			var verification = contract.Verification;
			var owner = verification.Owner;// đổi tên cho đúng với entity của bạn
			var branch = contract.Branch;     // nếu có, không bắt buộc

			return await Task.Run(() =>
			{
				var document = Document.Create(container =>
				{
					container.Page(page =>
					{
						page.Margin(40);
						page.Size(PageSizes.A4);
						page.DefaultTextStyle(x => x.FontSize(11));

						page.Header().Column(col =>
						{
							col.Item().Text("HỢP ĐỒNG HỢP TÁC CHO THUÊ THIẾT BỊ")
								.FontSize(18).Bold().AlignCenter();

							col.Item().Text($"Mã Hợp Đồng: {contract.Id}")
								.FontSize(10).AlignCenter();
						});

						page.Content().Column(col =>
						{
							col.Spacing(10);

							// 1. Bên A - Chủ sở hữu thiết bị
							col.Item().Text("1. Thông tin Bên A - Chủ sở hữu thiết bị (Owner)").Bold();
							col.Item().Text($"• Họ và tên: {owner.FullName}");
							col.Item().Text($"• Số điện thoại: {owner.Phone}");
							col.Item().Text($"• Email: {owner.Email}");
							if (owner.Address != null)
								col.Item().Text($"• Địa chỉ: {owner.Address.Province + "," + owner.Address.District}");

							// 2. Bên B - CamRent
							col.Item().Text("2. Thông tin Bên B - Sàn CamRent").Bold();
							col.Item().Text("• Tên doanh nghiệp: CamRent Platform");
							if (branch != null)
							{
								col.Item().Text($"• Chi nhánh quản lý: {branch.Name}");
								col.Item().Text($"• Địa chỉ chi nhánh: {branch.Address}");
							}
							col.Item().Text("• Vai trò: Sàn trung gian kết nối chủ thiết bị và người thuê, cung cấp nền tảng quản lý & vận hành.");

							// 3. Mục đích hợp đồng
							col.Item().Text("3. Mục đích hợp đồng").Bold();
							col.Item().Text(
								"Bên A đồng ý ủy quyền cho Bên B (CamRent) được phép quản lý, niêm yết và cho thuê các thiết bị " +
								"thuộc sở hữu của Bên A trên nền tảng CamRent, nhằm mục đích khai thác thương mại và chia sẻ doanh thu."
							);

							// 4. Thời hạn hợp đồng (tối thiểu 6 tháng)
							col.Item().Text("4. Thời hạn hợp đồng").Bold();
							col.Item().Text(
								"• Thời hạn tối thiểu của Hợp đồng là 06 (sáu) tháng kể từ ngày ký.\n" +
								"• Sau thời hạn tối thiểu, Hợp đồng tự động gia hạn theo chu kỳ 03 (ba) tháng " +
								"trừ khi một trong hai bên gửi yêu cầu chấm dứt bằng văn bản hoặc qua hệ thống CamRent trước ít nhất 15 ngày.\n" +
								"• Trong thời hạn tối thiểu 06 tháng, việc đơn phương chấm dứt Hợp đồng phải tuân thủ các điều khoản phạt xử lý do hai bên thỏa thuận."
							);

							// 5. Thiết bị áp dụng
							col.Item().Text("5. Thiết bị áp dụng").Bold();
							col.Item().Text(
								"Danh sách thiết bị cụ thể do Bên A cung cấp sẽ được quản lý trên hệ thống CamRent, " +
								"bao gồm thông tin chi tiết: loại thiết bị, mã thiết bị, tình trạng, giá trị ước tính, giá thuê đề xuất, mức đặt cọc, vv.\n" +
								"Mọi thay đổi, thêm/bớt thiết bị sẽ được ghi nhận trong lịch sử trên hệ thống và được xem là phụ lục của Hợp đồng này."
							);

							// 6. Doanh thu và chiết khấu
							col.Item().Text("6. Doanh thu và chiết khấu").Bold();
							col.Item().Text(
								"• Bên B có trách nhiệm thu hộ tiền thuê và tiền đặt cọc từ người thuê.\n" +
								"• Sau khi hoàn tất mỗi kỳ thuê và đối soát, Bên B sẽ thanh toán lại cho Bên A phần doanh thu thuộc về Bên A " +
								"sau khi đã trừ các khoản chiết khấu, phí nền tảng, chi phí xử lý phát sinh (nếu có) theo tỷ lệ đã thống nhất trên hệ thống CamRent.\n" +
								"• Chi tiết tỷ lệ chia doanh thu được hiển thị trong cấu hình tài khoản Owner trên nền tảng CamRent và có thể thay đổi " +
								"khi hai bên cùng chấp thuận."
							);

							// 7. Trách nhiệm Bên A
							col.Item().Text("7. Trách nhiệm của Bên A (Owner)").Bold();
							col.Item().Text(
								"• Cung cấp thông tin thiết bị chính xác, trung thực về nguồn gốc, tình trạng và giá trị.\n" +
								"• Đảm bảo thiết bị không vướng tranh chấp pháp lý, không phải tài sản cầm cố, thế chấp.\n" +
								"• Phối hợp với Bên B trong quá trình giao - nhận - bảo trì thiết bị.\n" +
								"• Thông báo kịp thời cho Bên B nếu có thay đổi quan trọng liên quan đến thiết bị."
							);

							// 8. Trách nhiệm Bên B
							col.Item().Text("8. Trách nhiệm của Bên B (CamRent)").Bold();
							col.Item().Text(
								"• Quản lý thông tin thiết bị của Bên A trên nền tảng CamRent.\n" +
								"• Thực hiện quy trình cho thuê, thu hộ tiền thuê và tiền đặt cọc từ người thuê.\n" +
								"• Hỗ trợ giải quyết các vấn đề phát sinh giữa người thuê và Bên A theo quy định, chính sách của CamRent.\n" +
								"• Đảm bảo minh bạch trong việc thống kê, đối soát doanh thu và thanh toán cho Bên A."
							);

							// 9. Xử lý hư hỏng, mất mát
							col.Item().Text("9. Xử lý hư hỏng, mất mát thiết bị").Bold();
							col.Item().Text(
								"• Trường hợp thiết bị bị hư hỏng, mất mát trong thời gian cho thuê, CamRent sẽ hỗ trợ thu thập thông tin và " +
								"làm việc với người thuê theo quy trình bồi thường.\n" +
								"• Mức bồi thường, khấu trừ từ tiền đặt cọc hoặc các phương thức khác được áp dụng theo chính sách CamRent " +
								"và thỏa thuận với Bên A.\n" +
								"• CamRent không chịu trách nhiệm cho các hao mòn tự nhiên của thiết bị do quá trình sử dụng bình thường."
							);

							// 10. Điều khoản chung & giải quyết tranh chấp
							col.Item().Text("10. Điều khoản chung và giải quyết tranh chấp").Bold();
							col.Item().Text(
								"• Hai bên cam kết thực hiện đúng các thỏa thuận trong Hợp đồng.\n" +
								"• Mọi sửa đổi, bổ sung Hợp đồng phải được thực hiện thông qua văn bản hoặc xác nhận trên hệ thống CamRent.\n" +
								"• Trường hợp phát sinh tranh chấp, hai bên ưu tiên giải quyết bằng thương lượng. " +
								"Nếu không đạt được thỏa thuận, tranh chấp sẽ được đưa ra cơ quan có thẩm quyền theo quy định pháp luật."
							);

							// 11. Chữ ký
							col.Item().Row(row =>
							{
								row.RelativeItem().Column(c =>
								{
									c.Item().Text("BÊN A - CHỦ SỞ HỮU THIẾT BỊ (Owner)").Bold();

									var sig = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Owner)
										?.SignatureAsset?.Url;

									if (!string.IsNullOrEmpty(sig))
										c.Item().Image(sig, ImageScaling.FitWidth);
									else
										c.Item().Text("\n(Ký và ghi rõ họ tên)\n");
								});

								row.RelativeItem().Column(c =>
								{
									c.Item().Text("BÊN B - ĐẠI DIỆN CAMRENT").Bold();

									var sig = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Platform)
										?.SignatureAsset?.Url;

									if (!string.IsNullOrEmpty(sig))
										c.Item().Image(sig, ImageScaling.FitWidth);
									else
										c.Item().Text("\n(Ký và ghi rõ họ tên)\n");
								});
							});
						});

						page.Footer()
							.AlignCenter()
							.Text($"Ngày in: {DateTime.Now:dd/MM/yyyy}");
					});
				});

				return document.GeneratePdf();
			});
		}
	}
}
