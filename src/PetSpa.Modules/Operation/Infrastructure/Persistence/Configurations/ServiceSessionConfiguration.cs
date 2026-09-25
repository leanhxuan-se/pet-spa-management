using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class ServiceSessionConfiguration: IEntityTypeConfiguration<ServiceSession>
    {
        public void Configure(EntityTypeBuilder<ServiceSession> Builder)
        {
            Builder.ToTable("service_session");

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.BookingDetailId)
                .HasColumnName("booking_detail_id");

            Builder.HasOne(x => x.Room)
                .WithMany(y => y.ServiceSessions)
                .HasForeignKey(z => z.RoomId)
                .HasConstraintName("fk_service_session_room")
                .OnDelete(DeleteBehavior.Cascade);

            Builder.Property(x => x.RoomId)
                .HasColumnName("room_id");

            Builder.Property(x => x.Status)
                .HasColumnName("status  ");

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
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");


        }
    }
}
