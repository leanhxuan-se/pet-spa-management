// Dữ liệu phản hồi một thông báo nội bộ gửi cho nhân viên.
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record StaffNotificationResponse(
        long Id,
        long StaffId,
        long ServiceSessionId,
        long StaffAssignmentId,
        StaffNotificationType Type,
        string Title,
        string Message,
        bool IsRead,
        DateTime? ReadAt,
        DateTime CreatedAt
    );
}
