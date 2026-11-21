using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetSpa.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "petspa_auth");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accounts",
                schema: "petspa_auth");
        }
    }
}
