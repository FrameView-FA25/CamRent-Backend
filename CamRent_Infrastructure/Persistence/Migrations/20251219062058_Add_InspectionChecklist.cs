using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add_InspectionChecklist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "handover_type",
                table: "inspections",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "inspection_checklist_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    inspection_type = table.Column<string>(type: "text", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_checklist_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_checklist_templates_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inspection_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_methods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inspection_checklist_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_checklist_sections", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_checklist_sections_inspection_checklist_template~",
                        column: x => x.template_id,
                        principalTable: "inspection_checklist_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_method_selections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_method_selections", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_method_selections_inspection_methods_method_id",
                        column: x => x.method_id,
                        principalTable: "inspection_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_method_selections_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_checklist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_checklist_items_inspection_checklist_sections_se~",
                        column: x => x.section_id,
                        principalTable: "inspection_checklist_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_checklist_item_allowed_methods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    checklist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_checklist_item_allowed_methods", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_checklist_item_allowed_methods_inspection_checkl~",
                        column: x => x.checklist_item_id,
                        principalTable: "inspection_checklist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inspection_checklist_item_allowed_methods_inspection_method~",
                        column: x => x.method_id,
                        principalTable: "inspection_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_item_allowed_methods_checklist_item_id",
                table: "inspection_checklist_item_allowed_methods",
                column: "checklist_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_item_allowed_methods_method_id",
                table: "inspection_checklist_item_allowed_methods",
                column: "method_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_items_section_id",
                table: "inspection_checklist_items",
                column: "section_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_sections_template_id",
                table: "inspection_checklist_sections",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_templates_branch_id",
                table: "inspection_checklist_templates",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_method_selections_inspection_id",
                table: "inspection_method_selections",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_method_selections_method_id",
                table: "inspection_method_selections",
                column: "method_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inspection_checklist_item_allowed_methods");

            migrationBuilder.DropTable(
                name: "inspection_method_selections");

            migrationBuilder.DropTable(
                name: "inspection_checklist_items");

            migrationBuilder.DropTable(
                name: "inspection_methods");

            migrationBuilder.DropTable(
                name: "inspection_checklist_sections");

            migrationBuilder.DropTable(
                name: "inspection_checklist_templates");

            migrationBuilder.DropColumn(
                name: "handover_type",
                table: "inspections");
        }
    }
}
