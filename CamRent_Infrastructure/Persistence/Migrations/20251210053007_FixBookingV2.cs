using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixBookingV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_settled",
                table: "bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "settled_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "money_flatform_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upfront_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    platform_fee_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    owner_share_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    late_fee_first_n_days = table.Column<int>(type: "integer", nullable: false),
                    late_fee_factor_first_n = table.Column<decimal>(type: "numeric", nullable: false),
                    late_fee_factor_after = table.Column<decimal>(type: "numeric", nullable: false),
                    downtime_factor = table.Column<decimal>(type: "numeric", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_flatform_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "money_flatform_settings");

            migrationBuilder.DropColumn(
                name: "is_settled",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "settled_at",
                table: "bookings");
        }
    }
}
