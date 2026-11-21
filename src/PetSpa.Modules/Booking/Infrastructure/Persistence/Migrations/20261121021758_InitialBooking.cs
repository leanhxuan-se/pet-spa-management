using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "booking",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    pet_id = table.Column<long>(type: "bigint", nullable: false),
                    booking_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_in = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    check_out = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    contact_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    sessions_created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handed_off_detail_ids = table.Column<long[]>(type: "bigint[]", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    legacy_handoff_needs_review = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PENDING"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking", x => x.id);
                    table.CheckConstraint("ck_booking_check_out_order", "check_out IS NULL OR check_in IS NULL OR check_out >= check_in");
                    table.CheckConstraint("ck_booking_handoff", "(sessions_created_at IS NULL AND handed_off_detail_ids IS NULL) OR (sessions_created_at IS NOT NULL AND handed_off_detail_ids IS NOT NULL AND cardinality(handed_off_detail_ids) > 0)");
                    table.CheckConstraint("ck_booking_status", "status IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'PAID', 'CANCELLED')");
                });

            migrationBuilder.CreateTable(
                name: "booking_details",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_id = table.Column<long>(type: "bigint", nullable: false),
                    service_options_id = table.Column<long>(type: "bigint", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ASSIGNED")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_details", x => x.id);
                    table.CheckConstraint("ck_booking_details_status", "status IN ('ASSIGNED', 'WORKING', 'COMPLETED', 'CANCELLED')");
                    table.CheckConstraint("ck_booking_details_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_booking_detail_booking",
                        column: x => x.booking_id,
                        principalSchema: "booking",
                        principalTable: "booking",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_booking_time",
                schema: "booking",
                table: "booking",
                column: "booking_time");

            migrationBuilder.CreateIndex(
                name: "IX_booking_contact_phone",
                schema: "booking",
                table: "booking",
                column: "contact_phone");

            migrationBuilder.CreateIndex(
                name: "IX_booking_customer_id",
                schema: "booking",
                table: "booking",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_pet_id",
                schema: "booking",
                table: "booking",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status",
                schema: "booking",
                table: "booking",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_booking_details_booking_id",
                schema: "booking",
                table: "booking_details",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_details_id_booking_id",
                schema: "booking",
                table: "booking_details",
                columns: new[] { "id", "booking_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "booking_details",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "booking",
                schema: "booking");
        }
    }
}
