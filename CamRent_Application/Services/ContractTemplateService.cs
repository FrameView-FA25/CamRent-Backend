using CamRent_Application.Interfaces;
using CamRent_Application.IServices;
using CamRent_Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CamRent_Application.Services
{
    public class ContractTemplateService : IContractTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ContractTemplateService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<byte[]> GeneratePreviewPdfAsync(Guid bookingId, CancellationToken cancellationToken = default)
        {
            // Load booking with related data
            var bookings = await _unitOfWork.Repository<Booking>().ListAsync(
                b => b.Id == bookingId,
                include: IncludeGraph);

            var booking = bookings.FirstOrDefault() ?? throw new InvalidOperationException("Booking not found");

            var renterName = booking.Renter?.FullName ?? "Unknown Renter";
            var start = booking.PickupAt.ToString("yyyy-MM-dd HH:mm");
            var end = booking.ReturnAt.ToString("yyyy-MM-dd HH:mm");
            var days = Math.Max(1, (int)Math.Ceiling((booking.ReturnAt - booking.PickupAt).TotalDays));

            // Compute simple totals from snapshot
            var subtotal = booking.SnapshotRentalTotal;
            var deposit = booking.SnapshotDepositAmount;
            var total = subtotal + deposit;

            // Prepare item rows
            var itemRows = booking.Items.Select(i => new
            {
                Name = i.Camera != null
                    ? string.Join(" ", new[] { i.Camera.Brand, i.Camera.Model, i.Camera.Variant }.Where(s => !string.IsNullOrWhiteSpace(s)))
                    : i.Accessory != null
                        ? string.Join(" ", new[] { i.Accessory.Brand, i.Accessory.Model, i.Accessory.Variant }.Where(s => !string.IsNullOrWhiteSpace(s)))
                        : i.Combo?.Name ?? "Item",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.UnitPrice * i.Quantity
            }).ToList();

            // Build PDF
            QuestPDF.Settings.License = LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);

                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CamRent Rental Agreement").FontSize(20).SemiBold();
                                col.Item().Text($"Contract Preview • Booking #{booking.Id}").FontSize(10).FontColor(Colors.Grey.Medium);
                            });
                        });

                    page.Content()
                        .Column(col =>
                        {
                            col.Spacing(10);

                            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                            col.Item().Text("Parties").FontSize(12).SemiBold();
                            col.Item().Text($"Renter: {renterName}");
                            col.Item().Text($"Period: {start} → {end} ({days} days)");

                            col.Item().PaddingTop(10).Text("Items").FontSize(12).SemiBold();
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn(5); // Name
                                    cols.RelativeColumn(2); // Qty
                                    cols.RelativeColumn(3); // Unit
                                    cols.RelativeColumn(3); // Total
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Name").SemiBold();
                                    header.Cell().AlignRight().Text("Qty").SemiBold();
                                    header.Cell().AlignRight().Text("Unit Price").SemiBold();
                                    header.Cell().AlignRight().Text("Line Total").SemiBold();
                                });

                                foreach (var r in itemRows)
                                {
                                    table.Cell().Text(r.Name);
                                    table.Cell().AlignRight().Text(r.Quantity.ToString());
                                    table.Cell().AlignRight().Text($"{r.UnitPrice:N0} VND");
                                    table.Cell().AlignRight().Text($"{r.LineTotal:N0} VND");
                                }
                            });

                            col.Item().PaddingTop(8).Row(r =>
                            {
                                r.RelativeItem();
                                r.ConstantItem(220).Column(sum =>
                                {
                                    sum.Item().Row(x => { x.RelativeItem().Text("Subtotal"); x.ConstantItem(100).AlignRight().Text($"{subtotal:N0} VND"); });
                                    sum.Item().Row(x => { x.RelativeItem().Text("Deposit"); x.ConstantItem(100).AlignRight().Text($"{deposit:N0} VND"); });
                                    sum.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).Row(x => {
                                        x.RelativeItem().Text("Total").SemiBold();
                                        x.ConstantItem(100).AlignRight().Text($"{total:N0} VND").SemiBold();
                                    });
                                });
                            });

                            col.Item().PaddingTop(16).Text("Terms").FontSize(12).SemiBold();
                            col.Item().Text("By signing this agreement, the renter agrees to the CamRent rental terms, including care of equipment, return on time, and payment of any applicable late or damage fees.").FontColor(Colors.Grey.Darken1);

                            col.Item().PaddingTop(30).Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Renter Signature");
                                    c.Item().Height(60).BorderBottom(1);
                                    c.Item().Text(renterName).FontColor(Colors.Grey.Medium).FontSize(10);
                                });
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Owner/Branch Signature");
                                    c.Item().Height(60).BorderBottom(1);
                                    c.Item().Text("CamRent Representative").FontColor(Colors.Grey.Medium).FontSize(10);
                                });
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text($"Generated for preview • {DateTime.UtcNow:yyyy-MM-dd HH:mm 'UTC'}");
                });
            });

            using var stream = new System.IO.MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
        }

        private static Func<IQueryable<Booking>, IIncludableQueryable<Booking, object>> IncludeGraph =>
            q => q
                .Include(b => b.Renter)
                .Include(b => b.Items)
                    .ThenInclude(i => i.Camera)
                .Include(b => b.Items)
                    .ThenInclude(i => i.Accessory)
                .Include(b => b.Items)
                    .ThenInclude(i => i.Combo);
    }
}


