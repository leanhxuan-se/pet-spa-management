using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Domain.Entities
{
    public class Category
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ResourceStatus Status { get; set; } = ResourceStatus.ACTIVE;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Service> Services { get; set; } = [];
    }
}
