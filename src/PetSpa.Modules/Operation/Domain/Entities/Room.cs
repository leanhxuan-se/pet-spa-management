
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class Room
    {
        public long Id { get; set; }
        public string RoomName { get; set; } = null!;
        public RoomType RoomType { get; set; }
        public RoomStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ServiceSession> ServiceSessions { get; set; } = [];
    }
}
