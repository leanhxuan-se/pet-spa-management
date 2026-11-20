// Định nghĩa các Minimal API endpoint cho phiên dịch vụ, phân công nhân viên và nhắc việc nội bộ.
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Endpoints
{
    // Gom nhóm các endpoint /api/service-sessions của module Operation.
    internal static class ServiceSessionEndpoint
    {
        // Đăng ký các route phiên dịch vụ, phân công (OPS-07 -> OPS-07C) và bắt đầu thực hiện phiên (OPS-08).
        public static IEndpointRouteBuilder MapServiceSessionEndpoint(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/service-sessions").WithTags("ServiceSession");

            group.MapGet("/{id:long}", GetServiceSessionByIdAsync).WithName("GetServiceSessionById").RequireAuthorization();
            group.MapPut("/{id:long}", UpdateServiceSessionAsync).WithName("UpdateServiceSession").RequireAuthorization();
            group.MapPost("/{id:long}/start", StartServiceSessionAsync).WithName("StartServiceSession").RequireAuthorization();

            group.MapPost("/{id:long}/assignments", AssignStaffAsync).WithName("AssignStaffToSession").RequireAuthorization();
            group.MapPut("/{id:long}/assignments/{assignmentId:long}", ReassignStaffAsync).WithName("ReassignStaffOnSession").RequireAuthorization();
            group.MapDelete("/{id:long}/assignments/{assignmentId:long}", RevokeAssignmentAsync).WithName("RevokeStaffAssignment").RequireAuthorization();

            group.MapPost("/{id:long}/notifications", SendAssignmentReminderAsync).WithName("SendAssignmentReminder").RequireAuthorization();

            return app;
        }

        // OPS-07C: Xem chi tiết phiên dịch vụ (kiểm tra quyền lễ tân/quản lý hoặc nhân viên còn phân công hiệu lực).
        private static async Task<IResult> GetServiceSessionByIdAsync(
            [FromRoute] long id,
            ServiceSessionService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.GetByIdAsync(id, currentUser, ct)).ToHttpResult();

        // OPS-06A: Điều chỉnh phòng và khung giờ dự kiến cho phiên WAITING.
        private static async Task<IResult> UpdateServiceSessionAsync(
            [FromRoute] long id,
            [FromBody] UpdateServiceSessionRequest req,
            ServiceSessionService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.UpdateAsync(id, req, currentUser, ct)).ToHttpResult();

        // OPS-08: Nhân viên được phân công bắt đầu thực hiện phiên dịch vụ.
        private static async Task<IResult> StartServiceSessionAsync(
            [FromRoute] long id,
            ServiceSessionService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.StartAsync(id, currentUser, ct)).ToHttpResult();

        // OPS-07: Phân công nhân viên và tự động gửi thông báo lần đầu.
        private static async Task<IResult> AssignStaffAsync(
            [FromRoute] long id,
            [FromBody] AssignStaffRequest req,
            StaffAssignmentService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.AssignAsync(id, req, currentUser, ct)).ToHttpResult();

        // OPS-07A: Đổi nhân viên phụ trách phân công và lưu lịch sử.
        private static async Task<IResult> ReassignStaffAsync(
            [FromRoute] long id,
            [FromRoute] long assignmentId,
            [FromBody] ReassignStaffRequest req,
            StaffAssignmentService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.ReassignAsync(id, assignmentId, req, currentUser, ct)).ToHttpResult();

        // OPS-07B: Hủy phân công mềm trên phiên WAITING.
        private static async Task<IResult> RevokeAssignmentAsync(
            [FromRoute] long id,
            [FromRoute] long assignmentId,
            StaffAssignmentService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
            => (await service.RevokeAsync(id, assignmentId, currentUser, ct)).ToHttpResult();

        // OPS-07C: Gửi nhắc việc thủ công kèm hỗ trợ header Idempotency-Key.
        private static async Task<IResult> SendAssignmentReminderAsync(
            [FromRoute] long id,
            [FromBody] RemindAssignmentRequest req,
            HttpRequest httpRequest,
            StaffNotificationService service,
            ClaimsPrincipal currentUser,
            CancellationToken ct)
        {
            var idempotencyKey = httpRequest.Headers["Idempotency-Key"].FirstOrDefault();
            return (await service.SendReminderAsync(id, req, idempotencyKey, currentUser, ct)).ToHttpResult();
        }
    }
}
