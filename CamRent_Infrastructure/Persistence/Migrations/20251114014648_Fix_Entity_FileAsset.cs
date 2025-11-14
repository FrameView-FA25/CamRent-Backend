using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Entity_FileAsset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_files_cameras_camera_id",
                table: "files");

            migrationBuilder.DropIndex(
                name: "IX_files_camera_id",
                table: "files");

            migrationBuilder.DropColumn(
                name: "camera_id",
                table: "files");

            migrationBuilder.AddColumn<Guid>(
                name: "owner_id",
                table: "files",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "owner_type",
                table: "files",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "reset_password_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reset_password_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_reset_password_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reset_password_tokens_user_id",
                table: "reset_password_tokens",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reset_password_tokens");

            migrationBuilder.DropColumn(
                name: "owner_id",
                table: "files");

            migrationBuilder.DropColumn(
                name: "owner_type",
                table: "files");

            migrationBuilder.AddColumn<Guid>(
                name: "camera_id",
                table: "files",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_files_camera_id",
                table: "files",
                column: "camera_id");

            migrationBuilder.AddForeignKey(
                name: "FK_files_cameras_camera_id",
                table: "files",
                column: "camera_id",
                principalTable: "cameras",
                principalColumn: "id");
        }
    }
}
