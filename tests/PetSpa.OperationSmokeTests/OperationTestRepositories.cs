using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Abstractions;

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
        new StaffResponse(11, "Available Staff One", null, null, "", "", "Groomer", StaffRole.STAFF, null, null, null, StaffStatus.ACTIVE),
        new StaffResponse(12, "Available Staff Two", null, null, "", "", "Receptionist", StaffRole.STAFF, null, null, null, StaffStatus.ACTIVE)
    ];

    public List<StaffResponse> UnavailableStaff { get; } =
    [
        new StaffResponse(13, "Unavailable Staff", null, null, "", "", "Groomer", StaffRole.STAFF, null, null, null, StaffStatus.ACTIVE)
    ];

}

internal sealed class FakeRoomRepository(OperationTestStore store) : IRoomRepository
{
    public Task<List<RoomResponse>> GetAsync(CancellationToken ct) =>
        Task.FromResult(store.AvailableRooms.Concat(store.UnavailableRooms).ToList());
    public Task<List<RoomResponse>> CreateAsync(CreateRoomRequest request, CancellationToken ct) =>
        throw new NotSupportedException();
    public Task<Result<List<RoomResponse>>> UpdateAsync(long id, UpdateRoomRequest request, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<List<RoomResponse>> GetAvailableAtAsync(DateTime nowUtc, CancellationToken ct) =>
        Task.FromResult(store.AvailableRooms.ToList());

    public Task<List<RoomResponse>> GetUnAvailableAtAsync(DateTime nowUtc, CancellationToken ct) =>
        Task.FromResult(store.UnavailableRooms.ToList());

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
    public Task<List<StaffResponse>> GetAsync(CancellationToken ct) => Task.FromResult(store.AvailableStaff.ToList());
    public Task<StaffResponse?> GetByIdAsync(long id, CancellationToken ct) =>
        Task.FromResult(store.AvailableStaff.SingleOrDefault(x => x.Id == id));
    public Task<StaffResponse> CreateAsync(CreateStaffRequest request, CancellationToken ct) =>
        throw new NotSupportedException();
    public Task<StaffResponse?> UpdateAsync(long id, UpdateStaffRequest request, CancellationToken ct) =>
        throw new NotSupportedException();
    public Task<StaffResponse?> DeleteAsync(long id, CancellationToken ct) =>
        throw new NotSupportedException();

    public Task<List<StaffResponse>> GetAvailabilityAsync(
        DateTime startUtc, DateTime? endUtc, bool available, CancellationToken ct)
        => Task.FromResult((available ? store.AvailableStaff : store.UnavailableStaff).ToList());
}
