using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Booking_Relations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_created_by_user_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_created_by_user_id",
                table: "bookings");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_renter_id",
                table: "bookings",
                column: "renter_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_renter_id",
                table: "bookings",
                column: "renter_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_renter_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_renter_id",
                table: "bookings");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_created_by_user_id",
                table: "bookings",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_created_by_user_id",
                table: "bookings",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
