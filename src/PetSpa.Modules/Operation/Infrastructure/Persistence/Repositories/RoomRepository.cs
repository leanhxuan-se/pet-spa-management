using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Responses;
namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class RoomRepository (OperationDbContext db): IRoomRepository
    {
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
