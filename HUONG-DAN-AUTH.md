# Hướng dẫn gộp đăng nhập và quản lý tài khoản PetSpa

Auth dùng **số điện thoại và mật khẩu** cho cả khách hàng lẫn nhân viên. Client gọi một API đăng nhập. Auth tìm tài khoản theo số điện thoại, đọc ID hồ sơ đã liên kết rồi hỏi đúng module để lấy trạng thái và quyền hiện tại. Khách tự đăng ký cho hồ sơ Customer đã có. Manager hoặc admin cấp tài khoản cho hồ sơ nhân viên do Operation quản lý.

## 1. Ba API xác thực

| API | Đầu vào | Kết quả |
| --- | --- | --- |
| `POST /api/auth/register` | `{ phone, password, activationToken }` | `201 { customerId }` sau khi liên kết tài khoản với khách hợp lệ. Client chuyển sang màn hình đăng nhập. |
| `POST /api/auth/login` | `{ phone, password }` | `200` với `accessToken`, `tokenType`, `expiresAtUtc`, `role` và `user { id, fullName, phone }` như DTO hiện tại. |
| `POST /api/auth/logout` | Header `Authorization: Bearer <accessToken>`, body rỗng | `204`. Server thu hồi các JWT đã cấp cho tài khoản. Client xóa token cục bộ. |

Ví dụ, một lễ tân gửi `{ "phone": "0900000001", "password": "…" }` đến cùng URL với khách. Nếu account liên kết `staff_id = 42` và Operation trả role `RECEPTIONIST`, kết quả chứa `role = "receptionist"` và `user.id = 42`. Với account liên kết `customer_id = 17`, kết quả chứa `role = "customer"` và `user.id = 17`. ID này cho phép client tiếp tục gọi API hồ sơ hiện có.

## 2. Contract giữa Auth và các module hồ sơ

Các contract xác thực nội bộ được đăng ký qua DI trong Customer và Operation. Mỗi module triển khai truy vấn bằng DbContext của mình.

| Module cung cấp | Phương thức cần có | Dữ liệu Auth cần |
| --- | --- | --- |
| Customer | `FindForRegistrationByPhoneAsync(normalizedPhone, ct)` | Trả đúng một hồ sơ gồm `CustomerId, FullName, Phone, Status`, hoặc kết quả không tìm thấy. Số khớp nhiều hồ sơ là lỗi dữ liệu cần đối soát. |
| Customer | `GetCustomerForAuthAsync(customerId, ct)` | Đọc lại hồ sơ theo ID đã liên kết để xét trạng thái khi đăng nhập hoặc xác thực JWT. |
| Operation | `GetStaffForAuthAsync(staffId, ct)` | Trả `StaffId, FullName, Phone, Status, Role` cho luồng quản trị và đăng nhập. Operation là nguồn xác định role nhân viên. |

Dùng một hàm chuẩn hóa số cho tra cứu, ghi hồ sơ và ghi account. Tách quy tắc hiện có trong `CustomerContact.NormalizePhone` thành hàm dùng chung. Ví dụ, `+84900000001` và `0900000001` có cùng khóa đăng nhập là `0900000001`. Các contract trên chỉ trao đổi dữ liệu hồ sơ. Auth sở hữu toàn bộ thao tác với mật khẩu và token.

## 3. Đăng ký và cấp tài khoản

**Khách hàng tự đăng ký.**

1. Quầy tạo hoặc tìm hồ sơ khách trong Customer. Để bảo vệ hồ sơ khi người khác biết số điện thoại, quầy xác minh khách và gọi `POST /api/auth/customers/{customerId}/activation` để cấp mã. Policy `CustomerManagement` giới hạn thao tác cho lễ tân và quản lý. Auth phát mã có chữ ký, ràng buộc với mục đích đăng ký, `customerId`, số đã chuẩn hóa và thời hạn ngắn. Cách này dùng được với cả hồ sơ có sẵn và hồ sơ mới tạo tại quầy.
2. Khi nhận `register`, Auth chuẩn hóa số, kiểm tra mật khẩu mới và xác minh mã. Auth gọi `FindForRegistrationByPhoneAsync`. Chỉ tiếp tục khi có đúng một khách `ACTIVE` và ID cùng số điện thoại khớp với mã. Hồ sơ thiếu, ngừng hoạt động, mã sai hoặc hết hạn đều nhận cùng lỗi đăng ký. Việc tạo hồ sơ mới thuộc luồng tại quầy.
3. Auth băm mật khẩu và chèn account với `customer_id` lấy từ contract. Số đăng nhập và hồ sơ đều phải còn trống. Nếu hai lần đăng ký đến cùng lúc, ràng buộc unique cho đúng một lần thành công. Lần còn lại trả `409` sau khi xác minh mã. Một lần đăng ký lại cũng không được ghi đè mật khẩu đang dùng.

**Manager hoặc admin quản lý tài khoản nhân viên.** Hai thao tác sau thuộc chức năng quản trị, áp dụng policy `ManagerOrAdmin`. Nhân viên dùng cùng API login và logout ở mục 1.

| Thao tác | Trình tự xử lý tại Auth |
| --- | --- |
| `POST /api/auth/staff-accounts` với `{ staffId, password }` | Kiểm quyền người gọi, gọi `GetStaffForAuthAsync`, yêu cầu hồ sơ `ACTIVE`, kiểm tra quyền trên nhân viên đích. Lấy số từ contract, băm mật khẩu, lưu `staff_id`. Trả `201 { staffId }`. |
| `PATCH /api/auth/staff-accounts/{staffId}` với `{ password?, isEnabled? }` | Tra lại nhân viên qua cùng contract và tìm account theo `staff_id`. Cho phép đặt lại mật khẩu, khóa hoặc mở tài khoản. Mở tài khoản cần hồ sơ `ACTIVE`. Ghi thay đổi cùng lần tăng `token_version` trong một giao dịch, trả `204`. |

Manager quản lý tài khoản `STAFF` và `RECEPTIONIST`. Admin quản lý thêm tài khoản `MANAGER` và `ADMIN`, đồng thời phải giữ ít nhất một admin có thể đăng nhập. Áp dụng cùng giới hạn khi sửa role hoặc trạng thái hồ sơ ở Operation để tránh tự nâng quyền qua API hồ sơ. Auth đọc role qua contract. Các request cấp hoặc sửa account chỉ nhận những trường liệt kê ở bảng trên.

## 4. Một bảng nghiệp vụ trong schema Auth

Dùng `petspa_auth.accounts` và giữ bảng lịch sử migration của EF. Chuyển `AuthDbContext` sang `DbContext` thông thường. Có thể tiếp tục dùng `PasswordHasher<T>` của ASP.NET Core mà vẫn dùng được schema sau.

| Cột | Kiểu và ràng buộc |
| --- | --- |
| `id` | `bigint`, khóa chính của account. |
| `login_phone` | Chuỗi số đã chuẩn hóa, `NOT NULL UNIQUE` trên toàn bảng để một lần tra cứu luôn xác định được một account. |
| `password_hash` | `text NOT NULL`. Lưu kết quả của hasher. |
| `customer_id`, `staff_id` | Hai cột `bigint` nullable. Đặt `CHECK ((customer_id IS NOT NULL) <> (staff_id IS NOT NULL))` và unique index riêng cho từng ID khác null. |
| `is_enabled` | `boolean NOT NULL DEFAULT true`. Cho phép khóa tài khoản độc lập với trạng thái hồ sơ. |
| `token_version` | `bigint NOT NULL DEFAULT 0`. Auth dùng phiên bản này để thu hồi JWT. |
| `locked_until_utc` | Mốc khóa tạm thời từ các tài khoản Identity đã được chuyển. |

Loại tài khoản suy ra từ cột ID có giá trị. Họ tên, số liên hệ, trạng thái hồ sơ và role nhân viên lấy từ module sở hữu. Số đăng nhập là định danh do Auth quản lý. Thay đổi số liên hệ trên hồ sơ chỉ thay đổi thông tin liên lạc. Khi cần đổi cả số đăng nhập, phải xác minh lại chủ tài khoản, kiểm tra unique và thu hồi token cũ trong cùng lần cập nhật.

## 5. Đăng nhập, xác thực request và đăng xuất

1. **Đăng nhập.** Auth tra một account theo `login_phone`, kiểm tra hash, `is_enabled` và hồ sơ `ACTIVE` theo ID đã lưu. Với khách, gán role cố định `customer`. Với nhân viên, ánh xạ role Operation sang `staff`, `receptionist`, `manager` hoặc `admin`. Thông tin sai, account bị khóa hay liên kết không còn hợp lệ cùng trả `401`. Khi contract gặp lỗi hạ tầng, dừng cấp token và trả `503`.
2. **Phát JWT.** Giữ `sub=customer:<id>` hoặc `sub=staff:<id>`, `role`, `account_type` và đúng một trong `customer_id`, `staff_id` để tương thích với `ClaimsPrincipalExtensions`. Bổ sung `account_id` và `token_version`. Access token mặc định có thời hạn 15 phút, dùng HTTPS, lưu khóa ký ở cấu hình bí mật và giới hạn tần suất đăng nhập, đăng ký. Dùng chính sách mật khẩu mới khi tạo hoặc đổi mật khẩu, hỗ trợ kiểm tra và nâng cấp hash cũ khi đăng nhập thành công.
3. **Xác thực Bearer.** Cấu hình bước xác thực một lần trong Auth. Kiểm chữ ký, issuer, audience và thời hạn, sau đó đối chiếu account, phiên bản, liên kết hồ sơ, trạng thái và role hiện tại. Token cũ sau khi khóa tài khoản hoặc hạ quyền phải nhận `401`. Token hợp lệ nhưng thiếu quyền nghiệp vụ nhận `403`. Các module nghiệp vụ dùng policy và claim đã xác thực để kiểm quyền truy cập tài nguyên.
4. **Đăng xuất.** Lấy account từ JWT đã xác thực rồi tăng phiên bản bằng cập nhật nguyên tử có điều kiện theo phiên bản trong JWT. Trả `204` sau khi cập nhật đã commit. Nếu phiên bản đã đổi do một yêu cầu khác, trả `401`. JWT mang phiên bản trước đó bị từ chối từ request kế tiếp. Với cách thu hồi này, **đăng xuất áp dụng cho mọi thiết bị**. Người dùng đăng nhập lại để nhận token mới.

## 6. Triển khai và chuyển dữ liệu

Host đăng ký Auth một lần trong `Program.cs`. Hai route đăng nhập cũ được thay bằng `POST /api/auth/login`. Client gửi cùng payload và dùng role do server trả về. Người dùng cần đăng nhập lại sau khi chuyển dữ liệu. [Điểm đăng ký API](src/PetSpa.Api/Program.cs) · [Ví dụ HTTP](src/PetSpa.Api/PetSpa.Api.http)

Migration Auth nhập tài khoản Identity đã liên kết cùng hash còn dùng ở Customer và Staff. Số trùng sau chuẩn hóa hoặc dữ liệu Identity cần đối soát sẽ làm migration dừng. Sau khi Auth đã nhập dữ liệu, migration của Customer và Operation kiểm tra liên kết rồi bỏ các cột thông tin xác thực cũ. Dùng cơ sở dữ liệu thử để diễn tập và sao lưu trước khi chuyển môi trường phục vụ. [Các lệnh migration](HUONG-DAN-CHAY.md#chuyển-dữ-liệu-sang-auth-dùng-chung) · [Mô hình Auth](src/PetSpa.Modules/Auth/Infrastructure/Persistence/AuthDbContext.cs)


## 7. Nghiệm thu

| Tình huống | Kết quả cần thấy |
| --- | --- |
| Khách ACTIVE, mã đúng, số nhập ở dạng `+84` | Register trả `201`. Có đúng một account trỏ tới đúng khách, số lượng hồ sơ Customer giữ nguyên. |
| Số lạ, hồ sơ INACTIVE, mã sai hoặc mã của hồ sơ khác | Cùng lỗi đăng ký `400`, không phát sinh account. |
| Hai request đăng ký hợp lệ cùng lúc | Một `201`, một `409`. Hash của account thành công giữ nguyên. |
| Manager cấp/sửa tài khoản lễ tân và thử sửa tài khoản admin | Hai thao tác hợp lệ thành công. Thao tác trên admin trả `403`. Khách và nhân viên thường gọi API quản trị cũng nhận `403`. |
| Admin cấp account cho nhân viên chưa có hồ sơ, ngừng hoạt động hoặc đã liên kết | Lần lượt nhận `404`, `409`, `409`. Không tạo account ngoài điều kiện. |
| Khách và nhân viên đăng nhập qua một URL | Cả hai nhận `200`, đúng role, ID nghiệp vụ và policy tương ứng. Số chưa đăng ký và mật khẩu sai cùng nhận `401`. |
| Đăng nhập hai thiết bị rồi logout trên một thiết bị | Logout trả `204`. Cả hai JWT cũ nhận `401` khi gọi API bảo vệ. Đăng nhập lại thành công với phiên bản mới. |
| Khóa account, chuyển hồ sơ sang INACTIVE hoặc hạ quyền nhân viên | JWT cũ bị từ chối ở request kế tiếp. Chỉ account và hồ sơ còn hoạt động mới đăng nhập lại được với quyền hiện tại. |
