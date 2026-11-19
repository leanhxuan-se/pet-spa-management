// Định nghĩa thực thể Booking và dữ liệu liên quan.

using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Domain.Entities

{
    public class Booking
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public long PetId { get; set; }
        public DateTime? BookingTime { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public DateTime? SessionsCreatedAt { get; set; }
        public long[]? HandedOffDetailIds { get; set; }
        public DateTime? CancelledAt { get; set; }
        public bool LegacyHandoffNeedsReview { get; set; }
        public string? Note { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.PENDING;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<BookingDetail> BookingDetails { get; set; } = [];

        public bool HasCompleteHandoff()
        {
            if (LegacyHandoffNeedsReview || SessionsCreatedAt is null || HandedOffDetailIds is not { Length: > 0 } ids)
                return false;

            var handedOff = ids.ToHashSet();
            var current = BookingDetails.Select(detail => detail.Id).ToHashSet();
            return handedOff.Count == ids.Length && handedOff.IsSubsetOf(current)
                && BookingDetails.All(detail => detail.Status == BookingDetailStatus.CANCELLED || handedOff.Contains(detail.Id));
        }

        // PostgreSQL lưu timestamptz đến micro giây, giữ kết quả ghi và đọc nhất quán.
        public static DateTime AtDatabasePrecision(DateTime time) => time.AddTicks(-(time.Ticks % 10));

        public static DateTime UtcNow() => AtDatabasePrecision(DateTime.UtcNow);
    }
}
