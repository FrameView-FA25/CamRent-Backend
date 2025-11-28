using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.VerificationRequestDTO;

namespace CamRent_Application.Services
{
	public class ContractTemplateService : IContractTemplateService
	{
		public IMapper _mapper;
		public ContractTemplateService(IMapper mapper)
		{
			_mapper = mapper;
		}

		// ====================== BOOKING CONTRACT ======================
		public async Task<byte[]> RenderBookingContractAsync(Contract contract)
		{
			var booking = contract.Booking!;
			var renter = booking.Renter!;
			var branch = contract.Branch!;
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

						page.Content().Column(col =>
						{
							col.Spacing(10);

							// ========== HEADER CHỈ TRANG 1 ==========
							col.Item().AlignCenter().Text("HỢP ĐỒNG THUÊ THIẾT BỊ CAMRENT")
								.FontSize(18).Bold();

							col.Item().AlignCenter().Text($"Mã Hợp Đồng: {contract.Id}")
								.FontSize(10);

							col.Item().Text(""); // khoảng trống nhẹ sau header

							// ---- Thông tin renter ----
							col.Item().Text("1. Thông tin Bên thuê (Renter)").Bold();
							col.Item().Text($"• Họ và tên: {renter.FullName}");
							col.Item().Text($"• Số điện thoại: {renter.Phone}");
							col.Item().Text($"• Email: {renter.Email}");

							// ---- Thông tin chi nhánh ----
							col.Item().Text("2. Thông tin Chi nhánh CamRent").Bold();
							col.Item().Text($"• Tên chi nhánh: {branch.Name}");
							if(branch.Address != null)
							{
								var addr = branch.Address;
								col.Item().Text($"• Địa chỉ chi nhánh: {addr.Province}, {addr.District}");
							}	
								

							// ---- Thông tin Booking ----
							col.Item().Text("3. Thông tin Booking").Bold();
							col.Item().Text($"• Ngày thuê: {booking.PickupAt:dd/MM/yyyy}");
							col.Item().Text($"• Ngày trả: {booking.ReturnAt:dd/MM/yyyy}");
							if (booking.Location != null)
								col.Item().Text($"• Địa chỉ giao hàng: {booking.Location.Province}, {booking.Location.District}");

							// ---- Danh sách thiết bị ----
							col.Item().Text("4. Thiết bị thuê").Bold();

							col.Item().Table(table =>
							{
								table.ColumnsDefinition(columns =>
								{
									columns.ConstantColumn(25); // #
									columns.RelativeColumn(4);  // Thiết bị
									columns.RelativeColumn(3);  // Giá thuê
									columns.RelativeColumn(3);  // Đặt cọc
								});

								// Helper vẽ cell có border
								static void HeaderCell(IContainer container, string text)
								{
									container
										.Border(0.5f)
										.Background(Colors.Grey.Lighten3)
										.Padding(3)
										.Text(text).Bold();
								}

								static void BodyCell(IContainer container, string text)
								{
									container
										.Border(0.5f)
										.Padding(3)
										.Text(text);
								}

								table.Header(header =>
								{
									header.Cell().Element(c => HeaderCell(c, "#"));
									header.Cell().Element(c => HeaderCell(c, "Thiết bị"));
									header.Cell().Element(c => HeaderCell(c, "Giá thuê"));
									header.Cell().Element(c => HeaderCell(c, "Đặt cọc"));
								});

								int index = 1;
								foreach (var item in items)
								{
									table.Cell().Element(c => BodyCell(c, index++.ToString()));
									table.Cell().Element(c => BodyCell(c, item.ItemName));
									table.Cell().Element(c => BodyCell(c, $"{item.UnitPrice:N0} đ"));
									table.Cell().Element(c => BodyCell(c, $"{item.DepositAmount:N0} đ"));
								}
							});

							// ---- Tính tiền ----
							var totalRental = booking.SnapshotRentalTotal;
							var totalDeposit = booking.SnapshotDepositAmount;
							var final = totalRental + totalDeposit;

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
								"• Mọi hư hỏng sẽ được bồi thường theo giá trị thiết bị.\n\n"
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

		// ================== VERIFICATION CONTRACT ==================
		public async Task<byte[]> RenderVerificationContractAsync(Contract contract)
		{
			var verification = contract.Verification
							  ?? throw new Exception("VerificationRequest is null on Contract");
			var owner = verification.Owner
						?? throw new Exception("Owner is null on VerificationRequest");
			var branch = verification.Branch;
			var items = _mapper.Map<List<VerificationItemDTO>>(verification.Items);

			return await Task.Run(() =>
			{
				var document = Document.Create(container =>
				{
					container.Page(page =>
					{
						page.Margin(40);
						page.Size(PageSizes.A4);
						page.DefaultTextStyle(x => x.FontSize(11));

						page.Content().Column(col =>
						{
							col.Spacing(10);

							// ========== HEADER CHỈ TRANG 1 ==========
							col.Item().AlignCenter().Text("HỢP ĐỒNG HỢP TÁC CHO THUÊ THIẾT BỊ")
								.FontSize(18).Bold();

							col.Item().AlignCenter().Text($"Mã Hợp Đồng: {contract.Id}")
								.FontSize(10);

							col.Item().Text(""); // khoảng trống sau header

							// 1. Bên A - Owner
							col.Item().Text("1. Thông tin Bên A - Chủ sở hữu thiết bị (Owner)").Bold();
							col.Item().Text($"• Họ và tên: {owner.FullName}");
							col.Item().Text($"• Số điện thoại: {owner.Phone}");
							col.Item().Text($"• Email: {owner.Email}");
							if (owner.Address != null)
							{
								var addr = owner.Address;
								col.Item().Text($"• Địa chỉ: {addr.Province}, {addr.District}");
							}

							// 2. Bên B - CamRent
							col.Item().Text("2. Thông tin Bên B - Sàn CamRent").Bold();
							col.Item().Text("• Tên doanh nghiệp: CamRent");
							if (branch != null)
							{
								col.Item().Text($"• Chi nhánh quản lý: {branch.Name}");
								var addr = branch.Address;
								col.Item().Text($"• Địa chỉ chi nhánh: {addr.Province}, {addr.District}");
							}
							col.Item().Text("• Vai trò: Sàn trung gian kết nối chủ thiết bị và người thuê, cung cấp nền tảng quản lý & vận hành.");

							// 3. Mục đích
							col.Item().Text("3. Mục đích hợp đồng").Bold();
							col.Item().Text(
								"Bên A đồng ý ủy quyền cho Bên B (CamRent) được phép quản lý, niêm yết và cho thuê các thiết bị " +
								"thuộc sở hữu của Bên A trên nền tảng CamRent, nhằm mục đích khai thác thương mại và chia sẻ doanh thu."
							);

							// 4. Thời hạn
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

							if (items.Any())
							{
								col.Item().Table(table =>
								{
									table.ColumnsDefinition(columns =>
									{
										columns.ConstantColumn(25);   // #
										columns.RelativeColumn(4);    // Thiết bị
										columns.RelativeColumn(3);    // Loại
									});

									static void HeaderCell(IContainer container, string text)
									{
										container
											.Border(0.5f)
											.Background(Colors.Grey.Lighten3)
											.Padding(3)
											.Text(text).Bold();
									}

									static void BodyCell(IContainer container, string text)
									{
										container
											.Border(0.5f)
											.Padding(3)
											.Text(text);
									}

									table.Header(header =>
									{
										header.Cell().Element(c => HeaderCell(c, "#"));
										header.Cell().Element(c => HeaderCell(c, "Thiết bị"));
										header.Cell().Element(c => HeaderCell(c, "Loại"));
									});

									int index = 1;
									foreach (var item in items)
									{
										table.Cell().Element(c => BodyCell(c, index++.ToString()));
										table.Cell().Element(c => BodyCell(c, item.ItemName));
										table.Cell().Element(c => BodyCell(c, item.ItemType.ToString()));
									}
								});
							}
							else
							{
								col.Item().Text("• (Chưa có thiết bị nào được khai báo trong yêu cầu verification này.)");
							}

							// 6. Doanh thu & chiết khấu
							col.Item().Text("6. Doanh thu và chiết khấu").Bold();
							col.Item().Text(
								"• Bên B có trách nhiệm thu hộ tiền thuê và tiền đặt cọc từ người thuê.\n" +
								"• Sau khi hoàn tất mỗi kỳ thuê và đối soát, Bên B sẽ thanh toán lại cho Bên A phần doanh thu thuộc về Bên A " +
								"sau khi đã trừ các khoản chiết khấu, phí nền tảng, chi phí xử lý phát sinh (nếu có) theo tỷ lệ đã thống nhất trên hệ thống CamRent.\n" +
								"• Chi tiết tỷ lệ chia doanh thu được hiển thị trong cấu hình tài khoản Owner trên nền tảng CamRent và có thể thay đổi " +
								"khi hai bên cùng chấp thuận.\n"
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

							// 9. Hư hỏng/mất mát
							col.Item().Text("9. Xử lý hư hỏng, mất mát thiết bị").Bold();
							col.Item().Text(
								"• Trường hợp thiết bị bị hư hỏng, mất mát trong thời gian cho thuê, CamRent sẽ hỗ trợ thu thập thông tin và " +
								"làm việc với người thuê theo quy trình bồi thường.\n" +
								"• Mức bồi thường, khấu trừ từ tiền đặt cọc hoặc các phương thức khác được áp dụng theo chính sách CamRent " +
								"và thỏa thuận với Bên A.\n" +
								"• CamRent không chịu trách nhiệm cho các hao mòn tự nhiên của thiết bị do quá trình sử dụng bình thường."
							);

							// 10. Điều khoản chung
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
