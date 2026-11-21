# Instruction: Tách xác thực thành module Auth trong Pet Spa

> Tài liệu lịch sử. Xem [hướng dẫn Auth hiện hành](HUONG-DAN-AUTH.md) để triển khai đăng ký gắn hồ sơ, đăng nhập chung và đăng xuất.

> Dành cho AI coding agent triển khai trong repository `pet-spa-management`.
> Khảo sát mã nguồn ngày 15/11/2026. Đây là **hướng dẫn triển khai kiến trúc đích**, không phải xác nhận hệ thống đã được chuyển đổi.
> Phạm vi: server .NET. Không sửa WPF, không triển khai OTP, OAuth/OIDC hay microservice.

## 1. Mục tiêu và nguyên tắc kế thừa

Tạo module `PetSpa.Modules.Auth` quản lý tập trung tài khoản, mật khẩu, đăng nhập, JWT, refresh token, role và vòng đời phiên. Customer và Operation tiếp tục quản lý hồ sơ khách hàng/nhân viên; các module nghiệp vụ tiếp tục quyết định quyền sở hữu và quy tắc thao tác dữ liệu của mình.

Tài liệu này điều chỉnh hướng dẫn “Xác thực & Phân quyền phía Server (Pet Spa)” theo repo thực tế, đồng thời thay thế các quyết định kiến trúc xác thực trong `docs/instructions/unify-login-authorization.md` khi triển khai đợt này. Không dùng ghi chú “đã triển khai” của tài liệu cũ để kết luận module Auth đã tồn tại.

Các khác biệt có chủ đích so với hướng dẫn gốc:

| Hướng dẫn gốc | Quyết định cho repo này |
| --- | --- |
| Một `AppDbContext`, mở rộng context hiện có | Tạo `AuthDbContext` riêng, schema `auth`, đúng kiến trúc mỗi module sở hữu dữ liệu riêng |
| Đăng nhập email bằng `/api/auth/login` | Giữ hai route đăng nhập bằng phone hiện có; chưa ép khách hàng có email |
| Bốn role viết hoa | Giữ năm role viết thường: `customer`, `staff`, `receptionist`, `manager`, `admin` |
| Có thể có nhiều role/user | Mỗi tài khoản có đúng một role; endpoint đổi role phải bảo vệ quy tắc này |
| Đặt auth dưới API hoặc thư mục chung | Implementation thuộc module Auth; API chỉ lắp ghép; SharedKernel chỉ giữ hợp đồng dùng chung |
| Ma trận phân quyền khởi điểm | Giữ ma trận hiện có tại mục 8; không tự tăng quyền staff hoặc đóng GET danh mục đang public |
| Có thể dùng SQLite cho integration test | Dùng PostgreSQL riêng cho test migration, schema, transaction và refresh đồng thời |
| Chưa xác định test framework | Repo có executable smoke test; giữ/chuyển đổi kiểm thử đó và bổ sung xUnit cho module mới |

Đây là modular monolith, cùng tiến trình API và cùng PostgreSQL, không tạo server xác thực chạy riêng. Các quyết định trên là mặc định của instruction mới; không cần hỏi lại về chúng khi đã được giao triển khai tài liệu.

## 2. Hiện trạng đã kiểm chứng

| Thành phần | Hiện trạng và nguồn đối chiếu |
| --- | --- |
| Solution | `PetSpa.slnx`: API, SharedKernel và năm module Billing, Booking, Customer, Operation, Resource |
| Framework | Các project đã đọc dùng `net10.0`; API dùng Minimal API |
| Persistence | EF Core `10.0.12`, Npgsql provider `10.0.3`; mỗi module có DbContext, schema và migrations riêng |
| Đăng ký ứng dụng | `src/PetSpa.Api/Program.cs` gọi `Add*Module` và `Map*ModuleEndpoint`; auth/authorization hiện đăng ký trong `Composition` |
| Mật khẩu/JWT | `PetSpa.Api/Authentication/IdentityPasswordService.cs` dùng `PasswordHasher<T>`; `JwtTokenService.cs` phát access token. Chưa có Identity user store/refresh token |
| Customer login | `Customer/Application/Services/CustomerLoginService.cs`, `Customer/Endpoints/CustomerAuthEndpoints.cs`: phone + password, kiểm tra ACTIVE, rehash, cập nhật LastLoginAt |
| Staff login | `Operation/Application/Services/StaffLoginService.cs`, `Operation/Endpoints/OperationAuthEndpoints.cs`: tương tự, role lấy từ `Staff.Role` |
| Dữ liệu xác thực cũ | `Customer.PasswordHash`, `Staff.PasswordHash`, `LastLoginAt` ở cả hai entity; email Customer nullable; phone unique riêng từng bảng |
| Điểm ghi mật khẩu khác | `CustomerService.CreateAsync/UpdateAsync` và `CreateCustomerRequest/UpdateCustomerRequest` vẫn nhận/ghi mật khẩu |
| Hợp đồng chung | SharedKernel có `ITokenService`, `IPasswordService<T>`, login DTO/validator, role/claim/policy constants và `ClaimsPrincipalExtensions` |
| Claim hiện tại | `sub=customer:<id>` hoặc `staff:<id>`; ID nghiệp vụ là `long`; helper kiểm tra nghiêm ngặt đúng một role và đúng một loại ID |
| Authorization | Đã có default/fallback policy và bốn named policy; Resource GET public, Resource write dùng `StaffOnly` |
| Booking | `BookingService` đã kiểm tra ownership, pet thuộc customer, giới hạn customer đổi status/check-in/check-out; phải giữ các ràng buộc này |
| Chưa có endpoint nghiệp vụ | `BillingEndpoints.cs` và `OperationEndpoints.cs` đang rỗng; không coi báo cáo/hóa đơn/thanh toán/CRUD nhân viên là API đã triển khai |
| Test | `tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj` là executable .NET, dùng fake repositories, không kết nối DB, chưa nằm trong solution |
| Test phụ thuộc mã cũ | Project smoke test compile-link một số source Customer login; assertions kiểm tra chính xác shape response, claim cũ và quyền hiện tại |
| Git | `.gitignore` có `/docs/` và `/tests/`; một số file test đã được track. File mới bên trong hai thư mục có thể bị bỏ sót |

Đường dẫn module rút gọn trong bảng tính từ `src/PetSpa.Modules/`. Khảo sát này không đọc dữ liệu DB thật và không xác nhận trạng thái build/test hiện tại; agent triển khai phải lấy baseline mới.

## 3. Ranh giới module và chiều phụ thuộc

### 3.1. Module Auth sở hữu

- Identity user/role store, password hash, lockout, security stamp, trạng thái tài khoản, thời điểm đăng nhập.
- Phát và xác thực JWT; lưu, xoay vòng và thu hồi refresh token.
- Endpoint login, refresh, logout, logout-all, me, change-password, register và quản trị tài khoản.
- Options, validators, seeder, cleanup job và đăng ký policy xác thực/phân quyền chung.
- Quy trình tạo/liên kết tài khoản với hồ sơ, nhưng không trực tiếp truy vấn bảng hoặc DbContext của module khác.

### 3.2. Module nghiệp vụ sở hữu

- Customer: hồ sơ khách hàng, pet, quan hệ pet–customer, trạng thái nghiệp vụ và kiểm tra ownership.
- Operation: hồ sơ nhân viên, vị trí công việc, ca làm, phòng, phân công, trạng thái nghiệp vụ.
- Booking, Resource, Billing: dữ liệu và authorization theo tài nguyên của mình.
- `IsActive` của Auth và `Status` của hồ sơ có ý nghĩa riêng: được đăng nhập khi **cả hai** cho phép. Kích hoạt Auth không tự kích hoạt hồ sơ nghiệp vụ.
- Phone/email hồ sơ là dữ liệu liên hệ; phone đăng nhập thuộc Auth. Sửa hồ sơ không âm thầm đổi credential. Thay đổi phone đăng nhập/khôi phục tài khoản để giai đoạn sau.

### 3.3. Quy tắc phụ thuộc

```text
PetSpa.Api -> Auth + Customer + Operation + Booking + Resource + Billing
Auth -> SharedKernel
Auth -> Customer.Contracts + Operation.Contracts
Các module nghiệp vụ -> SharedKernel (+ contract nghiệp vụ đã có)
SharedKernel -> không tham chiếu API hoặc module
```

`Contracts` hiện là namespace trong project module, chưa phải project riêng. Có thể reference project Customer/Operation như cách Booking đang reference Customer/Resource, nhưng Auth chỉ được sử dụng public contract/DTO; kiểm thử kiến trúc phải ngăn truy cập Domain/Infrastructure của module khác.

- Customer/Operation **không reference ngược Auth**. Auth điều phối qua contract của chúng, tránh vòng phụ thuộc.
- Mở rộng contract để đọc hồ sơ theo ID, kiểm tra ACTIVE và tạo hồ sơ khi đăng ký. `ICustomerModule` hiện chỉ có `PetBelongsToCustomerAsync`; `IOperationModule` đang rỗng và cần implementation/DI nếu được dùng.
- Contract chỉ trả DTO, ID và trạng thái cần thiết; không trả entity, IQueryable, DbContext hoặc password hash. Công cụ import một lần là ngoại lệ chuyển đổi riêng tại mục 9.
- Giữ `AccountRoles`, `AccountTypes`, `AuthClaimTypes`, `AuthorizationPolicies`, helper đọc principal an toàn trong SharedKernel. SharedKernel không chứa UserManager, Identity entity, DbContext hoặc dịch vụ phát token.
- Di chuyển `ITokenService`, password abstraction, DTO/validator chỉ phục vụ login vào Auth hoặc xóa nếu Identity thay thế hoàn toàn; không giữ hai bộ triển khai đang hoạt động.
- Có thể giữ `LoginResponse`/`LoginUser` trong SharedKernel ở lần chuyển đầu để giữ hợp đồng chung và OpenAPI; không tạo DTO trùng tên với shape khác.

### 3.4. Cấu trúc đích

```text
src/PetSpa.Modules/Auth/
  PetSpa.Modules.Auth.csproj
  AuthModule.cs
  Application/
    Abstractions/ITokenService.cs
    Dtos/
    Services/AuthService.cs, AccountAdministrationService.cs
    Validators/
  Infrastructure/
    Identity/AppUser.cs
    Persistence/AuthDbContext.cs
    Persistence/Configurations/
    Persistence/Migrations/
    Tokens/TokenService.cs
    Authentication/JwtBearerRegistration.cs
    Authorization/AuthorizationRegistration.cs
    Configuration/JwtOptions.cs, AuthOptions.cs
    Seed/IdentitySeeder.cs
    Jobs/RefreshTokenCleanupService.cs
  Endpoints/AuthEndpoints.cs, AdminUsersEndpoints.cs
```

Đây là một project module theo mẫu repo, không tự tách thêm bốn project Domain/Application/Infrastructure/API. `AppUser : IdentityUser` là chi tiết Identity ở Infrastructure; không ép domain nghiệp vụ kế thừa Identity.

Expose `AddAuthModule(configuration, connectionString)` và `MapAuthModuleEndpoint()`. API chỉ gọi các extension này, cấu hình middleware và gắn adapter nếu cần; không còn implementation token/hash trong API. Token service dùng DbContext phải có lifetime scoped, không giữ singleton như implementation JWT hiện tại.

## 4. Identity, định danh và mô hình dữ liệu

### 4.1. Identity store

Đăng ký `AddIdentityCore<AppUser>()`, `AddRoles<IdentityRole>()`, `AddEntityFrameworkStores<AuthDbContext>()`, `AddDefaultTokenProviders()`; tiếp tục JwtBearer, không bật cookie/UI Identity.

`AuthDbContext : IdentityDbContext<AppUser>`:

- Gọi `base.OnModelCreating` và cấu hình toàn bộ bảng của Auth trong schema `auth`.
- Migrations nằm trong assembly `PetSpa.Modules.Auth`; history table `auth.__EFMigrationsHistory`.
- Dùng cùng `ConnectionStrings:DefaultConnection`, không gộp các context nghiệp vụ vào IdentityDbContext.
- Không đổi tên schema/history các module khác. Customer hiện có schema dữ liệu `customer` nhưng history cấu hình `Customer`; phải giữ nguyên khi khảo sát migration đã áp dụng.

Thành phần bổ sung của `AppUser`:

| Trường | Quy tắc |
| --- | --- |
| `Id` | string Identity ID, sinh độc lập; không dùng ID số của Customer/Staff làm khóa chung |
| `AccountType` | `customer` hoặc `staff`, bất biến sau khi tạo |
| `CustomerId`, `StaffId` | `long?`; tài khoản đã hoàn tất liên kết có đúng một ID đúng AccountType |
| `UserName` | Tạo nội bộ từ loại tài khoản + phone chuẩn hóa; không nhận tùy ý từ request |
| `PhoneNumber` | Phone đăng nhập; giữ quy tắc trim hiện có, chưa tự chuyển toàn bộ dữ liệu sang E.164 |
| `Email` | Nullable; chưa dùng làm login và chưa yêu cầu unique toàn hệ thống |
| `IsActive` | Trạng thái cho phép đăng nhập, khác trạng thái hồ sơ |
| `ProvisioningState` | `Pending`/`Ready` để xử lý tạo hồ sơ qua nhiều context; Pending tuyệt đối không được cấp token |
| `CreatedAtUtc`, `LastLoginAtUtc` | UTC; thời điểm đăng nhập chuyển về Auth |
| `TokenVersion` | Số phiên bản dùng vô hiệu hóa JWT đã phát, được cập nhật có kiểm soát đồng thời |

- `UserName` unique qua Identity và index unique `(AccountType, PhoneNumber)`; ràng buộc phone không rỗng/đã chuẩn hóa. Một phone được dùng ở một customer account và một staff account riêng biệt.
- Unique partial index cho `CustomerId`/`StaffId` khi khác null; check constraint cho cặp AccountType/liên kết. Pending có thể chưa có ID; Ready bắt buộc đúng một ID dương. Không có tài khoản đồng thời customer và staff trong giai đoạn này.
- Chỉ lưu scalar ID sang module khác; không navigation/cascade delete/EF foreign key xuyên module. Contract xác minh liên kết trước khi tài khoản Ready và khi xác thực.
- Không tự gộp tài khoản theo email hoặc phone trùng giữa Customer và Staff.
- Identity role name giữ chữ thường. Mỗi Ready account có đúng một role phù hợp AccountType. Role không biết/không có/nhiều role làm tài khoản không hợp lệ.
- Sau chuyển đổi, Identity là nguồn quyền duy nhất. `Staff.Role` không tiếp tục được dùng để cấp quyền hoặc đồng bộ hai chiều; loại bỏ field/constraint trong bước contract sau khi hết consumer. `Staff.Position` vẫn là dữ liệu nghiệp vụ.

### 4.2. Refresh token

Lưu `Id`, `UserId`, `FamilyId`, `TokenHash`, `CreatedAtUtc`, `ExpiresAtUtc`, `RevokedAtUtc`, `ReplacedByTokenHash`, `CreatedByIp` nếu cần. FK `UserId` chỉ tới Identity user trong Auth; unique `TokenHash`, index `UserId`, `FamilyId`, `ExpiresAtUtc`.

- Random 64 byte bằng `RandomNumberGenerator`, encode base64url; lưu SHA-256 của token, không lưu bản gốc.
- TTL refresh 14 ngày cho mỗi token mới; access JWT 15 phút. Dùng `TimeProvider` để test thời gian.
- `FamilyId` phục vụ truy vết chuỗi; **reuse sẽ thu hồi mọi refresh token của user**, mạnh hơn chỉ thu hồi family. Không mô tả hai hành vi khác nhau ở các phần tài liệu.
- Dữ liệu đã thu hồi vẫn giữ để phát hiện replay. Job hằng ngày chỉ xóa token có `ExpiresAtUtc` cũ hơn 30 ngày; tạo scope riêng cho DbContext.

## 5. Login, JWT và vòng đời phiên

### 5.1. Login bằng phone

- Giữ `POST /api/auth/customer/login` và `POST /api/auth/staff/login`, body `{ phone, password }`. Route xác định AccountType; không nhận role/user ID từ body.
- Hai endpoint đặt trong Auth và gọi một luồng xác thực dùng chung. Không còn hai bộ kiểm tra mật khẩu độc lập ở Customer/Operation.
- Dùng `UserManager.FindByNameAsync` với username tạo từ type + phone, `CheckPasswordAsync`, `AccessFailedAsync`, `ResetAccessFailedCountAsync`. Bật lockout cho user mới lẫn user import.
- Kiểm tra Pending, `IsActive`, lockout, role, liên kết hồ sơ và trạng thái hồ sơ. Thiếu hồ sơ/INACTIVE/role sai đều không được cấp token.
- Sai phone/password, locked, inactive trả cùng 401 ProblemDetails: “Số điện thoại hoặc mật khẩu không đúng”. Khi không thấy user, chạy xác minh dummy hash bằng PasswordHasher để giảm chênh lệch thời gian.
- Lockout mặc định 5 lần sai trong chuỗi thất bại, khóa 15 phút; rate limit là lớp độc lập. Test lockout phải không bị rate limit che khuất.
- Login thành công cập nhật `LastLoginAtUtc` ở Auth; không ghi ngược `LastLoginAt` của hồ sơ.

### 5.2. Hợp đồng response và claim

Giữ các field response hiện có, thêm refresh và định danh Auth:

```json
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque-token>",
  "tokenType": "Bearer",
  "expiresAtUtc": "<UTC khớp exp tới giây>",
  "expiresInSeconds": 900,
  "role": "customer",
  "user": {
    "id": 1,
    "authUserId": "<Identity ID>",
    "fullName": "Nguyễn Văn A",
    "phone": "0900000001"
  }
}
```

`user.id` tiếp tục là ID hồ sơ nghiệp vụ, không đổi kiểu sang string. `user.phone` là phone đăng nhập Auth; fullName lấy từ profile contract. Không trả entity, security stamp, token hash hoặc trạng thái nội bộ. `/me` không phát token mới.

Claim đích:

- `sub = AppUser.Id`, không còn `customer:<id>`/`staff:<id>`.
- `role`, `account_type`, đúng một trong `customer_id`/`staff_id`.
- `jti`, `iss`, `aud`, `nbf`, `exp`, `token_version`; có thể thêm `name` và `email` khi tồn tại.
- `MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"`; chỉ chấp nhận HS256, issuer/audience/signature/lifetime đúng, clock skew tối đa 30 giây.

**Phải sửa đồng thời `ClaimsPrincipalExtensions`**: xác minh sub Identity không rỗng, role/type phù hợp, ID nghiệp vụ dương và không trùng/mâu thuẫn. Không còn so sánh sub với `${accountType}:${id}`. Helper chỉ kiểm tra cấu trúc; `OnTokenValidated` xác minh sub/liên kết/role/token_version với dữ liệu Auth và hồ sơ thật.

`OnTokenValidated` dùng scoped service, từ chối user không tồn tại, Pending, inactive, locked, profile thiếu/INACTIVE, version lệch hoặc claim liên kết/role khác DB. Token hỏng/claim danh tính không hợp lệ trả 401; principal hợp lệ nhưng thiếu quyền thao tác trả 403. Không mặc định JwtBearer tự kiểm tra SecurityStamp của Identity.

Cutover bắt buộc đăng nhập lại: token định dạng cũ bị từ chối, không fallback parser cũ. Việc đổi shape và phản hồi lỗi phải cập nhật OpenAPI/smoke test; không sửa WPF trong đợt này và không khẳng định client tương thích khi chưa test client.

### 5.3. Refresh và thu hồi

Interface token nằm trong Auth; tối thiểu có issue, refresh, revoke token của current user và revoke-all. Không giữ chữ ký `RevokeAsync(string token)` không ràng buộc chủ sở hữu ở endpoint logout.

Thuật toán refresh:

1. Validate kích thước token, băm và tra bản ghi; không thấy trả 401 chung.
2. Trong transaction, serialize thao tác theo user bằng khóa hàng PostgreSQL hoặc cơ chế concurrency tương đương đã kiểm thử. Tải lại token/user sau khi lấy khóa.
3. Nếu đã revoked: thu hồi toàn bộ refresh token user và tăng TokenVersion, **commit việc thu hồi trước khi trả 401**. Log sự kiện bằng user ID/family ID, không log token/hash.
4. Nếu hết hạn hoặc account/profile không còn hợp lệ: không phát token, trả 401.
5. Thu hồi token cũ, đặt hash kế tiếp, thêm token mới trong cùng transaction; role/claims mới lấy từ trạng thái hiện tại.
6. Chỉ trả cặp token mới khi commit thành công. Xung đột/concurrency không được tạo hai nhánh refresh hợp lệ.

Login issue, refresh, logout, logout-all, change-password và thay đổi quyền/trạng thái phải dùng cùng quy tắc serialize theo user. Chỉ mở transaction quanh `SaveChanges` không đủ ngăn hai request cùng đọc token còn hiệu lực.

| Sự kiện | Hành vi bắt buộc |
| --- | --- |
| Logout một phiên | Chỉ xử lý token thuộc user đang đăng nhập; thu hồi family của phiên đó để token đã rotate cũng được xử lý; idempotent, 204, không tiết lộ token người khác |
| Logout-all | Thu hồi mọi refresh token, tăng TokenVersion; JWT cũ thất bại ở request tiếp theo |
| Đổi mật khẩu thành công | Qua UserManager với mật khẩu hiện tại; revoke-all, đổi SecurityStamp, tăng TokenVersion trong cùng giao dịch Auth |
| Đổi role/vô hiệu hóa | Cập nhật Identity, revoke-all, đổi SecurityStamp, tăng TokenVersion; commit nguyên tử |
| Kích hoạt lại | Không hồi sinh token đã thu hồi; giữ version mới, yêu cầu đăng nhập lại |
| Phát hiện reuse | Revoke-all + tăng TokenVersion; request refresh thứ hai thất bại và token mới của request đầu cũng bị vô hiệu hóa |

Logout một phiên chỉ bảo đảm refresh bị thu hồi; access token của phiên đó có thể còn sống 15 phút cộng clock skew. Không tuyên bố logout một phiên thu hồi JWT tức thì nếu chưa có kiểm tra session ID. Tài khoản/profile bị khóa thì OnTokenValidated chặn ngay request kế tiếp; request đã chạy trước đó không thể bị “rút lại”.

## 6. Quản trị tài khoản và tạo hồ sơ

### 6.1. Endpoint

| Method | Route | Quyền / kết quả |
| --- | --- | --- |
| POST | `/api/auth/customer/login` | Public, phone/password, 200 token response |
| POST | `/api/auth/staff/login` | Public, phone/password, 200 token response |
| POST | `/api/auth/refresh` | Public, `{ refreshToken }`, 200 token response |
| POST | `/api/auth/logout` | AuthenticatedAccount, `{ refreshToken }`, 204 |
| POST | `/api/auth/logout-all` | AuthenticatedAccount, 204 |
| GET | `/api/auth/me` | AuthenticatedAccount, user + role, 200 |
| POST | `/api/auth/change-password` | AuthenticatedAccount, `{ currentPassword, newPassword }`, 204 |
| POST | `/api/auth/register` | Public khi bật; tạo customer account và profile mới, 201 token response; tắt trả 404 |
| GET | `/api/admin/users` | AdminOnly, phân trang có giới hạn |
| POST | `/api/admin/users` | AdminOnly, tạo tài khoản liên kết profile Customer/Staff có sẵn, 201 user summary |
| PUT | `/api/admin/users/{id}/roles` | AdminOnly, `{ roles: ["manager"] }`, đúng một role, 204 |
| POST | `/api/admin/users/{id}/deactivate` | AdminOnly, 204 |
| POST | `/api/admin/users/{id}/activate` | AdminOnly, 204 |

`{id}` trong quản trị là Identity string ID. Route plural `roles` kế thừa hướng dẫn gốc nhưng mảng bắt buộc đúng một phần tử. Không nhận role, AccountType hoặc profile ID từ public register; customer role do server quyết định.

- Chỉ DTO quản trị mới được nhận `customerId`/`staffId`; phải kiểm tra profile tồn tại, ACTIVE, đúng loại và chưa gắn tài khoản khác. Từ chối đổi loại tài khoản bằng endpoint đổi role.
- Không tự hạ quyền hoặc vô hiệu hóa chính mình; không để thao tác làm mất admin hoạt động cuối cùng. Hai admin cùng hạ quyền nhau phải được serialize bằng khóa quản trị chung ở DB, không chỉ đếm rồi ghi riêng lẻ.
- Không thêm endpoint xóa user trong đợt này. Constraint profile link ngăn cấp tài khoản trùng; không cấp quyền chỉ vì caller biết phone/email hoặc profile ID.
- Public register gặp phone đã có profile/account thì trả thông báo không lộ tài khoản, không tự chiếm hồ sơ đó. Liên kết profile có sẵn chỉ dành cho Admin.

### 6.2. Tách tài khoản khỏi CRUD Customer

- `POST /api/customers` chỉ tạo hồ sơ, không bắt buộc password và không tự tạo tài khoản. Quyền receptionist/manager/admin quản lý hồ sơ không đồng nghĩa quyền quản trị credential.
- Bỏ `Password` ở Create/UpdateCustomerRequest, validator và CustomerService; chặn trường password cũ bằng lỗi 400 có mô tả chuyển sang auth flow, không trả thành công như thể mật khẩu đã đổi.
- Customer tự cập nhật chỉ được sửa các trường hồ sơ cho phép; không sửa Status/Note nội bộ/role/credential. Tách DTO hoặc kiểm tra theo actor trước khi map, không mass-assign toàn bộ UpdateCustomerRequest.
- Thay đổi trạng thái hồ sơ được kiểm soát ở module nghiệp vụ. Auth luôn kiểm tra trạng thái hồ sơ khi login, refresh và xác thực JWT.

### 6.3. Nhất quán khi register qua hai module

Không giả định hai DbContext dùng cùng connection string thì tự có transaction chung. EF yêu cầu cùng DbConnection và DbTransaction khi chia sẻ transaction giữa contexts; xem [tài liệu transaction của EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

Mặc định chọn quy trình Pending có khả năng phục hồi để không buộc module lộ DbContext:

1. Auth tạo user Pending, giữ reservation unique cho type/phone và một provisioning ID bền vững.
2. Gọi Customer contract tạo profile với provisioning ID làm khóa idempotency; contract tra lại kết quả khi retry, không tạo profile lần hai.
3. Auth ghi liên kết, role customer, chuyển Ready và phát token sau khi commit.
4. Nếu bước nào lỗi, không có token; lưu trạng thái đủ để retry/reconcile. Có job hoặc lệnh quản trị nội bộ tiếp tục Pending theo provisioning ID. Không chỉ catch rồi bỏ quên hồ sơ mồ côi.
5. Kiểm thử crash sau khi profile đã tạo nhưng Auth chưa lưu link. Không xóa profile đã có dữ liệu nghiệp vụ để “rollback”.

Không cần message broker/outbox framework cho phạm vi này. `AllowSelfRegistration=false` mặc định; chỉ bật sau khi luồng Pending/recovery đã hoàn thiện và được test. Admin tạo tài khoản trên profile có sẵn cũng phải xác minh lại profile trước khi chuyển Ready.

## 7. Cấu hình, secrets và hardening

### 7.1. Options và packages

- Giữ tên `Jwt:Issuer`, `Jwt:Audience`, `Jwt:AccessTokenMinutes=15`; thêm `Jwt:RefreshTokenDays=14`.
- Đổi `Jwt:SigningKey` UTF-8 hiện tại sang **`Jwt:Key` base64 của ít nhất 32 byte ngẫu nhiên**, giải mã base64 trước khi ký/xác minh. Không tự coi chuỗi cũ là base64 và không dùng fallback bí mật cứng trong code.
- ValidateOnStart: issuer/audience không rỗng, key base64 hợp lệ và đủ byte, thời hạn dương. Không đưa giá trị secret vào thông báo lỗi.
- `Auth:AllowSelfRegistration=false`, `Auth:RequireConfirmedEmail=false`; nếu chưa hỗ trợ email confirmation mà cấu hình RequireConfirmedEmail=true thì fail fast, không âm thầm bỏ qua yêu cầu bảo mật.
- Giữ cột Identity `EmailConfirmed`, `PhoneNumberConfirmed`, `TwoFactorEnabled`; chưa bật OTP/2FA. `.AddDefaultTokenProviders()` là điểm mở rộng, không phải đã có quy trình OTP.

Package được phép dùng/thêm cho việc triển khai instruction:

| Nơi | Package |
| --- | --- |
| Auth | `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, EF Core, Npgsql, FluentValidation DI theo phiên bản tương thích trong repo |
| Tooling | `Microsoft.EntityFrameworkCore.Design` với PrivateAssets nếu cần tạo migration |
| Test mới | `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.AspNetCore.Mvc.Testing` |

Giữ dòng Microsoft/EF `10.0.x` phù hợp package đang dùng; không nâng framework/provider hàng loạt. Không thêm IdentityServer, custom crypto, MediatR, AutoMapper hoặc test container framework chỉ để tách Auth. Dùng PostgreSQL test được cấp riêng và xUnit assertions; chưa cần FluentAssertions. Package ngoài danh sách chỉ thêm khi thật sự cần và giải thích trước.

API đã có UserSecretsId; không chạy init để thay ID. Ví dụ cấu hình dưới đây chỉ là placeholder:

```powershell
dotnet user-secrets set "Jwt:Key" "<base64 của ít nhất 32 byte ngẫu nhiên>" --project src/PetSpa.Api/PetSpa.Api.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<kết nối PostgreSQL dev>" --project src/PetSpa.Api/PetSpa.Api.csproj
dotnet user-secrets set "Seed:AdminStaffId" "<ID hồ sơ nhân viên ACTIVE>" --project src/PetSpa.Api/PetSpa.Api.csproj
dotnet user-secrets set "Seed:AdminPassword" "<mật khẩu mạnh>" --project src/PetSpa.Api/PetSpa.Api.csproj
```

Production dùng `Jwt__Key`, `ConnectionStrings__DefaultConnection`, `Seed__AdminStaffId`, `Seed__AdminPassword` từ secret store/environment. Không copy connection string/password/key thật vào hướng dẫn, `.http`, log hoặc commit.

### 7.2. Seed

- Seed idempotent đủ năm role; không đổi casing khi seed lại.
- Chạy import tài khoản cũ trước khi quyết định seed admin. Nếu đã có admin thì không tự tạo/nâng quyền thêm.
- Admin đầu tiên phải gắn hồ sơ Staff ACTIVE có sẵn bằng `Seed:AdminStaffId`, phone lấy qua contract, password qua UserManager. Không tạo tài khoản staff không có hồ sơ để vượt helper.
- Thiếu cấu hình hoặc hồ sơ không hợp lệ thì bỏ qua và cảnh báo đã lọc dữ liệu nhạy cảm. Nếu DB mới chưa có Staff, ghi rõ bước chuẩn bị hồ sơ qua công cụ nội bộ; không thêm endpoint bootstrap public.
- Nếu hồ sơ đã gắn tài khoản không phải admin, không âm thầm nâng quyền hoặc reset password bằng seeder; cần luồng bootstrap nội bộ có chủ đích.
- Seeder phải xử lý chạy lại/chạy đồng thời; không tạo admin mặc định với mật khẩu hardcode.

### 7.3. Middleware và bảo vệ API

- HTTPS redirect; HSTS ngoài Development. ForwardedHeaders chỉ tin proxy/network đã cấu hình, chạy trước HTTPS redirect và trước khi đọc IP.
- Rate limit dùng limiter tích hợp: login khoảng 5/phút/IP dùng chung bucket cho hai route, refresh 20/phút/IP; register/admin tạo account cũng có giới hạn hợp lý. 429 có `Retry-After` và ProblemDetails.
- Có `UseRouting` trước endpoint-specific rate limiting; thứ tự tổng thể: forwarded headers, exception handling/HSTS/HTTPS, routing, CORS, authentication, rate limiter, authorization, endpoints.
- Thay CORS `AllowAll` bằng allowlist cấu hình. WPF không cần CORS; nếu không có browser client thì không mở policy. Không dùng wildcard origin với credentials.
- `AddProblemDetails` + exception handler; chuyển FluentValidation lỗi field sang ValidationProblem. Cấu hình challenge/forbid/rate limit có response thống nhất, giữ `WWW-Authenticate` khi 401.
- Header: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`.
- Giới hạn body auth, ví dụ 16 KiB, và độ dài chuỗi request. `Cache-Control: no-store` cho token/account responses.
- Không log mật khẩu, Authorization header, JWT, refresh token hay hash. Không trả stack trace, SQL/connection string ra client. Audit login, lockout, reuse, đổi role/trạng thái bằng ID và thời điểm.
- Không map auth route ở group `.AllowAnonymous()` rồi hy vọng `.RequireAuthorization()` ở route con bảo vệ lại. Chỉ đánh dấu public từng route cụ thể.

## 8. Ma trận quyền áp dụng cho endpoint thực tế

Giữ `DefaultPolicy` và `FallbackPolicy` là `AuthenticatedAccount`. Mỗi named policy tự yêu cầu principal hợp lệ; không giả định fallback được cộng vào named policy.

| Policy | Điều kiện |
| --- | --- |
| `AuthenticatedAccount` | Authenticated + cấu trúc claim hợp lệ + đã qua kiểm tra trạng thái trong JwtBearer |
| `StaffOnly` | account_type staff và một role staff/receptionist/manager/admin |
| `CustomerManagement` | receptionist/manager/admin, account_type staff |
| `ManagerOrAdmin` | manager/admin, account_type staff |
| `AdminOnly` (mới) | admin, account_type staff |

| Endpoint hiện có | Quyền đích |
| --- | --- |
| GET `/health` | Public, chỉ liveness đơn giản |
| GET `/openapi/v1.json`, `/swagger/v1/swagger.json`, Swagger UI | Chỉ public trong Development như host hiện tại |
| GET `/api/categories`, `/api/categories/{id}` | Public |
| GET `/api/services`, `/api/services/{id}`, `/api/categories/{categoryId}/services` | Public |
| GET `/api/service-options`, `/api/service-options/{id}`, `/api/services/{serviceId}/options` | Public |
| POST/PUT/DELETE categories/services/service-options | StaffOnly, giữ hành vi hiện có |
| POST `/api/customers` | CustomerManagement, chỉ tạo profile |
| GET/PUT `/api/customers/{id}` | CustomerManagement hoặc customer chính chủ; PUT có allowlist field theo actor |
| GET/POST `/api/customers/{customerId}/pets` | CustomerManagement hoặc customer chính chủ; đối chiếu ID URL/body |
| GET/PUT `/api/pets/{id}` | CustomerManagement hoặc owner thực tế lấy từ pet |
| GET `/api/bookings/{id}` | StaffOnly hoặc customer sở hữu booking |
| POST `/api/bookings` | StaffOnly hoặc customer tạo cho chính mình, pet phải thuộc customer |
| PUT `/api/bookings/{id}`, `/api/bookings/{id}/services` | StaffOnly hoặc customer chính chủ trong giới hạn nghiệp vụ hiện có |

Không tự nâng staff thường thành người quản lý mọi hồ sơ Customer/Pet: hiện quyền đó thuộc receptionist/manager/admin. Không tự hạ quyền ghi Resource của staff xuống manager chỉ vì ma trận mẫu khác repo. Siết các quyền này là thay đổi sản phẩm riêng, phải được ghi rõ nếu yêu cầu sau này.

Các điều chỉnh bảo mật thuộc đợt này:

- Customer truy cập tài nguyên/owner của người khác trả 404 thống nhất, bao gồm Customer, Pet, Booking. Người có danh tính hợp lệ nhưng sai policy trả 403. Sửa assertions đang mong 403 ở các ca ownership tương ứng.
- Quyền sở hữu dựa trên ID đã xác thực và quan hệ DB, không dựa riêng `customerId` body/query.
- Khi sửa pet/booking, kiểm tra cả chủ hiện tại và chủ/quan hệ mới. Không cho customer chuyển owner để chiếm dữ liệu.
- Giữ kiểm tra `BookingActor`, `PetBelongsToCustomerAsync`, trạng thái PENDING/CONFIRMED, chặn customer thay Status/CheckIn/CheckOut.
- Ownership handler hoặc service dùng chung theo từng module; Auth không truy cập repository Booking/Pet/Invoice để quyết định ownership.
- Không dựng CRUD Billing, thanh toán, báo cáo hoặc Operation chỉ để lấp đầy ma trận mẫu. Khi các endpoint đó được triển khai sau này mới thêm policy phù hợp.

## 9. Chuyển đổi dữ liệu không mất tài khoản

### 9.1. Khảo sát trước khi chuyển

Trên DB test/bản sao được phép truy cập, lập báo cáo số lượng theo loại account/status/role, phone thiếu/trùng sau trim, hash thiếu/không đúng định dạng, hồ sơ thiếu liên kết. Chỉ báo cáo ID/số lượng; không in hash, mật khẩu, số điện thoại đầy đủ hoặc connection string.

Đọc migration history của từng schema. Operation có hai migration tên `InitialOperation` với timestamp khác nhau; không mặc định chạy trọn chuỗi trên DB mới sẽ thành công. Rà Up/Down/designer/snapshot và tạo kế hoạch baseline riêng nếu cần, không xóa lịch sử hoặc sửa migration đã triển khai để né lỗi.

### 9.2. Expand → import → cutover → contract

1. **Expand:** thêm Auth project/schema/Identity/refresh tables và migration riêng; chưa drop password/role cũ. Profile không có tài khoản là hợp lệ ở mô hình mới; chuẩn bị migration nới NOT NULL của password cũ khi chuyển CRUD sang profile-only, không điền hash giả.
2. **Import thử:** công cụ nội bộ có dry-run, chạy idempotent theo `(AccountType, legacyProfileId)`. Có thể đọc bảng cũ bằng truy vấn chỉ phục vụ migration trong công cụ tách biệt, không biến truy cập xuyên schema thành đường runtime của Auth. Không expose import qua HTTP.
3. **Ánh xạ:** mỗi Customer/Staff thành tài khoản riêng; giữ ID profile, phone, email, status, thời điểm đăng nhập; ánh xạ STAFF/RECEPTIONIST/MANAGER/ADMIN tường minh sang role chữ thường, customer luôn customer. Không suy diễn role không biết.
4. **Hash:** dữ liệu hiện dùng PasswordHasher của Identity; giữ nguyên hash hợp lệ để PasswordHasher<AppUser> xác minh và rehash khi cần. Kiểm thử bằng fixtures có mật khẩu biết trước ở cả IdentityV2/V3. Không hash lại chuỗi hash, không reset đồng loạt, không giả định mọi dữ liệu cũ đều đúng định dạng. Bản ghi lỗi bị chặn/đưa vào báo cáo xử lý, không tự tạo password mặc định.
5. **Hoàn tất import:** tạo username/normalized fields/security stamp/concurrency stamp qua luồng import kiểm soát; mọi IdentityResult phải được kiểm tra. Dùng transaction per account, lưu checkpoint, không ghi đè password/role đã đổi trong Auth khi chạy lại.
6. **Cutover:** chọn cửa sổ bảo trì, dừng ghi credential cũ/login cũ, chạy import cuối và đối chiếu số lượng/mapping, chuyển route/DI sang Auth và đổi JWT config/claims. Yêu cầu client đăng nhập lại. Không chạy song song hai nguồn password đang ghi.
7. **Contract:** sau khi chứng minh mọi tài khoản cần chuyển đã được xử lý và rollback đã được chuẩn bị, tạo migration mới gỡ password/LastLoginAt auth-only khỏi profile và Staff.Role nếu không còn consumer. Không sửa migration cũ. Nếu còn case lỗi, giữ cột legacy read-only và báo migration chưa hoàn tất.

Backup/restore phải được thử trên bản sao. Sau cutover đã có đổi password trong Auth, rollback về binary cũ sẽ dùng hash lỗi thời: không chỉ chạy Down migration rồi bật app cũ. Rollback cần bảo trì và kế hoạch khôi phục dữ liệu nhất quán; không sao chép bí mật ra file văn bản. Không tự áp migration lên DB thật khi nhiệm vụ chỉ là sửa source/tạo script.

## 10. Các giai đoạn thực hiện và nghiệm thu

### Giai đoạn 0 — Baseline và kế hoạch cụ thể

- Đọc code, instruction liên quan, tests kể cả thư mục bị ignore; xác minh các nhận định mục 2 còn đúng.
- Chạy build solution và executable smoke test riêng, ghi lỗi/warning có sẵn. Không dùng `dotnet test PetSpa.slnx` để khẳng định đã chạy smoke test.
- Lập danh sách file di chuyển/xóa, thay đổi API có chủ đích, kế hoạch migration và DB test. Chưa sửa DB thật.

### Giai đoạn 1 — Khung Auth và composition

- Tạo project net10.0, FrameworkReference Microsoft.AspNetCore.App, packages cần thiết; thêm vào solution và API reference.
- Chuyển options/token registration/authorization registration sang Auth, giữ tên constants dùng chung. Bổ sung contract profile tối thiểu, tránh cycle.
- Nếu dùng adapter tạm để build từng bước, đánh dấu thời hạn và xóa ở cutover; không được kết thúc với hash/token implementation còn phân tán.
- Nghiệm thu: build và các test đang áp dụng pass; kiểm tra dependency direction.

### Giai đoạn 2 — Identity store và migration/import

- Model/schema/index/check constraint, seeder và migration Auth; công cụ import dry-run/idempotent, fixtures dữ liệu legacy.
- Sinh và xem SQL trước khi áp lên PostgreSQL test. Kiểm tra chạy lại không trùng tài khoản/role.
- Nghiệm thu: DB test từ baseline hợp lệ và bản sao nâng cấp đều có mapping đúng; hash cũ đăng nhập được; không ảnh hưởng bảng nghiệp vụ.

### Giai đoạn 3 — Login/JWT/refresh và quản trị

- Implement Identity login, khóa tài khoản, claims mới, OnTokenValidated, refresh transaction/concurrency, logout/đổi mật khẩu, endpoints admin.
- Implement Pending/recovery cho self-registration; cấu hình mặc định vẫn tắt.
- Nghiệm thu: luồng HTTP thực tế qua middleware và DB test, bao gồm hai refresh đồng thời, role change và admin cuối cùng.

### Giai đoạn 4 — Cutover nghiệp vụ và loại code cũ

- Bỏ map login khỏi CustomerModule/OperationModule; Auth map mỗi route đúng một lần.
- Xóa CustomerLoginService/StaffLoginService, auth endpoint/request/result/validator cũ khi hết consumer; xóa password/token abstractions thừa.
- Sửa Customer CRUD, helper principal, ownership responses; giữ Booking business constraints và ma trận Resource.
- Sửa OpenApiRegistration đang đọc LoginResponse/role enum, `openapi.yaml`, `.http` và smoke test cùng lúc với hợp đồng mới.
- Nghiệm thu: không có cấp token hoặc ghi mật khẩu ngoài Auth; module khác không reference Identity implementation.

### Giai đoạn 5 — Hardening, tài liệu và kiểm tra cuối

- Secrets/fail-fast, limiter, CORS, ProblemDetails, proxy/HTTPS, cleanup và audit.
- Hoàn thiện `docs/auth.md`: kiến trúc, secrets, endpoint/policy, schema, import/rollback, giả định, hạn chế, thêm role/policy, vận hành Pending.
- Cập nhật `HUONG-DAN-CHAY.md` đang hướng dẫn SigningKey/appsettings; cập nhật `src/PetSpa.Api/appsettings.example.json` chỉ với giá trị không bí mật.
- Cập nhật `src/PetSpa.Api/PetSpa.Api.http`; không tạo thêm file requests trùng mục đích nếu file này đủ dùng.
- Khi triển khai, điều chỉnh `.gitignore` có chọn lọc để auth docs/tests mới được track; không mở toàn bộ thư mục chứa tài liệu local nhạy cảm. Tự kiểm tra bằng `git check-ignore` và `git status`.
- Mỗi giai đoạn báo ngắn: thay đổi, lệnh kiểm chứng, kết quả thật và phần còn vướng. Không dừng xin duyệt lại các thao tác source/test đã được giao; chỉ tách việc triển khai DB thật ngoài phạm vi.

## 11. Kiểm thử bắt buộc

Giữ executable smoke test và cập nhật nó sử dụng public Auth entry points; bỏ các Compile Include đang kéo source login Customer đã xóa. Có thể thay nội dung executable bằng runner smoke mới nhưng không xóa độ bao phủ hoặc bỏ assertion để che lỗi. Thêm project xUnit test được đăng ký trong solution; dùng `WebApplicationFactory<Program>` và public partial Program nếu cần.

| Nhóm | Ca bắt buộc |
| --- | --- |
| Import | Khách/nhân viên cùng ID hoặc phone vẫn tách biệt; email null; role mapping; hash V2/V3 + rehash; lỗi dữ liệu; chạy lại không ghi đè credential |
| Login | Đúng thông tin; sai phone/password/inactive/lockout/profile thiếu cùng 401; Pending không login; role không hợp lệ bị từ chối |
| Lockout/rate limit | Sai liên tiếp khóa user; không vượt giới hạn bằng đổi route; 429 + Retry-After; test hai cơ chế độc lập |
| JWT | Sai chữ ký/issuer/audience/algorithm/hết hạn; sub cũ; claim trùng; cả hai profile ID; version/role/link lệch DB → 401 |
| Refresh | Hợp lệ, hết hạn, không tồn tại; chỉ lưu hash; rotation; replay làm token mới vô hiệu; hai request đồng thời không tạo hai nhánh sống |
| Thu hồi | Logout không thu hồi phiên user khác; token cha đã rotate vẫn logout family được; logout-all/đổi password/đổi role/vô hiệu hóa chặn JWT và refresh cũ |
| Admin | Đúng role; không tự hạ/vô hiệu; không mất admin cuối cùng khi hai request đồng thời; không gán customer role cho staff; không link trùng profile |
| Registration | Cờ off 404; bật luôn customer; gửi role/IsActive/profile ID không được nâng quyền; duplicate; Pending retry/crash recovery không tạo hồ sơ trùng |
| Profile CRUD | Tạo profile không cần mật khẩu; password cũ bị từ chối rõ; customer không sửa Status/credential/field nội bộ |
| Quyền | Mỗi role qua endpoint thật trong ma trận; fallback và named policy; các GET public còn truy cập được |
| Ownership | Customer A không đọc/sửa Customer/Pet/Booking B (404); kiểm tra owner cũ/mới; pet khác chủ; khách không sửa trường vận hành booking |
| Regression | Login response giữ các field nghiệp vụ cũ, thêm field mới đúng; OpenAPI generated/static nhất quán; client contract được ghi rõ |
| Persistence | Migration trên PostgreSQL test, schema/history đúng, unique/check constraint thực thi, seed lặp/chạy đồng thời, rollback/restore rehearsal |
| Hardening | Thiếu/sai key dừng startup; lỗi 500 không rò chi tiết; limiter/headers/CORS đúng; cleanup không xóa token còn trong thời gian lưu |
| Kiến trúc | API không hash/phát token; Customer/Operation không quản lý credential; SharedKernel không reference Auth/Identity; không DbContext xuyên module |

Không dùng EF InMemory/fake repositories để chứng minh transaction, row lock, uniqueness hay migration đúng. Test này phải chạy với PostgreSQL riêng; không dùng database nghiệp vụ. Nếu thiếu DB test, ghi rõ nhóm chưa chạy và chưa đạt Definition of Done.

Lệnh baseline và kiểm chứng cơ bản tại root:

```powershell
dotnet build PetSpa.slnx
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
dotnet test PetSpa.slnx
git diff --check
```

`dotnet test` chỉ có ý nghĩa cho test runner sau khi đã thêm xUnit projects vào solution; executable smoke vẫn cần `dotnet run`. Báo số lượng test thực tế, không báo “test pass” nếu runner tìm thấy 0 test.

Lệnh migration tham khảo, chỉ chạy sau khi Auth project đã tồn tại và cấu hình dev/test sẵn sàng:

```powershell
dotnet ef migrations add InitialAuth --project src/PetSpa.Modules/Auth/PetSpa.Modules.Auth.csproj --startup-project src/PetSpa.Api/PetSpa.Api.csproj --context AuthDbContext --output-dir Infrastructure/Persistence/Migrations
dotnet ef migrations script --idempotent --project src/PetSpa.Modules/Auth/PetSpa.Modules.Auth.csproj --startup-project src/PetSpa.Api/PetSpa.Api.csproj --context AuthDbContext
```

Các lệnh này sinh migration/SQL, không thay thế kiểm tra migration history hoặc tự cho phép áp DB production. Dùng startup project/factory có cùng options Identity khi tạo migration theo [hướng dẫn Identity model của Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0).

## 12. Definition of Done

- [ ] Module Auth là nơi duy nhất quản lý credential, JWT, refresh và tài khoản; API chỉ composition, SharedKernel chỉ hợp đồng chung.
- [ ] Mọi route login được map duy nhất ở Auth; ma trận quyền và kiểm tra ownership vẫn đầy đủ.
- [ ] Account/profile tách biệt, role có một nguồn chuẩn; định danh Identity không nhập nhằng ID Customer/Staff.
- [ ] Migration/import được kiểm thử với dữ liệu legacy; không mất hash hợp lệ, không gộp tài khoản, không xóa lịch sử migration.
- [ ] Không còn đường đổi mật khẩu qua Customer CRUD; không có dual-write credential sau cutover.
- [ ] Refresh rotation/reuse/concurrency, lockout, JWT version, admin cuối cùng và Pending recovery có test thực tế.
- [ ] Build, smoke và xUnit/PostgreSQL tests chạy thành công; báo riêng warning/lỗi nền và nhóm chưa chạy.
- [ ] OpenAPI, `.http`, hướng dẫn chạy, `docs/auth.md`, test và file mới cần thiết đều được Git theo dõi.
- [ ] Đã ghi rõ các breaking change: sub mới, field response mới, SigningKey → Key, CRUD bỏ password, ownership 404, token cũ buộc login lại.
- [ ] Không có secret thật, plaintext password/hash/token trong log hoặc artifact commit; không sửa WPF/OTP ngoài phạm vi.

Nếu chưa chuyển DB thật, báo “source và kế hoạch migration sẵn sàng”, không báo “đã chuyển tài khoản production”. Nếu còn cột legacy do chưa xử lý hết dữ liệu, báo contract migration còn chờ và không đánh dấu hoàn tất toàn bộ.

## 13. Prompt giao việc cho agent triển khai

```text
Đọc PETSPA_AUTH_MODULE_INSTRUCTIONS.md và mã nguồn hiện tại.
Thực hiện giai đoạn 0, sau đó triển khai lần lượt module Auth theo các giai đoạn trong tài liệu.
Giữ modular monolith, hai route login bằng phone, năm role và ma trận quyền đã chốt.
Chỉ sửa server; không sửa WPF và không làm OTP. Không tự áp migration lên DB thật.
Sau mỗi giai đoạn chạy build/kiểm thử phù hợp và báo kết quả; không bỏ test khi contract đổi.
Báo rõ phần đã triển khai, phần chỉ sinh script và phần chưa được kiểm chứng.
```
