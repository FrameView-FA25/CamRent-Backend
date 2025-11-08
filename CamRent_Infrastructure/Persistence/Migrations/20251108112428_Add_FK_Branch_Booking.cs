using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_FK_Branch_Booking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_Country",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_District",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PickupLocation_Latitude",
                table: "bookings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_Line1",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_Line2",
                table: "bookings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupLocation_Longitude",
                table: "bookings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_PostalCode",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_Province",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupLocation_Ward",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_branch_id",
                table: "bookings",
                column: "branch_id");

            migrationBuilder.AddForeignKey(
                name: "FK_bookings_branches_branch_id",
                table: "bookings",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_branches_branch_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_branch_id",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Country",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_District",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Latitude",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Line1",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Line2",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Longitude",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_PostalCode",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Province",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "PickupLocation_Ward",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "bookings");
        }
    }
}
