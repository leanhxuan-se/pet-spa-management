// Dữ liệu phản hồi một phiên dịch vụ đang được giao cho nhân viên đăng nhập (OPS-11).
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record MyServiceSessionResponse(
        long AssignmentId,
        DateTime AssignedAt,
        long ServiceSessionId,
        long BookingDetailId,
        long RoomId,
        string RoomName,
        ServiceSessionStatus Status,
        DateTime? ExpectedStartAt,
        DateTime? ExpectedFinishAt,
        DateTime? StartedAt,
        DateTime? FinishedAt
    );
}
