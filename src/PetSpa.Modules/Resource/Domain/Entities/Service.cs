using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSpa.Modules.Resource.Domain.Entities
{
    public class Service
    {
        public long Id { get; set; }
        // relationship with Category 1 : N services
        public long CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ServiceOption> ServiceOptions { get; set; } = [];
    }
}
