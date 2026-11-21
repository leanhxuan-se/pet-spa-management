using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkServiceSessionsToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "booking_id",
                schema: "operation",
                table: "service_session",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF to_regclass('booking.booking_details') IS NOT NULL THEN
                        UPDATE operation.service_session AS session
                        SET booking_id = detail.booking_id
                        FROM booking.booking_details AS detail
                        WHERE session.booking_details_id = detail.id;
                    END IF;
                END;
                $migration$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_service_session_booking_id",
                schema: "operation",
                table: "service_session",
                column: "booking_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_session_booking_id",
                schema: "operation",
                table: "service_session");

            migrationBuilder.DropColumn(
                name: "booking_id",
                schema: "operation",
                table: "service_session");
        }
    }
}
