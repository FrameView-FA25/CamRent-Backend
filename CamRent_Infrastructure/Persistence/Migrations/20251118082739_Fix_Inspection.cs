using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Inspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "item_type",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "renter_signature_url",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "staff_signature_url",
                table: "inspections");

            migrationBuilder.RenameColumn(
                name: "item_id",
                table: "inspections",
                newName: "camera_id");

            migrationBuilder.AddColumn<Guid>(
                name: "accessory_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspections_accessory_id",
                table: "inspections",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_camera_id",
                table: "inspections",
                column: "camera_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_accessories_accessory_id",
                table: "inspections",
                column: "accessory_id",
                principalTable: "accessories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_cameras_camera_id",
                table: "inspections",
                column: "camera_id",
                principalTable: "cameras",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_accessories_accessory_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_cameras_camera_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_accessory_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_camera_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "accessory_id",
                table: "inspections");

            migrationBuilder.RenameColumn(
                name: "camera_id",
                table: "inspections",
                newName: "item_id");

            migrationBuilder.AddColumn<string>(
                name: "item_type",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "renter_signature_url",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "staff_signature_url",
                table: "inspections",
                type: "text",
                nullable: true);
        }
    }
}
