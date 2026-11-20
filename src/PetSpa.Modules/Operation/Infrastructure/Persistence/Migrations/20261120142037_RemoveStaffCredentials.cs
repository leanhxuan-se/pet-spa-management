using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaffCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_staff_shift_status",
                schema: "operation",
                table: "staff_shift");

            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM operation.staff WHERE NULLIF(password_hash, '') IS NOT NULL) THEN
                        IF to_regclass('petspa_auth.accounts') IS NULL THEN
                            RAISE EXCEPTION 'Apply ConsolidateAccounts before dropping profile credentials.';
                        END IF;
                        IF EXISTS (
                            SELECT 1 FROM operation.staff p
                            WHERE NULLIF(p.password_hash, '') IS NOT NULL
                              AND NOT EXISTS (
                                  SELECT 1 FROM petspa_auth.accounts a WHERE a.staff_id = p.id
                              )
                        ) THEN
                            RAISE EXCEPTION 'Some profile credentials have not been migrated to Auth.';
                        END IF;
                    END IF;
                END $migration$;
                """);

            migrationBuilder.DropColumn(
                name: "last_login_at",
                schema: "operation",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "operation",
                table: "staff");

            migrationBuilder.AddColumn<DateTime>(
                name: "finished_at",
                schema: "operation",
                table: "staff_shift",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "started_at",
                schema: "operation",
                table: "staff_shift",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE operation.staff SET gender = 'OTHER' WHERE gender IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "operation",
                table: "staff",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "room_type",
                schema: "operation",
                table: "room",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_staff_shift_actual_time_order",
                schema: "operation",
                table: "staff_shift",
                sql: "started_at IS NULL OR finished_at IS NULL OR finished_at >= started_at");

            migrationBuilder.AddCheckConstraint(
                name: "ck_staff_shift_status",
                schema: "operation",
                table: "staff_shift",
                sql: "status IN ('SCHEDULED', 'WORKING', 'COMPLETED', 'CANCELLED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Restore the pre-cutover backup to revert profile credential removal.");
    }
}
