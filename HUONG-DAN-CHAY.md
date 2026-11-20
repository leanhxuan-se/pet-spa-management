# Chạy server PetSpa

Cần .NET 10 SDK. Mở PowerShell tại thư mục `pet-spa-management`.

1. Nếu chưa có `appsettings.json`, tạo từ file mẫu.

   ```powershell
   Copy-Item src/PetSpa.Api/appsettings.example.json src/PetSpa.Api/appsettings.json
   ```

2. Trong `src/PetSpa.Api/appsettings.json`, điền chuỗi kết nối PostgreSQL vào `ConnectionStrings.DefaultConnection`. Phần `Jwt` cần có `Issuer`, `Audience`, `AccessTokenMinutes` lớn hơn 0 và `SigningKey` dài ít nhất 32 byte. File mẫu có sẵn các giá trị để tham khảo, hãy đặt khóa riêng cho `SigningKey`. Nếu file đã tồn tại từ trước và thiếu phần `Jwt`, thêm phần đó theo file mẫu mà giữ lại chuỗi kết nối đang dùng. Khóa cũng có thể được đặt bằng .NET User Secrets.

   Khi chỉ kiểm tra `/health`, PostgreSQL có thể chưa chạy và chưa cần chạy migration. Các API đọc hoặc ghi dữ liệu cần PostgreSQL đang chạy và cơ sở dữ liệu đã áp dụng migration tương ứng.

3. Chạy server bằng profile HTTP cục bộ và giữ cửa sổ PowerShell này mở.

   ```powershell
   dotnet run --project src/PetSpa.Api/PetSpa.Api.csproj --launch-profile http
   ```

4. Từ một cửa sổ PowerShell khác, kiểm tra API trả về HTTP `200` và `{"status":"ok"}`.

   ```powershell
   curl.exe -i http://localhost:5052/health
   ```

Sau đó mở [hướng dẫn chạy WPF client](../Animal-Spa/HUONG-DAN-CHAY.md) để kiểm tra kết nối trên cùng máy.

## Chuyển dữ liệu sang Auth dùng chung

Áp dụng trên cơ sở dữ liệu thử trước. Khi chuyển môi trường đang phục vụ, sao lưu đồng bộ PostgreSQL và dừng các luồng ghi hồ sơ và thông tin xác thực cho tới khi bàn giao. Các lệnh dùng cấu hình PostgreSQL của host PetSpa. Kiểm tra kết nối đích trước khi chạy.

```powershell
$api = 'src/PetSpa.Api/PetSpa.Api.csproj'
$modules = 'src/PetSpa.Modules'
dotnet ef database update 20261119035358_AlignCustomerProfile --project "$modules/Customer/PetSpa.Modules.Customer.csproj" --startup-project $api --context CustomerDbContext
dotnet ef database update 20261018114058_AddAdminStaffRole --project "$modules/Operation/PetSpa.Modules.Operation.csproj" --startup-project $api --context OperationDbContext
dotnet ef database update --project "$modules/Auth/PetSpa.Modules.Auth.csproj" --startup-project $api --context AuthDbContext
dotnet ef database update --project "$modules/Customer/PetSpa.Modules.Customer.csproj" --startup-project $api --context CustomerDbContext
dotnet ef database update --project "$modules/Operation/PetSpa.Modules.Operation.csproj" --startup-project $api --context OperationDbContext
```

Hai lệnh có mốc cụ thể ở đầu dành cho lần chuyển đổi đầu tiên. Dừng nếu có lệnh báo lỗi. Auth ưu tiên tài khoản Identity đã liên kết hợp lệ trên cùng hồ sơ, sau đó nhập hash từ hồ sơ còn lại. Migration sẽ rollback khi số đăng nhập trùng sau chuẩn hóa, liên kết bị thiếu hoặc gặp dữ liệu Identity cần đối soát. Xác minh chủ sở hữu của từng số và chỉnh dữ liệu trước khi chạy lại. Dữ liệu cũ còn lại có thể khôi phục từ bản sao lưu trước chuyển đổi. Hai migration cuối kiểm tra liên kết account trước khi bỏ các cột cũ. Với DB trống, áp dụng cả migration của Resource, Booking và Billing để dùng các module tương ứng.

Khách đăng ký qua mã quầy cấp bằng `POST /api/auth/customers/{customerId}/activation`. Gửi mã sau khi nhân viên đã xác minh khách. Mã có hiệu lực 15 phút. Giữ key ring Data Protection qua các lần khởi động và dùng chung giữa các instance phục vụ luồng cấp mã và đăng ký.

Nhân viên được tạo hồ sơ qua `POST /api/staff`, sau đó manager hoặc admin gọi `POST /api/auth/staff-accounts` với ID hồ sơ đã nhận. Với DB trống, bước thiết lập ban đầu cần một hồ sơ `ADMIN` hoạt động và một dòng `petspa_auth.accounts` trỏ tới hồ sơ đó. Người vận hành tạo hash từ mật khẩu ban đầu bằng `PasswordHasher<T>` của ASP.NET Core trong quy trình thiết lập có quyền, sau đó ghi hash cùng `staff_id` và `login_phone` qua câu lệnh SQL tham số hóa. Các tài khoản sau đó dùng luồng quản trị có xác thực.

Sau khi chuyển, hai màn hình đăng nhập gọi `POST /api/auth/login` với `phone` và `password`. Yêu cầu người dùng đăng nhập lại vì JWT đã cấp trước chuyển đổi thiếu định danh và phiên bản account. Xem [ví dụ HTTP](src/PetSpa.Api/PetSpa.Api.http) và [hướng dẫn Auth](HUONG-DAN-AUTH.md).
