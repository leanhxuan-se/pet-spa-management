using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string? Avt_Url { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime Created_at { get; set; } = DateTime.UtcNow;
        public DateTime Updated_at { get; set; }
        public DateTime Last_login_at { get; set; }

        // reference to get pets from a Customer, obligated for a 1 (Customer) : N (pet) relationship, will not create Pets column in database
        public ICollection<Pet> Pets { get; set; } = [];
    }
}
