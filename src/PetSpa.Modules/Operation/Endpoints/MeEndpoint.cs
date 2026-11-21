// Định nghĩa các Minimal API endpoint cá nhân (/api/me) cho nhân viên đang đăng nhập.
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;

namespace PetSpa.Modules.Operation.Endpoints
{
    // Gom nhóm các endpoint /api/me dùng chung cho OPS-07C và OPS-11.
    internal static class MeEndpoint
    {
        // Đăng ký các route tải phiên được giao, tải thông báo và đánh dấu đã đọc của nhân viên.
        public static IEndpointRouteBuilder MapMeEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/me").WithTags("Me");

            group.MapGet("/service-sessions", GetMyServiceSessionsAsync).WithName("GetMyServiceSessions").RequireAuthorization();
            group.MapGet("/notifications", GetMyNotificationsAsync).WithName("GetMyNotifications").RequireAuthorization();
            group.MapPut("/notifications/{id:long}/read", MarkNotificationAsReadAsync).WithName("MarkMyNotificationAsRead").RequireAuthorization();

            return app;
        }

        // OPS-11: Lấy danh sách phiên dịch vụ có phân công còn hiệu lực của nhân viên đăng nhập.
        private static async Task<IResult> GetMyServiceSessionsAsync(
            ServiceSessionService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.GetMyAssignedSessionsAsync(currentUser, ct)).ToHttpResult();

        // OPS-07C & OPS-11: Lấy danh sách thông báo và số chưa đọc của nhân viên đăng nhập.
        private static async Task<IResult> GetMyNotificationsAsync(
            StaffNotificationService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.GetMyNotificationsAsync(currentUser, ct)).ToHttpResult();

        // OPS-07C: Đánh dấu một thông báo của nhân viên là đã đọc.
        private static async Task<IResult> MarkNotificationAsReadAsync(
            [FromRoute] long id,
            StaffNotificationService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.MarkAsReadAsync(id, currentUser, ct)).ToHttpResult();
    }
}
