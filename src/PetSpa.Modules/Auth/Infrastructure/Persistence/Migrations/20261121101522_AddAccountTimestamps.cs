using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "petspa_auth",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_login_at",
                schema: "petspa_auth",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                schema: "petspa_auth",
                table: "accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "petspa_auth",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "last_login_at",
                schema: "petspa_auth",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "updated_at",
                schema: "petspa_auth",
                table: "accounts");
        }
    }
}
