# Booking API

Tất cả endpoint yêu cầu bearer token. Nhân viên (`account_type=staff`) có thể
thao tác cho khách hàng; khách hàng (`account_type=customer`, `customer_id`)
chỉ được đọc/tạo/sửa booking của chính mình. Chỉ nhân viên được thay đổi
`status`, `checkIn`, `checkOut` hoặc chuyển booking sang khách hàng khác.
Khách hàng chỉ được sửa booking đang `PENDING` hoặc `CONFIRMED`.

| Method | Endpoint | Chức năng |
|---|---|---|
| POST | `/api/bookings` | Tạo booking cùng danh sách dịch vụ |
| GET | `/api/bookings/{id}` | Lấy booking và chi tiết dịch vụ |
| PUT | `/api/bookings/{id}/services` | Thay toàn bộ danh sách tùy chọn dịch vụ |
| PUT | `/api/bookings/{id}` | Cập nhật thông tin booking, giữ nguyên danh sách dịch vụ |

## Tạo booking

```json
{
  "customerId": 1,
  "petId": 10,
  "bookingTime": "2026-10-17T03:00:00Z",
  "note": "Thú cưng nhạy cảm với tiếng ồn",
  "serviceOptionIds": [100, 200]
}
```

Trả `201` với `Location` trỏ đến GET chi tiết. Booking bắt đầu ở `PENDING`,
chi tiết ở `ASSIGNED`. Client chỉ gửi ID option; backend lấy giá từ contract
Resource và lưu `BookingDetail.UnitPrice` làm snapshot.

## Đổi dịch vụ

`PUT /api/bookings/1/services`:

```json
{
  "serviceOptionIds": [100, 300]
}
```

Danh sách mới phải có ít nhất một ID, tất cả ID dương và không trùng.
Option giữ lại giữ nguyên ID chi tiết, trạng thái và giá snapshot. Option mới
lấy giá hiện tại và bắt đầu ở `ASSIGNED`. Chi tiết không được chọn sẽ bị xóa;
không được xóa chi tiết đang `WORKING` hoặc đã `COMPLETED`.
Chỉ được đổi dịch vụ khi booking đang `PENDING` hoặc `CONFIRMED`.
Trả `200` với booking sau khi thay đổi.

## Cập nhật booking

`PUT /api/bookings/1`:

```json
{
  "customerId": 1,
  "petId": 10,
  "bookingTime": "2026-10-17T04:00:00Z",
  "note": "Đổi giờ hẹn",
  "status": "CONFIRMED",
  "checkIn": null,
  "checkOut": null
}
```

Đây là PUT toàn bộ thông tin có thể sửa. `customerId`, `petId`, `bookingTime`
và `status` bắt buộc hợp lệ; bỏ `note`, `checkIn`, `checkOut` đưa chúng về null.
Khách hàng phải gửi nguyên trạng thái và thời gian check-in/out hiện tại.
Trả `200`. Không thay đổi giá hay chi tiết dịch vụ.

Trạng thái hợp lệ: `PENDING`, `CONFIRMED`, `CHECKED_IN`, `COMPLETED`, `CANCELLED`.
API này kiểm tra giá trị enum và thứ tự thời gian; chưa áp dụng đồ thị chuyển
trạng thái, điều phối phòng/nhân viên hoặc tự đồng bộ trạng thái chi tiết.

## Kiểm tra trước khi lưu

Mọi luồng ghi đều gọi contract trước khi sửa entity và `SaveChangesAsync`:

- `ICustomerModule.PetBelongsToCustomerAsync`: Pet và Customer tồn tại,
  Pet thuộc đúng Customer. Áp dụng với cặp mới khi cập nhật thông tin và cặp
  đang lưu khi đổi dịch vụ.
- `IResourceModule.GetActiveServiceOptionsAsync`: tất cả option sẽ được lưu
  phải tồn tại; option, service và category cha đều `ACTIVE`. Khi cập nhật
  thông tin, kiểm tra lại toàn bộ option hiện có, kể cả khi chỉ đổi ghi chú.

Booking chỉ sử dụng contract, không đọc DbContext/entity của module khác.
Khi kiểm tra thất bại, không thay đổi entity hoặc gọi lưu. Mỗi request hợp lệ
lưu booking và chi tiết bằng một lần `SaveChangesAsync` trong BookingDbContext.
Contract kiểm tra trạng thái tại thời điểm đọc; không khóa liên module nên
không bảo đảm chống thay đổi đồng thời ở Customer/Resource sau bước kiểm tra.

ID phải dương. Mọi thời gian truyền ở UTC với hậu tố `Z`; không nhận thời gian
không có múi giờ. Check-out yêu cầu check-in và không được trước check-in.

## Response và mã lỗi

Response gồm `id`, `customerId`, `petId`, `bookingTime`, `checkIn`, `checkOut`,
`note`, `status`, `createdAt`, `updatedAt`, `services`. Mỗi phần tử `services`
gồm `id` (ID chi tiết), `serviceOptionId`, `unitPrice`, `status`.

| HTTP | Ý nghĩa |
|---|---|
| 200 / 201 | Thành công |
| 400 | JSON, trường dữ liệu hoặc contract không hợp lệ; lỗi theo trường trong ValidationProblem |
| 401 | Chưa xác thực |
| 403 | Không có quyền với khách hàng/booking hoặc thay đổi trường dành cho nhân viên |
| 404 | Booking không tồn tại |
| 409 | Booking không cho phép sửa dịch vụ hoặc dịch vụ đang thực hiện/đã hoàn thành |

## Kiểm thử

```powershell
dotnet build PetSpa.slnx -m:1 -p:UseSharedCompilation=false
dotnet run --project tests/PetSpa.BookingSmokeTests/PetSpa.BookingSmokeTests.csproj --no-build
```

Smoke test dùng Kestrel và endpoint/application service thật, repository và
contract giả lập; kiểm tra production DI nhưng không kết nối database và
không kiểm chứng JWT. Không tạo hay áp dụng migration trong thay đổi API này.
