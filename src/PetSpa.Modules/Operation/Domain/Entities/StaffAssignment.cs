// Định nghĩa thực thể StaffAssignment và dữ liệu liên quan.


namespace PetSpa.Modules.Operation.Domain.Entities
{
    internal class StaffAssignment
    {
        public long Id { get; set; }
        public long ServiceSessionId { get; set; }
        public ServiceSession ServiceSession { get; set; } = null!;
        public long StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        // Đánh dấu phân công còn hiệu lực; phân công bị đổi hoặc hủy sẽ đặt IsActive = false để giữ lịch sử.
        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        // Thời điểm thu hồi/hủy phân công khi đổi người hoặc rút nhân viên.
        public DateTime? RevokedAt { get; set; }
        // Mã phân công mới thay thế bản ghi này (chỉ có giá trị khi đổi người ở OPS-07A).
        public long? ReplacedByAssignmentId { get; set; }
    }
}
