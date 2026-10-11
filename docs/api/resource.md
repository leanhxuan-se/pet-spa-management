# Resource API

Module Resource cung cấp CRUD cho Category → Service → ServiceOption.
Repository hiện chỉ có backend. Frontend có thể dùng ba request sau để triển khai danh sách xổ xuống và chọn đối tượng.

| Thao tác trên giao diện | Request |
|---|---|
| Bấm nút Danh mục | `GET /api/resource/categories` |
| Chọn một danh mục | `GET /api/resource/categories/{categoryId}/services` |
| Chọn một dịch vụ | `GET /api/resource/services/{serviceId}/options` |

Khi đổi danh mục, xóa lựa chọn dịch vụ và tùy chọn cũ trước khi tải danh sách mới.
Khi đổi dịch vụ, xóa lựa chọn tùy chọn cũ. Frontend cần bỏ qua response của lựa chọn cũ nếu người dùng đã chuyển sang lựa chọn khác.

## Các endpoint

| Đối tượng | Danh sách | Chi tiết / cập nhật / xóa | Tạo mới |
|---|---|---|---|
| Category | `GET /api/resource/categories` | `GET / PUT / DELETE /api/resource/categories/{id}` | `POST /api/resource/categories` |
| Service | `GET /api/resource/services?categoryId=1&keyword=tắm` | `GET / PUT / DELETE /api/resource/services/{id}` | `POST /api/resource/services` |
| ServiceOption | `GET /api/resource/service-options?serviceId=1` | `GET / PUT / DELETE /api/resource/service-options/{id}` | `POST /api/resource/service-options` |

`categoryId`, `keyword` và `serviceId` là các query parameter tùy chọn.
Đường dẫn lấy con theo cha tương đương với lọc bằng query parameter.
Cha tồn tại nhưng chưa có con trả về `200 []`; cha không tồn tại hoặc đã xóa trả về `404`.

Các endpoint GET có thể đọc công khai.
POST, PUT và DELETE yêu cầu bearer token có claim `account_type=staff`.
Token khách hàng không có quyền ghi.

## Payload

Category — dùng cho POST và PUT:

```json
{
  "name": "Chăm sóc thú cưng",
  "description": "Danh mục dịch vụ spa",
  "status": "ACTIVE"
}
```

Service — dùng cho POST và PUT:

```json
{
  "categoryId": 1,
  "serviceName": "Tắm và sấy",
  "description": "Tắm, vệ sinh và sấy lông",
  "status": "ACTIVE"
}
```

ServiceOption — dùng cho POST và PUT:

```json
{
  "serviceId": 1,
  "species": "DOG",
  "minWeight": 0,
  "maxWeight": 5,
  "durationMinutes": 30,
  "unitPrice": 150000,
  "status": "ACTIVE"
}
```

Response chứa các trường tương ứng và `id`.
Tên dịch vụ là `serviceName`; giá nằm ở `ServiceOption.unitPrice`, thời lượng nằm ở `durationMinutes`.
Giá và cân nặng dùng decimal. Đơn vị cân nặng là kg.
Loài hợp lệ: `DOG`, `CAT`, `CHICKEN`, `FISH`, `OTHER`.
`species` và `unitPrice` bắt buộc có trong JSON tùy chọn dịch vụ.
Tên danh mục tối đa 50 ký tự; tên dịch vụ tối đa 256 ký tự. Tên được trim.
Cân nặng không âm; khi có cả hai cận thì `maxWeight >= minWeight`.
Hai cận có thể null để biểu diễn không giới hạn. Thời lượng phải lớn hơn 0; giá không âm.

`description` có thể null. `status` mặc định `ACTIVE` nếu bỏ qua.
PUT cập nhật toàn bộ các trường có thể sửa: bỏ `description` sẽ đưa nó về null, bỏ `status` sẽ đưa về `ACTIVE`.
Có thể chuyển dịch vụ sang danh mục khác, hoặc chuyển tùy chọn sang dịch vụ khác, nếu cha mới tồn tại và chưa bị xóa.

## Trạng thái và xóa

Ba entity dùng chung enum `ResourceStatus`: `ACTIVE`, `MAINTAINED`, `INACTIVE`, `DELETED`.
API trả enum dưới dạng chuỗi.
POST/PUT chỉ nhận ba trạng thái đầu; dùng DELETE để xóa.
Danh sách trả các đối tượng chưa xóa, bao gồm trạng thái INACTIVE và MAINTAINED để phục vụ quản lý.

DELETE đổi trạng thái thành DELETED và cập nhật `UpdatedAt`, giữ nguyên bản ghi và khóa ngoại.
Xóa danh mục ẩn các dịch vụ và tùy chọn bên dưới; xóa dịch vụ ẩn các tùy chọn bên dưới.
Trạng thái của các con được giữ nguyên, nhưng chúng không thể đọc/sửa qua API khi cha đã xóa.
GET theo id, PUT hoặc DELETE lại đối tượng đã xóa trả về 404.
Tên danh mục vẫn phải duy nhất, kể cả khi danh mục cũ đã xóa, theo unique index hiện tại.

| Mã HTTP | Ý nghĩa |
|---|---|
| 200 | Đọc hoặc cập nhật thành công |
| 201 | Tạo thành công, có Location trỏ tới endpoint chi tiết |
| 204 | Xóa thành công |
| 400 | ValidationProblem với lỗi theo trường, hoặc JSON không hợp lệ |
| 401 | Thiếu xác thực cho thao tác ghi |
| 403 | Tài khoản không phải nhân viên |
| 404 | Đối tượng hoặc cha không tồn tại/đã xóa |
| 409 | Trùng tên danh mục |

## Migration và kiểm thử

Migration `AddResourceStatuses` thêm cột status cho category và service_options.
Các bản ghi cũ được gán ACTIVE trước khi áp dụng NOT NULL.
Cột status hiện có của service được giữ nguyên.

Áp dụng migration vào database đã cấu hình trước khi chạy API:

```powershell
dotnet ef database update --project src/PetSpa.Modules/Resource --startup-project src/PetSpa.Api --context ResourceDbContext
```

Chạy smoke test:

```powershell
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.ResourceSmokeTests.csproj
```

Smoke test chạy HTTP qua Kestrel với endpoint, DI, authorization và application service thật.
Persistence và authentication token được giả lập, không kết nối database và không xác minh JWT.
Test cũng kiểm tra production DI, model/snapshot EF và sinh SQL migration bằng provider PostgreSQL.
