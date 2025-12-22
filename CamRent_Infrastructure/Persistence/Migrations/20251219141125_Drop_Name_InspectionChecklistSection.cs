using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Drop_Name_InspectionChecklistSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                table: "inspection_checklist_sections");

            migrationBuilder.AddColumn<Guid>(
                name: "checklist_item_id",
                table: "inspections",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "checklist_item_id",
                table: "inspections");

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "inspection_checklist_sections",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
