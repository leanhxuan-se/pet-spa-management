using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Domain.Entities
{
    public class Customer
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public Gender? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Email { get; set; }
        public string Phone { get; set; } = null!;
        public string? PasswordHash { get; set; }
        public string? AvtURL { get; set; }
        public string? Note { get; set; }
        public CustomerStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // reference to get pets from a Customer, obligated for a 1 (Customer) : N (pet) relationship, will not create Pets column in database
        public ICollection<Pet> Pets { get; set; } = [];
    }
}
