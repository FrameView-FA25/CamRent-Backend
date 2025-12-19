using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Drop_FK_BranchId_InspectionCheckList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_inspection_checklist_templates_branches_branch_id",
                table: "inspection_checklist_templates");

            migrationBuilder.DropIndex(
                name: "IX_inspection_checklist_templates_branch_id",
                table: "inspection_checklist_templates");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "inspection_checklist_templates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "inspection_checklist_templates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspection_checklist_templates_branch_id",
                table: "inspection_checklist_templates",
                column: "branch_id");

            migrationBuilder.AddForeignKey(
                name: "FK_inspection_checklist_templates_branches_branch_id",
                table: "inspection_checklist_templates",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
