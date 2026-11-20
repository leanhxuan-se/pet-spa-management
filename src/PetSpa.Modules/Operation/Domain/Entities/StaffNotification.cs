// Định nghĩa thực thể StaffNotification lưu thông báo nội bộ trong Operation.
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Domain.Entities
{
    // Thông báo in-app gửi cho nhân viên khi được giao việc, đổi phân công hoặc nhắc việc.
    internal class StaffNotification
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public long ServiceSessionId { get; set; }
        public ServiceSession ServiceSession { get; set; } = null!;
        public long StaffAssignmentId { get; set; }
        public StaffAssignment StaffAssignment { get; set; } = null!;
        public StaffNotificationType Type { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        // Khóa duy nhất theo sự kiện giao việc hoặc header Idempotency-Key để chống tạo trùng thông báo.
        public string? IdempotencyKey { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
