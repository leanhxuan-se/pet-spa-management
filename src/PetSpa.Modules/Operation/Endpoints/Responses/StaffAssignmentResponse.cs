// Dữ liệu phản hồi thông tin phân công nhân viên trên phiên dịch vụ.
namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record StaffAssignmentResponse(
        long Id,
        long ServiceSessionId,
        long StaffId,
        string StaffName,
        bool IsActive,
        DateTime AssignedAt,
        DateTime? RevokedAt,
        long? ReplacedByAssignmentId,
        long? NotificationId
    );
}
