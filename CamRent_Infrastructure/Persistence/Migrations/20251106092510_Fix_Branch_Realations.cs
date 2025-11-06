using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Branch_Realations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "manager_id",
                table: "branches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_branches_manager_id",
                table: "branches",
                column: "manager_id");

            migrationBuilder.AddForeignKey(
                name: "FK_branches_users_manager_id",
                table: "branches",
                column: "manager_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branches_users_manager_id",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "IX_branches_manager_id",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "manager_id",
                table: "branches");
        }
    }
}
