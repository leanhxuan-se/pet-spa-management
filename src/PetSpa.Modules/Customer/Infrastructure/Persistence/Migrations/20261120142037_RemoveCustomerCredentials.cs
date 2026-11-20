using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomerCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM customer.customer WHERE NULLIF(password_hash, '') IS NOT NULL) THEN
                        IF to_regclass('petspa_auth.accounts') IS NULL THEN
                            RAISE EXCEPTION 'Apply ConsolidateAccounts before dropping profile credentials.';
                        END IF;
                        IF EXISTS (
                            SELECT 1 FROM customer.customer p
                            WHERE NULLIF(p.password_hash, '') IS NOT NULL
                              AND NOT EXISTS (
                                  SELECT 1 FROM petspa_auth.accounts a WHERE a.customer_id = p."Id"
                              )
                        ) THEN
                            RAISE EXCEPTION 'Some profile credentials have not been migrated to Auth.';
                        END IF;
                    END IF;
                END $migration$;
                """);

            migrationBuilder.DropColumn(
                name: "last_login_at",
                schema: "customer",
                table: "customer");

            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "customer",
                table: "customer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Restore the pre-cutover backup to revert profile credential removal.");
    }
}