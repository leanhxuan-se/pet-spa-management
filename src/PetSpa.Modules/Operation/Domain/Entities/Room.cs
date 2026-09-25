
namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class Room
    {
        public long Id { get; set; }
        public string RoomName { get; set; } = null!;
        public string RoomType { get; set; } = null!;
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ServiceSession> ServiceSessions { get; set; } = [];
    }
}
