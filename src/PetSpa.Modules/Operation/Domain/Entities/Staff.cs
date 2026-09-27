
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class Staff
    {
        public long Id { get; set; }
        public string FullName { get; set; } = null!;
        public Gender Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Email { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public StaffPosition Position { get; set; }
        public DateOnly? HiredDate { get; set; }
        public string PasswordHash { get; set; } = null!;
        public string? AvtURL { get; set; }
        public string? Note { get; set; }
        public StaffStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public ICollection<StaffShift> StaffShifts { get; set; } = [];
        public ICollection<StaffAssignment> StaffAssignments { get; set; } = [];
    }
}
