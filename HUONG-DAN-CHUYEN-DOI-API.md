# Hướng dẫn chuyển đổi API PetSpa

Tài liệu ghi nhận các quyết định chuyển đổi API theo từng đợt. Nội dung được cập nhật khi các mục tiếp theo được chốt.

## API cần thiết đã xác nhận

Sáu API sau cần thiết cho chương trình và được giữ trong hợp đồng API khi chuyển đổi.

| API | Mục đích |
| --- | --- |
| `GET /health` | Kiểm tra máy chủ |
| `POST /api/auth/customers/{customerId}/activation` | Cấp mã kích hoạt cho khách tại quầy |
| `POST /api/auth/register` | Kích hoạt tài khoản đăng nhập của khách bằng mã đã cấp |
| `POST /api/auth/logout` | Thu hồi các JWT đã cấp của tài khoản |
| `POST /api/auth/staff-accounts` | Cấp tài khoản cho hồ sơ nhân viên |
| `PATCH /api/auth/staff-accounts/{staffId}` | Đổi mật khẩu, khóa hoặc mở tài khoản nhân viên |

## Chuyển đổi tra cứu dịch vụ theo danh mục

Đã xác nhận sử dụng `GET /api/services?categoryId={categoryId}` theo RES-02 để thay thế `GET /api/categories/{categoryId}/services`.

Khi chuyển đổi client, đưa mã danh mục từ đoạn đường dẫn `{categoryId}` sang tham số query `categoryId`. Các tham số `keyword` và `activeOnly` tiếp tục được hỗ trợ. Ví dụ, chuyển `GET /api/categories/12/services?activeOnly=true` thành `GET /api/services?categoryId=12&activeOnly=true`.

Client dùng `GET /api/services?categoryId=...`. Backend đã gỡ route lồng theo danh mục.

## Đăng nhập chung

AUTH-01, CUS-06 và OPS-21 sử dụng `POST /api/auth/login` cho cả khách hàng và nhân viên. Client gửi số điện thoại và mật khẩu, sau đó dùng vai trò trong phản hồi để mở màn hình phù hợp.

Khi chuyển đổi client hoặc sơ đồ PDF, thay `POST /api/auth/customer/login` và `POST /api/auth/staff/login` bằng đường dẫn đăng nhập chung. Mã nguồn hiện chỉ đăng ký đường dẫn chung.

## Quyết định về Operation và Resource

- Dùng `GET /api/staff/available` và `GET /api/staff/unavailable` cho tra cứu nhân viên.
- Bổ sung `POST /api/service-sessions` để bàn giao booking và tạo nhóm phiên, `DELETE /api/service-sessions/{id}` để hủy phiên, `POST /api/service-sessions/{id}/complete` để hoàn tất phiên và `GET /api/service-sessions/current` để theo dõi phiên theo booking.
- Khi bắt đầu, hoàn tất hoặc hủy phiên, đồng bộ trạng thái booking detail qua contract Booking trong cùng giao dịch.
- Chặn thay đổi trạng thái phòng khi phòng còn phiên đang sử dụng.
- Tra cứu nhân viên cần xét ca làm phù hợp và chỉ xét phân công còn hiệu lực.
- Các API ghi danh mục, dịch vụ và tùy chọn yêu cầu vai trò manager.

Tra cứu nhân viên hỗ trợ giờ máy chủ hoặc khoảng UTC `[startTime, endTime)`, trả `200` với danh sách có thể rỗng. Cập nhật client từ `/api/staff/availability` và `/api/staff/unavailability` sang các route trên.

Áp dụng migration `LinkServiceSessionsToBookings` của Operation sau khi đã áp dụng migration Booking. Migration điền `booking_id` cho phiên cũ có dòng booking tương ứng. Phiên cũ thiếu liên kết hoặc thiếu mốc bàn giao đã xác minh cần được đối chiếu trước khi dùng API đồng bộ trạng thái.

## Các luồng chính thức đã xác nhận

- `GET /api/bookings` dùng để tìm kiếm và phân trang lịch đặt.
- `PUT /api/bookings/{id}` dùng để cập nhật thông tin lịch đặt.
- `GET /api/me/service-sessions` dùng để tải công việc được giao của nhân viên.
- `GET /api/me/notifications` dùng để tải thông báo của nhân viên.

Các API này đã được đăng ký và là một phần của luồng chính thức.

Chi tiết request, response và quyền truy cập được mô tả trong [openapi.yaml](openapi.yaml). Các mục còn lại trong [báo cáo đối chiếu API](BAO-CAO-DOI-CHIEU-API.md) sẽ được chốt trong các đợt tiếp theo.
