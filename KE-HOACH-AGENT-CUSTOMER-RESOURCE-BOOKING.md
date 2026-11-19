# Kế hoạch triển khai Customer, Resource và Booking

**Mục tiêu nghiệm thu** là để lễ tân hoặc quản lý chọn đúng khách, thú cưng và dịch vụ, tạo lịch có giá đã chốt, tiếp nhận khách rồi chốt hoặc hủy lịch theo kết quả bàn giao. Agent phụ trách backend của ba module. Thành viên Operation phụ trách đăng nhập nhân sự, phòng, ca, phân công, phiên dịch vụ và màn hình theo dõi nhóm phiên. Billing phụ trách hóa đơn và thu tiền.

**Đăng nhập trong đợt này** tiếp tục đi qua `POST /api/auth/customer/login` ở Customer và `POST /api/auth/staff/login` ở Operation. Host `PetSpa.Api` cấu hình JWT và phân quyền. Thư mục `src/PetSpa.Modules/Auth` có trong workspace nhưng chưa được host đăng ký. Việc tách Auth là một đợt riêng.

## Nguồn và thứ tự thực hiện

| Tài liệu thiết kế ngày 19/11/2026 | Phần agent cần dùng |
|---|---|
| [UseCase.docx](docs/Base/UseCase.docx) | Mục 2–3 xác định actor, API và phạm vi bắt buộc. Mục 4–5 mô tả contract và nghiệm thu tại quầy. |
| [Diagram.pdf](docs/Base/Diagram.pdf) | Trang 3–18 cho đăng nhập, Customer, Resource và Booking. Trang 29, 31, 37–38, 41, 43 và 45 cho các điểm giao với Operation và Billing. |
| [Quy tắc nghiệp vụ](<docs/Base/03. Quy tắc nghiệp vụ. Quy tắc nghiệp vụ.docx>) | BR-CUS-01–08, BR-RES-01–07, BR-BOOK-01–10. Dùng mục 8–13 để kiểm tra quyền, trạng thái, giao dịch và các chênh lệch đã biết. |
| [ERD](<docs/Base/02. ERD - Sơ đồ thiết kế database..docx>) | Mục 6 và 7.1–7.3 cho quyền sở hữu dữ liệu, khóa, kiểu cột và các trường Booking cần bổ sung. |

1. **Ghi nhận hiện trạng và chốt contract.** Chạy build và smoke test bên dưới để biết lỗi có sẵn. Kiểm tra migration, snapshot và thay đổi chưa commit trên nhánh đang làm. Gửi đặc tả contract Booking cho thành viên Operation ngay từ đầu để hai bên triển khai độc lập.
2. **Hoàn thiện Customer và Resource.** Hai gói này độc lập. Đầu ra là API đã siết quyền và hai contract cung cấp dữ liệu cho Booking.
3. **Hoàn thiện Booking.** Làm schema và xử lý dữ liệu cũ trước. Tiếp theo làm tạo/sửa lịch, bàn giao và chuyển trạng thái, cuối cùng là tìm kiếm và phân trang.
4. **Nghiệm thu và bàn giao.** Kiểm tra ba module qua HTTP và PostgreSQL thử nghiệm. Khi consumer sẵn sàng, chạy kịch bản bàn giao thật với thành viên Operation. Đầu ra cho WPF là hợp đồng API và ví dụ request/response đã kiểm chứng.

## Gói 1 · Customer

Điểm bắt đầu là [PetEndpoints](src/PetSpa.Modules/Customer/Endpoints/PetEndpoints.cs), [CustomerModuleContract](src/PetSpa.Modules/Customer/Infrastructure/Services/CustomerModuleContract.cs) và [PetConfiguration](src/PetSpa.Modules/Customer/Infrastructure/Persistence/Configurations/PetConfiguration.cs). Hiện kiểm tra quyền sở hữu cho phép chủ gọi cả đường ghi pet. Hàm `PetBelongsToCustomerAsync` kiểm tra quan hệ nhưng thiếu trạng thái của cả hai hồ sơ.

| Use case | Đầu việc và điều kiện nghiệm thu |
|---|---|
| CUS-01, CUS-03 | Quầy xem danh sách, tạo khách ACTIVE bằng họ tên, điện thoại và mật khẩu. Khách đọc/sửa thông tin cá nhân của mình. Phân quyền riêng cho trường trạng thái. Chuẩn hóa điện thoại nhất quán giữa tạo, sửa và login. Số hoặc email trùng trả 409 cả khi hai yêu cầu đến đồng thời. Email trống lưu NULL, mật khẩu lưu hash. |
| CUS-02, CUS-04 | Khách chỉ xem pet của mình. Quầy tạo/sửa pet, giữ cố định chủ. Khi tạo, `customersId` trong body phải khớp `customerId` trên URL và chủ phải ACTIVE. Kiểm tra tên, loài, số đo dương và ngày sinh. Khách có hồ sơ nhưng chưa có pet nhận `200 []`. |
| CUS-02/04 và phần Customer của OPS-19 | Kiểm tra sự **xuất hiện** của `specialNote` ở POST, PUT và đường ghi riêng `PUT /api/pets/{id}/special-note` nếu triển khai luồng mở rộng. Chỉ lễ tân được ghi hoặc xóa ghi chú. Quản lý gửi cả tên hợp lệ và `specialNote: null` nhận 403, tên và ghi chú cũ đều còn nguyên. Bỏ trường này khỏi request thì giữ ghi chú cũ. |
| CUS-05 · mở rộng | Dùng PUT chuyển hồ sơ sang INACTIVE theo quyền quầy. Booking mới phải từ chối khách **hoặc** pet INACTIVE ngay cả khi chỉ triển khai các luồng bắt buộc. Lịch cũ vẫn tra cứu được. |
| AUTH-01, CUS-06 | Kiểm tra login khách rồi dùng Bearer đọc hồ sơ và danh sách pet. Tài khoản INACTIVE hoặc sai mật khẩu nhận 401. Ca thành công kiểm tra `last_login_at`, nâng cấp hash khi cần và response gồm `accessToken`, `tokenType`, `expiresAtUtc`, `role`, `user`. Với login nhân sự, dùng route sẵn có để thử quyền quầy. |

**Contract cho Booking.** Mở rộng contract Customer để một lần đọc xác minh cặp ID, quyền sở hữu, trạng thái ACTIVE và trả tên, điện thoại cho bản sao liên hệ. Cung cấp loài/cân nặng khi dùng bộ lọc option. Giữ tương thích các phương thức đã có người gọi. Kiểm tra cả nhánh lỗi của `ICustomerContract` để hồ sơ thiếu không bị ánh xạ thành DTO thành công.

**Dữ liệu.** Đồng bộ validator, EF và migration với ERD, đặc biệt là độ dài tên pet (mapping hiện là 50, ERD là 100), CHECK số đo và `fk_pet_customer` từ CASCADE sang RESTRICT. Đối chiếu cả giới hạn họ tên, email, điện thoại và cột ảnh của khách. Giữ dữ liệu hiện hữu khi đổi tên cột và cập nhật `updated_at` khi sửa hồ sơ.

## Gói 2 · Resource

[ResourceEndpointResults](src/PetSpa.Modules/Resource/Endpoints/ResourceEndpointResults.cs) đang dùng `StaffOnly` cho thao tác ghi. [ResourceModuleContract](src/PetSpa.Modules/Resource/Infrastructure/Services/ResourceModuleContract.cs) đã kiểm tra ACTIVE ở cả ba cấp và trả ID, đơn giá. Giữ truy vấn này làm cơ sở.

- **RES-01.** Giữ GET danh mục công khai theo Diagram trang 10. Chỉ Manager được POST, PUT, DELETE ở cả `categories`, `services`, `service-options`. DELETE chuyển INACTIVE và trả 204. Kiểm tra tên category duy nhất, tên service duy nhất trong cùng category và liên kết cha hợp lệ. Thử trùng tên khi hai lượt ghi đồng thời.
- **RES-02.** Kiểm tra `GET /api/services?categoryId=…&keyword=…` và `GET /api/services/{serviceId}/options`. Trả loài, khoảng cân nặng, phút, giá và trạng thái. Danh sách quản lý có thể hiển thị dữ liệu INACTIVE. Khi chọn để đặt, cả category, service và option đều phải ACTIVE. Kiểm tra hai biên cân nặng nếu áp dụng lọc theo pet.
- **Contract và nghiệm thu.** Mở rộng DTO đặt được để cung cấp thời lượng bên cạnh giá. Với BILL-01, cung cấp mô tả của option đã đặt, kể cả khi danh mục sau đó ngừng hoạt động. Thử duration bằng 0, giá âm, max nhỏ hơn min, và trường hợp giá bằng 0 hợp lệ. Đổi giá Resource sau khi đặt phải giữ nguyên đơn giá lịch cũ.

## Gói 3 · Booking

[BookingEndpoints](src/PetSpa.Modules/Booking/Endpoints/BookingEndpoints.cs) hiện có bốn route cho tạo, xem theo ID, sửa thông tin và thay dịch vụ. [BookingService](src/PetSpa.Modules/Booking/Application/Services/BookingService.cs) đã chụp giá khi tạo và giữ dòng cũ khi thay danh sách. PUT thông tin chung hiện cho gửi cả chủ, trạng thái và mốc giờ. [IBookingModule](src/PetSpa.Modules/Booking/Contracts/IBookingModule.cs) hiện chưa có phương thức.

**Migration và dữ liệu cũ.** Bổ sung `contact_name`, `contact_phone`, `sessions_created_at`, `cancelled_at` và cách nhận diện chính xác tập ID dòng đã bàn giao. Đồng bộ enum, CHECK và OpenAPI theo năm trạng thái trong bảng bên dưới. Trước khi thay CHECK, đếm lịch `CHECKED_IN`, đối chiếu mốc tiếp nhận và thống nhất ánh xạ lịch đã đến sang `CONFIRMED`. Bổ sung liên hệ lịch cũ qua contract Customer khi còn tra được. Với nhóm phiên cũ, đối soát tập ID với thành viên Operation trước khi đánh dấu đã bàn giao. Ghi danh sách bản ghi thiếu căn cứ để xử lý riêng. Thử migration trên cả cơ sở dữ liệu trống và bản có dữ liệu cũ.

| Use case và route | Hành vi cần triển khai |
|---|---|
| BOOK-01 · `POST /api/bookings` | Chỉ quầy tạo. Nhận khách, pet, giờ UTC, danh sách option và ghi chú. Kiểm tra đúng chủ và cả hai ACTIVE, danh sách có ít nhất một ID hợp lệ, không trùng. Lấy giá từ Resource và liên hệ từ Customer. Lưu booking PENDING cùng các dòng ASSIGNED trong một lần ghi. Phòng và người thực hiện do Operation bố trí sau. |
| BOOK-02 · `PUT /api/bookings/{id}/services` | Khi PENDING hoặc CONFIRMED trước bàn giao, nhận toàn bộ danh sách muốn giữ. Kiểm tra các option, giữ ID và giá cũ cho dòng còn lại, lấy giá hiện hành cho dòng mới. Sau bàn giao trả 409. Thao tác thay dịch vụ và bàn giao cùng lúc phải dùng cùng cơ chế khóa booking. |
| BOOK-03 · `PUT /api/bookings/{id}` | Chỉ sửa `bookingTime`, `contactName`, `contactPhone`, `note` khi PENDING. Trường ngoài hợp đồng trả 400, lịch đã tiếp nhận trả 409. Sửa số liên hệ của lịch phải giữ nguyên hồ sơ Customer. Báo thay đổi body này cho phía WPF. |
| BOOK-04 · `DELETE /api/bookings/{id}` | Trước bàn giao, hủy mềm lịch PENDING hoặc CONFIRMED cùng các dòng. Sau bàn giao, chỉ hủy CONFIRMED khi đối chiếu đủ tập ID và mọi dòng đều CANCELLED. Ghi thời điểm hủy một lần. Hủy lại trả 204. Nhóm thiếu dòng, còn việc hoặc có dòng đã hoàn thành trả 409. |
| BOOK-06 · `PATCH /api/booking/{id}` | Dùng đúng đường dẫn **số ít** ở Diagram trang 17. Body chỉ có `status`. `confirmed` yêu cầu PENDING có dịch vụ hợp lệ và ghi `check_in` một lần. `completed` yêu cầu CONFIRMED đã bàn giao, đủ đúng tập dòng, mọi dòng kết thúc và ít nhất một dòng COMPLETED. Lặp cùng thao tác khi đã ở trạng thái đích trả 200 với kết quả cũ. Tập rỗng, thiếu dòng hoặc hủy hết không đủ điều kiện chốt. |
| BOOK-05, BOOK-07 · `GET /api/bookings`, `GET /api/bookings/{id}` | Quầy lọc theo ngày, trạng thái, mã khách và số liên hệ lưu trên lịch. Tìm kiếm và danh sách tổng hợp dùng cùng truy vấn. Thứ tự trước phân trang là **COMPLETED → CONFIRMED → PAID → PENDING → CANCELLED**, rồi giờ hẹn tăng dần (trống cuối), ID tăng dần. Cursor mang đủ ba khóa. Trả `items`, `nextCursor` và trang rỗng với 200. Giữ quyền đọc chi tiết lịch của chính khách theo contract hiện có. |

**Phân quyền.** Quầy gồm Receptionist và Manager. Staff phòng chỉ đọc dữ liệu phục vụ phiên được phân công qua luồng Operation. Khách được đọc lịch của mình, mọi lệnh ghi Booking thuộc quầy. ADMIN dùng quyền được cấp rõ theo từng API. Khi bổ sung policy, giới hạn áp dụng vào route của ba module để tránh thay đổi quyền của Operation.

## Bàn giao contract cho Operation và Billing

Agent triển khai **phía cung cấp** tại Customer, Resource và Booking. DTO chỉ mang dữ liệu nghiệp vụ cần thiết. Mỗi module ghi qua chính nó, dùng ID logic qua ranh giới schema. Nếu database đang có FK từ `operation.service_session` sang Booking, thành viên Operation phụ trách migration chuyển đổi bên schema của họ.

| Bên gọi | Cam kết của contract | Trách nhiệm bên gọi |
|---|---|---|
| Operation · OPS-11 | Booking khóa lịch CONFIRMED, kiểm tra đúng và đủ tập ID dòng còn hiệu lực. Ghi `sessions_created_at` và cố định tập ID đúng một lần. Cùng tập gửi lại trả thông tin bàn giao đã lưu, tập khác trả xung đột. | Tạo nhóm phiên, lưu ID logic và gọi ghi mốc trong cùng giao dịch. |
| Operation · OPS-13, OPS-18, OPS-20, OPS-22 | Booking kiểm tra cặp booking/dòng thuộc tập bàn giao, đồng bộ CANCELLED, COMPLETED hoặc WORKING nếu hai bên dùng trạng thái trung gian. Hoàn tất một dòng vẫn giữ cha CONFIRMED để quầy chốt. | Kiểm tra quyền trên phiên, cập nhật phiên và gọi đồng bộ. Operation sở hữu truy vấn nhóm phiên cho OPS-22. |
| Operation · OPS-19 | Customer sở hữu thao tác ghi ghi chú đúng pet và kiểm tra role lễ tân. | Xác định đúng pet trong ngữ cảnh chăm sóc và chuyển yêu cầu cho lễ tân. |
| Billing · BILL-01, BILL-03 | Booking trả các dòng COMPLETED của lịch đã chốt kèm ID option và đơn giá đã lưu. Chỉ contract nội bộ cho Billing chuyển COMPLETED sang PAID. | Billing lấy mô tả từ Resource, lập hóa đơn và gọi chuyển PAID cùng giao dịch với lần thu. |

**Điều kiện tích hợp.** Thống nhất chữ ký DTO, mã xung đột, hành vi gọi lặp và cách các DbContext tham gia cùng một PostgreSQL transaction. Bên điều phối commit sau cùng. Test gây lỗi sau khi Booking đã ghi để chứng minh rollback được cả hai bước. Trong lúc chờ consumer, dùng bên gọi thử trong test để kiểm tra contract, khóa và rollback ở phía cung cấp.

## Kiểm chứng trước khi bàn giao

Tại thư mục gốc, ghi nhận kết quả các lệnh sau trước và sau thay đổi. Hai bộ smoke test của repo chạy bằng `dotnet run`.

```powershell
dotnet build PetSpa.slnx
dotnet run --project tests/PetSpa.IntegrationTests/PetSpa.AuthenticationSmokeTests.csproj
dotnet run --project tests/PetSpa.OperationSmokeTests/PetSpa.OperationSmokeTests.csproj
```

- **Quyền.** Cập nhật các assertion cũ đang cho khách tự tạo booking và mọi nhân sự ghi category. Thử Manager, Receptionist, Staff, Customer, ADMIN và yêu cầu thiếu token. Kiểm tra cả mã trả về lẫn dữ liệu sau yêu cầu bị từ chối.
- **Luồng tại quầy.** Tạo khách/pet → chọn hai dịch vụ → tạo lịch → đổi giá danh mục → sửa liên hệ riêng → tiếp nhận → bàn giao → đồng bộ một dòng COMPLETED, một dòng CANCELLED → quầy chốt → contract Billing chỉ lấy đúng một dòng với giá ban đầu. Thử thêm nhánh toàn bộ dòng CANCELLED và hủy lặp hai lần.
- **PostgreSQL.** Thử thay dịch vụ đồng thời với bàn giao, chốt đồng thời với đồng bộ dòng cuối và gửi lặp thao tác tiếp nhận. Kiểm tra mốc giờ không bị ghi lại, tập bàn giao không thay, giao dịch lỗi không lưu dở. Với phân trang, dùng nhiều lịch trùng giờ ở ranh giới hai trang và kiểm tra thứ tự cùng tính duy nhất của ID.
- **Đầu ra.** Cập nhật OpenAPI cho các route và DTO thay đổi. Bàn giao ví dụ body BOOK-03 và BOOK-06, contract liên module, kết quả migration và kết quả test. Ghi riêng ca đã chạy với bên gọi thử và ca E2E đã chạy với Operation/Billing thật.
