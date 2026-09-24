using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Booking.Domain.Entities
{
    public class BookingDetail
    {
        public long Id { get; set; }
        public long BookingId { get; set; }
        public Booking Booking { get; set; } = null!;
        public long ServiceOptionsId { get; set; }
        public long UnitPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
