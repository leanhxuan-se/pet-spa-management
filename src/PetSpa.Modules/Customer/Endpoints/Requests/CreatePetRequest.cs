// Định nghĩa dữ liệu đầu vào CreatePetRequest.
using System.Text.Json.Serialization;
using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record CreatePetRequest
    {
        private string? specialNote;

        public long CustomersId { get; init; }
        public string Name { get; init; } = string.Empty;
        public Species? Species { get; init; }
        public Gender? Gender { get; init; }
        public decimal? Weight { get; init; }
        public decimal? Height { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public PetStatus Status { get; init; } = PetStatus.ACTIVE;

        [JsonIgnore]
        internal bool HasSpecialNote { get; private init; }

        public string? SpecialNote
        {
            get => specialNote;
            init
            {
                specialNote = value;
                HasSpecialNote = true;
            }
        }
    }
}
