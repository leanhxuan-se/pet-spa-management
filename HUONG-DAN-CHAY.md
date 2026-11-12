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
