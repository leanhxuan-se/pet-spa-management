
namespace PetSpa.Modules.Customer.Domain.Entities
{
    public class Customer
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly? Date_of_Birth { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvtURL { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }

        // reference to get pets from a Customer, obligated for a 1 (Customer) : N (pet) relationship, will not create Pets column in database
        public ICollection<Pet> Pets { get; set; } = [];
    }
}
