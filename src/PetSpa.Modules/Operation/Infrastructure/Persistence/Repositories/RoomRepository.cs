using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Endpoints.Requests;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class RoomRepository (OperationDbContext db): IRoomRepository
    {
        public async Task<List<RoomResponse>> GetAsync(CancellationToken ct)
        {
            var rooms = await db.Rooms
                .AsNoTracking()
                .Select(x => new RoomResponse(x.Id, x.RoomName, x.RoomType))
                .ToListAsync(ct);
            return rooms;
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
            var room = await db.Rooms.Where(x => x.Id == id).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException($"Room with ID {id} not found.");

            room.RoomName = request.RoomName ?? room.RoomName;
            room.RoomType = request.RoomType ?? room.RoomType;
            room.Status = request.Status ?? room.Status;
            room.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return await GetAsync(ct);
        }

        public async Task<List<RoomResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            var AllRoom = await db.Rooms
                .AsNoTracking()
                .Where(x => x.Status == RoomStatus.AVAILABLE)
                .Select(x => new RoomResponse (x.Id, x.RoomName, x.RoomType ))
                .ToListAsync(ct);

            var MatchServiceSession = await db.ServiceSessions
                .AsNoTracking()
                .Where(
                    x => x.Status == ServiceSessionStatus.WAITING &&
                    x.ExpectedStartAt < endTime &&
                    x.ExpectedFinishAt > startTime)
                .Select(x => x.RoomId)
                .ToListAsync(ct);

           var availableRoom = AllRoom
                .Where(x => MatchServiceSession.Contains(x.Id))
                .ToList();

            return availableRoom;
        }
        public async Task<List<RoomResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            var AllRoom = await db.Rooms
                .AsNoTracking()
                .Where(x => x.Status == RoomStatus.AVAILABLE)
                .Select(x => new RoomResponse (x.Id, x.RoomName, x.RoomType ))
                .ToListAsync(ct);

            var MatchServiceSession = await db.ServiceSessions
                .AsNoTracking()
                .Where(
                    x => x.Status == ServiceSessionStatus.WAITING &&
                    x.ExpectedStartAt < endTime &&
                    x.ExpectedFinishAt > startTime)
                .Select(x => x.RoomId)
                .ToListAsync(ct);

            var unAvailableRoom = AllRoom
                .Where(x => !MatchServiceSession.Contains(x.Id))
                .ToList();

            return unAvailableRoom;
        }
    }
}
