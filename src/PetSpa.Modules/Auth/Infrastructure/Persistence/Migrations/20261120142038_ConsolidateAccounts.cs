using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PetSpa.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "petspa_auth",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    login_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: true),
                    staff_id = table.Column<long>(type: "bigint", nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    token_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                    table.CheckConstraint("ck_accounts_profile", "(customer_id IS NOT NULL AND customer_id > 0 AND staff_id IS NULL) OR (staff_id IS NOT NULL AND staff_id > 0 AND customer_id IS NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_accounts_customer_id",
                schema: "petspa_auth",
                table: "accounts",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_login_phone",
                schema: "petspa_auth",
                table: "accounts",
                column: "login_phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_staff_id",
                schema: "petspa_auth",
                table: "accounts",
                column: "staff_id",
                unique: true);
            migrationBuilder.Sql("""
                CREATE FUNCTION pg_temp.petspa_normalize_phone(value text)
                RETURNS text LANGUAGE sql IMMUTABLE STRICT AS $phone$
                    WITH trimmed AS (
                        SELECT regexp_replace(value, '^[[:space:]]+|[[:space:]]+$', '', 'g') AS phone
                    )
                    SELECT CASE WHEN phone ~ '^[+]84[35789][0-9]{8}$'
                                THEN '0' || substr(phone, 4) ELSE phone END FROM trimmed;
                $phone$;

                DO $migration$
                BEGIN
                    IF to_regclass('customer.customer') IS NULL OR to_regclass('operation.staff') IS NULL THEN
                        RAISE EXCEPTION 'Apply Customer and Operation profile migrations before ConsolidateAccounts.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM petspa_auth."AspNetUserClaims")
                        OR EXISTS (SELECT 1 FROM petspa_auth."AspNetRoleClaims")
                        OR EXISTS (SELECT 1 FROM petspa_auth."AspNetUserLogins")
                        OR EXISTS (
                            SELECT 1 FROM petspa_auth."AspNetUsers" u
                            LEFT JOIN customer.customer c ON c."Id" = u."CustomerId"
                            LEFT JOIN operation.staff s ON s.id = u."StaffId"
                            LEFT JOIN petspa_auth."AspNetUserRoles" ur ON ur."UserId" = u."Id"
                            LEFT JOIN petspa_auth."AspNetRoles" r ON r."Id" = ur."RoleId"
                            WHERE u."ProvisioningState" <> 'Ready' OR u."TwoFactorEnabled"
                               OR NULLIF(u."PasswordHash", '') IS NULL
                               OR (c."Id" IS NULL AND s.id IS NULL)
                               OR (r."Id" IS NOT NULL AND lower(r."Name") IS DISTINCT FROM
                                   CASE WHEN c."Id" IS NOT NULL THEN 'customer' ELSE lower(s.role) END)
                        )
                    THEN
                        RAISE EXCEPTION 'Resolve Identity credentials, roles or profile links before ConsolidateAccounts.';
                    END IF;
                END $migration$;

                INSERT INTO petspa_auth.accounts
                    (login_phone, password_hash, customer_id, staff_id, is_enabled, token_version, locked_until_utc)
                SELECT pg_temp.petspa_normalize_phone("PhoneNumber"), "PasswordHash", "CustomerId", "StaffId",
                       "IsActive", "TokenVersion",
                       CASE WHEN "LockoutEnabled" THEN "LockoutEnd" END
                FROM petspa_auth."AspNetUsers";

                INSERT INTO petspa_auth.accounts (login_phone, password_hash, customer_id)
                SELECT pg_temp.petspa_normalize_phone(c.phone), c.password_hash, c."Id"
                FROM customer.customer c
                WHERE NULLIF(c.password_hash, '') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM petspa_auth.accounts a WHERE a.customer_id = c."Id");

                INSERT INTO petspa_auth.accounts (login_phone, password_hash, staff_id)
                SELECT pg_temp.petspa_normalize_phone(s.phone), s.password_hash, s.id
                FROM operation.staff s
                WHERE NULLIF(s.password_hash, '') IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM petspa_auth.accounts a WHERE a.staff_id = s.id);

                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM petspa_auth.accounts WHERE login_phone = '') THEN
                        RAISE EXCEPTION 'Resolve blank login phone numbers before ConsolidateAccounts.';
                    END IF;
                END $migration$;
                """);


            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "refresh_tokens",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "petspa_auth");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "petspa_auth");

        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Restore the pre-cutover backup to revert the account migration.");
    }
}