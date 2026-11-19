# Bàn giao Customer, Resource và Booking

## Hợp đồng HTTP cho WPF

Dùng `POST /api/auth/staff/login` cho tài khoản nhân sự và `POST /api/auth/customer/login` cho khách. Lễ tân và quản lý thực hiện các thao tác tại quầy. Khách xem hồ sơ, thú cưng và lịch của mình, đồng thời có thể sửa hồ sơ cá nhân theo quyền. Chỉ lễ tân được gửi `specialNote` khi tạo hoặc sửa thú cưng, kể cả khi gửi `null` để xóa. Chỉ quản lý được ghi danh mục dịch vụ. [OpenAPI](openapi.yaml)

Để hiển thị tùy chọn có thể đặt, dùng `GET /api/services?categoryId=7&keyword=tam&activeOnly=true` và `GET /api/services/12/options?activeOnly=true&species=DOG&weight=5`. Bộ lọc cân nặng tính cả hai đầu khoảng. Khi tạo lịch, backend kiểm tra cặp khách và thú cưng đang ACTIVE, kiểm tra tùy chọn đang cung cấp và lưu bản sao liên hệ cùng đơn giá tại thời điểm đặt.

Gửi `PUT /api/bookings/42` khi lịch còn PENDING. Có thể gửi một hoặc nhiều trường trong ví dụ. Trường bỏ qua giữ giá trị hiện tại. `note: null` xóa ghi chú. Số liên hệ thay đổi trên lịch được lưu riêng với số điện thoại của hồ sơ khách.

```json
{
  "bookingTime": "2026-11-20T03:00:00Z",
  "contactName": "Nguyễn An",
  "contactPhone": "0900000001",
  "note": "Đón thú cưng tại quầy"
}
```

Tiếp nhận bằng `PATCH /api/booking/42` với body `{"status":"confirmed"}`. Sau khi các phiên đã kết thúc, gọi cùng route với body `{"status":"completed"}` để chốt trước khi yêu cầu lập hóa đơn. Mỗi thao tác trả `200` cùng lịch và có thể được gửi lại khi lịch vẫn ở trạng thái đích. Khi mọi dòng đã bàn giao đều bị hủy, dùng `DELETE /api/bookings/42`. Gọi hủy lại trả `204` và giữ nguyên `cancelledAt`.

Ví dụ phản hồi `200` sau khi tiếp nhận.

```json
{
  "id": 42,
  "customerId": 17,
  "petId": 32,
  "bookingTime": "2026-11-20T03:00:00Z",
  "checkIn": "2026-11-20T03:00:00Z",
  "checkOut": null,
  "note": "Đón thú cưng tại quầy",
  "status": "CONFIRMED",
  "createdAt": "2026-11-19T08:15:00Z",
  "updatedAt": "2026-11-20T03:00:00Z",
  "services": [
    { "id": 65, "serviceOptionId": 8, "unitPrice": 115000, "status": "ASSIGNED" }
  ],
  "contactName": "Nguyễn An",
  "contactPhone": "0900000001",
  "sessionsCreatedAt": null,
  "cancelledAt": null
}
```

`GET /api/bookings?date=2026-11-20&pageSize=25` lọc theo ngày UTC. Có thể thêm `status`, `customerId` và `contactPhone`. Gửi nguyên `nextCursor` cùng bộ lọc ở lần gọi tiếp theo. `nextCursor: null` báo hết kết quả. Thứ tự là COMPLETED, CONFIRMED, PAID, PENDING, CANCELLED, sau đó là giờ hẹn tăng dần với giờ trống ở cuối, rồi tới ID lịch.

## Contract nội bộ cho Operation và Billing

Khi tạo lịch, Booking gọi `ICustomerModule.GetBookingCustomerAsync(customerId, petId, ct)` để lấy cặp hồ sơ hợp lệ cùng liên hệ, loài và cân nặng. `IResourceModule.GetActiveServiceOptionsAsync(ids, ct)` cung cấp ID, đơn giá, thời lượng và giới hạn của tùy chọn đang hoạt động.

| Bên gọi | Phương thức | Trách nhiệm |
| --- | --- | --- |
| Operation | `IBookingModule.GetHandoffCandidateAsync(bookingId, transaction, ct)` | Gọi trước khi tạo phiên để khóa lịch và lấy tập dòng dự kiến. Nếu đã có `SessionsCreatedAt`, dùng tập dòng đã bàn giao để nhận diện lần gọi lại. |
| Operation | `IBookingModule.MarkSessionsCreatedAsync(bookingId, bookingDetailIds, transaction, ct)` | Lưu phiên của tất cả dòng dự kiến rồi gửi đúng tập ID trong cùng giao dịch. Gửi lại cùng tập trả mốc đã lưu. Tập thiếu, thừa hoặc trùng trả `Conflict`. |
| Operation | `IBookingModule.SyncDetailStatusAsync(bookingId, bookingDetailId, status, transaction, ct)` | Gọi cùng giao dịch với thay đổi của phiên. Booking kiểm tra cặp ID, tập bàn giao và chuyển trạng thái của dòng. Quầy chốt lịch qua PATCH. |
| Billing | `IBookingModule.GetBillableBookingAsync(bookingId, ct)` | Lấy các dòng COMPLETED với ID tùy chọn và đơn giá đã chốt. Dùng `IResourceModule.GetServiceOptionDescriptionsAsync(ids, ct)` để tra mô tả, kể cả khi danh mục đã ngừng cung cấp. |
| Billing | `IBookingModule.MarkPaidAsync(bookingId, transaction, ct)` | Gọi trong giao dịch ghi nhận thanh toán thành công. Booking trả lại kết quả khi lịch đã PAID. |

Bên điều phối mở giao dịch PostgreSQL và truyền `db.Database.CurrentTransaction.GetDbTransaction()` cho các lời gọi có tham số `transaction`. Booking tham gia giao dịch trên cùng kết nối. Commit sau khi mọi bước thành công. Rollback khi có lỗi hoặc khi contract trả `Conflict` hay `NotFound` ở bước ghi. Nếu kết nối bị gián đoạn lúc commit, dùng ID nghiệp vụ và kết quả gọi lặp để đối chiếu trước khi tạo thêm phiên hoặc lần thu.

Phía cung cấp contract nằm trong backend này. Việc gọi contract từ các luồng thật của Operation và Billing cần được nối ở các module tương ứng. Nếu cơ sở dữ liệu đang có khóa ngoại vật lý từ `operation.service_session` sang Booking, việc chuyển sang ID logic thuộc migration của Operation.

Bộ nghiệm thu PostgreSQL dùng các endpoint backend và bên gọi mô phỏng cho Operation và Billing. [Cách chạy bộ test](tests/README.md#booking-provider-acceptance-tests)

## Dữ liệu cũ

Trên cùng cơ sở dữ liệu, áp dụng migration Customer trước Booking. Customer chuẩn hóa điện thoại và email hiện có. Nếu các giá trị trùng sau chuẩn hóa, migration dừng để dữ liệu được đối soát rồi chạy lại. Migration Booking điền tên và điện thoại từ hồ sơ còn tra được. Lịch CHECKED_IN chuyển thành CONFIRMED và giữ nguyên `check_in` đã lưu. Những lịch có dấu hiệu đã có phiên cần đối soát tập dòng với Operation trước khi tiếp tục thao tác bàn giao.

```sql
SELECT booking_id, reason, identified_at
FROM booking.legacy_booking_review
ORDER BY booking_id, reason;
```

Dùng danh sách này để đối chiếu lịch từng ở CHECKED_IN, lịch thiếu mốc nhận khách hoặc mốc hủy, lịch thiếu liên hệ và lịch có tập phiên chưa xác minh. Mốc bàn giao và tập ID lịch sử cần có căn cứ từ dữ liệu phiên. Xem các trường hợp cụ thể trong migration trước khi xử lý dữ liệu vận hành.
