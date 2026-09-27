using PetSpa.Modules.Billing.Domain.Enums;

namespace PetSpa.Modules.Billing.Domain.Entities;

public class Invoice
{
    public long Id { get; set; }

    // Logical reference sang Booking module
    public long BookingId { get; set; }

    public long Subtotal { get; set; }

    public long DiscountAmount { get; set; }

    public long TotalAmount { get; set; }

    public InvoiceStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime FinishedAt { get; set; }

    // Navigation trong Billing module
    public ICollection<InvoiceItem> Items { get; set; }
        = new List<InvoiceItem>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
