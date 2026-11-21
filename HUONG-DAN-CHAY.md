# Chạy server PetSpa

Cần .NET 10 SDK. Mở PowerShell tại thư mục `pet-spa-management`.

1. Nếu chưa có `src/PetSpa.Api/.env.local`, tạo từ file mẫu.

   ```powershell
   Copy-Item src/PetSpa.Api/.env.example src/PetSpa.Api/.env.local
   ```

2. Trong `src/PetSpa.Api/.env.local`, điền chuỗi kết nối vào `ConnectionStrings__DefaultConnection` và đặt `Jwt__SigningKey` dài ít nhất 32 byte. Dùng `Jwt__Issuer`, `Jwt__Audience` và `Jwt__AccessTokenMinutes` (số nguyên dương) để chỉnh JWT. Mỗi dòng có dạng `NAME=value`, với giá trị được đọc nguyên văn sau dấu `=` đầu tiên. Dòng trống và dòng bắt đầu bằng `#` được bỏ qua. Git bỏ qua `.env.local`.

   API tự đọc file khi khởi động. Tham số dòng lệnh có ưu tiên cao nhất, tiếp theo là biến môi trường của tiến trình, `.env.local` và cấu hình ASP.NET Core mặc định như .NET User Secrets hoặc `appsettings.json`.

   Khi chỉ kiểm tra `/health`, PostgreSQL có thể chưa chạy và chưa cần chạy migration. Các API đọc hoặc ghi dữ liệu cần PostgreSQL đang chạy và cơ sở dữ liệu đã áp dụng migration tương ứng.

3. Chạy server bằng profile HTTP cục bộ và giữ cửa sổ PowerShell này mở.

   ```powershell
   dotnet run --project src/PetSpa.Api/PetSpa.Api.csproj --launch-profile http
   ```

4. Từ một cửa sổ PowerShell khác, kiểm tra API trả về HTTP `200` và `{"status":"ok"}`. Nếu đã đặt `URLS` trong `.env.local`, dùng địa chỉ tương ứng.

   ```powershell
   curl.exe -i http://localhost:5052/health
   ```

Sau đó mở [hướng dẫn chạy WPF client](../Animal-Spa/HUONG-DAN-CHAY.md) để kiểm tra kết nối trên cùng máy.

## Khởi tạo schema trên Supabase

Các migration đã được gộp thành một migration khởi tạo cho mỗi module, phản ánh model hiện tại. Hướng dẫn này dành cho database trống: sáu schema ứng dụng `petspa_auth`, `billing`, `booking`, `customer`, `operation`, `resource` chưa tồn tại hoặc đã được xóa cùng bảng lịch sử EF. Không xóa các schema hệ thống của Supabase. Bộ migration này không chuyển dữ liệu từ database cũ; nếu còn dữ liệu cần giữ, dùng bản sao lưu và lịch sử migration cũ để lập phương án chuyển đổi riêng.

Đảm bảo `ConnectionStrings__DefaultConnection` trong `src/PetSpa.Api/.env.local` hoặc biến môi trường của tiến trình trỏ đúng database Supabase và tài khoản có quyền tạo schema, bảng và chỉ mục. Kiểm tra kết nối đích trước khi chạy. Cài .NET 10 SDK và `dotnet-ef` tương thích phiên bản EF Core của dự án, sau đó chạy PowerShell tại thư mục gốc:

```powershell
$api = 'src/PetSpa.Api/PetSpa.Api.csproj'
$modules = 'src/PetSpa.Modules'
dotnet ef --version
dotnet build $api
if ($LASTEXITCODE -ne 0) { throw 'Build thất bại' }
foreach ($module in @('Auth', 'Billing', 'Booking', 'Customer', 'Operation', 'Resource')) {
    dotnet ef database update --no-build --project "$modules/$module/PetSpa.Modules.$module.csproj" --startup-project $api --context "${module}DbContext"
    if ($LASTEXITCODE -ne 0) { throw "Migration $module thất bại; kiểm tra lỗi trước khi chạy tiếp." }
}
```

Nếu chưa cài công cụ EF, chạy `dotnet tool install --global dotnet-ef --version 10.0.13` (hoặc `dotnet tool update --global dotnet-ef --version 10.0.13` nếu đã cài). Mỗi context lưu lịch sử trong schema riêng. Nếu một lệnh thất bại, sửa nguyên nhân rồi chạy lại vòng lặp; EF bỏ qua migration đã áp dụng thành công. Trong Supabase SQL Editor, kiểm tra sáu schema ứng dụng và các bảng `__EFMigrationsHistory` trong từng schema. Migration chỉ tạo cấu trúc và không khôi phục dữ liệu đã xóa.

Khách đăng ký qua mã quầy cấp bằng `POST /api/auth/customers/{customerId}/activation`. Gửi mã sau khi nhân viên đã xác minh khách. Mã có hiệu lực 15 phút. Giữ key ring Data Protection qua các lần khởi động và dùng chung giữa các instance phục vụ luồng cấp mã và đăng ký.

Nhân viên được tạo hồ sơ qua `POST /api/staff`, sau đó manager hoặc admin gọi `POST /api/auth/staff-accounts` với ID hồ sơ đã nhận. Với DB trống, bước thiết lập ban đầu cần một hồ sơ `ADMIN` hoạt động và một dòng `petspa_auth.accounts` trỏ tới hồ sơ đó. Người vận hành tạo hash từ mật khẩu ban đầu bằng `PasswordHasher<T>` của ASP.NET Core trong quy trình thiết lập có quyền, sau đó ghi hash cùng `staff_id` và `login_phone` qua câu lệnh SQL tham số hóa. Các tài khoản sau đó dùng luồng quản trị có xác thực.

WPF gọi `POST /api/auth/login` với `phone` và `password`. [Ví dụ HTTP](src/PetSpa.Api/PetSpa.Api.http) · [Hướng dẫn Auth](HUONG-DAN-AUTH.md)
