# Đối chiếu sơ đồ trình tự với API PetSpa

Đối chiếu [diagram PDF gốc](docs/Base/Diagram.pdf), [sơ đồ trình tự cập nhật 3](SO-DO-TRINH-TU-CAP-NHAT-3.md), [hướng dẫn chuyển đổi](HUONG-DAN-CHUYEN-DOI-API.md) và các route của [PetSpa.Api](src/PetSpa.Api/Program.cs). Hợp đồng HTTP hiện hành nằm trong [OpenAPI](openapi.yaml).

## Các luồng đã xác nhận

| Luồng | Hợp đồng sau chuyển đổi |
| --- | --- |
| AUTH-01, CUS-06, OPS-21 | Khách hàng và nhân viên dùng `POST /api/auth/login` với số điện thoại và mật khẩu. Client đọc vai trò trong phản hồi. |
| RES-01, RES-02 | `GET /api/services?categoryId=...&keyword=...&activeOnly=true` là route tra dịch vụ theo danh mục. API ghi danh mục, dịch vụ và tùy chọn yêu cầu manager. |
| BOOK-03, BOOK-05, BOOK-07 | `PUT /api/bookings/{id}` cập nhật thông tin lịch. `GET /api/bookings` tìm kiếm và phân trang. |
| OPS-01, OPS-09 | Đổi trạng thái phòng trả `409` khi phòng còn phiên đang làm hoặc còn lịch sử dụng. `GET /api/room/available` và `/api/room/unavailable` hỗ trợ tra theo giờ máy chủ hoặc khoảng giờ. |
| OPS-10 | `GET /api/staff/available` và `/api/staff/unavailable` hỗ trợ giờ máy chủ hoặc khoảng UTC, chỉ xét phân công còn hiệu lực. Tra rảnh cần ca WORKING ở hiện tại hoặc ca SCHEDULED/WORKING bao phủ khoảng yêu cầu. Danh sách rỗng trả `200`. |
| OPS-11 | `POST /api/service-sessions` tạo cả nhóm WAITING cho lịch CONFIRMED, giữ tập dòng bàn giao và kiểm tra trùng phòng. Gửi lại đúng tập dòng trả nhóm hiện tại. |
| OPS-13, OPS-18, OPS-20 | Hủy, bắt đầu hoặc hoàn tất phiên cập nhật dòng dịch vụ trong cùng giao dịch với Booking. Hủy phiên WAITING thu hồi các phân công còn hiệu lực. Bắt đầu và hoàn tất yêu cầu phân công còn hiệu lực của người gọi. |
| OPS-17, OPS-21 | `GET /api/me/service-sessions` và `GET /api/me/notifications` dùng định danh nhân viên từ JWT. |
| OPS-22 | `GET /api/service-sessions/current` phân trang theo booking, tải đủ các phiên và trả điều kiện chốt lịch, hủy lịch hoặc lập hóa đơn. Nhóm đã lập hóa đơn hiệu lực rời hàng chờ. |

Các API hỗ trợ chính thức gồm `GET /health`, `POST /api/auth/customers/{customerId}/activation`, `POST /api/auth/register`, `POST /api/auth/logout`, `POST /api/auth/staff-accounts` và `PATCH /api/auth/staff-accounts/{staffId}`.

## Các điểm còn mở trong sơ đồ

| Luồng | Hiện trạng |
| --- | --- |
| CUS-03 | `POST /api/customers` tạo hồ sơ. Cấp mã kích hoạt và đặt mật khẩu qua Auth. Lỗi kiểm tra dữ liệu hồ sơ trả `422`. |
| OPS-01 | `POST /api/room` và `PUT /api/room/{id}` trả `200` với danh sách gồm `id`, `roomName`, `roomType`. Sơ đồ PDF minh họa tạo phòng trả `201`. |
| OPS-02 | Ngừng sử dụng nhân viên có kiểm tra điều kiện giữ ít nhất một admin có thể đăng nhập. Kiểm tra ca hoặc phiên đang phân công thuộc đợt tiếp theo. |
| BILL-01 đến BILL-04, E2E-01 | Billing cung cấp truy vấn nội bộ về hóa đơn hiệu lực cho OPS-22. API hóa đơn và thanh toán vẫn nằm trong thiết kế. `PATCH /api/booking/{id}` nhận `confirmed` và `completed`; bước `paid` thuộc tích hợp thanh toán. |
