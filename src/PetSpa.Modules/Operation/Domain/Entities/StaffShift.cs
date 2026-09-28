using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Domain.Entities
{
    public class StaffShift
    {
        public long Id { get; set; }
        public long StaffId { get; set; }
        public Staff Staff { get; set; } = null!;
        public DateOnly ShiftDate { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public StaffShiftStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
       
    }
}
