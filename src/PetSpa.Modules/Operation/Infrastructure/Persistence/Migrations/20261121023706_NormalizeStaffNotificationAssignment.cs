using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeStaffNotificationAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Do not silently change a notification's recipient or session on existing databases.
            migrationBuilder.Sql("""
                LOCK TABLE operation.staff_notification, operation.staff_assignment IN SHARE ROW EXCLUSIVE MODE;
                DO $migration$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM operation.staff_notification AS n
                        JOIN operation.staff_assignment AS a ON a.id = n.staff_assignment_id
                        WHERE n.staff_id IS DISTINCT FROM a.staff_id
                           OR n.service_session_id IS DISTINCT FROM a.service_session_id
                    ) THEN
                        RAISE EXCEPTION 'Staff notification recipient or session does not match its assignment. Resolve inconsistent rows before migrating.';
                    END IF;
                END $migration$;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_staff_notification_service_session",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.DropForeignKey(
                name: "fk_staff_notification_staff",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.DropIndex(
                name: "IX_staff_notification_service_session_id",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.DropIndex(
                name: "IX_staff_notification_staff_assignment_id",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.DropIndex(
                name: "IX_staff_notification_staff_id_is_read_created_at",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.DropColumn(
                name: "service_session_id",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.DropColumn(
                name: "staff_id",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.CreateIndex(
                name: "IX_staff_notification_staff_assignment_id_is_read_created_at",
                schema: "operation",
                table: "staff_notification",
                columns: new[] { "staff_assignment_id", "is_read", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_staff_notification_staff_assignment_id_is_read_created_at",
                schema: "operation",
                table: "staff_notification");

            migrationBuilder.AddColumn<long>(
                name: "service_session_id",
                schema: "operation",
                table: "staff_notification",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "staff_id",
                schema: "operation",
                table: "staff_notification",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE operation.staff_notification AS n
                SET staff_id = a.staff_id, service_session_id = a.service_session_id
                FROM operation.staff_assignment AS a
                WHERE a.id = n.staff_assignment_id;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "service_session_id",
                schema: "operation",
                table: "staff_notification",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "staff_id",
                schema: "operation",
                table: "staff_notification",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_notification_service_session_id",
                schema: "operation",
                table: "staff_notification",
                column: "service_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_notification_staff_assignment_id",
                schema: "operation",
                table: "staff_notification",
                column: "staff_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_staff_notification_staff_id_is_read_created_at",
                schema: "operation",
                table: "staff_notification",
                columns: new[] { "staff_id", "is_read", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_staff_notification_service_session",
                schema: "operation",
                table: "staff_notification",
                column: "service_session_id",
                principalSchema: "operation",
                principalTable: "service_session",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_staff_notification_staff",
                schema: "operation",
                table: "staff_notification",
                column: "staff_id",
                principalSchema: "operation",
                principalTable: "staff",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
