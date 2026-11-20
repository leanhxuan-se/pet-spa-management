// Khai báo hợp đồng truy xuất và cập nhật dữ liệu phân công nhân viên trên phiên dịch vụ.
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    // Giao diện làm việc với bảng operation.staff_assignment và các điều kiện ràng buộc liên quan.
    internal interface IStaffAssignmentRepository
    {
        // Lấy phiên dịch vụ kèm phòng và toàn bộ phân công (có tracking để thực hiện đổi/hủy phân công).
        Task<ServiceSession?> GetSessionWithAssignmentsAsync(long serviceSessionId, CancellationToken ct);

        // Lấy thông tin nhân viên theo Id để kiểm tra tồn tại và trạng thái ACTIVE (không tracking).
        Task<Staff?> GetStaffByIdAsync(long staffId, CancellationToken ct);

        // Kiểm tra nhân viên có ca làm SCHEDULED hoặc WORKING bao phủ trọn khoảng giờ dự kiến của phiên.
        Task<bool> HasCoveringShiftAsync(long staffId, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, CancellationToken ct);

        // Kiểm tra nhân viên có phân công hiệu lực khác trùng khung giờ trên phiên chưa hoàn tất/chưa hủy hay không.
        Task<bool> HasOverlappingActiveAssignmentAsync(long staffId, DateTime startUtc, DateTime endUtc, long excludeSessionId, CancellationToken ct);

        // Thêm bản ghi phân công mới vào DbContext.
        Task AddAsync(StaffAssignment assignment, CancellationToken ct);

        // Lưu thay đổi xuống cơ sở dữ liệu.
        Task SaveChangesAsync(CancellationToken ct);

        // Thực thi chuỗi thao tác ghi phân công và thông báo trong cùng một database transaction.
        Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
    }
}
