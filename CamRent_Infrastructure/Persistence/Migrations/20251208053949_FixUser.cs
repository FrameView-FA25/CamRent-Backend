using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kyc_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "national_id_number",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "verification_code",
                table: "verification_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "avatar_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "signature_asset_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "booking_code",
                table: "bookings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_users_avatar_id",
                table: "users",
                column: "avatar_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_signature_asset_id",
                table: "users",
                column: "signature_asset_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_files_avatar_id",
                table: "users",
                column: "avatar_id",
                principalTable: "files",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_files_signature_asset_id",
                table: "users",
                column: "signature_asset_id",
                principalTable: "files",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_files_avatar_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_files_signature_asset_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_avatar_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_signature_asset_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "verification_code",
                table: "verification_requests");

            migrationBuilder.DropColumn(
                name: "avatar_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "signature_asset_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "booking_code",
                table: "bookings");

            migrationBuilder.AddColumn<string>(
                name: "kyc_status",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "national_id_number",
                table: "users",
                type: "text",
                nullable: true);
        }
    }
}
