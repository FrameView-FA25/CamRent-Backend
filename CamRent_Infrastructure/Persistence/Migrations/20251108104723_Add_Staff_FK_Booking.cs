using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Staff_FK_Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "staff_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_staff_id",
                table: "bookings",
                column: "staff_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_staff_id",
                table: "bookings",
                column: "staff_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_staff_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_staff_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "staff_id",
                table: "bookings");
        }
    }
}
