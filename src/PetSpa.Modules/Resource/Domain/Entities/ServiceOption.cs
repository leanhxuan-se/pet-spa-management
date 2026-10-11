

using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Domain.Entities
{
    public class ServiceOption
    {
        public long Id { get; set; }
        public long ServiceId { get; set; }
        public Species Species { get; set; }
        public decimal? MinWeight { get; set; }
        public decimal? MaxWeight { get; set; }
        public int DurationMinutes { get; set; }
        public decimal UnitPrice { get; set; }
        public ResourceStatus Status { get; set; } = ResourceStatus.ACTIVE;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation trong Resource module
        public Service Service { get; set; } = null!;
    }
}
