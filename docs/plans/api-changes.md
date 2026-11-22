# Hướng dẫn thay đổi

Tài liệu này dùng để ghi nhận các yêu cầu thay đổi và hướng dẫn thực hiện cho dự án Pet Spa Management. Bổ sung từng mục bên dưới khi có thông tin mới.

## Danh sách thay đổi

### Quy tắc cập nhật API

- **Trạng thái:** Cần làm
- Chuyển các API thực hiện thao tác cập nhật từ phương thức `PUT` sang `PATCH`.
- Phân biệt ID định danh tài nguyên đang thao tác với ID là khóa ngoại: chỉ chuyển ID định danh tài nguyên từ body request lên URL tương ứng. Giữ các ID khóa ngoại trong body khi chúng là dữ liệu liên kết; các đường dẫn lồng nhau được yêu cầu cụ thể bên dưới vẫn sử dụng ID tài nguyên cha trong URL.
- Cập nhật tài liệu API và kiểm tra các luồng bị ảnh hưởng sau khi thay đổi.

### Gộp các thao tác PATCH

- **Trạng thái:** Cần làm
- Gộp cập nhật thông tin và trạng thái vào một API `PATCH` theo từng tài nguyên chính. Các trường gửi trong body quyết định nội dung cần cập nhật; trường không gửi giữ nguyên giá trị.
- Dùng `PATCH /api/bookings/{id}` để cập nhật booking, bao gồm trạng thái và danh sách dịch vụ; gộp các thao tác cập nhật booking hiện có vào API này.
- Dùng `PATCH /api/staff-shifts/{id}` để cập nhật ca làm, bao gồm chuyển trạng thái thay cho các thao tác start và finish.
- Dùng `PATCH /api/service-sessions/{id}` để cập nhật phiên dịch vụ, bao gồm chuyển trạng thái thay cho các thao tác start và complete.
- Gộp cập nhật ghi chú riêng của thú cưng vào `PATCH /api/pets/{id}`.
- Giữ các API PATCH cho tài nguyên con có định danh riêng, như assignment, service option và notification. Giữ kiểm tra quyền, chuyển trạng thái hợp lệ và các nghiệp vụ phụ hiện có khi gộp.

### Chuẩn hóa API category, service và service option

- **Trạng thái:** Cần làm
- **Mục tiêu:** Bổ sung các API dưới đây. Nếu đã có thao tác nghiệp vụ tương ứng thì đổi tên hoặc đường dẫn cho phù hợp; nếu chưa có thì tạo mới.

**Category**

```http
GET    /api/categories
POST   /api/categories
GET    /api/categories/{categoryId}
PATCH  /api/categories/{categoryId}
DELETE /api/categories/{categoryId}
```

**Service** — lồng dưới category để tạo và liệt kê; dùng đường dẫn phẳng để tìm kiếm và thao tác trực tiếp.

```http
GET    /api/categories/{categoryId}/services
POST   /api/categories/{categoryId}/services
GET    /api/services?categoryId=&keyword=
GET    /api/services/{serviceId}
PATCH  /api/services/{serviceId}
DELETE /api/services/{serviceId}
```

**Service option** — lồng dưới service.

```http
GET    /api/services/{serviceId}/options
POST   /api/services/{serviceId}/options
PATCH  /api/services/{serviceId}/options/{optionId}
DELETE /api/services/{serviceId}/options/{optionId}
```

- **Phạm vi ảnh hưởng:** Module Resource và tài liệu API liên quan.
- **Cách kiểm tra kết quả:** Kiểm tra từng phương thức và đường dẫn; xác nhận các thao tác lồng nhau dùng đúng category hoặc service trong đường dẫn và các nghiệp vụ hiện có vẫn hoạt động.

## Mẫu bổ sung thay đổi

Sao chép mẫu sau vào phần **Danh sách thay đổi** và điền những thông tin đã biết; các mục còn lại có thể bổ sung sau.

```markdown
### [Tên thay đổi]

- **Ngày ghi nhận:**
- **Trạng thái:** Cần làm / Đang thực hiện / Hoàn thành
- **Mục tiêu:**
- **Nội dung cần thay đổi:**
- **Phạm vi ảnh hưởng:**
- **Cách kiểm tra kết quả:**
- **Ghi chú:**
```
