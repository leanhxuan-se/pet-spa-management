
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Domain.Entities
{
    public class Booking
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public long PetId { get; set; }
        public DateTime BookingTime { get; set; } = DateTime.UtcNow;
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string? Note { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<BookingDetail> BookingDetails { get; set; } = [];
        
    }
}
