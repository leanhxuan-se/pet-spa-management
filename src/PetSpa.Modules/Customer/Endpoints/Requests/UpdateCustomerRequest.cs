// Định nghĩa dữ liệu đầu vào UpdateCustomerRequest.
using System.Text.Json.Serialization;
using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Endpoints.Requests
{
    public record UpdateCustomerRequest
    {
        private string? email;
        private CustomerStatus? status;

        public string? FullName { get; init; }
        public Gender? Gender { get; init; }
        public DateOnly? DateOfBirth { get; init; }
        public string? Phone { get; init; }
        public string? AvtURL { get; init; }
        public string? Note { get; init; }

        [JsonIgnore]
        internal bool HasEmail { get; private init; }

        [JsonIgnore]
        internal bool HasStatus { get; private init; }

        public string? Email
        {
            get => email;
            init
            {
                email = value;
                HasEmail = true;
            }
        }

        public CustomerStatus? Status
        {
            get => status;
            init
            {
                status = value;
                HasStatus = true;
            }
        }
    }
}
