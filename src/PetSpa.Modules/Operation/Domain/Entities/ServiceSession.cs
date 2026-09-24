
namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class ServiceSession
    {
        public long Id { get; set; } 
        public long BookingDetailId { get; set; }
        public long RoomId { get; set; }
        public Room Room { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime? ExpectedStartAt { get; set; }
        public DateTime? ExpectedFinishAt { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<StaffAssignment> StaffAssignments { get; set; } = [];


    }
}
