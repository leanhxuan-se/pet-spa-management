using PetSpa.Modules.Billing.Domain.Enums;

namespace PetSpa.Modules.Billing.Domain.Entities;

public class Payment
{
    public long Id { get; set; }

    // FK thật tới Invoice
    public long InvoiceId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus Status { get; set; }

    public string TransactionReference { get; set; } = string.Empty;

    public DateTime PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation nội bộ Billing
    public Invoice Invoice { get; set; } = null!;
}
