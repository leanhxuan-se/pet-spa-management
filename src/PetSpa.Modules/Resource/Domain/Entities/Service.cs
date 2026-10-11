using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Domain.Entities
{
    public class Service
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public string ServiceName { get; set; } = null!;
        public string? Description { get; set; }
        public ResourceStatus Status { get; set; } = ResourceStatus.ACTIVE;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        // Navigation trong Resource module
        public Category Category { get; set; } = null!;
        public ICollection<ServiceOption> ServiceOptions { get; set; } = [];
    }
}
