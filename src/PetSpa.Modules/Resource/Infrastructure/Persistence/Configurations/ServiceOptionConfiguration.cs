using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceOptionConfiguration: IEntityTypeConfiguration<ServiceOption>
    {
        public void Configure(EntityTypeBuilder<ServiceOption> Builder)
        {
            Builder.ToTable("service_options");

            Builder.HasKey(x => x.Id);

            //Create 1 : N relationship Service : ServiceOptions
            Builder.HasOne(x => x.Service)
                .WithMany(y => y.ServiceOptions)
                .HasForeignKey(x => x.ServiceId)
                .HasConstraintName("fk_service_option_Service")
                .OnDelete(DeleteBehavior.Cascade); // auto delete Service_option when Service is deleted

            Builder.Property(x => x.ServiceId)
                .HasColumnName("service_id")
                .IsRequired();

            Builder.Property(x => x.Species)
                .HasColumnName("species")
                .HasMaxLength(50)
                .IsRequired();

            Builder.Property(x => x.MinWeight)
                .HasColumnName("min_weight");

            Builder.Property(x => x.MaxWeight)
                .HasColumnName("max_weight");

            Builder.Property(x => x.DurationMinutes)
                .HasColumnName("duration_minutes");

            Builder.Property(x => x.UnitPrice)
                .HasColumnName("unit_price");
        }
    }
}
