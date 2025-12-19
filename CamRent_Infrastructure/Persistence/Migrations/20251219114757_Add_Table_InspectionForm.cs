using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_Table_InspectionForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_accessories_accessory_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_bookings_booking_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_branches_branch_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_cameras_camera_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_users_created_by_user_id",
                table: "inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_verification_requests_verification_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_accessory_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_booking_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_branch_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_camera_id",
                table: "inspections");

            migrationBuilder.DropIndex(
                name: "IX_inspections_created_by_user_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "accessory_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "booking_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "camera_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "handover_type",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "type",
                table: "inspections");

            migrationBuilder.RenameColumn(
                name: "verification_id",
                table: "inspections",
                newName: "form_id");

            migrationBuilder.RenameIndex(
                name: "IX_inspections_verification_id",
                table: "inspections",
                newName: "IX_inspections_form_id");

            migrationBuilder.CreateTable(
                name: "inspection_forms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    inspection_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_type = table.Column<string>(type: "text", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    overall_passed = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_forms", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_forms_inspection_checklist_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "inspection_checklist_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_forms_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_forms_created_by_user_id",
                table: "inspection_forms",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_forms_template_id",
                table: "inspection_forms",
                column: "template_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_inspection_forms_form_id",
                table: "inspections",
                column: "form_id",
                principalTable: "inspection_forms",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_inspection_forms_form_id",
                table: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_forms");

            migrationBuilder.RenameColumn(
                name: "form_id",
                table: "inspections",
                newName: "verification_id");

            migrationBuilder.RenameIndex(
                name: "IX_inspections_form_id",
                table: "inspections",
                newName: "IX_inspections_verification_id");

            migrationBuilder.AddColumn<Guid>(
                name: "accessory_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "booking_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "camera_id",
                table: "inspections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handover_type",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspections_accessory_id",
                table: "inspections",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_booking_id",
                table: "inspections",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_branch_id",
                table: "inspections",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_camera_id",
                table: "inspections",
                column: "camera_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_created_by_user_id",
                table: "inspections",
                column: "created_by_user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_accessories_accessory_id",
                table: "inspections",
                column: "accessory_id",
                principalTable: "accessories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_bookings_booking_id",
                table: "inspections",
                column: "booking_id",
                principalTable: "bookings",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_branches_branch_id",
                table: "inspections",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_cameras_camera_id",
                table: "inspections",
                column: "camera_id",
                principalTable: "cameras",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_users_created_by_user_id",
                table: "inspections",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_verification_requests_verification_id",
                table: "inspections",
                column: "verification_id",
                principalTable: "verification_requests",
                principalColumn: "id");
        }
    }
}
