using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class ServiceSessionConfiguration: IEntityTypeConfiguration<ServiceSession>
    {
        public void Configure(EntityTypeBuilder<ServiceSession> Builder)
        {
            Builder.ToTable("service_session", table =>
            {
                table.HasCheckConstraint("ck_service_session_status", "status IN ('WAITING', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')");
                table.HasCheckConstraint(
                    "ck_service_session_expected_time_order",
                    "expected_start_at IS NULL OR expected_finish_at IS NULL OR expected_finish_at >= expected_start_at");
                table.HasCheckConstraint(
                    "ck_service_session_actual_time_order",
                    "started_at IS NULL OR finished_at IS NULL OR finished_at >= started_at");
            });

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.Property(x => x.BookingDetailId)
                .HasColumnName("booking_details_id");

            Builder.HasIndex(x => x.BookingDetailId).IsUnique();
            Builder.HasIndex(x => x.RoomId);
            Builder.HasIndex(x => x.Status);

            Builder.HasOne(x => x.Room)
                .WithMany(y => y.ServiceSessions)
                .HasForeignKey(z => z.RoomId)
                .HasConstraintName("fk_service_session_room")
                .OnDelete(DeleteBehavior.Restrict);

            Builder.Property(x => x.RoomId)
                .HasColumnName("room_id");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(ServiceSessionStatus.WAITING)
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.ExpectedStartAt)
                .HasColumnName("expected_start_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.ExpectedFinishAt)
                .HasColumnName("expected_finish_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.FinishedAt)
                .HasColumnName("finished_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");


        }
    }
}
