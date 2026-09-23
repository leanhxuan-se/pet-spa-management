using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using PetSpa.Modules.Customer.Domain.Entities;

namespace PetSpa.Modules.Customer.Domain.Entities
{
    public class Pet
    {
        public long Id { get; set; }

        public long CustomersId { get; set; }
        // // reference to get Customer from Pet, obligated for a 1 (Customer) : N (pet) relationship, will not create Customer column in database
        public Customer Customer { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public double? Weight { get; set; }
        public int? Height { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Status { get; set; } = "Active";
        public string? SpecialNote { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
