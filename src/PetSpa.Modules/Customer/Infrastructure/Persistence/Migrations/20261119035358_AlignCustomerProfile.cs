using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignCustomerProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    WITH trimmed AS (
                        SELECT "Id",
                            regexp_replace(phone, '^[[:space:]]+|[[:space:]]+$', '', 'g') AS phone,
                            NULLIF(lower(regexp_replace(email, '^[[:space:]]+|[[:space:]]+$', '', 'g')), '') AS email
                        FROM customer.customer
                    ),
                    normalized AS (
                        SELECT "Id", email,
                            CASE WHEN phone ~ '^[+]84[35789][0-9]{8}$'
                                THEN '0' || substring(phone FROM 4) ELSE phone END AS phone
                        FROM trimmed
                    )
                    UPDATE customer.customer AS customer
                    SET phone = normalized.phone, email = normalized.email
                    FROM normalized
                    WHERE customer."Id" = normalized."Id"
                        AND (customer.phone IS DISTINCT FROM normalized.phone
                            OR customer.email IS DISTINCT FROM normalized.email);
                EXCEPTION WHEN unique_violation THEN
                    RAISE EXCEPTION 'Customer contact values collide after normalization. Resolve the duplicate contacts before retrying the migration.'
                        USING ERRCODE = '23505';
                END
                $migration$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_pet_customer",
                schema: "customer",
                table: "pet");

            migrationBuilder.RenameColumn(
                name: "avt_url",
                schema: "customer",
                table: "customer",
                newName: "avatar_url");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "customer",
                table: "pet",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "customer",
                table: "pet",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "customer",
                table: "pet",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "customer",
                table: "pet",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "customer",
                table: "pet",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "customer",
                table: "customer",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "customer",
                table: "customer",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "customer",
                table: "customer",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "customer",
                table: "customer",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fullname",
                schema: "customer",
                table: "customer",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "customer",
                table: "customer",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "customer",
                table: "customer",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "avatar_url",
                schema: "customer",
                table: "customer",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_pet_height_positive",
                schema: "customer",
                table: "pet",
                sql: "height > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_pet_weight_positive",
                schema: "customer",
                table: "pet",
                sql: "weight > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_pet_customer",
                schema: "customer",
                table: "pet",
                column: "customer_id",
                principalSchema: "customer",
                principalTable: "customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pet_customer",
                schema: "customer",
                table: "pet");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pet_height_positive",
                schema: "customer",
                table: "pet");

            migrationBuilder.DropCheckConstraint(
                name: "ck_pet_weight_positive",
                schema: "customer",
                table: "pet");

            migrationBuilder.RenameColumn(
                name: "avatar_url",
                schema: "customer",
                table: "customer",
                newName: "avt_url");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "customer",
                table: "pet",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "customer",
                table: "pet",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "customer",
                table: "pet",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "customer",
                table: "pet",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "customer",
                table: "pet",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "customer",
                table: "customer",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "customer",
                table: "customer",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                schema: "customer",
                table: "customer",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                schema: "customer",
                table: "customer",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fullname",
                schema: "customer",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                schema: "customer",
                table: "customer",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "customer",
                table: "customer",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "avt_url",
                schema: "customer",
                table: "customer",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_pet_customer",
                schema: "customer",
                table: "pet",
                column: "customer_id",
                principalSchema: "customer",
                principalTable: "customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
