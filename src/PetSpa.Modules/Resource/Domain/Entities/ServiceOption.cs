using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetSpa.Modules.Resource.Domain.Entities
{
    public class ServiceOption
    {
        public long Id { get; set; }
        public long ServiceId { get; set; }
        public Service Service { get; set; } = null!;
        public string Species { get; set; } = string.Empty;
        public int? MinWeight { get; set; }
        public int? MaxWeight { get; set; }
        public int? DurationMinutes { get; set; }
        public long UnitPrice { get; set; }
    }
}
