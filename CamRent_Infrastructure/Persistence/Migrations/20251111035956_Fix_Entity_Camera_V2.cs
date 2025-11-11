using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Entity_Camera_V2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cameras_branches_branch_id",
                table: "cameras");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "cameras",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_cameras_branches_branch_id",
                table: "cameras",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cameras_branches_branch_id",
                table: "cameras");

            migrationBuilder.AlterColumn<Guid>(
                name: "branch_id",
                table: "cameras",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cameras_branches_branch_id",
                table: "cameras",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
