using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Quantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantity",
                table: "combo_items");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "booking_items");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "combo_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "booking_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
