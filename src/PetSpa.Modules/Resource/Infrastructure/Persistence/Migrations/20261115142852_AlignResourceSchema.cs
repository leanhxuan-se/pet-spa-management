using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignResourceSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL rounds values when converting to a narrower numeric scale.
            migrationBuilder.Sql("""
                LOCK TABLE resource.category, resource.service, resource.service_options IN ACCESS EXCLUSIVE MODE;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM resource.service_options
                        WHERE unit_price <> trunc(unit_price, 2)
                           OR min_weight <> trunc(min_weight, 2)
                           OR max_weight <> trunc(max_weight, 2)
                    ) THEN
                        RAISE EXCEPTION 'Resource migration requires existing prices and weights to have at most two decimal places.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_service_Category",
                schema: "resource",
                table: "service");

            migrationBuilder.DropForeignKey(
                name: "fk_service_option_Service",
                schema: "resource",
                table: "service_options");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "resource",
                table: "service_options",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                schema: "resource",
                table: "service_options",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "service_options",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "character varying");

            migrationBuilder.AlterColumn<string>(
                name: "species",
                schema: "resource",
                table: "service_options",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "min_weight",
                schema: "resource",
                table: "service_options",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "max_weight",
                schema: "resource",
                table: "service_options",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "resource",
                table: "service_options",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "resource",
                table: "service",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "service",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "character varying");

            migrationBuilder.AlterColumn<string>(
                name: "service_name",
                schema: "resource",
                table: "service",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "resource",
                table: "service",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "resource",
                table: "category",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "category",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ACTIVE",
                oldClrType: typeof(string),
                oldType: "character varying");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "resource",
                table: "category",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "resource",
                table: "category",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_options_duration",
                schema: "resource",
                table: "service_options",
                sql: "duration_minutes > 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_options_max_weight",
                schema: "resource",
                table: "service_options",
                sql: "max_weight >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_options_min_weight",
                schema: "resource",
                table: "service_options",
                sql: "min_weight >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_options_status",
                schema: "resource",
                table: "service_options",
                sql: "status IN ('ACTIVE', 'INACTIVE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_options_unit_price",
                schema: "resource",
                table: "service_options",
                sql: "unit_price >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_options_weight_range",
                schema: "resource",
                table: "service_options",
                sql: "min_weight IS NULL OR max_weight IS NULL OR max_weight >= min_weight");

            migrationBuilder.CreateIndex(
                name: "IX_service_category_id_service_name",
                schema: "resource",
                table: "service",
                columns: new[] { "category_id", "service_name" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_service_status",
                schema: "resource",
                table: "service",
                sql: "status IN ('ACTIVE', 'INACTIVE')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_category_status",
                schema: "resource",
                table: "category",
                sql: "status IN ('ACTIVE', 'INACTIVE')");

            migrationBuilder.AddForeignKey(
                name: "fk_service_category",
                schema: "resource",
                table: "service",
                column: "category_id",
                principalSchema: "resource",
                principalTable: "category",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_service_option_service",
                schema: "resource",
                table: "service_options",
                column: "service_id",
                principalSchema: "resource",
                principalTable: "service",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_service_category",
                schema: "resource",
                table: "service");

            migrationBuilder.DropForeignKey(
                name: "fk_service_option_service",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_options_duration",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_options_max_weight",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_options_min_weight",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_options_status",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_options_unit_price",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_options_weight_range",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropIndex(
                name: "IX_service_category_id_service_name",
                schema: "resource",
                table: "service");

            migrationBuilder.DropCheckConstraint(
                name: "ck_service_status",
                schema: "resource",
                table: "service");

            migrationBuilder.DropCheckConstraint(
                name: "ck_category_status",
                schema: "resource",
                table: "category");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "resource",
                table: "service_options",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<decimal>(
                name: "unit_price",
                schema: "resource",
                table: "service_options",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "service_options",
                type: "character varying",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "species",
                schema: "resource",
                table: "service_options",
                type: "character varying",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "min_weight",
                schema: "resource",
                table: "service_options",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "max_weight",
                schema: "resource",
                table: "service_options",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(6,2)",
                oldPrecision: 6,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "resource",
                table: "service_options",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "resource",
                table: "service",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "service",
                type: "character varying",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "service_name",
                schema: "resource",
                table: "service",
                type: "character varying",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "resource",
                table: "service",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "resource",
                table: "category",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "category",
                type: "character varying",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldDefaultValue: "ACTIVE");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                schema: "resource",
                table: "category",
                type: "character varying",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "resource",
                table: "category",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AddForeignKey(
                name: "fk_service_Category",
                schema: "resource",
                table: "service",
                column: "category_id",
                principalSchema: "resource",
                principalTable: "category",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_service_option_Service",
                schema: "resource",
                table: "service_options",
                column: "service_id",
                principalSchema: "resource",
                principalTable: "service",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
