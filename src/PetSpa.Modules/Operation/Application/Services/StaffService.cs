using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Operation.Application.Services
{
    internal class StaffService(IStaffRepository _staffRepository)
    {
        public async Task<Result<List<StaffResponse>>> GetAvailableStaffsAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            if(startTime > endTime)
            {
                return Result<List<StaffResponse>>.TimeError("Thời gian bắt đầu không được lớn hơn thời gian kết thúc!");

            }

            var availStaff = await  _staffRepository.GetAvailableAsync(startTime, endTime, ct);

            if(availStaff is [] or null)
            {
                return Result<List<StaffResponse>>.NotFound("Không có nhân viên nào đang rảnh ở khoảng thời gian đã chọn!");

            }

            return Result<List<StaffResponse>>.Success(availStaff);
        }
        public async Task<Result<List<StaffResponse>>> GetUnAvailableStaffsAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            if(startTime > endTime)
            {
                return Result<List<StaffResponse>>.TimeError("Thời gian bắt đầu không được lớn hơn thời gian kết thúc!");

            }

            var unAvailStaff = await  _staffRepository.GetAvailableAsync(startTime, endTime, ct);

            if(unAvailStaff is [] or null)
            {
                return Result<List<StaffResponse>>.NotFound("Không có nhân viên nào đang bận ở khoảng thời gian đã chọn!");
            }

            return Result<List<StaffResponse>>.Success(unAvailStaff);
        }
    }
}
