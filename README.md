# PetSpa Management API

Backend quản lý spa thú cưng, xây dựng bằng ASP.NET Core Minimal APIs, .NET 10, EF Core và PostgreSQL. API phục vụ ứng dụng WPF nằm ở repo riêng.

## Các module

| Module | Phạm vi | Schema PostgreSQL |
| --- | --- | --- |
| Auth | Đăng nhập bằng số điện thoại, JWT, kích hoạt tài khoản khách và quản trị tài khoản nhân viên | `petspa_auth` |
| Customer | Hồ sơ khách hàng, thú cưng và ghi chú chăm sóc | `customer` |
| Resource | Danh mục, dịch vụ và tùy chọn dịch vụ | `resource` |
| Booking | Tạo, tra cứu, cập nhật và theo dõi trạng thái lịch hẹn | `booking` |
| Operation | Phòng, nhân viên, ca làm, phiên dịch vụ, phân công và thông báo | `operation` |
| Billing | Mô hình hóa đơn, thanh toán và tra cứu hóa đơn nội bộ. API hóa đơn, thanh toán đang ở giai đoạn thiết kế | `billing` |

## Kiến trúc

PetSpa là một monolith phân module. `PetSpa.Api` khởi tạo ứng dụng, đăng ký module và cấu hình middleware. Mỗi module có project và `DbContext` riêng. Các module gọi nhau qua contract nội bộ. `PetSpa.SharedKernel` chứa các kiểu và contract dùng chung, gồm kết quả thao tác và định danh tài khoản.

```text
PetSpa.slnx
openapi.yaml
src/
├── PetSpa.Api/                 # Host API, Swagger và cấu hình
├── PetSpa.Modules/
│   ├── Auth/
│   ├── Billing/
│   ├── Booking/
│   ├── Customer/
│   ├── Operation/
│   └── Resource/
└── PetSpa.SharedKernel/
tests/
├── PetSpa.AuthenticationSmokeTests/
├── PetSpa.BookingProviderTests/
└── PetSpa.OperationSmokeTests/
docs/
├── api/
├── architecture/
├── guides/
└── plans/
```

Trong từng module, `Endpoints` chứa route và kiểu dữ liệu HTTP. `Application` chứa xử lý nghiệp vụ và các giao diện mà ca sử dụng cần. `Domain` chứa thực thể và kiểu nghiệp vụ. `Contracts` chứa giao diện và DTO dành cho module khác. `Infrastructure/Persistence` chứa EF Core, migration và repository. Tệp `<TênModule>Module.cs` là điểm đăng ký thành phần của module.

## Chạy cục bộ

Cài .NET 10 SDK. Để gọi API đọc hoặc ghi dữ liệu, cần thêm một database PostgreSQL cục bộ hoặc trên Supabase. Chạy các lệnh PowerShell từ thư mục gốc của repo.

1. Tạo tệp cấu hình nếu chưa có.

   ```powershell
   if (-not (Test-Path src/PetSpa.Api/appsettings.json)) {
       Copy-Item src/PetSpa.Api/appsettings.example.json src/PetSpa.Api/appsettings.json
   }
   ```

2. Điền `ConnectionStrings:DefaultConnection` và `Jwt:SigningKey` (tối thiểu 32 byte) trong `src/PetSpa.Api/appsettings.json`. Biến môi trường của tiến trình có thể ghi đè giá trị trong tệp, ví dụ `Jwt__SigningKey`.
3. [Áp dụng migration](#khởi-tạo-cơ-sở-dữ-liệu) nếu cần sử dụng các API truy cập dữ liệu. `GET /health` có thể chạy trước bước này, miễn là ứng dụng đã có cấu hình kết nối và JWT hợp lệ.
4. Khởi động ứng dụng.

   ```powershell
   dotnet run --project src/PetSpa.Api/PetSpa.Api.csproj --launch-profile http
   ```

5. Từ một cửa sổ PowerShell khác, kiểm tra API trả về `200 OK` và `{"status":"ok"}`.

   ```powershell
   curl.exe -i http://localhost:5052/health
   ```

Ở môi trường Development, mở Swagger UI tại `http://localhost:5052/swagger` hoặc lấy JSON OpenAPI tại `http://localhost:5052/openapi/v1.json`. WPF dùng `http://localhost:5052` làm URL gốc khi chạy cùng máy theo profile HTTP. Có thể thử các request bằng [PetSpa.Api.http](src/PetSpa.Api/PetSpa.Api.http).

## Khởi tạo cơ sở dữ liệu

Bộ migration tạo sáu schema trong database mới và quản lý lịch sử riêng cho từng module. Database đã dùng bộ migration hiện hành có thể áp dụng tiếp các migration đang chờ. Với database dùng lịch sử từ trước khi gộp migration, xem [hướng dẫn Auth](docs/guides/auth.md#khởi-tạo-cơ-sở-dữ-liệu) để lên phương án chuyển dữ liệu.

Nếu chưa có EF CLI, cài bằng `dotnet tool install --global dotnet-ef --version 10.0.13`. Chạy các lệnh sau với tài khoản PostgreSQL có quyền tạo schema và bảng. Booking chạy trước Operation vì migration Operation sử dụng bảng của Booking.

```powershell
$api = 'src/PetSpa.Api/PetSpa.Api.csproj'
dotnet build $api
if ($LASTEXITCODE -ne 0) { throw 'Build API thất bại.' }

foreach ($module in 'Auth', 'Billing', 'Booking', 'Customer', 'Operation', 'Resource') {
    dotnet ef database update --no-build --project "src/PetSpa.Modules/$module/PetSpa.Modules.$module.csproj" --startup-project $api --context "${module}DbContext"
    if ($LASTEXITCODE -ne 0) { throw "Migration $module thất bại." }
}
```

Sau khi tạo schema, thiết lập tài khoản quản trị đầu tiên theo [hướng dẫn Auth](docs/guides/auth.md#khởi-tạo-cơ-sở-dữ-liệu). Các tài khoản còn lại dùng luồng cấp tài khoản nhân viên hoặc mã kích hoạt cho khách.

## Kiểm thử

Các bộ kiểm thử chạy bằng `dotnet run` với môi trường mặc định `Production`. Operation dùng kho dữ liệu mô phỏng. Hai bộ còn lại cần database PostgreSQL thử nghiệm trống, riêng cho mỗi lần chạy.

```powershell
dotnet build PetSpa.slnx
dotnet run --project tests/PetSpa.OperationSmokeTests

$env:PETSPA_AUTH_TEST_CONNECTION = '<chuỗi kết nối DB thử Auth>'
dotnet run --project tests/PetSpa.AuthenticationSmokeTests

$env:PETSPA_BOOKING_TEST_CONNECTION = '<chuỗi kết nối DB thử Booking>'
dotnet run --project tests/PetSpa.BookingProviderTests
```

Xem phạm vi của từng bộ trong [hướng dẫn kiểm thử](tests/README.md).

## Tài liệu

- [Hợp đồng OpenAPI](openapi.yaml) và [đối chiếu giữa API và sơ đồ](docs/api/api-alignment.md)
- [Xác thực, phân quyền và tích hợp WPF](docs/guides/auth.md)
- [Bàn giao Customer, Resource và Booking](docs/api/customer-resource-booking.md)
- [Hướng dẫn chuyển đổi đường dẫn API cho client](docs/api/api-transition.md)
- [Sơ đồ trình tự nghiệp vụ](docs/architecture/sequence-diagrams.md)
- [Các thay đổi API dự kiến](docs/plans/api-changes.md)
