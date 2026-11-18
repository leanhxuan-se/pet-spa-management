using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Resource.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class ServiceSessionService(IServiceSessionRepository _ServiceSessionRepository)
    {
        public async Task<Result<ServiceSession>> CreateAsync(CreateServiceSessionRequest request, string currentRole, CancellationToken ct)
        {

            if (!(currentRole == "MANAGER" || currentRole == "RECEPTIONIST"))
            {

                return Result<ServiceSession>.BadRequest("message", "khách hàng và staff Không có quyền tạo phiên dịch vụ");
            }

            if (request?.ExpectedStartAt >= request?.ExpectedFinishAt)
            {
                return Result<ServiceSession>.TimeError("Thời gian bắt đầu dự kiến phải nhỏ hơn thời gian kết thúc dự kiến!");

            }

            if(request is null)
            {
                return Result<ServiceSession>.BadRequest("message", "Yêu cầu không hợp lệ, nội dung không được để trống!");
            }

            var serrviceSession = await _ServiceSessionRepository.CreateServiceSessionAsync(request, ct);

            return Result<ServiceSession>.Success(serrviceSession);
        }
        
    }
}
