using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_users_manager_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_manager_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "manager_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "performed_at",
                table: "inspections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "manager_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "performed_at",
                table: "inspections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspections_manager_id",
                table: "inspections",
                column: "manager_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_users_manager_id",
                table: "inspections",
                column: "manager_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
