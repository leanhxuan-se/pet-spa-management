// Định nghĩa các giá trị InvoiceStatus.
namespace PetSpa.Modules.Billing.Domain.Enums;

public enum InvoiceStatus
{
    DRAFT,
    UNPAID,
    PARTIALLY_PAID,
    PAID,
    CANCELLED
}