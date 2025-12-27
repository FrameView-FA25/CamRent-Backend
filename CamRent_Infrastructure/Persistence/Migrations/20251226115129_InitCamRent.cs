using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CamRent_Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitCamRent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "combos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    price_override = table.Column<decimal>(type: "numeric", nullable: true),
                    deposit_override = table.Column<decimal>(type: "numeric", nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "files",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "text", nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    label = table.Column<string>(type: "text", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: true),
                    provider_key = table.Column<string>(type: "text", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_type = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_files", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inspection_checklist_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    inspection_type = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_checklist_templates", x => x.id);
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
                name: "money_flatform_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    upfront_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    platform_fee_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    owner_share_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    late_fee_first_n_days = table.Column<int>(type: "integer", nullable: false),
                    late_fee_factor_first_n = table.Column<decimal>(type: "numeric", nullable: false),
                    late_fee_factor_after = table.Column<decimal>(type: "numeric", nullable: false),
                    downtime_factor = table.Column<decimal>(type: "numeric", nullable: false),
                    cancel_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_flatform_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    provider = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    request_hash = table.Column<string>(type: "text", nullable: false),
                    raw_data = table.Column<string>(type: "text", nullable: false),
                    response_code = table.Column<string>(type: "text", nullable: true),
                    transaction_no = table.Column<string>(type: "text", nullable: true),
                    bank_code = table.Column<string>(type: "text", nullable: true),
                    card_type = table.Column<string>(type: "text", nullable: true),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SeedHistories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    applied_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeedHistories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_slot_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slot_index = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_slot_definitions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "home_page_blocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    image_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_page_blocks", x => x.id);
                    table.ForeignKey(
                        name: "FK_home_page_blocks_files_image_asset_id",
                        column: x => x.image_asset_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "home_page_carousel_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    link_url = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    image_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_home_page_carousel_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_home_page_carousel_items_files_image_asset_id",
                        column: x => x.image_asset_id,
                        principalTable: "files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    normalized_email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    Address_Country = table.Column<string>(type: "text", nullable: true),
                    Address_Province = table.Column<string>(type: "text", nullable: true),
                    Address_District = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    bank_account_number = table.Column<string>(type: "text", nullable: true),
                    bank_name = table.Column<string>(type: "text", nullable: true),
                    bank_account_name = table.Column<string>(type: "text", nullable: true),
                    signature_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    avatar_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_files_avatar_id",
                        column: x => x.avatar_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_users_files_signature_asset_id",
                        column: x => x.signature_asset_id,
                        principalTable: "files",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inspection_checklist_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    Address_Country = table.Column<string>(type: "text", nullable: false),
                    Address_Province = table.Column<string>(type: "text", nullable: false),
                    Address_District = table.Column<string>(type: "text", nullable: false),
                    manager_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                    table.ForeignKey(
                        name: "FK_branches_users_manager_id",
                        column: x => x.manager_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "inspection_forms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "text", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    inspection_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handover_type = table.Column<string>(type: "text", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    overall_passed = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_forms", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspection_forms_inspection_checklist_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "inspection_checklist_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inspection_forms_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    balance = table.Column<decimal>(type: "numeric", nullable: false),
                    frozen_balance = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                    table.ForeignKey(
                        name: "FK_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
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
                name: "accessories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    variant = table.Column<string>(type: "text", nullable: true),
                    serial_number = table.Column<string>(type: "text", nullable: true),
                    specs_json = table.Column<string>(type: "text", nullable: true),
                    base_daily_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    estimated_value_vnd = table.Column<decimal>(type: "numeric", nullable: false),
                    deposit_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    platform_fee_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    deposit_cap_min_vnd = table.Column<decimal>(type: "numeric", nullable: true),
                    deposit_cap_max_vnd = table.Column<decimal>(type: "numeric", nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessories", x => x.id);
                    table.ForeignKey(
                        name: "FK_accessories_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_accessories_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_code = table.Column<string>(type: "text", nullable: true),
                    renter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pickup_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Location_Country = table.Column<string>(type: "text", nullable: true),
                    Location_Province = table.Column<string>(type: "text", nullable: true),
                    Location_District = table.Column<string>(type: "text", nullable: true),
                    return_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    is_settled = table.Column<bool>(type: "boolean", nullable: false),
                    settled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    snapshot_base_daily_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    snapshot_platform_fee_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    snapshot_rental_total = table.Column<decimal>(type: "numeric", nullable: false),
                    snapshot_deposit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.ForeignKey(
                        name: "FK_bookings_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bookings_users_renter_id",
                        column: x => x.renter_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_bookings_users_staff_id",
                        column: x => x.staff_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "branch_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_memberships", x => x.id);
                    table.ForeignKey(
                        name: "FK_branch_memberships_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_branch_memberships_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cameras",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    brand = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    variant = table.Column<string>(type: "text", nullable: true),
                    serial_number = table.Column<string>(type: "text", nullable: true),
                    specs_json = table.Column<string>(type: "text", nullable: true),
                    base_daily_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    platform_fee_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    estimated_value_vnd = table.Column<decimal>(type: "numeric", nullable: false),
                    deposit_percent = table.Column<decimal>(type: "numeric", nullable: false),
                    deposit_cap_min_vnd = table.Column<decimal>(type: "numeric", nullable: true),
                    deposit_cap_max_vnd = table.Column<decimal>(type: "numeric", nullable: true),
                    is_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    location = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cameras", x => x.id);
                    table.ForeignKey(
                        name: "FK_cameras_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_cameras_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "verification_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_code = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    inspection_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_verification_requests_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_verification_requests_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_verification_requests_users_staff_id",
                        column: x => x.staff_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    form_id = table.Column<Guid>(type: "uuid", nullable: true),
                    checklist_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    section = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspections", x => x.id);
                    table.ForeignKey(
                        name: "FK_inspections_inspection_forms_form_id",
                        column: x => x.form_id,
                        principalTable: "inspection_forms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateTable(
                name: "booking_issue_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reporter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    handled_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    handled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handler_note = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_issue_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_issue_reports_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_issue_reports_users_handled_by_staff_id",
                        column: x => x.handled_by_staff_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_booking_issue_reports_users_reporter_user_id",
                        column: x => x.reporter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.id);
                    table.ForeignKey(
                        name: "FK_disputes_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<string>(type: "text", nullable: false),
                    provider_payment_id = table.Column<string>(type: "text", nullable: true),
                    authorized_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    captured_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_payments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "booking_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    camera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accessory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    combo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    deposit_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_booking_items_accessories_accessory_id",
                        column: x => x.accessory_id,
                        principalTable: "accessories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_items_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_booking_items_cameras_camera_id",
                        column: x => x.camera_id,
                        principalTable: "cameras",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_booking_items_combos_combo_id",
                        column: x => x.combo_id,
                        principalTable: "combos",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "combo_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    combo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    camera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accessory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_combo_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_combo_items_accessories_accessory_id",
                        column: x => x.accessory_id,
                        principalTable: "accessories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_combo_items_cameras_camera_id",
                        column: x => x.camera_id,
                        principalTable: "cameras",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_combo_items_combos_combo_id",
                        column: x => x.combo_id,
                        principalTable: "combos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_camera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_accessory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    reviewed_by_staff_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    moderation_notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_reviews_accessories_target_accessory_id",
                        column: x => x.target_accessory_id,
                        principalTable: "accessories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reviews_cameras_target_camera_id",
                        column: x => x.target_camera_id,
                        principalTable: "cameras",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_reviews_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reviews_users_reviewed_by_staff_id",
                        column: x => x.reviewed_by_staff_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verification_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    file_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_hash = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contracts", x => x.id);
                    table.ForeignKey(
                        name: "FK_contracts_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_contracts_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_contracts_files_file_asset_id",
                        column: x => x.file_asset_id,
                        principalTable: "files",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_contracts_verification_requests_verification_id",
                        column: x => x.verification_id,
                        principalTable: "verification_requests",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "verification_request_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    verification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    camera_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accessory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_verification_request_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_verification_request_items_accessories_accessory_id",
                        column: x => x.accessory_id,
                        principalTable: "accessories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_verification_request_items_cameras_camera_id",
                        column: x => x.camera_id,
                        principalTable: "cameras",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_verification_request_items_verification_requests_verificati~",
                        column: x => x.verification_id,
                        principalTable: "verification_requests",
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
                name: "dispute_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    dispute_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dispute_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_dispute_items_disputes_dispute_id",
                        column: x => x.dispute_id,
                        principalTable: "disputes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    captured_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_lines_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    is_credit = table.Column<bool>(type: "boolean", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallet_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_wallet_transactions_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_transactions_payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payments",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_wallet_transactions_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "handover_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    party_type = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inspection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    handover_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    party_signature_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_handover_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_handover_receipts_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_handover_receipts_contracts_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contracts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_handover_receipts_inspections_inspection_id",
                        column: x => x.inspection_id,
                        principalTable: "inspections",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_handover_receipts_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_handover_receipts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_accessories_branch_id",
                table: "accessories",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_accessories_owner_user_id",
                table: "accessories",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_issue_reports_booking_id",
                table: "booking_issue_reports",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_issue_reports_handled_by_staff_id",
                table: "booking_issue_reports",
                column: "handled_by_staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_issue_reports_reporter_user_id",
                table: "booking_issue_reports",
                column: "reporter_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_accessory_id",
                table: "booking_items",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_booking_id",
                table: "booking_items",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_camera_id",
                table: "booking_items",
                column: "camera_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_items_combo_id",
                table: "booking_items",
                column: "combo_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_branch_id",
                table: "bookings",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_renter_id",
                table: "bookings",
                column: "renter_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_staff_id",
                table: "bookings",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_memberships_branch_id",
                table: "branch_memberships",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_branch_memberships_user_id",
                table: "branch_memberships",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_branches_manager_id",
                table: "branches",
                column: "manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_cameras_branch_id",
                table: "cameras",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_cameras_owner_user_id",
                table: "cameras",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_items_accessory_id",
                table: "combo_items",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_items_camera_id",
                table: "combo_items",
                column: "camera_id");

            migrationBuilder.CreateIndex(
                name: "IX_combo_items_combo_id",
                table: "combo_items",
                column: "combo_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_contracts_booking_id",
                table: "contracts",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_branch_id",
                table: "contracts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_file_asset_id",
                table: "contracts",
                column: "file_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_contracts_verification_id",
                table: "contracts",
                column: "verification_id");

            migrationBuilder.CreateIndex(
                name: "IX_dispute_items_dispute_id",
                table: "dispute_items",
                column: "dispute_id");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_booking_id",
                table: "disputes",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_handover_receipts_branch_id",
                table: "handover_receipts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_handover_receipts_contract_id",
                table: "handover_receipts",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "IX_handover_receipts_created_by_user_id",
                table: "handover_receipts",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_handover_receipts_inspection_id",
                table: "handover_receipts",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_handover_receipts_user_id",
                table: "handover_receipts",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_home_page_blocks_image_asset_id",
                table: "home_page_blocks",
                column: "image_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_home_page_blocks_key",
                table: "home_page_blocks",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_home_page_carousel_items_image_asset_id",
                table: "home_page_carousel_items",
                column: "image_asset_id");

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
                name: "IX_inspection_forms_created_by_user_id",
                table: "inspection_forms",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_forms_template_id",
                table: "inspection_forms",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_method_selections_inspection_id",
                table: "inspection_method_selections",
                column: "inspection_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_method_selections_method_id",
                table: "inspection_method_selections",
                column: "method_id");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_form_id",
                table: "inspections",
                column: "form_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_events_request_hash",
                table: "payment_events",
                column: "request_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payment_lines_payment_id",
                table: "payment_lines",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_payments_booking_id",
                table: "payments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_reset_password_tokens_user_id",
                table: "reset_password_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_author_user_id",
                table: "reviews",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_reviewed_by_staff_id",
                table: "reviews",
                column: "reviewed_by_staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_target_accessory_id",
                table: "reviews",
                column: "target_accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_reviews_target_camera_id",
                table: "reviews",
                column: "target_camera_id");

            migrationBuilder.CreateIndex(
                name: "IX_SeedHistories_key",
                table: "SeedHistories",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_avatar_id",
                table: "users",
                column: "avatar_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_signature_asset_id",
                table: "users",
                column: "signature_asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_request_items_accessory_id",
                table: "verification_request_items",
                column: "accessory_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_request_items_camera_id",
                table: "verification_request_items",
                column: "camera_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_request_items_verification_id",
                table: "verification_request_items",
                column: "verification_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requests_branch_id",
                table: "verification_requests",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requests_created_by_user_id",
                table: "verification_requests",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_verification_requests_staff_id",
                table: "verification_requests",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_booking_id",
                table: "wallet_transactions",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_payment_id",
                table: "wallet_transactions",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallet_transactions_wallet_id",
                table: "wallet_transactions",
                column: "wallet_id");

            migrationBuilder.CreateIndex(
                name: "IX_wallets_user_id",
                table: "wallets",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_issue_reports");

            migrationBuilder.DropTable(
                name: "booking_items");

            migrationBuilder.DropTable(
                name: "branch_memberships");

            migrationBuilder.DropTable(
                name: "combo_items");

            migrationBuilder.DropTable(
                name: "contract_signatures");

            migrationBuilder.DropTable(
                name: "dispute_items");

            migrationBuilder.DropTable(
                name: "handover_receipts");

            migrationBuilder.DropTable(
                name: "home_page_blocks");

            migrationBuilder.DropTable(
                name: "home_page_carousel_items");

            migrationBuilder.DropTable(
                name: "inspection_checklist_item_allowed_methods");

            migrationBuilder.DropTable(
                name: "inspection_method_selections");

            migrationBuilder.DropTable(
                name: "money_flatform_settings");

            migrationBuilder.DropTable(
                name: "payment_events");

            migrationBuilder.DropTable(
                name: "payment_lines");

            migrationBuilder.DropTable(
                name: "reset_password_tokens");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "SeedHistories");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "verification_request_items");

            migrationBuilder.DropTable(
                name: "wallet_transactions");

            migrationBuilder.DropTable(
                name: "work_slot_definitions");

            migrationBuilder.DropTable(
                name: "combos");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "contracts");

            migrationBuilder.DropTable(
                name: "inspection_checklist_items");

            migrationBuilder.DropTable(
                name: "inspection_methods");

            migrationBuilder.DropTable(
                name: "inspections");

            migrationBuilder.DropTable(
                name: "accessories");

            migrationBuilder.DropTable(
                name: "cameras");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "wallets");

            migrationBuilder.DropTable(
                name: "verification_requests");

            migrationBuilder.DropTable(
                name: "inspection_checklist_sections");

            migrationBuilder.DropTable(
                name: "inspection_forms");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "inspection_checklist_templates");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "files");
        }
    }
}
