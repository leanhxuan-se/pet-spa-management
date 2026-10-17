using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Domain.Entities
{
    public class BookingDetail
    {
        public long Id { get; set; }
        public long BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public long ServiceOptionsId { get; set; }
        public decimal UnitPrice { get; set; }
        public BookingDetailStatus Status { get; set; } = BookingDetailStatus.ASSIGNED;
    }
}
