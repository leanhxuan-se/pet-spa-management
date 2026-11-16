// Định nghĩa các giá trị PaymentStatus.
namespace PetSpa.Modules.Billing.Domain.Enums;

public enum PaymentStatus
{
    PENDING,
    SUCCESS,
    FAILED,
    REFUNDED
}