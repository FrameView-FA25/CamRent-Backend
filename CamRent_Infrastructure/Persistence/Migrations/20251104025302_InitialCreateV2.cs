using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_accessories_target_accessory_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_cameras_target_camera_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_users_author_user_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_users_reviewed_by_staff_id",
                table: "reviews");

            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.AddColumn<string>(
                name: "bank_account_name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_account_number",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "bank_name",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "kyc_status",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "national_id_number",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_accessories_target_accessory_id",
                table: "reviews",
                column: "target_accessory_id",
                principalTable: "accessories",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_cameras_target_camera_id",
                table: "reviews",
                column: "target_camera_id",
                principalTable: "cameras",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_users_author_user_id",
                table: "reviews",
                column: "author_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_users_reviewed_by_staff_id",
                table: "reviews",
                column: "reviewed_by_staff_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_reviews_accessories_target_accessory_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_cameras_target_camera_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_users_author_user_id",
                table: "reviews");

            migrationBuilder.DropForeignKey(
                name: "FK_reviews_users_reviewed_by_staff_id",
                table: "reviews");

            migrationBuilder.DropColumn(
                name: "bank_account_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bank_account_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bank_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "kyc_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "national_id_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "normalized_email",
                table: "users");

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bank_account_name = table.Column<string>(type: "text", nullable: true),
                    bank_account_number = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    kyc_status = table.Column<string>(type: "text", nullable: false),
                    national_id_number = table.Column<string>(type: "text", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_user_id",
                table: "user_profiles",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_accessories_target_accessory_id",
                table: "reviews",
                column: "target_accessory_id",
                principalTable: "accessories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_cameras_target_camera_id",
                table: "reviews",
                column: "target_camera_id",
                principalTable: "cameras",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_users_author_user_id",
                table: "reviews",
                column: "author_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_reviews_users_reviewed_by_staff_id",
                table: "reviews",
                column: "reviewed_by_staff_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
