using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Entity_Inspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_bookings_booking_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_users_staff_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_verification_requests_verify_request_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_verification_requests_users_target_user_id",
                table: "verification_requests");

            migrationBuilder.RenameColumn(
                name: "target_user_id",
                table: "verification_requests",
                newName: "staff_id");

            migrationBuilder.RenameIndex(
                name: "IX_verification_requests_target_user_id",
                table: "verification_requests",
                newName: "IX_verification_requests_staff_id");

            migrationBuilder.RenameColumn(
                name: "staff_id",
                table: "inspections",
                newName: "manager_id");

            migrationBuilder.RenameIndex(
                name: "IX_inspections_staff_id",
                table: "inspections",
                newName: "IX_inspections_manager_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "verify_request_id",
                table: "inspections",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "booking_id",
                table: "inspections",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_bookings_booking_id",
                table: "inspections",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_users_manager_id",
                table: "inspections",
                column: "manager_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_verification_requests_verify_request_id",
                table: "inspections",
                column: "verify_request_id",
                principalTable: "verification_requests",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_verification_requests_users_staff_id",
                table: "verification_requests",
                column: "staff_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_bookings_booking_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_users_manager_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_verification_requests_verify_request_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_verification_requests_users_staff_id",
                table: "verification_requests");

            migrationBuilder.RenameColumn(
                name: "staff_id",
                table: "verification_requests",
                newName: "target_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_verification_requests_staff_id",
                table: "verification_requests",
                newName: "IX_verification_requests_target_user_id");

            migrationBuilder.RenameColumn(
                name: "manager_id",
                table: "inspections",
                newName: "staff_id");

            migrationBuilder.RenameIndex(
                name: "IX_inspections_manager_id",
                table: "inspections",
                newName: "IX_inspections_staff_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "verify_request_id",
                table: "inspections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "booking_id",
                table: "inspections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_bookings_booking_id",
                table: "inspections",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_users_staff_id",
                table: "inspections",
                column: "staff_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_verification_requests_verify_request_id",
                table: "inspections",
                column: "verify_request_id",
                principalTable: "verification_requests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_verification_requests_users_target_user_id",
                table: "verification_requests",
                column: "target_user_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
