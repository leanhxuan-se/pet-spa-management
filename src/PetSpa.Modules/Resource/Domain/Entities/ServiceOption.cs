

using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Domain.Entities
{
    public class ServiceOption
    {
        public long Id { get; set; }
        public long ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public Species Species { get; set; }
        public int? MinWeight { get; set; }
        public int? MaxWeight { get; set; }
        public int? DurationMinutes { get; set; }
        public long UnitPrice { get; set; }
    }
}
