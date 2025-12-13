using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixHandoverReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "condition_note",
                table: "handover_receipts");

            migrationBuilder.DropColumn(
                name: "items_json",
                table: "handover_receipts");

            migrationBuilder.DropColumn(
                name: "receipt_pdf_url",
                table: "handover_receipts");

            migrationBuilder.DropColumn(
                name: "staff_signature_url",
                table: "handover_receipts");

            migrationBuilder.DropColumn(
                name: "is_available",
                table: "cameras");

            migrationBuilder.DropColumn(
                name: "is_available",
                table: "accessories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "condition_note",
                table: "handover_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "items_json",
                table: "handover_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "receipt_pdf_url",
                table: "handover_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "staff_signature_url",
                table: "handover_receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "cameras",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_available",
                table: "accessories",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
