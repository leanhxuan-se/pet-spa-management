using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class RoomRepository(OperationDbContext db) : IRoomRepository
    {
        public async Task<List<RoomResponse>> GetAsync(CancellationToken ct)
        {
            return await db.Rooms
                .AsNoTracking()
                .Select(x => new RoomResponse(x.Id, x.RoomName, x.RoomType))
                .ToListAsync(ct);
        }

        public async Task<List<RoomResponse>> CreateAsync(CreateRoomRequest request, CancellationToken ct)
        {
            var room = new Room
            {
                RoomName = request.RoomName,
                RoomType = request.RoomType,
                Status = RoomStatus.AVAILABLE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Rooms.Add(room);
            await db.SaveChangesAsync(ct);
            return await GetAsync(ct);
        }

        public async Task<List<RoomResponse>> UpdateAsync(long id, UpdateRoomRequest request, CancellationToken ct)
        {
            var room = await db.Rooms.Where(x => x.Id == id).FirstOrDefaultAsync(ct)
                ?? throw new KeyNotFoundException($"Room with ID {id} not found.");

            room.RoomName = request.RoomName ?? room.RoomName;
            room.RoomType = request.RoomType ?? room.RoomType;
            room.Status = request.Status ?? room.Status;
            room.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return await GetAsync(ct);
        }

        public Task<List<RoomResponse>> GetAvailableAtAsync(DateTime nowUtc, CancellationToken ct)
            => QueryAvailabilityAtAsync(nowUtc, available: true, ct);

        public Task<List<RoomResponse>> GetUnAvailableAtAsync(DateTime nowUtc, CancellationToken ct)
            => QueryAvailabilityAtAsync(nowUtc, available: false, ct);

        public Task<List<RoomResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
            => QueryAvailabilityInRangeAsync(startTime, endTime, available: true, ct);

        public Task<List<RoomResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
            => QueryAvailabilityInRangeAsync(startTime, endTime, available: false, ct);

        private Task<List<RoomResponse>> QueryAvailabilityAtAsync(DateTime nowUtc, bool available, CancellationToken ct)
        {
            // Phiên chiếm phòng tại thời điểm hiện tại gồm phiên IN_PROGRESS hoặc phiên hiệu lực bao phủ nowUtc.
            var occupiedRoomIds = db.ServiceSessions
                .AsNoTracking()
                .Where(s =>
                    s.Status != ServiceSessionStatus.CANCELLED
                    && s.Status != ServiceSessionStatus.COMPLETED
                    && (s.Status == ServiceSessionStatus.IN_PROGRESS
                        || (s.ExpectedStartAt != null
                            && s.ExpectedFinishAt != null
                            && s.ExpectedStartAt <= nowUtc
                            && s.ExpectedFinishAt > nowUtc)))
                .Select(s => s.RoomId);

            return QueryRoomsByOccupancyAsync(occupiedRoomIds, available, ct);
        }

        private Task<List<RoomResponse>> QueryAvailabilityInRangeAsync(DateTime startTime, DateTime endTime, bool available, CancellationToken ct)
        {
            // Hai khoảng [ExpectedStartAt, ExpectedFinishAt) và [startTime, endTime) không trùng khi chạm đúng mốc đầu/cuối.
            var occupiedRoomIds = db.ServiceSessions
                .AsNoTracking()
                .Where(s =>
                    s.Status != ServiceSessionStatus.CANCELLED
                    && s.Status != ServiceSessionStatus.COMPLETED
                    && s.ExpectedStartAt != null
                    && s.ExpectedFinishAt != null
                    && s.ExpectedStartAt < endTime
                    && s.ExpectedFinishAt > startTime)
                .Select(s => s.RoomId);

            return QueryRoomsByOccupancyAsync(occupiedRoomIds, available, ct);
        }

        private async Task<List<RoomResponse>> QueryRoomsByOccupancyAsync(
            IQueryable<long> occupiedRoomIds,
            bool available,
            CancellationToken ct)
        {
            var query = available
                ? db.Rooms.AsNoTracking().Where(r => r.Status == RoomStatus.AVAILABLE && !occupiedRoomIds.Contains(r.Id))
                : db.Rooms.AsNoTracking().Where(r => r.Status != RoomStatus.AVAILABLE || occupiedRoomIds.Contains(r.Id));

            return await query
                .OrderBy(r => r.Id)
                .Select(r => new RoomResponse(r.Id, r.RoomName, r.RoomType))
                .ToListAsync(ct);
        }
    }
}
