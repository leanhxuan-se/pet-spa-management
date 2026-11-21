// Dữ liệu phản hồi danh sách thông báo và số lượng chưa đọc của nhân viên đang đăng nhập.
namespace PetSpa.Modules.Operation.Endpoints.Responses
{
    internal record MyNotificationListResponse(
        int UnreadCount,
        IReadOnlyList<StaffNotificationResponse> Items
    );
}
