using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "resource",
                table: "service_options",
                type: "character varying",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "resource",
                table: "category",
                type: "character varying",
                nullable: true);

            // Existing rows must have a valid enum value before enforcing NOT NULL.
            migrationBuilder.Sql("UPDATE resource.service_options SET status = 'ACTIVE';");
            migrationBuilder.Sql("UPDATE resource.category SET status = 'ACTIVE';");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "service_options",
                type: "character varying",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "resource",
                table: "category",
                type: "character varying",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                schema: "resource",
                table: "service_options");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "resource",
                table: "category");
        }
    }
}
