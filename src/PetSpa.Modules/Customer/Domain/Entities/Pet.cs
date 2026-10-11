using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Domain.Entities
{
    public class Pet
    {
        public long Id { get; set; }

        public long CustomersId { get; set; }
        // // reference to get Customer from Pet, obligated for a 1 (Customer) : N (pet) relationship, will not create Customer column in database
        public Customer Customer { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public Species Species { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public Gender? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public PetStatus Status { get; set; }
        public string? SpecialNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
