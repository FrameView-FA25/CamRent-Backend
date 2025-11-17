using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Entity_VerificationRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "type",
                table: "verification_requests");

            migrationBuilder.AddColumn<DateTime>(
                name: "inspection_date",
                table: "verification_requests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "verification_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone_number",
                table: "verification_requests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "inspection_date",
                table: "verification_requests");

            migrationBuilder.DropColumn(
                name: "name",
                table: "verification_requests");

            migrationBuilder.DropColumn(
                name: "phone_number",
                table: "verification_requests");

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "verification_requests",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
