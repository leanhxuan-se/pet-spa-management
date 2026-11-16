using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Domain.Repositories;
using PetSpa.Modules.Operation.Endpoints.Responses;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;

namespace PetSpa.OperationSmokeTests;

internal sealed class OperationTestStore
{
    public List<RoomResponse> AvailableRooms { get; } =
    [
        new RoomResponse(1, "Bath Room", "BATH")
    ];

    public List<RoomResponse> UnavailableRooms { get; } =
    [
        new RoomResponse(2, "Grooming Room", "GROOMING")
    ];

    public List<StaffResponse> AvailableStaff { get; } =
    [
        new StaffResponse(11, "Available Staff One", "Groomer", null),
        new StaffResponse(12, "Available Staff Two", "Receptionist", null)
    ];

    public List<StaffResponse> UnavailableStaff { get; } =
    [
        new StaffResponse(13, "Unavailable Staff", "Groomer", null)
    ];

    public List<StaffTable> StaffMembers { get; } =
    [
        new StaffTable
        {
            Id = 1,
            FullName = "Test Manager",
            Email = "manager@example.test",
            Phone = "0900000001",
            Position = "Manager",
            Role = StaffRole.MANAGER,
            Status = StaffStatus.ACTIVE,
            PasswordHash = string.Empty
        }
    ];

    public int StaffSaveCount { get; set; }
}

internal sealed class FakeRoomRepository(OperationTestStore store) : IRoomRepository
{
    public Task<List<RoomResponse>> GetAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct)
        => Task.FromResult(store.AvailableRooms.ToList());

    public Task<List<RoomResponse>> GetUnAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct)
        => Task.FromResult(store.UnavailableRooms.ToList());
}

internal sealed class FakeStaffRepository(OperationTestStore store) : IStaffRepository
{
    public Task<List<StaffResponse>> GetAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct)
        => Task.FromResult(store.AvailableStaff.ToList());

    public Task<List<StaffResponse>> GetUnAvailableAsync(
        DateTime startTime,
        DateTime endTime,
        CancellationToken ct)
        => Task.FromResult(store.UnavailableStaff.ToList());
}

internal sealed class FakeStaffAuthRepository(OperationTestStore store) : IStaffAuthRepository
{
    public Task<StaffTable?> GetByPhoneAsync(string phone, CancellationToken ct)
        => Task.FromResult(store.StaffMembers.SingleOrDefault(staff => staff.Phone == phone));

    public Task SaveAsync(CancellationToken ct)
    {
        store.StaffSaveCount++;
        return Task.CompletedTask;
    }
}
