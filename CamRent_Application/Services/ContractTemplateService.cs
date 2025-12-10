using AutoMapper;
using CamRent_Application.IServices;
using CamRent_Domain.Common;
using CamRent_Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Http;
using static CamRent_Application.DTOs.BookingDTO;
using static CamRent_Application.DTOs.VerificationRequestDTO;

namespace CamRent_Application.Services
{
	public class ContractTemplateService : IContractTemplateService
	{
		private readonly IMapper _mapper;

		// Màu sắc theo style hợp đồng “formal”
		private static readonly string PrimaryColor = Colors.Grey.Darken4;  // tiêu đề lớn
		private static readonly string SectionTitleColor = Colors.Grey.Darken3;  // heading mục
		private static readonly string BoxBorderColor = Colors.Grey.Darken2;  // viền box
		private static readonly string DangerColor = Colors.Red.Darken2;   // cảnh báo

		public ContractTemplateService(IMapper mapper)
		{
			_mapper = mapper;
		}

		#region SignatureBlock helper

		private static void SignatureBlock(
			IContainer container,
			string title,
			string signerName,
			string? signatureUrl)
		{
			container.Column(col =>
			{
				col.Spacing(5);

				col.Item().AlignCenter().Text(title).Bold();
				col.Item().AlignCenter().Text("(Ký và ghi rõ họ tên)");

				col.Item()
					.MinHeight(120)         // chỗ trống để ký
					.Border(0.5f)
					.Padding(5)
					.AlignCenter()
					.Element(e =>
					{
						if (!string.IsNullOrEmpty(signatureUrl))
						{
							try
							{
								using var http = new HttpClient();
								var bytes = http
									.GetByteArrayAsync(signatureUrl)
									.GetAwaiter()
									.GetResult();

								e.Image(bytes, ImageScaling.FitWidth);
							}
							catch
							{
								// Nếu lỗi (mạng, URL sai...) thì để khung trống
								e.Text(string.Empty);
							}
						}
						else
						{
							e.Text(string.Empty);
						}
					});

				col.Item().AlignCenter().Text(signerName);
			});
		}

		#endregion

		// ====================== BOOKING CONTRACT ======================
		public async Task<byte[]> RenderBookingContractAsync(Contract contract)
		{
			var booking = contract.Booking!;
			var renter = booking.Renter!;
			var branch = contract.Branch!;
			var items = _mapper.Map<List<BookingItemDTO>>(booking.Items);

			// Tính số ngày thuê (ít nhất 1 ngày)
			var rentalDays = Math.Max(1,
				(booking.ReturnAt.Date - booking.PickupAt.Date).Days);

			var totalRental = booking.SnapshotRentalTotal;      // tổng tiền thuê
			var totalDeposit = booking.SnapshotDepositAmount;    // tổng tiền cọc
			var grandTotal = totalRental + totalDeposit;

			// Dòng tiền
			var upfrontPercent = 0.10m;
			var upfrontRental = totalRental * upfrontPercent;   // 10% trả trước
			var remainingRental = totalRental - upfrontRental;    // 90% còn lại
			var payOnPickup = remainingRental + totalDeposit; // khi nhận máy
			var upfrontPercentText = $"{upfrontPercent * 100:0}%";

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

							// ========== HEADER ==========
							col.Item().AlignCenter()
								.Text("HỢP ĐỒNG THUÊ THIẾT BỊ CAMRENT")
								.FontSize(18).Bold()
								.FontColor(PrimaryColor);

							col.Item().AlignCenter()
								.Text($"Mã Hợp Đồng: {contract.Id}")
								.FontSize(10);

							col.Item().AlignRight()
								.Text($"Ngày in: {DateTime.Now:dd/MM/yyyy}")
								.FontSize(9);

							col.Item()
							   .LineHorizontal(0.5f)
							   .LineColor(Colors.Grey.Lighten1);

							// I. Thông tin bên thuê & chi nhánh
							col.Item().Text("1. Thông tin Bên thuê (Renter)")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text($"• Họ và tên: {renter.FullName}");
							col.Item().Text($"• Số điện thoại: {renter.Phone}");
							col.Item().Text($"• Email: {renter.Email}");

							col.Item().Text("2. Thông tin Chi nhánh CamRent")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text($"• Tên chi nhánh: {branch.Name}");
							if (branch.Address != null)
							{
								var addr = branch.Address;
								col.Item().Text($"• Địa chỉ chi nhánh: {addr.Province}, {addr.District}");
							}

							col.Item().Text("3. Thông tin Booking")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text($"• Ngày thuê: {booking.PickupAt:dd/MM/yyyy}");
							col.Item().Text($"• Ngày trả: {booking.ReturnAt:dd/MM/yyyy}");
							col.Item().Text($"• Số ngày thuê dự kiến: {rentalDays} ngày");
							if (booking.Location != null)
								col.Item().Text($"• Địa chỉ giao hàng (nếu chọn giao): {booking.Location.Province}, {booking.Location.District}");
							// Hiệu lực hợp đồng = từ thời điểm nhận máy đến thời điểm trả máy theo booking
							col.Item().Text(
								$"• Thời hạn hiệu lực hợp đồng: từ {booking.PickupAt:dd/MM/yyyy HH:mm} đến {booking.ReturnAt:dd/MM/yyyy HH:mm}");

							// II. Danh sách thiết bị & dòng tiền theo thiết bị
							col.Item().Text("4. Thiết bị thuê và dòng tiền")
								.Bold().FontColor(SectionTitleColor);

							col.Item().Border(0.5f)
								.BorderColor(BoxBorderColor)
								.Padding(5)
								.Table(table =>
								{
									table.ColumnsDefinition(columns =>
									{
										columns.ConstantColumn(20);    // #
										columns.RelativeColumn(3);     // Thiết bị
										columns.RelativeColumn(1.2f);  // Số ngày
										columns.RelativeColumn(2);     // Giá/ngày
										columns.RelativeColumn(2.2f);  // Tiền thuê
										columns.RelativeColumn(2);     // Tiền cọc
										columns.RelativeColumn(2);     // Trạng thái thiết bị
									});

									static void HeaderCell(IContainer container, string text)
									{
										container
											.Background(Colors.Grey.Lighten3)
											.Padding(3)
											.Text(text).Bold();
									}

									static void BodyCell(IContainer container, string text)
									{
										container
											.Padding(3)
											.Text(text);
									}

									table.Header(header =>
									{
										header.Cell().Element(c => HeaderCell(c, "#"));
										header.Cell().Element(c => HeaderCell(c, "Thiết bị"));
										header.Cell().Element(c => HeaderCell(c, "Số ngày"));
										header.Cell().Element(c => HeaderCell(c, "Giá/ngày"));
										header.Cell().Element(c => HeaderCell(c, "Tiền thuê"));
										header.Cell().Element(c => HeaderCell(c, "Tiền cọc"));
										header.Cell().Element(c => HeaderCell(c, "Trạng thái"));
									});

									int index = 1;
									foreach (var item in items)
									{
										var rentalLine = item.UnitPrice * rentalDays;

										table.Cell().Element(c => BodyCell(c, index++.ToString()));
										table.Cell().Element(c => BodyCell(c, item.ItemName));
										table.Cell().Element(c => BodyCell(c, rentalDays.ToString()));
										table.Cell().Element(c => BodyCell(c, $"{item.UnitPrice:N0} đ"));
										table.Cell().Element(c => BodyCell(c, $"{rentalLine:N0} đ"));
										table.Cell().Element(c => BodyCell(c, $"{item.DepositAmount:N0} đ"));
										// Trạng thái thiết bị: dựa vào IsConfirmed, nếu cần có thể chi tiết hơn theo Inspection
										var statusText = "Chưa xác minh";
										var entityItem = booking.Items.FirstOrDefault(bi => bi.CameraId == item.ItemId || bi.AccessoryId == item.ItemId || bi.ComboId == item.ItemId);
										if (entityItem?.Camera != null)
										{
											statusText = entityItem.Camera.IsConfirmed ? "Đã xác minh" : "Chưa xác minh";
										}
										else if (entityItem?.Accessory != null)
										{
											statusText = entityItem.Accessory.IsConfirmed ? "Đã xác minh" : "Chưa xác minh";
										}
										table.Cell().Element(c => BodyCell(c, statusText));
									}
								});

							// III. Dòng tiền tổng hợp
							col.Item().Text("5. Dòng tiền thanh toán")
								.Bold().FontColor(SectionTitleColor);

							col.Item().Border(0.5f)
								.BorderColor(BoxBorderColor)
								.Background(Colors.Grey.Lighten4)
								.Padding(8)
								.Table(table =>
								{
									table.ColumnsDefinition(columns =>
									{
										columns.RelativeColumn(2); // Mục
										columns.RelativeColumn(1); // Giá trị
									});

									static void HeaderCell(IContainer container, string text)
									{
										container
											.Background(Colors.Grey.Lighten3)
											.Padding(3)
											.Text(text).Bold();
									}

									static void BodyCell(IContainer container, string text)
									{
										container
											.Padding(3)
											.Text(text);
									}

									table.Header(header =>
									{
										header.Cell().Element(c => HeaderCell(c, "Hạng mục"));
										header.Cell().Element(c => HeaderCell(c, "Giá trị"));
									});

									table.Cell().Element(c => BodyCell(c, "Tổng giá thuê (chưa gồm cọc)"));
									table.Cell().Element(c => BodyCell(c, $"{totalRental:N0} đ"));

									table.Cell().Element(c => BodyCell(c, "Tổng tiền đặt cọc thiết bị"));
									table.Cell().Element(c => BodyCell(c, $"{totalDeposit:N0} đ"));

									table.Cell().Element(c => BodyCell(c, "Tổng giá trị cần thanh toán"));
									table.Cell().Element(c => BodyCell(c, $"{grandTotal:N0} đ"));

									table.Cell().Element(c => BodyCell(c,
										$"Thanh toán khi tạo Booking ({upfrontPercentText} × tổng giá thuê)"));
									table.Cell().Element(c => BodyCell(c, $"{upfrontRental:N0} đ"));

									table.Cell().Element(c => BodyCell(c,
										"Thanh toán khi nhận thiết bị (90% giá thuê + tổng cọc)"));
									table.Cell().Element(c => BodyCell(c, $"{payOnPickup:N0} đ"));
								});

							// Ghi chú quan trọng
							col.Item().PaddingVertical(8).Element(box =>
							{
								box.Border(0.75f)
								   .BorderColor(BoxBorderColor)
								   .Background(Colors.Grey.Lighten4)
								   .Padding(10)
								   .Column(note =>
								   {
									   note.Spacing(4);

									   note.Item().Text("LƯU Ý VỀ THANH TOÁN")
										   .Bold()
										   .FontSize(11)
										   .FontColor(SectionTitleColor);

									   note.Item().Text(text =>
									   {
										   text.Span("• Khách thanh toán trước ");
										   text.Span(upfrontPercentText).Bold();
										   text.Span(" tổng giá thuê khi tạo Booking.");
									   });

									   note.Item().Text(
										   "• Khi nhận thiết bị, khách thanh toán phần 90% còn lại cộng với toàn bộ tiền đặt cọc thiết bị.");

									   note.Item().Text(text =>
									   {
										   text.Span("• Trả trễ: ").Bold().FontColor(DangerColor);
										   text.Span("tiền thuê phát sinh mỗi ngày = số ngày trễ × 1.5 × đơn giá thuê theo ngày; phần ngày lẻ được làm tròn thành 1 ngày thuê.");
									   });
								   });
							});

							// IV. Điều khoản
							col.Item().Text("6. Điều khoản về sử dụng, trả hàng và bồi thường")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Bên thuê có trách nhiệm kiểm tra tình trạng thiết bị khi nhận, báo ngay cho CamRent nếu phát hiện lỗi.\n" +
								"• Trả hàng trước hoặc đúng 16h ngày trả theo Booking."
							);
							col.Item().Text(
								"• Nếu trả trễ, tiền thuê phát sinh mỗi ngày được tính bằng: số ngày trễ × 1.5 × đơn giá thuê theo ngày; " +
								"phần ngày lẻ được làm tròn thành 1 ngày thuê."
							);
							col.Item().Text(
								"• Trường hợp thiết bị hư hỏng, mất mát do lỗi của Bên thuê: CamRent cùng Bên thuê lập biên bản, chi phí bồi thường tối đa bằng giá trị thiết bị theo thoả thuận, sau khi trừ tiền đặt cọc đã thu. Phần cọc được ưu tiên dùng để trừ vào chi phí bồi thường."
							);
							col.Item().Text(
								"• Trường hợp CamRent giao hàng trễ hơn thời gian dự kiến do lỗi vận hành của CamRent, CamRent có trách nhiệm hỗ trợ điều chỉnh thời gian thuê hoặc có chính sách hỗ trợ/giảm trừ phù hợp theo quy định hiện hành của CamRent."
							);

							col.Item().Text("7. Điều khoản chung")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Hai bên cam kết cung cấp thông tin trung thực và thực hiện đúng các nội dung trong Hợp đồng.\n" +
								"• Mọi điều chỉnh, gia hạn, huỷ hoặc thay đổi Hợp đồng phải được thực hiện trên hệ thống CamRent hoặc bằng văn bản có xác nhận của hai bên.\n" +
								"• Trường hợp có tranh chấp, hai bên ưu tiên giải quyết bằng thương lượng. Nếu không đạt được thoả thuận, tranh chấp sẽ được đưa ra cơ quan có thẩm quyền theo quy định pháp luật."
							);

							// V. Chữ ký
							col.Item().Row(row =>
							{
								row.RelativeItem().Element(c =>
								{
									var renterSigUrl = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Renter)
										?.SignatureAsset?.Url;

									SignatureBlock(
										c,
										"BÊN THUÊ (Renter)",
										renter.FullName,
										renterSigUrl
									);
								});

								row.RelativeItem().Element(c =>
								{
									var platformSig = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Platform);

									var platformSigUrl = platformSig?.SignatureAsset?.Url;
									var platformName = platformSig?.User?.FullName ?? "Đại diện CamRent";

									SignatureBlock(
										c,
										"ĐẠI DIỆN CAMRENT",
										platformName,
										platformSigUrl
									);
								});
							});
						});

						page.Footer().Column(c => { });
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

							// HEADER
							col.Item().AlignCenter()
								.Text("HỢP ĐỒNG HỢP TÁC CHO THUÊ THIẾT BỊ")
								.FontSize(18).Bold()
								.FontColor(PrimaryColor);

							col.Item().AlignCenter()
								.Text($"Mã Hợp Đồng: {contract.Id}")
								.FontSize(10);

							col.Item().AlignRight()
								.Text($"Ngày in: {DateTime.Now:dd/MM/yyyy}")
								.FontSize(9);

							col.Item()
							   .LineHorizontal(0.5f)
							   .LineColor(Colors.Grey.Lighten1);

							// I. Thông tin các bên
							col.Item().Text("1. Thông tin Bên A - Chủ sở hữu thiết bị (Owner)")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text($"• Họ và tên: {owner.FullName}");
							col.Item().Text($"• Số điện thoại: {owner.Phone}");
							col.Item().Text($"• Email: {owner.Email}");
							if (owner.Address != null)
							{
								var addr = owner.Address;
								col.Item().Text($"• Địa chỉ: {addr.Province}, {addr.District}");
							}

							col.Item().Text("2. Thông tin Bên B - Sàn CamRent")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text("• Tên doanh nghiệp: CamRent");
							if (branch != null)
							{
								col.Item().Text($"• Chi nhánh quản lý: {branch.Name}");
								var addr = branch.Address;
								col.Item().Text($"• Địa chỉ chi nhánh: {addr.Province}, {addr.District}");
							}
							col.Item().Text("• Vai trò: Sàn trung gian kết nối chủ thiết bị và người thuê, cung cấp nền tảng quản lý & vận hành.");

							// II. Mục đích & phạm vi
							col.Item().Text("3. Mục đích và phạm vi hợp tác")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Bên A ủy quyền cho CamRent được phép quản lý, niêm yết và cho thuê các thiết bị thuộc sở hữu của Bên A trên nền tảng CamRent.\n" +
								"• CamRent hỗ trợ quy trình cho thuê, thu hộ tiền thuê và đặt cọc, quản lý giao nhận và hỗ trợ xử lý sự cố với người thuê."
							);

							// III. Thiết bị áp dụng
							col.Item().Text("4. Thiết bị áp dụng trong Hợp đồng")
								.Bold().FontColor(SectionTitleColor);

							// mô tả nằm ngoài box
							col.Item().Text(
								"Danh sách thiết bị cụ thể do Bên A cung cấp được quản lý trên hệ thống CamRent. " +
								"Mọi thay đổi, thêm/bớt thiết bị đều được ghi nhận trên hệ thống và được xem là phụ lục của Hợp đồng này."
							);

							if (items.Any())
							{
								// chỉ riêng bảng nằm trong khung
								col.Item().Border(0.5f)
									.BorderColor(BoxBorderColor)
									.Background(Colors.Grey.Lighten4)
									.Padding(8)
									.Table(table =>
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
												.Background(Colors.Grey.Lighten3)
												.Padding(3)
												.Text(text).Bold();
										}

										static void BodyCell(IContainer container, string text)
										{
											container
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


							// IV. Dòng tiền & chia sẻ doanh thu
							col.Item().Text("5. Dòng tiền và chia sẻ doanh thu")
								.Bold().FontColor(SectionTitleColor);

							col.Item().Border(0.5f)
								.BorderColor(BoxBorderColor)
								.Padding(8)
								.Column(block =>
								{
									block.Spacing(3);

									block.Item().Text(
										"• CamRent thu hộ từ người thuê: tiền thuê và tiền đặt cọc thiết bị theo từng Booking.");
									block.Item().Text(
										"• Sau khi trừ phí nền tảng và các chi phí xử lý phát sinh (nếu có) theo tỷ lệ đã cấu hình trong tài khoản Owner trên hệ thống CamRent, phần doanh thu còn lại được thanh toán cho Bên A theo chu kỳ đối soát do CamRent quy định."
									);
									block.Item().Text(
										"• Tỷ lệ chia sẻ doanh thu và phí nền tảng được hiển thị công khai trong phần cấu hình tài khoản Owner trên hệ thống CamRent và có hiệu lực khi Bên A chấp thuận. Mọi thay đổi tỷ lệ sẽ được lưu vết trên hệ thống."
									);
								});

							// V. Trách nhiệm các bên
							col.Item().Text("6. Trách nhiệm của Bên A (Owner)")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Cung cấp thông tin thiết bị trung thực về nguồn gốc, tình trạng và giá trị.\n" +
								"• Đảm bảo thiết bị không vướng tranh chấp pháp lý, không là tài sản đang cầm cố, thế chấp.\n" +
								"• Phối hợp với CamRent trong quá trình kiểm tra, bảo trì, và xử lý các sự cố phát sinh liên quan đến thiết bị."
							);

							col.Item().Text("7. Trách nhiệm của Bên B (CamRent)")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Quản lý thông tin thiết bị của Bên A trên nền tảng CamRent và đảm bảo hiển thị rõ ràng các thông tin quan trọng.\n" +
								"• Thực hiện quy trình cho thuê, thu hộ tiền thuê và tiền đặt cọc từ người thuê theo đúng quy trình vận hành.\n" +
								"• Đảm bảo minh bạch trong việc thống kê, đối soát doanh thu và thanh toán cho Bên A theo kỳ.\n" +
								"• Hỗ trợ giải quyết tranh chấp giữa người thuê và Bên A dựa trên dữ liệu giao dịch trên hệ thống."
							);

							// VI. Hư hỏng, mất mát & điều khoản chung
							col.Item().Text("8. Xử lý hư hỏng, mất mát thiết bị")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Khi xảy ra hư hỏng, mất mát trong thời gian cho thuê, CamRent hỗ trợ thu thập thông tin, làm việc với người thuê và áp dụng chính sách bồi thường theo quy định.\n" +
								"• Tiền đặt cọc được ưu tiên dùng để bù đắp thiệt hại cho Bên A. Nếu chi phí thực tế lớn hơn tiền đặt cọc, CamRent hỗ trợ tiếp tục làm việc với người thuê để thu thêm phần chênh lệch."
							);

							col.Item().Text("9. Điều khoản chung và giải quyết tranh chấp")
								.Bold().FontColor(SectionTitleColor);
							col.Item().Text(
								"• Hai bên cam kết tuân thủ các điều khoản trong Hợp đồng và các chính sách công bố trên nền tảng CamRent.\n" +
								"• Các sửa đổi, bổ sung Hợp đồng được thực hiện thông qua hệ thống CamRent hoặc bằng văn bản điện tử có xác nhận từ hai bên.\n" +
								"• Trường hợp phát sinh tranh chấp, hai bên ưu tiên thương lượng. Nếu không đạt được thỏa thuận, tranh chấp sẽ được giải quyết tại cơ quan có thẩm quyền theo quy định pháp luật."
							);

							// VII. Chữ ký
							col.Item().Row(row =>
							{
								row.RelativeItem().Element(c =>
								{
									var ownerSigUrl = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Owner)
										?.SignatureAsset?.Url;

									SignatureBlock(
										c,
										"CHỦ SỞ HỮU THIẾT BỊ (Owner)",
										owner.FullName,
										ownerSigUrl
									);
								});

								row.RelativeItem().Element(c =>
								{
									var platformSig = contract.Signatures
										.FirstOrDefault(x => x.Role == ContractSignerRole.Platform);

									var platformSigUrl = platformSig?.SignatureAsset?.Url;
									var platformName = platformSig?.User?.FullName ?? "Đại diện CamRent";

									SignatureBlock(
										c,
										"ĐẠI DIỆN CAMRENT",
										platformName,
										platformSigUrl
									);
								});
							});
						});

						page.Footer().Column(c => { });
					});
				});

				return document.GeneratePdf();
			});
		}
	}
}
