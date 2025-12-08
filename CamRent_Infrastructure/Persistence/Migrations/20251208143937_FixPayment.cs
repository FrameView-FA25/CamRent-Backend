using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "payments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "purpose",
                table: "payments");
        }
    }
}
