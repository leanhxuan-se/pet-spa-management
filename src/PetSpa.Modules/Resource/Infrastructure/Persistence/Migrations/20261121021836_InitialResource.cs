using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "resource");

            migrationBuilder.CreateTable(
                name: "category",
                schema: "resource",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.id);
                    table.CheckConstraint("ck_category_status", "status IN ('ACTIVE', 'INACTIVE')");
                });

            migrationBuilder.CreateTable(
                name: "service",
                schema: "resource",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category_id = table.Column<long>(type: "bigint", nullable: false),
                    service_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service", x => x.id);
                    table.CheckConstraint("ck_service_status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.ForeignKey(
                        name: "fk_service_category",
                        column: x => x.category_id,
                        principalSchema: "resource",
                        principalTable: "category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_options",
                schema: "resource",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<long>(type: "bigint", nullable: false),
                    species = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    min_weight = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    max_weight = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "ACTIVE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_options", x => x.id);
                    table.CheckConstraint("ck_service_options_duration", "duration_minutes > 0");
                    table.CheckConstraint("ck_service_options_max_weight", "max_weight >= 0");
                    table.CheckConstraint("ck_service_options_min_weight", "min_weight >= 0");
                    table.CheckConstraint("ck_service_options_status", "status IN ('ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("ck_service_options_unit_price", "unit_price >= 0");
                    table.CheckConstraint("ck_service_options_weight_range", "min_weight IS NULL OR max_weight IS NULL OR max_weight >= min_weight");
                    table.ForeignKey(
                        name: "fk_service_option_service",
                        column: x => x.service_id,
                        principalSchema: "resource",
                        principalTable: "service",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_category_name",
                schema: "resource",
                table: "category",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_category_id",
                schema: "resource",
                table: "service",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_category_id_service_name",
                schema: "resource",
                table: "service",
                columns: new[] { "category_id", "service_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_options_service_id",
                schema: "resource",
                table: "service_options",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_options",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "service",
                schema: "resource");

            migrationBuilder.DropTable(
                name: "category",
                schema: "resource");
        }
    }
}
