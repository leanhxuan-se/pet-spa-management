namespace PetSpa.Modules.Booking.Domain.Exceptions;

public sealed class BookingDetailInUseException(Exception cause)
    : Exception("Không thể bỏ dòng dịch vụ đã có phiên thực hiện.", cause)
{
}
