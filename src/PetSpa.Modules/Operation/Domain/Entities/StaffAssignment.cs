

namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class StaffAssignment
    {
        public long Id { get; set; }
        public long ServiceSessionId { get; set; }
        public ServiceSession ServiceSession { get; set; } = null!;
        public long StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public DateTime? AssignedAt { get; set; }
    }
}
