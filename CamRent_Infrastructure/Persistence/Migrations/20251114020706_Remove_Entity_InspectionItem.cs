using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Remove_Entity_InspectionItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_accessories_accessory_id",
                table: "files");

            migrationBuilder.DropForeignKey(
                name: "FK_files_disputes_dispute_id",
                table: "files");

            migrationBuilder.DropForeignKey(
                name: "FK_files_inspections_inspection_id",
                table: "files");

            migrationBuilder.DropForeignKey(
                name: "FK_inspections_users_performed_by_user_id",
                table: "inspections");

            migrationBuilder.DropTable(
                name: "inspection_items");

            migrationBuilder.DropIndex(
                name: "IX_files_accessory_id",
                table: "files");

            migrationBuilder.DropIndex(
                name: "IX_files_dispute_id",
                table: "files");

            migrationBuilder.DropIndex(
                name: "IX_files_inspection_id",
                table: "files");

            migrationBuilder.DropColumn(
                name: "accessory_id",
                table: "files");

            migrationBuilder.DropColumn(
                name: "dispute_id",
                table: "files");

            migrationBuilder.DropColumn(
                name: "inspection_id",
                table: "files");

            migrationBuilder.RenameColumn(
                name: "performed_by_user_id",
                table: "inspections",
                newName: "staff_id");

            migrationBuilder.RenameIndex(
                name: "IX_inspections_performed_by_user_id",
                table: "inspections",
                newName: "IX_inspections_staff_id");

            migrationBuilder.AddColumn<string>(
                name: "label",
                table: "inspections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "passed",
                table: "inspections",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "section",
                table: "inspections",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "value",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "owner_type",
                table: "files",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_id",
                table: "files",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_override",
                table: "combos",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_users_staff_id",
                table: "inspections",
                column: "staff_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspections_users_staff_id",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "label",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "passed",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "section",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "value",
                table: "inspections");

            migrationBuilder.DropColumn(
                name: "deposit_override",
                table: "combos");

            migrationBuilder.RenameColumn(
                name: "staff_id",
                table: "inspections",
                newName: "performed_by_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_inspections_staff_id",
                table: "inspections",
                newName: "IX_inspections_performed_by_user_id");

            migrationBuilder.AlterColumn<int>(
                name: "owner_type",
                table: "files",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "owner_id",
                table: "files",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "accessory_id",
                table: "files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "dispute_id",
                table: "files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "inspection_id",
                table: "files",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inspection_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false),
                    section = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_items_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_files_accessory_id",
                table: "files",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_dispute_id",
                table: "files",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "IX_files_inspection_id",
                table: "files",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_items_inspection_id",
                table: "inspection_items",
                column: "inspection_id");

            migrationBuilder.AddForeignKey(
                name: "FK_files_accessories_accessory_id",
                table: "files",
                column: "accessory_id",
                principalTable: "accessories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_files_disputes_dispute_id",
                table: "files",
                column: "dispute_id",
                principalTable: "disputes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_files_inspections_inspection_id",
                table: "files",
                column: "inspection_id",
                principalTable: "inspections",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspections_users_performed_by_user_id",
                table: "inspections",
                column: "performed_by_user_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
