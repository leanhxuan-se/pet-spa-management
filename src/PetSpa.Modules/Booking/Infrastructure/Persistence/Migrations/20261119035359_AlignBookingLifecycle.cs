using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignBookingLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_booking_status",
                schema: "booking",
                table: "booking");

            migrationBuilder.AlterColumn<DateTime>(
                name: "booking_time",
                schema: "booking",
                table: "booking",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                schema: "booking",
                table: "booking",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_name",
                schema: "booking",
                table: "booking",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                schema: "booking",
                table: "booking",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<long[]>(
                name: "handed_off_detail_ids",
                schema: "booking",
                table: "booking",
                type: "bigint[]",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "legacy_handoff_needs_review",
                schema: "booking",
                table: "booking",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "sessions_created_at",
                schema: "booking",
                table: "booking",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE TABLE booking.legacy_booking_review (
                    booking_id bigint NOT NULL,
                    reason text NOT NULL,
                    identified_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    PRIMARY KEY (booking_id, reason)
                );

                DO $booking_lifecycle$
                DECLARE
                    checked_in_total bigint;
                    checked_in_with_timestamp bigint;
                BEGIN
                    SELECT count(*), count(check_in) INTO checked_in_total, checked_in_with_timestamp
                    FROM booking.booking WHERE status = 'CHECKED_IN';
                    RAISE NOTICE 'Legacy CHECKED_IN bookings: %, with check_in timestamp: %',
                        checked_in_total, checked_in_with_timestamp;

                    INSERT INTO booking.legacy_booking_review (booking_id, reason)
                    SELECT id, 'CHECKED_IN_MAPPED' FROM booking.booking WHERE status = 'CHECKED_IN';

                    INSERT INTO booking.legacy_booking_review (booking_id, reason)
                    SELECT id, 'CHECK_IN_TIMESTAMP_MISSING' FROM booking.booking
                    WHERE status IN ('CHECKED_IN', 'CONFIRMED') AND check_in IS NULL;

                    INSERT INTO booking.legacy_booking_review (booking_id, reason)
                    SELECT id, 'CANCELLATION_TIMESTAMP_MISSING' FROM booking.booking WHERE status = 'CANCELLED';

                    IF to_regclass('customer.customer') IS NOT NULL THEN
                        UPDATE booking.booking b
                        SET contact_name = CASE
                                WHEN char_length(c.full_name) BETWEEN 1 AND 150 THEN c.full_name END,
                            contact_phone = CASE
                                WHEN c.phone ~ '^[+]84[35789][0-9]{8}$' THEN '0' || substring(c.phone FROM 4)
                                WHEN char_length(c.phone) BETWEEN 1 AND 30 THEN c.phone END
                        FROM (
                            SELECT "Id", regexp_replace(fullname, '^[[:space:]]+|[[:space:]]+$', '', 'g') AS full_name,
                                regexp_replace(phone, '^[[:space:]]+|[[:space:]]+$', '', 'g') AS phone
                            FROM customer.customer
                        ) c
                        WHERE c."Id" = b.customer_id;
                    END IF;

                    INSERT INTO booking.legacy_booking_review (booking_id, reason)
                    SELECT id, 'CONTACT_SNAPSHOT_MISSING' FROM booking.booking
                    WHERE contact_name IS NULL OR contact_phone IS NULL;

                    INSERT INTO booking.legacy_booking_review (booking_id, reason)
                    SELECT b.id, 'SESSION_HANDOFF_UNVERIFIED' FROM booking.booking b
                    WHERE b.status = 'COMPLETED' OR EXISTS (
                        SELECT 1 FROM booking.booking_details d
                        WHERE d.booking_id = b.id AND d.status IN ('WORKING', 'COMPLETED'));

                    IF to_regclass('operation.service_session') IS NOT NULL THEN
                        INSERT INTO booking.legacy_booking_review (booking_id, reason)
                        SELECT d.booking_id, 'SESSION_HANDOFF_UNVERIFIED'
                        FROM booking.booking_details d
                        JOIN operation.service_session s ON s.booking_details_id = d.id
                        UNION
                        SELECT b.id, 'SESSION_HANDOFF_UNVERIFIED'
                        FROM booking.booking b
                        JOIN operation.service_session s ON to_jsonb(s)->>'booking_id' = b.id::text
                        ON CONFLICT (booking_id, reason) DO NOTHING;
                    END IF;

                    UPDATE booking.booking b SET legacy_handoff_needs_review = true
                    WHERE EXISTS (
                        SELECT 1 FROM booking.legacy_booking_review r
                        WHERE r.booking_id = b.id AND r.reason IN ('CHECK_IN_TIMESTAMP_MISSING', 'SESSION_HANDOFF_UNVERIFIED'));

                    UPDATE booking.booking SET status = 'CONFIRMED' WHERE status = 'CHECKED_IN';
                END;
                $booking_lifecycle$;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_booking_booking_time",
                schema: "booking",
                table: "booking",
                column: "booking_time");

            migrationBuilder.CreateIndex(
                name: "IX_booking_contact_phone",
                schema: "booking",
                table: "booking",
                column: "contact_phone");

            migrationBuilder.CreateIndex(
                name: "IX_booking_customer_id",
                schema: "booking",
                table: "booking",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_pet_id",
                schema: "booking",
                table: "booking",
                column: "pet_id");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status",
                schema: "booking",
                table: "booking",
                column: "status");

            migrationBuilder.AddCheckConstraint(
                name: "ck_booking_handoff",
                schema: "booking",
                table: "booking",
                sql: "(sessions_created_at IS NULL AND handed_off_detail_ids IS NULL) OR (sessions_created_at IS NOT NULL AND handed_off_detail_ids IS NOT NULL AND cardinality(handed_off_detail_ids) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_booking_status",
                schema: "booking",
                table: "booking",
                sql: "status IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'PAID', 'CANCELLED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_booking_booking_time",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "IX_booking_contact_phone",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "IX_booking_customer_id",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "IX_booking_pet_id",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropIndex(
                name: "IX_booking_status",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropCheckConstraint(
                name: "ck_booking_handoff",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropCheckConstraint(
                name: "ck_booking_status",
                schema: "booking",
                table: "booking");

            migrationBuilder.Sql("""
                DO $booking_lifecycle$
                BEGIN
                    IF EXISTS (SELECT 1 FROM booking.booking WHERE status = 'PAID' OR booking_time IS NULL) THEN
                        RAISE EXCEPTION 'Previous Booking schema cannot represent PAID bookings or missing booking_time.';
                    END IF;
                END;
                $booking_lifecycle$;

                UPDATE booking.booking b SET status = 'CHECKED_IN'
                FROM booking.legacy_booking_review r
                WHERE r.booking_id = b.id AND r.reason = 'CHECKED_IN_MAPPED' AND b.status = 'CONFIRMED';

                DROP TABLE booking.legacy_booking_review;
                """);

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "contact_name",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "handed_off_detail_ids",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "legacy_handoff_needs_review",
                schema: "booking",
                table: "booking");

            migrationBuilder.DropColumn(
                name: "sessions_created_at",
                schema: "booking",
                table: "booking");

            migrationBuilder.AlterColumn<DateTime>(
                name: "booking_time",
                schema: "booking",
                table: "booking",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_booking_status",
                schema: "booking",
                table: "booking",
                sql: "status IN ('PENDING', 'CONFIRMED', 'CHECKED_IN', 'COMPLETED', 'CANCELLED')");
        }
    }
}
