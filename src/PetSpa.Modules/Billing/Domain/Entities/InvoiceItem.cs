namespace PetSpa.Modules.Billing.Domain.Entities;

public class InvoiceItem
{
    public long Id { get; set; }

    // FK thật trong Billing module
    public long InvoiceId { get; set; }

    // Logical reference sang Booking module
    public long BookingDetailId { get; set; }

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public long UnitPrice { get; set; }

    public long Amount { get; set; }

    // Navigation nội bộ Billing
    public Invoice Invoice { get; set; } = null!;
}
