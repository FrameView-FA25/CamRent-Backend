using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixContractTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contracts_contract_templates_template_id",
                table: "contracts");

            migrationBuilder.DropTable(
                name: "contract_events");

            migrationBuilder.DropTable(
                name: "contract_signers");

            migrationBuilder.DropTable(
                name: "contract_templates");

            migrationBuilder.DropIndex(
                name: "IX_contracts_template_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "end_date",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "template_id",
                table: "contracts");

            migrationBuilder.RenameColumn(
                name: "start_date",
                table: "contracts",
                newName: "signed_at");

            migrationBuilder.RenameColumn(
                name: "signed_file_url",
                table: "contracts",
                newName: "type");

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "file_asset_id",
                table: "contracts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_hash",
                table: "contracts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contract_signatures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_signed = table.Column<bool>(type: "boolean", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    signature_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signed_ip = table.Column<string>(type: "text", nullable: true),
                    signed_user_agent = table.Column<string>(type: "text", nullable: true),
                    document_hash_at_sign_time = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_signatures", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_signatures_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_contract_signatures_files_signature_asset_id",
                        column: x => x.signature_asset_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_contract_signatures_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_contracts_branch_id",
                table: "contracts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_file_asset_id",
                table: "contracts",
                column: "file_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signatures_contract_id",
                table: "contract_signatures",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signatures_signature_asset_id",
                table: "contract_signatures",
                column: "signature_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signatures_user_id",
                table: "contract_signatures",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_branches_branch_id",
                table: "contracts",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_files_file_asset_id",
                table: "contracts",
                column: "file_asset_id",
                principalTable: "files",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_contracts_branches_branch_id",
                table: "contracts");

            migrationBuilder.DropForeignKey(
                name: "FK_contracts_files_file_asset_id",
                table: "contracts");

            migrationBuilder.DropTable(
                name: "contract_signatures");

            migrationBuilder.DropIndex(
                name: "IX_contracts_branch_id",
                table: "contracts");

            migrationBuilder.DropIndex(
                name: "IX_contracts_file_asset_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "file_asset_id",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "file_hash",
                table: "contracts");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "contracts",
                newName: "signed_file_url");

            migrationBuilder.RenameColumn(
                name: "signed_at",
                table: "contracts",
                newName: "start_date");

            migrationBuilder.AddColumn<DateTime>(
                name: "end_date",
                table: "contracts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "template_id",
                table: "contracts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "contract_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    data_json = table.Column<string>(type: "text", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_events_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_signers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    email = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    sign_order = table.Column<int>(type: "integer", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_signers", x => x.id);
                    table.ForeignKey(
                        name: "FK_contract_signers_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    template_url = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contract_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contracts_template_id",
                table: "contracts",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_events_contract_id",
                table: "contract_events",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_contract_signers_contract_id",
                table: "contract_signers",
                column: "contract_id");

            migrationBuilder.AddForeignKey(
                name: "FK_contracts_contract_templates_template_id",
                table: "contracts",
                column: "template_id",
                principalTable: "contract_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
