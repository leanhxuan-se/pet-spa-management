# Booking và Resource theo ERD

Nguồn thiết kế: [ERD phiên bản 2.0](<02. ERD - Sơ đồ thiết kế database.docx>),
đối chiếu mục 3, 6, 7.2, 7.3, 8 và 9. Phần này ghi lại cách áp dụng thiết kế
cho hai module Booking và Resource.

## Trạng thái

Booking dùng `PENDING`, `CONFIRMED`, `CHECKED_IN`, `COMPLETED`, `CANCELLED`;
mặc định là `PENDING`.

ERD mâu thuẫn giữa danh sách trạng thái BookingDetail ở mục 4 và từ điển dữ liệu
ở mục 7.3. Theo lựa chọn của người dùng, code dùng `ASSIGNED`, `WORKING`,
`COMPLETED`, `CANCELLED`, mặc định `ASSIGNED`. File Word gốc được lưu nguyên bản.

Resource dùng `ACTIVE`, `INACTIVE` cho category, service và service option.
Giữ giá trị số cũ `INACTIVE = 3` để tránh thay đổi ý nghĩa đối với client gửi
giá trị số; PostgreSQL vẫn lưu tên chuỗi. `MAINTAINED` và `DELETED` không còn
là giá trị hợp lệ.

DELETE trên các API Resource chuyển bản ghi sang `INACTIVE`. API tra cứu vẫn
trả bản ghi inactive để quản lý, xem lịch sử và kích hoạt lại bằng PUT. Vô hiệu
hóa bản ghi cha không tự đổi trạng thái bản ghi con. Khi chọn dịch vụ cho
booking mới, Application/Contract phải kiểm tra cả category, service và option
đều `ACTIVE`.

## Entity và ánh xạ

- Khóa chính map sang `id`, kiểu bigint identity by default.
- Tiền dùng .NET `decimal` và PostgreSQL `numeric(18,2)`. Giá BookingDetail là
  snapshot, không tự cập nhật theo giá Resource.
- Cân nặng option dùng `numeric(6,2)`, có thể NULL khi không giới hạn.
- Các thời điểm dùng DateTime UTC và `timestamp with time zone`.
- `created_at`, `updated_at` có mặc định `NOW()` khi INSERT; Application cập
  nhật `UpdatedAt` khi sửa. BookingTime được truyền khi tạo lịch.
- Enum trạng thái map thành varchar(30), có CHECK constraint và mặc định.
- Category name tối đa 100 ký tự; service name tối đa 150 ký tự.

FK nội bộ Category–Service, Service–ServiceOption và Booking–BookingDetail đều
dùng `ON DELETE RESTRICT`. CustomerId, PetId và ServiceOptionsId chỉ là tham
chiếu logic; không thêm navigation hoặc FK xuyên module.

## Ràng buộc và index

Category name là unique. Service name là unique trong cùng category; API trả
409 khi trùng tên, bao gồm trường hợp database báo unique violation khi ghi.
Tên của bản ghi inactive vẫn tham gia kiểm tra trùng.

Booking có index trên customer_id, pet_id, booking_time và status. BookingDetail
có index trên booking_id, service_options_id. Resource có index trên các FK
nội bộ và unique index trên (category_id, service_name).

CHECK constraint kiểm tra các trạng thái hợp lệ, giá không âm, thời lượng
dương, cân nặng không âm, max_weight không nhỏ hơn min_weight khi cả hai có
giá trị và check_out không trước check_in khi cả hai có giá trị. Validation
Resource kiểm tra giới hạn numeric và tối đa hai chữ số thập phân trước khi ghi.

## Migration đang hoãn

Theo yêu cầu mới nhất, chưa tạo hoặc áp dụng migration cho các thay đổi này.
Các migration và snapshot Resource cũ được giữ nguyên; Booking chưa có
migration. Database hiện tại chưa được đồng bộ với model mới.

Khi tiếp tục migration, cần đối chiếu dữ liệu hiện có: trạng thái Resource
`MAINTAINED`/`DELETED`, tên vượt độ dài mới, tên service trùng trong category,
giá/cân nặng vượt precision hoặc không thỏa CHECK. Nếu database đã có bảng
Booking tạo bằng SQL thì cần đối chiếu baseline trước khi tạo bảng bằng EF.
Lịch sử migration Booking được cấu hình trong schema `booking` chữ thường.

Kiểm thử hiện tại kiểm tra DI, model và SQL tạo schema từ model, cùng hành vi
HTTP Resource; không dùng snapshot cũ để khẳng định database đã đồng bộ.
