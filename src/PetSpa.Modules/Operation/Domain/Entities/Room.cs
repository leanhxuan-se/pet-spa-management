
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class Room
    {
        public long Id { get; set; }
        public string RoomName { get; set; } = null!;
        public string? RoomType { get; set; }
        public RoomStatus Status { get; set; } = RoomStatus.AVAILABLE;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<ServiceSession> ServiceSessions { get; set; } = [];
    }
}
