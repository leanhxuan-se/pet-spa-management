# Kế hoạch refactor xác thực và phân quyền PetSpa

**Bản chờ duyệt · 20/11/2026.** Khách hàng tự đăng ký. Khách và nhân sự dùng chung một API đăng nhập bằng **tên tài khoản hoặc số điện thoại và mật khẩu**. Admin có thể cấp, thay hoặc thu hồi vai trò nội bộ của một tài khoản nhân sự. PetSpa tự xác thực, phát JWT và dùng PostgreSQL hiện có. Quên mật khẩu đã được gác lại theo yêu cầu. Đợt này cũng chưa cần refresh token hoặc dịch vụ xác thực bên ngoài.

## 1. Nghiệp vụ lấy từ diagram

| Nguồn trong [Diagram.pdf](docs/Base/Diagram.pdf) | Kết quả phải giữ khi thay Auth |
| --- | --- |
| AUTH-01, tr. 3 | Khách ACTIVE đăng nhập, dùng `user.id` và Bearer để đọc hồ sơ và danh sách thú cưng của mình. Khách chưa có pet nhận `200 []`. |
| CUS-03, tr. 6 và CUS-06, tr. 9 | Lễ tân hoặc quản lý đăng nhập, mở danh sách khách ACTIVE và tạo khách tại quầy. Khách vừa tạo có thể đăng nhập. Đăng ký công khai là luồng mới dùng chung thao tác tạo tài khoản và hồ sơ. |
| CUS-05, tr. 8 và OPS-02, tr. 20 | Ngừng hoạt động hồ sơ phải chặn truy cập và giữ lịch sử. Tạo nhân sự và cấp vai trò là nghiệp vụ có quyền. |
| OPS-21, tr. 39 | Nhân viên đăng nhập, nhận đúng role và `staff_id`. Hai API lấy danh sách việc và thông báo được ghi là **đề xuất** trong PDF. Đợt Auth nghiệm thu phần đăng nhập và quyền trên một API Operation đã map. |

**Hiện trạng repo.** [Program.cs](src/PetSpa.Api/Program.cs) đăng ký JWT/policy từ API và map hai route login qua Customer và Operation. Project Auth đã có code, `AuthDbContext` và migration `InitialAuth`, nhưng chưa nằm trong [solution](PetSpa.slnx) hay danh sách project reference của host. Mật khẩu vẫn được ghi trong hồ sơ Customer/Staff. Bộ policy trong project Auth cũ cũng thiếu các policy đang được Booking và Resource sử dụng. Chỉ thay URL hoặc gọi thêm `AddAuthModule` sẽ để lại nhiều đường xác thực.

**Mốc build trước refactor.** Tại commit `2531be7`, lệnh `dotnet build PetSpa.slnx --no-restore -m:1` gặp 15 lỗi biên dịch và 3 cảnh báo. Dấu ngoặc đóng sớm trong `UpdatePetAsync` làm câu `return` ở [PetEndpoints.cs](src/PetSpa.Modules/Customer/Endpoints/PetEndpoints.cs#L118) nằm ngoài phương thức. Bước đầu tiên sau khi duyệt là sửa lỗi chặn này, build lại và ghi nhận các lỗi tiếp theo nếu xuất hiện.

## 2. Hợp đồng API đề xuất

| Endpoint | Dữ liệu và kết quả |
| --- | --- |
| `POST /api/auth/register` · công khai | Nhận `{ username, fullName, phone, password }`. Server tạo hồ sơ ACTIVE và tài khoản khách trong một giao dịch. Trả `201` với `{ id, username, fullName, phone }`, trong đó `id` là mã khách. Từ chối trường cấp quyền như `role`, `staffId`, `status`. Dữ liệu sai → `400`, tên hoặc số đã dùng → `409`, quá giới hạn → `429`. |
| `POST /api/auth/login` · công khai | Nhận `{ identifier, password }`. Ví dụ `identifier` là `minhanh` hoặc `0912345678`. Cả hai màn hình gửi cùng body, server xác định loại tài khoản và role. Trả `200` với `accessToken, tokenType, expiresAtUtc, role, user { id, fullName, phone }` như hợp đồng WPF hiện tại. Body sai → `400`, thông tin sai, tài khoản khóa hoặc hồ sơ INACTIVE → cùng `401`, quá giới hạn → `429`. |
| `PUT /api/admin/accounts/{username}/role` · chỉ admin | Tìm tài khoản theo username chuẩn hóa. Nhận ví dụ `{ "role": "receptionist" }`, trả `204` khi thành công, kể cả khi vai trò đã đúng. Giá trị ngoài danh sách vai trò nhân sự → `400`, thiếu/sai JWT → `401`, người gọi thiếu quyền → `403`, tài khoản đích không tồn tại → `404`, xung đột với loại hồ sơ hoặc quy tắc bảo vệ admin → `409`. |
| `POST /api/customers` · lễ tân, quản lý | Giữ thao tác tạo tại quầy, body hiện hành và các mã `201/409/422`. Nếu quầy chỉ nhập tên, điện thoại và mật khẩu như diagram, server cấp username bằng quy tắc dành cho hồ sơ cũ ở mục 3. Khách dùng ngay số điện thoại để đăng nhập. |

JWT giữ `sub=customer:<id>` hoặc `staff:<id>`, `role`, `account_type` và đúng một trong `customer_id` hoặc `staff_id`. `user.id` là ID nghiệp vụ để client tiếp tục gọi đúng hồ sơ. Bổ sung claim phiên bản thông tin xác thực để thu hồi JWT khi cần. Cập nhật WPF ở repo client và chuyển hai route login cũ sang route chung trong cùng đợt phát hành. Ghi rõ thay đổi này trong hướng dẫn tích hợp và yêu cầu người dùng đăng nhập lại sau chuyển đổi.

`user.phone` tiếp tục trả số liên hệ từ hồ sơ, kể cả khi tài khoản cũ chỉ đăng nhập được bằng username. WPF dùng `role` để chọn màn hình. Server kiểm quyền độc lập ở mỗi request.

### Luồng admin trao quyền

1. Admin chọn username của tài khoản nhân sự rồi gán **một** vai trò trong `staff`, `receptionist`, `manager`, `admin`. Ví dụ, đổi từ `staff` sang `receptionist` để cấp quyền tại quầy, gán lại `staff` để thu hồi. Máy chủ kiểm tra policy mới `AdminOnly` trước khi tra cứu tài khoản đích. Khách giữ vai trò `customer`; người vừa là khách vừa làm việc tại spa dùng tài khoản nhân sự riêng gắn với hồ sơ Staff.
2. Module Auth điều phối thay đổi qua contract của Operation. Trong một giao dịch PostgreSQL, kiểm tra lại người gọi vẫn là admin ACTIVE, cập nhật `operation.staff.role` và tăng `accounts.credential_version` của tài khoản đích. Nếu một bước lỗi, rollback cả hai. Gửi lại đúng vai trò hiện tại trả `204` và giữ phiên bản hiện tại. Sau thay đổi thực sự, JWT cũ nhận `401` từ request kế tiếp; đăng nhập lại bằng cùng tài khoản sẽ nhận đúng quyền mới. Ghi log người thao tác, đối tượng, vai trò cũ/mới và thời điểm sau khi commit.
3. Nhân sự mới mặc định là `staff`. Bỏ quyền ghi trực tiếp `Role` khỏi DTO tạo/sửa hồ sơ nhân viên và chặn trường này ở các API hồ sơ để việc đổi vai trò luôn qua policy admin. Kiểm tra cả quyền sửa mật khẩu/trạng thái của hồ sơ admin ở các đường quản lý nhân viên. Chặn tự hạ quyền và bảo vệ admin ACTIVE cuối cùng, kể cả qua luồng vô hiệu hóa. Các thao tác ảnh hưởng đến nhóm admin dùng chung khóa giao dịch PostgreSQL và kiểm tra lại sau khi lấy khóa để hai yêu cầu đồng thời không làm mất toàn bộ quyền quản trị.
4. Khi triển khai, đối soát và giữ admin ACTIVE hiện có. Với DB mới, hướng dẫn vận hành cấp admin đầu tiên một lần cho tài khoản nhân sự đích đã xác minh, bằng quyền triển khai và có ghi nhận. Các lần cấp sau dùng API trên. Đây là luồng quản trị bổ sung theo yêu cầu, liên quan tới nghiệp vụ quản lý nhân sự OPS-02.

## 3. Dữ liệu và bảo mật

**Một bảng nghiệp vụ mới `petspa_auth.accounts`** trong schema đã có trong mã nguồn. EF tiếp tục quản lý bảng lịch sử migration. Khách có role cố định `customer`. Với nhân viên, Operation sở hữu `Staff.Role`. Trạng thái ACTIVE và họ tên lấy từ hồ sơ tương ứng. Auth sở hữu tên đăng nhập, số đăng nhập, hash mật khẩu và trạng thái khóa tạm thời.

| Bảng trong [migration InitialAuth](src/PetSpa.Modules/Auth/Infrastructure/Persistence/Migrations/20261118020243_InitialAuth.cs) | Xử lý khi thu gọn schema |
| --- | --- |
| `AspNetUsers` | Đối soát với hồ sơ Customer/Staff theo ID liên kết, chuyển credential, trạng thái bảo mật và thời điểm đăng nhập phù hợp sang `accounts`. Giữ hiệu lực của trạng thái khóa hoặc vô hiệu hóa từ nguồn cũ. |
| `AspNetRoles`, `AspNetUserRoles` | Đối chiếu role với `operation.staff.role` và vai trò cố định của khách. Ghi nhận mọi vai trò chưa ánh xạ hoặc liên kết mâu thuẫn để xử lý trước khi gỡ bảng. |
| `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `refresh_tokens` | Kiểm tra dữ liệu và nơi sử dụng thực tế. Nếu có quyền tùy chỉnh hoặc tài khoản chỉ đăng nhập ngoài thì phải đối soát quyền và bảo đảm chủ tài khoản đăng nhập được bằng phương thức mới trước khi chuyển. Thu hồi phiên cũ khi chuyển host. |

Sau đối soát, migration dọn dẹp gỡ tám bảng trên theo thứ tự khóa ngoại. Schema đích gồm **`accounts` và `__EFMigrationsHistory`**. Chuyển `AuthDbContext` sang `DbContext` thường, dùng `PasswordHasher<T>` độc lập và bỏ các đăng ký Identity/refresh không còn consumer. Giữ lịch sử migration đã phát hành. Áp dụng cả chuỗi migration lên DB trống và nâng cấp từ DB đã có `InitialAuth` phải ra cùng schema đích.

| Cột | Ràng buộc và mục đích |
| --- | --- |
| `id`, `customer_id`, `staff_id` | Khóa chính và hai FK tới hồ sơ trong cùng PostgreSQL. CHECK buộc đúng một FK có giá trị. UNIQUE trên từng FK bảo đảm một hồ sơ có tối đa một tài khoản. |
| `username`, `login_phone` | Lưu dạng chuẩn, unique riêng trên từng cột. Username 3–40 ký tự, bắt đầu bằng chữ ASCII, dùng chữ thường, số, dấu chấm, gạch ngang hoặc gạch dưới. Số di động Việt Nam dùng lại quy tắc chuẩn hóa `+84`/đầu `0` của Customer. Hai loại định danh phân biệt được ngay từ cú pháp. |
| `password_hash`, `failed_attempts`, `locked_until_utc`, `last_login_at_utc`, `credential_version` | Dùng `PasswordHasher<T>` sẵn có, hỗ trợ xác minh hash cũ và nâng cấp khi đăng nhập thành công. Phiên bản tăng khi đổi mật khẩu hoặc vai trò. Các mốc thời gian dùng UTC. |

- **Dữ liệu cũ.** Sinh username ổn định như `kh_123` hoặc `nv_45` từ loại hồ sơ và ID, dành riêng hai tiền tố này. Kiểm kê số trùng sau chuẩn hóa trong và giữa hai bảng, số ngoài định dạng mới, hash thiếu hoặc hồ sơ trùng liên kết. Xuất danh sách theo ID để đối soát. Một số chỉ làm định danh đăng nhập cho một tài khoản đã đối soát. Hồ sơ thứ hai có thể giữ số liên hệ và dùng username với `login_phone=NULL`. Không tự gộp tài khoản, tự chọn chủ số hoặc tự cấp lại mật khẩu. Số đăng nhập và số liên hệ được mô tả riêng trong DTO/tài liệu. Bản ghi có số cũ chưa phù hợp cần có cách đăng nhập bằng username đã bàn giao trước khi chuyển hệ thống.
- **Bảo vệ đăng nhập.** Mật khẩu mới đề xuất 12–128 ký tự, cho phép khoảng trắng, có giới hạn kích thước request. Chính sách độ dài mới áp dụng khi tạo hoặc đổi mật khẩu. Dùng hash giả khi không tìm thấy tài khoản. Sau 5 lần sai liên tiếp, khóa đăng nhập 15 phút. Username và số điện thoại cùng dùng bộ đếm của tài khoản. Dùng cập nhật nguyên tử khi tăng bộ đếm, ghi nhận thành công hoặc nâng cấp hash. Giới hạn request theo IP trên login/register, cấu hình được để test, trả `429` và `Retry-After`.
- **Bảo vệ API.** JWT HS256, TTL đề xuất 15 phút, xác minh chữ ký, issuer, audience, lifetime và cấu trúc claim. Signing key lấy từ user-secrets hoặc biến môi trường. Các API dùng HTTPS và tránh ghi password, hash hay JWT vào log. Khi xác thực Bearer, đối chiếu phiên bản, role và trạng thái hồ sơ hiện tại, để khóa hồ sơ hoặc hạ quyền có hiệu lực từ request kế tiếp. Thiếu/sai JWT trả `401`; tài khoản hợp lệ gọi ngoài quyền trả `403`.
- **Một nơi ghi mật khẩu.** Luồng tạo khách tại quầy, đăng ký công khai và tạo hồ sơ nhân sự đều giao thao tác credential cho Auth. Rà cả hai đường cập nhật hồ sơ đang nhận `password`. Bảo toàn thao tác đổi đã được phép, kiểm quyền trước khi ghi và tăng phiên bản cùng giao dịch. Cập nhật điện thoại liên hệ qua hồ sơ không được âm thầm đổi định danh đăng nhập. Kiểm tra việc cấp vai trò nhân sự để người dùng hoặc quản lý không thể tự nâng quyền lên admin.

**Các quyền dễ bị thay đổi khi gộp Auth.** `FrontDesk` và `CustomerManagement` đang áp dụng cho lễ tân và quản lý. `ResourceManager` chỉ nhận quản lý. `ManagerOrAdmin` nhận quản lý và admin. Ví dụ, admin có thể quản lý ca theo quyền Operation nhưng gọi thao tác tạo khách tại quầy phải nhận `403`. Đối chiếu từng endpoint với policy và kiểm tra trong service, nhất là các handler Operation đang dùng lỗi dữ liệu cho trường hợp thiếu quyền.

## 4. Các bước triển khai sau khi duyệt

| Bước | Phạm vi file và kết quả bàn giao |
| --- | --- |
| 1. Khôi phục build và ghi kết quả ban đầu | Sửa lỗi biên dịch chặn ở Customer. Build solution, chạy các executable smoke hiện có. Chốt các đường dẫn thực sự được map, các policy và hành vi client từ code, OpenAPI và diagram. Lưu lỗi có từ trước riêng trong báo cáo. |
| 2. Tạo schema và chuyển dữ liệu | Cập nhật `AuthDbContext`, entity, cấu hình, EF migrations và snapshot của Auth/Customer/Operation theo thay đổi. Đọc migration history thực tế của từng DB, gồm trường hợp đã chạy `InitialAuth`. Dùng migration nối tiếp, kiểm tra bảng Identity hiện hữu có dữ liệu và đối soát trước khi hợp nhất. Thứ tự là migrate hồ sơ, thêm bảng Auth, dừng ngắn các thao tác ghi, backfill hash/ID/thời điểm cũ, kiểm số lượng và các dòng ngoại lệ, rồi chuyển host. Giữ bản sao lưu nhất quán, diễn tập khôi phục trên DB thử. Chỉ bỏ cột mật khẩu, lần login và bảng cũ sau khi đã đối soát và hết thời gian quay lại. Nếu đã phát sinh dữ liệu mới, phải đồng bộ lại trước khi quay về bản cũ. |
| 3. Nối Auth vào ứng dụng | Thêm project vào `PetSpa.slnx` và `PetSpa.Api.csproj`. Ở `Program.cs` đăng ký và map Auth một lần, cấu hình rate limiter trước khi chạy endpoint. Trong `src/PetSpa.Modules/Auth` dùng tối thiểu endpoint, service xác thực, hasher/JWT và EF context. Xóa implementation, route và DI trùng trong `PetSpa.Api/Authentication`, `Composition`, `Customer`, `Operation` và `SharedKernel` khi đã thay mọi nơi sử dụng. SharedKernel giữ role, claim, policy và hợp đồng dùng chung. |
| 4. Tích hợp hồ sơ và quyền | Sửa DTO, validator, service, repository và module registration ở Customer/Operation. Dùng contract để lấy ID, role và trạng thái hồ sơ. Phần điều phối tạo tài khoản dùng cùng `DbConnection` và transaction PostgreSQL cho hai DbContext. Cả hai cùng commit hoặc rollback, kể cả khi hai yêu cầu đăng ký đến đồng thời. Kiểm tra các policy đang dùng bởi Customer/Pet, Booking, Resource và Operation. Đặt quyền ở endpoint và giữ kiểm tra chủ sở hữu ở nghiệp vụ. |
| 4a. Trao quyền quản trị | Bổ sung endpoint admin ở Auth, `AdminOnly` trong SharedKernel và đăng ký policy ở Auth. Sửa `CreateStaffRequest`, `UpdateStaffRequest`, `StaffRepository` và contract Operation để tập trung đường ghi role. Cập nhật OpenAPI, ví dụ `.http`, sơ đồ cấp/thu hồi quyền, ma trận vai trò và hướng dẫn tạo admin đầu tiên. ERD ghi rõ hai bảng còn lại trong schema Auth và nơi lưu vai trò nhân sự. |
| 5. Đồng bộ hợp đồng và tài liệu | Sửa `openapi.yaml`, transformer OpenAPI, `PetSpa.Api.http`, `appsettings.example.json`, `README.md`, `HUONG-DAN-CHAY.md`, `tests/README.md`, hướng dẫn Auth cũ và tài liệu bàn giao Customer/Resource/Booking. Cập nhật hai bản `SO-DO-TRINH-TU*.md`, ERD/schema và bản diagram phát hành có route mới. Giữ `docs/Base/Diagram.pdf` làm mốc thiết kế, liên kết rõ bản thay thế. Ghi ví dụ cho cả username lẫn số điện thoại, cách bàn giao username cũ, thứ tự migration và cách đổi URL/body ở WPF. Kiểm tra file mới cần bàn giao có được Git theo dõi vì repo đang ignore một phần `docs/` và `tests/`. |
| 6. Nghiệm thu và báo cáo | Sửa các test đang compile-link source login cũ để chúng gọi route và middleware thật của host. Chạy build, smoke và integration test PostgreSQL. Đối chiếu cả OpenAPI sinh khi chạy với YAML và ví dụ HTTP. Xuất báo cáo theo mục 5. |

## 5. Ma trận kiểm thử và điều kiện bàn giao

| Luồng | Bằng chứng phải thu được sau triển khai |
| --- | --- |
| Đăng ký công khai | `201`, đúng một account và một hồ sơ ACTIVE. Thử lại với tên khác hoa/thường hoặc số dạng `+84`: `409`. Hai request đồng thời chỉ một request tạo được dữ liệu. Nếu ghi Auth lỗi thì hồ sơ cũng rollback. Gửi role/staffId bị từ chối. |
| AUTH-01 và CUS-01/02 | Đăng nhập cùng một khách bằng username rồi bằng số. Cả hai trả cùng `user.id`, JWT đọc được hồ sơ và pet của mình kể cả `200 []`. Dùng token đó đọc khách, pet hoặc booking của người khác nhận `403`. |
| CUS-03 và CUS-06 | Lễ tân và quản lý dùng route chung, mở danh sách, tạo khách tại quầy rồi đăng nhập bằng tài khoản vừa tạo. Khách và nhân viên thực hiện dịch vụ gọi chức năng tạo khách bị từ chối. |
| OPS-21 và quyền theo role | Dùng fixture cho đủ `customer/staff/receptionist/manager/admin`. Nhân viên có thể ghi nhận vào ca được phân công, bị chặn ở ca khác. Kiểm tra chỉ quản lý được ghi Resource, chỉ quầy dùng các thao tác Booking của quầy, quyền admin theo từng policy. Quyền với dữ liệu vẫn được kiểm tra khi có JWT hợp lệ. |
| Admin cấp và thu hồi quyền | Admin cấp `receptionist` cho `staff` rồi thu hồi về `staff`. Mỗi lần đổi, JWT cũ nhận `401`. Sau khi đăng nhập lại, cùng yêu cầu `GET /api/customers` trên fixture có khách ACTIVE lần lượt trả `200` và `403`. Gửi lại cùng role giữ JWT hiện hành hợp lệ. Lỗi giữa cập nhật role và phiên bản phải rollback được cả hai. |
| Giới hạn quản trị | Thiếu token → `401`; khách, staff, lễ tân và manager có token hợp lệ → `403`. Kiểm tra role lạ, username không tồn tại, gán role nhân sự cho khách, tự hạ admin và hai admin đồng thời hạ nhau. Thử gửi `role` qua đăng ký/tạo/sửa hồ sơ. Đối chiếu role, phiên bản và số admin ACTIVE sau mỗi trường hợp. |
| Schema tối giản | Dùng danh mục bảng PostgreSQL để xác nhận schema `petspa_auth` có đúng `accounts` và `__EFMigrationsHistory` sau migration dọn dẹp trên cả DB trống lẫn DB nâng cấp. Đối chiếu số tài khoản, role, liên kết hồ sơ, trạng thái khóa và thử đăng nhập dữ liệu cũ. Dữ liệu bất nhất phải được báo trước khi gỡ bảng nguồn. |
| Sai thông tin, khóa và JWT | Sai mật khẩu, ID lạ và hồ sơ INACTIVE trả cùng `401`. Test khóa, hết khóa và rate limit độc lập. Token hết hạn, sai chữ ký hoặc claim mâu thuẫn nhận `401`; thiếu quyền nhận `403`. Vô hiệu hóa hồ sơ, đổi mật khẩu hoặc hạ role rồi dùng lại JWT cũ. |
| Nâng cấp dữ liệu và bàn giao client | Chạy migration trên DB rỗng và DB có hồ sơ cũ, cả biến thể đã có `InitialAuth`. Kiểm tra đăng nhập bằng mật khẩu cũ, giữ ID/ownership, nâng cấp hash, chạy lại backfill an toàn, xử lý số trùng và tình huống rollback. Gọi hai route cũ sau chuyển đổi, xác nhận chúng đã bị gỡ. Kiểm tra hợp đồng mới bằng `.http`, Swagger và client WPF khi có bản tích hợp. |

Chạy từ gốc repo trên PostgreSQL dành riêng cho test, với dữ liệu giả cho cả năm vai trò. Các project test hiện có là executable nên cần chạy bằng `dotnet run`.

```powershell
dotnet build PetSpa.slnx
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
dotnet run --project tests/PetSpa.OperationSmokeTests/PetSpa.OperationSmokeTests.csproj
# Đặt PETSPA_BOOKING_TEST_CONNECTION trỏ tới DB thử trước khi chạy.
dotnet run --project tests/PetSpa.BookingProviderTests/PetSpa.BookingProviderTests.csproj
```

Bổ sung các ca transaction, migration và đăng nhập qua API thật vào bộ integration, dùng chuỗi kết nối DB thử riêng cho Auth. Báo cáo bàn giao ghi **mã use case · actor · dữ liệu đầu vào · kết quả mong đợi/thực tế · mã HTTP hoặc kiểm tra DB · đạt/lỗi/chưa chạy** kèm commit, lệnh và môi trường. Chỉ ghi hoàn thành phần có bằng chứng chạy. Với OPS-21, kết quả đăng nhập, phần danh sách việc/thông báo và kiểm tra màn hình WPF có trạng thái riêng. Tại thời điểm lập kế hoạch mới có kết quả build ban đầu ở mục 1.
