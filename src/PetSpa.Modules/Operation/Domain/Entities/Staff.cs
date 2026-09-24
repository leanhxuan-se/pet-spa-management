
namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class Staff
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public DateOnly? DateOfBirth { get; set; }
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Position { get; set; } = null!;
        public DateTime? HiredDate { get; set; }
        public string PasswordHash { get; set; } = null!;
        public string? AvtURL { get; set; }
        public string? Note { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public ICollection<StaffShift> StaffShifts { get; set; } = [];
        public ICollection<StaffAssignment> StaffAssignments { get; set; } = [];
    }
}
