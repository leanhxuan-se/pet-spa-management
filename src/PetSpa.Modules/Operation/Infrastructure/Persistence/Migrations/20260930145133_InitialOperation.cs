using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetSpa.Modules.Operation.Migrations
{
    /// <inheritdoc />
    public partial class InitialOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operation");

            migrationBuilder.CreateTable(
                name: "room",
                schema: "operation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    room_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    room_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "AVAILABLE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room", x => x.id);
                    table.CheckConstraint("ck_room_status", "status IN ('AVAILABLE', 'UNAVAILABLE', 'MAINTENANCE')");
                });

            migrationBuilder.CreateTable(
                name: "staff",
                schema: "operation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fullname = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    hired_date = table.Column<DateOnly>(type: "date", nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    note = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff", x => x.id);
                    table.CheckConstraint("ck_staff_gender", "gender IN ('MALE', 'FEMALE', 'OTHER')");
                    table.CheckConstraint("ck_staff_role", "role IN ('MANAGER', 'RECEPTIONIST', 'STAFF')");
                    table.CheckConstraint("ck_staff_status", "status IN ('ACTIVE', 'INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "service_session",
                schema: "operation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    booking_details_id = table.Column<long>(type: "bigint", nullable: false),
                    room_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "WAITING"),
                    expected_start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expected_finish_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_session", x => x.id);
                    table.CheckConstraint("ck_service_session_actual_time_order", "started_at IS NULL OR finished_at IS NULL OR finished_at >= started_at");
                    table.CheckConstraint("ck_service_session_expected_time_order", "expected_start_at IS NULL OR expected_finish_at IS NULL OR expected_finish_at >= expected_start_at");
                    table.CheckConstraint("ck_service_session_status", "status IN ('WAITING', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')");
                    table.ForeignKey(
                        name: "fk_service_session_room",
                        column: x => x.room_id,
                        principalSchema: "operation",
                        principalTable: "room",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_shift",
                schema: "operation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    shift_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "SCHEDULED"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_shift", x => x.id);
                    table.CheckConstraint("ck_staff_shift_status", "status IN ('SCHEDULED', 'COMPLETED', 'CANCELLED')");
                    table.CheckConstraint("ck_staff_shift_time_order", "end_time > start_time");
                    table.ForeignKey(
                        name: "fk_staff_shift_staff",
                        column: x => x.staff_id,
                        principalSchema: "operation",
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "staff_assignment",
                schema: "operation",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_session_id = table.Column<long>(type: "bigint", nullable: false),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_assignment", x => x.id);
                    table.ForeignKey(
                        name: "fk_staff_assignment_service_session",
                        column: x => x.service_session_id,
                        principalSchema: "operation",
                        principalTable: "service_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_staff_assignment_staff",
                        column: x => x.staff_id,
                        principalSchema: "operation",
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_room_room_name",
                schema: "operation",
                table: "room",
                column: "room_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_session_booking_details_id",
                schema: "operation",
                table: "service_session",
                column: "booking_details_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_session_room_id",
                schema: "operation",
                table: "service_session",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_session_status",
                schema: "operation",
                table: "service_session",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_staff_email",
                schema: "operation",
                table: "staff",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_phone",
                schema: "operation",
                table: "staff",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_assignment_service_session_id",
                schema: "operation",
                table: "staff_assignment",
                column: "service_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_assignment_service_session_id_staff_id",
                schema: "operation",
                table: "staff_assignment",
                columns: new[] { "service_session_id", "staff_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_assignment_staff_id",
                schema: "operation",
                table: "staff_assignment",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_shift_shift_date",
                schema: "operation",
                table: "staff_shift",
                column: "shift_date");

            migrationBuilder.CreateIndex(
                name: "IX_staff_shift_staff_id",
                schema: "operation",
                table: "staff_shift",
                column: "staff_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_shift_staff_id_shift_date",
                schema: "operation",
                table: "staff_shift",
                columns: new[] { "staff_id", "shift_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_assignment",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "staff_shift",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "service_session",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "staff",
                schema: "operation");

            migrationBuilder.DropTable(
                name: "room",
                schema: "operation");
        }
    }
}
