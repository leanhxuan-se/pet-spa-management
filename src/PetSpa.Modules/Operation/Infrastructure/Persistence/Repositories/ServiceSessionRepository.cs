using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Resource.Contracts;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class ServiceSessionRepository(OperationDbContext db): IServiceSessionRepository
    {
        public async Task<ServiceSession> CreateServiceSessionAsync(CreateServiceSessionRequest request, CancellationToken ct)
        {

            var serviceSession = new ServiceSession
            {
                RoomId = request.RoomId,
                BookingDetailId = request.BookingDetailId,
                ExpectedStartAt = request.ExpectedStartAt,
                ExpectedFinishAt = request.ExpectedFinishAt
            };

            await db.ServiceSessions.AddAsync(serviceSession, ct);
            await db.SaveChangesAsync(ct);
            return serviceSession;
        }
    }
}
