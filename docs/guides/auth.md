# Hướng dẫn gộp đăng nhập và quản lý tài khoản PetSpa

Auth dùng **số điện thoại và mật khẩu** cho cả khách hàng lẫn nhân viên. Client gọi một API đăng nhập. Auth tìm tài khoản theo số điện thoại, đọc ID hồ sơ đã liên kết rồi hỏi đúng module để lấy trạng thái và quyền hiện tại. Khách tự đăng ký cho hồ sơ Customer đã có. Manager hoặc admin cấp tài khoản cho hồ sơ nhân viên do Operation quản lý.

Backend đã có các contract tra hồ sơ và migration gộp tài khoản. WPF có màn hình đăng ký, đăng nhập chung, đăng xuất, cấp mã tại quầy và quản trị tài khoản nhân viên. Khi áp dụng lên một bản cũ, dùng mục 1–5 để đối chiếu mã nguồn. Dùng mục 6–7 để chuyển dữ liệu theo từng môi trường và nghiệm thu. [Endpoint Auth](../../src/PetSpa.Modules/Auth/Endpoints/AuthEndpoints.cs) · [Đặc tả HTTP](../../openapi.yaml)

## 1. Ba API xác thực

| API | Đầu vào | Kết quả |
| --- | --- | --- |
| `POST /api/auth/register` | `{ phone, password, activationToken }` | `201 { customerId }` sau khi liên kết tài khoản với khách hợp lệ. Client chuyển sang màn hình đăng nhập. |
| `POST /api/auth/login` | `{ phone, password }` | `200` với `accessToken`, `tokenType`, `expiresAtUtc`, `role` và `user { id, fullName, phone }` như DTO hiện tại. |
| `POST /api/auth/logout` | Header `Authorization: Bearer <accessToken>`, body rỗng | `204`. Server thu hồi các JWT đã cấp cho tài khoản. Client xóa token cục bộ. |

Ví dụ, một lễ tân gửi `{ "phone": "0900000001", "password": "…" }` đến cùng URL với khách. Nếu account liên kết `staff_id = 42` và Operation trả role `RECEPTIONIST`, kết quả chứa `role = "receptionist"` và `user.id = 42`. Với account liên kết `customer_id = 17`, kết quả chứa `role = "customer"` và `user.id = 17`. ID này cho phép client tiếp tục gọi API hồ sơ hiện có.

## 2. Contract giữa Auth và các module hồ sơ

Các contract xác thực nội bộ được đăng ký qua DI trong Customer và Operation. Mỗi module triển khai truy vấn bằng DbContext của mình.

| Contract và module cung cấp | Phương thức đang dùng | Ý nghĩa kết quả |
| --- | --- | --- |
| `ICustomerAuthProfiles` · Customer | `FindForRegistrationByPhoneAsync(normalizedPhone, ct)` | Trả `AccountProfile(Id, FullName, Phone, IsActive, Role, AccessStamp)` khi số khớp đúng một hồ sơ. Nếu khớp 0 hoặc nhiều hồ sơ, trả `null`. Trường hợp nhiều hồ sơ cần đối soát tại Customer. |
| `ICustomerAuthProfiles` · Customer | `GetCustomerForAuthAsync(customerId, ct)` | Tra theo ID khi cấp mã tại quầy, đăng nhập và xác thực JWT. Customer tính `IsActive` từ trạng thái hồ sơ và trả role `customer`. |
| `IStaffAuthProfiles` · Operation | `GetStaffForAuthAsync(staffId, ct)` | Trả cùng DTO cho luồng cấp, sửa và đăng nhập. Operation tính `IsActive` và ánh xạ vai trò nhân viên từ hồ sơ. |
| `IStaffAuthProfiles` · Operation | `GetActiveAdminIdsAsync(ct)` | Auth đối chiếu các ID admin đang hoạt động với account còn đăng nhập được trước khi cho khóa hoặc hạ quyền admin. |

Dùng `AccountPhone.Normalize` cho tra cứu và ghi số. Ví dụ, `+84900000001` và `0900000001` có cùng khóa đăng nhập là `0900000001`. Customer tra cả hai dạng để đọc được hồ sơ cũ. Guard theo chiều ngược lại là `IStaffAccountGuard` do Auth triển khai để Operation kiểm tra người thao tác và bảo vệ tài khoản admin cuối cùng. Các interface nằm trong SharedKernel, các truy vấn hồ sơ chạy ở module sở hữu qua DI. [Khai báo contract](../../src/PetSpa.SharedKernel/Application/Authentication/AccountProfiles.cs) · [Provider Customer](../../src/PetSpa.Modules/Customer/Infrastructure/Services/CustomerAuthProfiles.cs) · [Provider Operation](../../src/PetSpa.Modules/Operation/Infrastructure/Services/StaffAuthProfiles.cs)

## 3. Đăng ký và cấp tài khoản

**Khách hàng tự đăng ký.**

1. Quầy tìm hoặc tạo hồ sơ trong Customer rồi xác minh người đến đăng ký là chủ hồ sơ. Lễ tân hoặc manager gọi `POST /api/auth/customers/{customerId}/activation` theo policy `CustomerManagement`. Auth dùng ASP.NET Core Data Protection để cấp mã gắn với ID và số chuẩn hóa, hiệu lực 15 phút. Quầy trao mã cho khách. Đây là bước chứng minh quyền sử dụng hồ sơ khi đăng ký và dùng được với DB Auth một bảng.
2. Khi nhận `register`, Auth chuẩn hóa số di động Việt Nam và kiểm tra mật khẩu mới (12–128 ký tự, có ký tự khác khoảng trắng). Auth gọi `FindForRegistrationByPhoneAsync`, yêu cầu đúng một khách `ACTIVE`, rồi đối chiếu mã với ID và số. Hồ sơ thiếu, ngừng hoạt động, số khớp nhiều hồ sơ, mã sai hoặc hết hạn đều nhận cùng lỗi đăng ký. Việc tạo hồ sơ mới thuộc luồng tại quầy.
3. Auth băm mật khẩu và chèn account với `customer_id` lấy từ contract. Số đăng nhập và hồ sơ đều phải còn trống. Nếu hai lần đăng ký đến cùng lúc, ràng buộc unique cho đúng một lần thành công. Lần còn lại trả `409` sau khi xác minh mã. Một lần đăng ký lại cũng không được ghi đè mật khẩu đang dùng.

**Manager hoặc admin quản lý tài khoản nhân viên.** Người quản trị chọn nhân viên từ danh sách của Operation (`GET /api/staff`), hoặc tạo hồ sơ nhân viên qua `POST /api/staff` để nhận `staffId`. Sau đó dùng hai thao tác quản trị sau, áp dụng policy `ManagerOrAdmin`. Nhân viên dùng cùng API login và logout ở mục 1.

| Thao tác | Trình tự xử lý tại Auth |
| --- | --- |
| `POST /api/auth/staff-accounts` với `{ staffId, password }` | Kiểm quyền người gọi, gọi `GetStaffForAuthAsync`, yêu cầu hồ sơ `ACTIVE`, kiểm tra quyền trên nhân viên đích. Lấy số từ contract, băm mật khẩu, lưu `staff_id`. Trả `201 { staffId }`. |
| `PATCH /api/auth/staff-accounts/{staffId}` với `{ password?, isEnabled? }` | Tra lại nhân viên qua cùng contract và tìm account theo `staff_id`. Cho phép đặt lại mật khẩu, khóa hoặc mở tài khoản. Mở tài khoản cần hồ sơ `ACTIVE`. Ghi thay đổi cùng lần tăng `token_version` trong một giao dịch, trả `204`. |

Manager quản lý tài khoản `STAFF` và `RECEPTIONIST`. Admin quản lý thêm tài khoản `MANAGER` và `ADMIN`, đồng thời phải giữ ít nhất một admin có thể đăng nhập. Áp dụng cùng giới hạn khi sửa role hoặc trạng thái hồ sơ ở Operation để tránh tự nâng quyền qua API hồ sơ. Auth đọc role qua contract. Các request cấp hoặc sửa account chỉ nhận những trường liệt kê ở bảng trên.

## 4. Một bảng nghiệp vụ trong schema Auth

Dùng `petspa_auth.accounts` làm bảng nghiệp vụ duy nhất của Auth. `AuthDbContext` kế thừa `DbContext` thông thường và dùng `PasswordHasher<Account>` của ASP.NET Core. EF theo dõi migration trong `petspa_auth."__EFMigrationsHistory"`.

| Cột | Kiểu và ràng buộc |
| --- | --- |
| `id` | `bigint`, khóa chính của account. |
| `login_phone` | Chuỗi số đã chuẩn hóa, `NOT NULL UNIQUE` trên toàn bảng để một lần tra cứu luôn xác định được một account. |
| `password_hash` | `text NOT NULL`. Lưu kết quả của hasher. |
| `customer_id`, `staff_id` | Hai cột `bigint` nullable. Ràng buộc `ck_accounts_profile` yêu cầu đúng một ID dương và cột kia NULL. Unique index riêng trên mỗi cột bảo đảm mỗi hồ sơ gắn với tối đa một account. |
| `is_enabled` | `boolean NOT NULL DEFAULT true`. Cho phép khóa tài khoản độc lập với trạng thái hồ sơ. |
| `token_version` | `bigint NOT NULL DEFAULT 0`. Auth dùng phiên bản này để thu hồi JWT. |
| `locked_until_utc` | Mốc khóa tạm thời từ các tài khoản Identity đã được chuyển. |
| `created_at` | `timestamptz NOT NULL`. Thời điểm tạo tài khoản. |
| `updated_at` | `timestamptz NOT NULL`. Thời điểm gần nhất đổi mật khẩu, trạng thái tài khoản, nâng cấp hash hoặc thu hồi phiên khi đăng xuất. Ban đầu bằng `created_at`. |
| `last_login_at` | `timestamptz` nullable. Thời điểm đăng nhập thành công gần nhất. |

Migration `AddAccountTimestamps` dùng thời điểm áp dụng migration cho `created_at` và `updated_at` của tài khoản hiện có. `last_login_at` có giá trị `NULL` cho đến lần đăng nhập thành công đầu tiên sau migration.

Loại tài khoản suy ra từ cột ID có giá trị. Họ tên, số liên hệ, trạng thái hồ sơ và role nhân viên lấy từ module sở hữu. Số đăng nhập là định danh do Auth quản lý. Sửa số liên hệ ở Customer hoặc Operation chỉ cập nhật thông tin hồ sơ. Nếu một khách và một nhân viên cùng dùng một số, ràng buộc unique chặn account thứ hai. Quầy cần đối soát chủ sở hữu và thống nhất số đăng nhập riêng trước khi cấp tài khoản tiếp theo.

## 5. Đăng nhập, xác thực request và đăng xuất

1. **Đăng nhập.** Auth tra một account theo `login_phone`, kiểm tra hash, `is_enabled` và hồ sơ `ACTIVE` theo ID đã lưu. Với khách, gán role cố định `customer`. Với nhân viên, ánh xạ role Operation sang `staff`, `receptionist`, `manager` hoặc `admin`. Thông tin sai, account bị khóa hay liên kết không còn hợp lệ cùng trả `401`. Khi contract gặp lỗi hạ tầng, dừng cấp token và trả `503`.
2. **Phát JWT.** Giữ `sub=customer:<id>` hoặc `sub=staff:<id>`, `role`, `account_type` và đúng một trong `customer_id`, `staff_id` để tương thích với `ClaimsPrincipalExtensions`. Bổ sung `account_id` và `token_version`. Access token mặc định có thời hạn 15 phút, dùng HTTPS, lưu khóa ký ở cấu hình bí mật và giới hạn tần suất đăng nhập, đăng ký. Dùng chính sách mật khẩu mới khi tạo hoặc đổi mật khẩu, hỗ trợ kiểm tra và nâng cấp hash cũ khi đăng nhập thành công.
3. **Xác thực Bearer.** Auth kiểm chữ ký, issuer, audience và thời hạn, sau đó đối chiếu account, phiên bản, liên kết hồ sơ, trạng thái và role hiện tại. JWT mang thêm `profile_access_stamp`. Customer đổi dấu này khi đổi trạng thái khách, Operation đổi khi đổi vai trò hoặc trạng thái nhân viên. JWT cũ vẫn nhận `401` sau khi khôi phục vai trò hoặc kích hoạt lại hồ sơ. Token hợp lệ thiếu quyền nghiệp vụ nhận `403`. Các module nghiệp vụ dùng principal đã xác thực để kiểm quyền truy cập tài nguyên.
4. **Đăng xuất.** Lấy account từ JWT đã xác thực rồi tăng phiên bản bằng cập nhật nguyên tử có điều kiện theo phiên bản trong JWT. Trả `204` sau khi cập nhật đã commit. Nếu phiên bản đã đổi do một yêu cầu khác, trả `401`. JWT mang phiên bản trước đó bị từ chối từ request kế tiếp. Với cách thu hồi này, **đăng xuất áp dụng cho mọi thiết bị**. Người dùng đăng nhập lại để nhận token mới.

## 6. Triển khai theo từng phần của dự án

### Backend và các module nghiệp vụ

| Nơi làm việc | Việc cần đối chiếu khi chuyển từ bản cũ |
| --- | --- |
| Auth | Gom route vào `AuthEndpoints`. `AccountService` sở hữu đăng ký và quản trị account. `LoginService` sở hữu kiểm tra mật khẩu, chọn contract theo ID, xác thực phiên và thu hồi. `AuthModule` đăng ký JWT Bearer, rate limit và policy. Hai route cũ `/api/auth/customer/login`, `/api/auth/staff/login` phải biến mất khỏi bảng route. |
| SharedKernel, Customer và Operation | SharedKernel công bố contract, DTO, tên policy và cách đọc principal đã xác thực. Customer và Operation đăng ký provider tương ứng trong DI, giữ CRUD hồ sơ và trả dữ liệu qua contract. Cả sửa account ở Auth và sửa role/trạng thái ở Operation phải kiểm lại quyền trên đối tượng trong giao dịch với cùng khóa quản trị nhân viên. |
| Host `PetSpa.Api` | Gọi `AddAuthModule` và `MapAuthModuleEndpoint` mỗi hàm một lần. Giữ `UseAuthentication`, `UseAuthorization` và `UseRateLimiter` trong pipeline. Thiết lập issuer, audience và khóa ký tối thiểu 32 byte qua cấu hình bí mật. Nếu chạy nhiều instance, dùng chung key ring Data Protection để mã kích hoạt do instance này cấp có thể dùng ở instance khác. |
| Customer, Booking, Operation, Resource và Billing | Giữ phân quyền tài nguyên ở module sở hữu. Ví dụ, Booking lấy `customer_id` từ principal để lọc lịch của khách, các route `/api/me` của Operation dùng `staff_id`, Resource dùng policy cho thao tác ghi. Billing dùng cùng cơ chế khi mở endpoint cần xác thực. Các module này tiêu thụ danh tính đã xác thực qua middleware. Mật khẩu, khóa ký và thu hồi phiên thuộc Auth. |

Các điểm vào để rà thay đổi là [host](../../src/PetSpa.Api/Program.cs), [đăng ký Auth](../../src/PetSpa.Modules/Auth/AuthModule.cs), [xử lý account](../../src/PetSpa.Modules/Auth/Application/Services/AccountService.cs), [xử lý phiên](../../src/PetSpa.Modules/Auth/Application/Services/LoginService.cs) và [quản trị hồ sơ nhân viên](../../src/PetSpa.Modules/Operation/Application/Services/StaffService.cs).

### Khởi tạo cơ sở dữ liệu

Áp dụng migration Auth và các module liên quan trước khi thiết lập tài khoản. Với database dùng lịch sử từ trước khi gộp migration Identity và credential, dùng bản sao lưu và lịch sử migration cũ để lập phương án chuyển dữ liệu. [Lệnh khởi tạo cơ sở dữ liệu](../../README.md#khởi-tạo-cơ-sở-dữ-liệu)

Với database trống, người vận hành tạo hồ sơ nhân viên `ADMIN` đang hoạt động và account liên kết trong `petspa_auth.accounts`. Tạo hash cho mật khẩu ban đầu bằng `PasswordHasher<T>` của ASP.NET Core trong quy trình thiết lập có quyền, rồi ghi hash cùng `staff_id` và `login_phone` qua câu lệnh SQL tham số hóa. Sau đó dùng các API quản trị để cấp tài khoản cho nhân viên khác.

Nếu có nhiều instance phục vụ cấp và đổi mã kích hoạt, dùng cùng key ring Data Protection và giữ key ring qua các lần khởi động.

### Client WPF và tài liệu tích hợp

| Thành phần | Việc cần làm |
| --- | --- |
| Đăng nhập WPF | Trong checkout `Animal-Spa`, `Services/ApiClient.cs` đã gửi `{ phone, password }` đến URL chung. `Views/LoginViews.xaml.cs` dùng cùng lời gọi cho hai nút User/Staff. `MainWindow.xaml.cs` chọn màn hình từ `session.Role`. Khi tích hợp bản cũ, bỏ việc chọn URL theo tab và giữ `user.id` là ID hồ sơ. |
| Đăng ký và thao tác tại quầy | Lễ tân hoặc manager chọn hồ sơ khách trong cửa sổ **Cấp mã kích hoạt**. Khách nhập số, mật khẩu và mã trong cửa sổ đăng ký. Sau `201`, ứng dụng trở về đăng nhập. Trong cửa sổ **Tài khoản nhân viên**, manager/admin chọn hồ sơ rồi cấp account, đặt lại mật khẩu, khóa hoặc mở tài khoản. |
| Phiên và đăng xuất | Nút **Đăng xuất** gửi Bearer của phiên hiện tại và body rỗng. Khi nhận `204` hoặc `401`, WPF đóng cửa sổ phiên hiện tại và trở về đăng nhập. Nếu server chưa xác nhận, WPF giữ phiên cục bộ để người dùng thử lại. Cửa sổ quản trị gửi cùng Bearer token và hiển thị thông báo nếu server trả `403`. |
| Đặc tả và tài liệu bàn giao | Đặc tả OpenAPI và tệp HTTP có các ví dụ đăng ký, đăng nhập, đăng xuất và cấp hoặc sửa tài khoản. [OpenAPI](../../openapi.yaml) · [PetSpa.Api.http](../../src/PetSpa.Api/PetSpa.Api.http) |

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
| Kích hoạt lại hồ sơ khách hoặc khôi phục role nhân viên | JWT cấp trước lần thay đổi vẫn nhận `401`. Đăng nhập lại trả token với dấu hồ sơ mới. |
| Số đăng nhập đã thuộc account loại khác | Cấp thêm account cùng số trả `409`. Tài khoản đã tồn tại tiếp tục đăng nhập với đúng ID và role. |
| Phân quyền sau chuyển đổi | Khách A truy cập hồ sơ của khách B bị từ chối. Staff xem đúng công việc của mình. Manager thử khóa hoặc hạ quyền admin qua cả Auth lẫn Operation đều bị từ chối. |
| Hợp đồng client | Kiểm tra danh sách route trong OpenAPI sinh từ host. Chỉ có một đường dẫn login. Qua WPF, đăng ký khách, đăng nhập khách và nhân viên trên cùng màn hình, thử thao tác có quyền rồi đăng xuất. |

Để kiểm tra backend, đặt `PETSPA_AUTH_TEST_CONNECTION` trỏ tới một PostgreSQL thử nghiệm **trống, dùng riêng cho lần chạy**. Chương trình smoke tự áp dụng migration, chèn dữ liệu giả và gọi API qua HTTP. Từ thư mục gốc chạy các lệnh sau. [Bộ kiểm thử Auth](../../tests/PetSpa.AuthenticationSmokeTests/Program.cs) · [Request thử thủ công](../../src/PetSpa.Api/PetSpa.Api.http)

```powershell
dotnet build PetSpa.slnx
dotnet run --project tests/PetSpa.AuthenticationSmokeTests
```
