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
            migrationBuilder.AddForeignKey(
                name: "FK_branches_users_manager_id",
                table: "branches",
                column: "manager_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_branches_users_manager_id",
                table: "branches");

            migrationBuilder.AddForeignKey(
                name: "FK_branches_users_manager_id",
                table: "branches",
                column: "manager_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
