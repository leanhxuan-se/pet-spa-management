# Sơ đồ trình tự PetSpa

[Mở bản PDF có mục lục](output/pdf/SO-DO-TRINH-TU-CAP-NHAT-3.pdf) · Đối chiếu ngày 19/11/2026.

**Cách đọc.** Đọc từ trên xuống. Mũi tên liền là thao tác hoặc lời gọi, mũi tên nét đứt là dữ liệu trả về. Khung alt/else thể hiện các kết quả loại trừ nhau, opt là thao tác tùy chọn, par/and là các lệnh độc lập có thể chạy song song, loop là thao tác lặp.

**Phạm vi.** 43 sơ đồ bao phủ 30 use case gốc (24 bắt buộc, 6 mở rộng), các quy trình con theo góp ý, đăng nhập khách và luồng tổng hợp. “Quầy” gồm lễ tân và quản lý. OPS-22 tiếp nhận use case theo dõi phiên từ tài liệu gốc. BOOK-07 dành cho danh sách lịch đặt. WPF là thiết kế tích hợp cho client ở repo riêng.

**Trạng thái API.** “Đã có” đối chiếu với mã nguồn hiện hành. “Đề xuất” là hợp đồng cần triển khai. “Kết hợp” có cả thao tác đã có và thay đổi đề xuất, kể cả thay DTO hoặc siết quyền trên đường dẫn hiện có. E2E-01 tóm tắt thiết kế đích.

**Xác thực.** Hai API login trả accessToken, tokenType, expiresAtUtc, role và user (id, fullName, phone). Auth tra tài khoản ACTIVE qua kho Customer hoặc Operation hiện hữu và phát JWT. Chưa cấp refresh token. WPF gửi Bearer cho API cần quyền, khi token hết hạn và nhận 401 thì quay lại đăng nhập. Với 403, thông báo thiếu quyền.

**Ranh giới.** Server PetSpa là một ứng dụng ASP.NET Core. Cột module biểu diễn contract nội bộ cùng tiến trình. Nhãn PATCH trên lời gọi Billing sang Booking cho biết thao tác API tương ứng. Cột PostgreSQL chỉ thể hiện dữ liệu do module tương ứng quản lý. Những truy vấn nội bộ đơn giản được lược bớt khi hợp đồng HTTP là trọng tâm.

**Quyền hiện hành.** Màn hình theo actor của use case. Backend cho Customer sửa/thêm pet của mình, cho mọi tài khoản nhân sự ghi Resource và truy cập Booking, cho khách thao tác một số booking của chính mình. Các quyền này rộng hơn use case. Admin đăng nhập nội bộ và quản lý được hồ sơ khách, nhưng policy tra cứu rảnh/bận hiện chỉ nhận receptionist hoặc manager.

**Quyết định thiết kế.** PENDING là chờ tiếp nhận. CONFIRMED ghi nhận khách đã đến. COMPLETED là booking đã hoàn tất dịch vụ và đủ điều kiện lập hóa đơn. PAID là booking đã được thanh toán xong sau BILL-03. Quầy hủy qua BOOK-04. BOOK-06 chỉ tiếp nhận và chốt dịch vụ. Quầy tải các nhóm phiên dịch vụ mỗi 15 giây, nhân viên thăm dò thông báo mỗi 30 giây. Hai chu kỳ là mặc định đề xuất, có thể cấu hình. Tạm dừng khi rời màn hình tương ứng hoặc đăng xuất, bỏ nhịp nếu lệnh trước chưa xong.

## Mục lục

| Mã | Sơ đồ | Phạm vi | API |
| --- | --- | --- | --- |
| [AUTH-01](#auth-01) | Khách đăng nhập và mở trang cá nhân | Bổ sung | Đã có |
| [CUS-01](#cus-01) | Xem và sửa hồ sơ khách hàng | Bắt buộc | Đã có |
| [CUS-02](#cus-02) | Xem và cập nhật hồ sơ thú cưng | Bắt buộc | Kết hợp |
| [CUS-03](#cus-03) | Thêm khách hàng | Bắt buộc | Đã có |
| [CUS-04](#cus-04) | Thêm thú cưng cho khách | Bắt buộc | Kết hợp |
| [CUS-05](#cus-05) | Vô hiệu hóa khách hàng hoặc thú cưng | Mở rộng | Kết hợp |
| [CUS-06](#cus-06) | Đăng nhập tại quầy | Bắt buộc | Đã có |
| [RES-01](#res-01) | Xem và quản lý danh mục dịch vụ | Bắt buộc | Đã có |
| [RES-02](#res-02) | Tìm dịch vụ và chọn tùy chọn phù hợp | Bắt buộc | Đã có |
| [BOOK-01](#book-01) | Tạo lịch đặt | Bắt buộc | Kết hợp |
| [BOOK-02](#book-02) | Thay đổi dịch vụ trong lịch đặt | Bắt buộc | Kết hợp |
| [BOOK-03](#book-03) | Cập nhật thông tin lịch đặt | Bắt buộc | Kết hợp |
| [BOOK-04](#book-04) | Hủy lịch đặt tại quầy | Bắt buộc | Đề xuất |
| [BOOK-05](#book-05) | Tìm lịch đặt và mở chi tiết tại quầy | Bắt buộc | Kết hợp |
| [BOOK-06](#book-06) | Cập nhật trạng thái booking | Bắt buộc | Đề xuất |
| [BOOK-07](#book-07) | Xem tổng hợp các lịch đặt | Quy trình con | Kết hợp |
| [OPS-01](#ops-01) | Quản lý phòng | Mở rộng | Đề xuất |
| [OPS-02](#ops-02) | Xem, thêm, sửa và ngừng sử dụng nhân viên | Mở rộng | Đề xuất |
| [OPS-03](#ops-03) | Xem toàn bộ ca làm | Mở rộng | Đề xuất |
| [OPS-04](#ops-04) | Tìm ca làm theo tiêu chí | Quy trình con | Đề xuất |
| [OPS-05](#ops-05) | Tạo ca làm | Quy trình con | Đề xuất |
| [OPS-06](#ops-06) | Điều chỉnh ca làm | Quy trình con | Đề xuất |
| [OPS-07](#ops-07) | Hủy ca làm | Quy trình con | Đề xuất |
| [OPS-08](#ops-08) | Ghi nhận vào ca và kết thúc ca | Quy trình con | Đề xuất |
| [OPS-09](#ops-09) | Tra cứu phòng hiện tại hoặc theo khoảng giờ | Bắt buộc | Đề xuất |
| [OPS-10](#ops-10) | Tra nhân viên rảnh hoặc bận theo thời gian | Bắt buộc | Đề xuất |
| [OPS-11](#ops-11) | Tạo nhóm phiên dịch vụ trong Operation | Bắt buộc | Đề xuất |
| [OPS-12](#ops-12) | Điều chỉnh phiên dịch vụ | Quy trình con | Đề xuất |
| [OPS-13](#ops-13) | Hủy phiên dịch vụ | Quy trình con | Đề xuất |
| [OPS-14](#ops-14) | Phân công và tự gửi thông báo lần đầu | Bắt buộc | Đề xuất |
| [OPS-15](#ops-15) | Điều chỉnh phân công | Quy trình con | Đề xuất |
| [OPS-16](#ops-16) | Hủy phân công | Quy trình con | Đề xuất |
| [OPS-17](#ops-17) | Nhắc việc và nhận thông báo tự động | Quy trình con | Đề xuất |
| [OPS-18](#ops-18) | Bắt đầu dịch vụ được giao | Bắt buộc | Đề xuất |
| [OPS-19](#ops-19) | Lễ tân cập nhật ghi chú chăm sóc | Mở rộng | Đề xuất |
| [OPS-20](#ops-20) | Hoàn tất phiên dịch vụ | Bắt buộc | Đề xuất |
| [OPS-21](#ops-21) | Nhân viên đăng nhập và nhận danh sách việc | Bắt buộc | Kết hợp |
| [OPS-22](#ops-22) | Theo dõi phiên dịch vụ, gom theo booking | Bắt buộc | Đề xuất |
| [BILL-01](#bill-01) | Tạo hóa đơn | Bắt buộc | Đề xuất |
| [BILL-02](#bill-02) | Áp dụng giảm giá | Mở rộng | Đề xuất |
| [BILL-03](#bill-03) | Ghi nhận thanh toán | Bắt buộc | Đề xuất |
| [BILL-04](#bill-04) | Tìm và xem hóa đơn | Bắt buộc | Đề xuất |
| [E2E-01](#e2e-01) | Từ tiếp nhận đến chốt nhóm phiên và thu tiền | Tổng hợp | Kết hợp |

<a id="auth-01"></a>

## AUTH-01 · Khách đăng nhập và mở trang cá nhân

**Bổ sung · API đã có**  
**Người tham gia** — Khách hàng

Khách nhập số điện thoại và mật khẩu. WPF chỉ bắt đầu tải dữ liệu cá nhân sau khi nhận token.

```mermaid
sequenceDiagram
    actor NguoiDung as Khách hàng
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Nhập số điện thoại, mật khẩu và nhấn Đăng nhập
    Client->>Server: POST /api/auth/customer/login (phone, password)
    alt Dữ liệu thiếu hoặc sai định dạng
        Server-->>Client: 400 • Thông tin trường cần sửa
        Client-->>NguoiDung: Đánh dấu trường lỗi trên biểu mẫu
    else Yêu cầu hợp lệ
        Server->>DuLieu: Tìm khách ACTIVE theo số điện thoại
        DuLieu-->>Server: Hồ sơ và hash mật khẩu, hoặc không tìm thấy
        Server->>Server: Auth xác thực tài khoản và đối chiếu mật khẩu với hash
        opt Mật khẩu hợp lệ
            Server->>DuLieu: Ghi last_login_at, nâng cấp hash nếu cần
            DuLieu-->>Server: Số hồ sơ cập nhật
        end
        alt Mật khẩu đúng và đã cập nhật một hồ sơ
            Server->>Server: Phát JWT có role customer và customer_id
            Server-->>Client: 200 • accessToken, tokenType, expiresAtUtc, role, user
            par Tải hồ sơ
                Client->>Server: GET /api/customers/{id}
                Server-->>Client: 200 • Thông tin khách hàng
            and Tải danh sách thú cưng
                Client->>Server: GET /api/customers/{id}/pets
                Server-->>Client: 200 • Danh sách thú cưng
            end
            Client-->>NguoiDung: Hiển thị hồ sơ và danh sách thú cưng
        else Xác thực hoặc cập nhật lần đăng nhập thất bại
            Server-->>Client: 401 • Từ chối đăng nhập
            Client-->>NguoiDung: Báo đăng nhập thất bại và giữ màn hình đăng nhập
        end
    end
```

`id` ở hai lệnh GET là `user.id` trong kết quả login. WPF gửi `Authorization: Bearer <accessToken>` cho cả hai. Hai lệnh đọc độc lập. Nếu một lệnh lỗi mạng, giữ phần đã tải và cho thử lại phần lỗi. `200 []` ở danh sách thú cưng hiển thị “Chưa có thú cưng”.

<a id="cus-01"></a>

## CUS-01 · Xem và sửa hồ sơ khách hàng

**Bắt buộc · API đã có**  
**Người tham gia** — Khách hàng (hồ sơ của mình), lễ tân, quản lý

Khách mở trang cá nhân bằng ID trong phiên. Tại quầy, lễ tân hoặc quản lý chọn khách từ danh sách đang hoạt động.

```mermaid
sequenceDiagram
    actor NguoiDung as Khách / Quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    opt Người dùng tại quầy cần chọn khách
        NguoiDung->>Client: Mở danh sách khách hàng
        Client->>Server: GET /api/customers
        Server-->>Client: 200 • Khách ACTIVE, hoặc 404 nếu danh sách rỗng
    end
    NguoiDung->>Client: Mở hồ sơ cá nhân hoặc chọn một khách
    Client->>Server: GET /api/customers/{id}
    Server->>DuLieu: Đọc hồ sơ sau khi kiểm tra quyền
    DuLieu-->>Server: Hồ sơ khách hàng
    Server-->>Client: 200 • Hồ sơ
    Client-->>NguoiDung: Hiển thị thông tin hiện tại
    opt Người dùng sửa và nhấn Lưu
        NguoiDung->>Client: Sửa họ tên, liên hệ hoặc thông tin cá nhân
        Client->>Server: PUT /api/customers/{id}
        alt Đúng quyền và dữ liệu hợp lệ
            Server->>DuLieu: Ghi thay đổi hồ sơ
            Server-->>Client: 201 • Hồ sơ sau cập nhật
            Client-->>NguoiDung: Hiển thị thông tin vừa lưu
        else Có lỗi
            Server-->>Client: 403 sai quyền / 404 thiếu hồ sơ / 422 dữ liệu sai
            Client-->>NguoiDung: Giữ nội dung nhập và hiển thị lỗi tương ứng
        end
    end
```

`PUT` hiện trả `201` kèm hồ sơ đã lưu. Màn hình tự phục vụ chỉ gửi thông tin cá nhân được sửa. Quyền đổi trạng thái được bàn riêng ở CUS-05. `note` trên hồ sơ khách không cập nhật `specialNote` của thú cưng.

<a id="cus-02"></a>

## CUS-02 · Xem và cập nhật hồ sơ thú cưng

**Bắt buộc · API kết hợp**  
**Người tham gia** — Khách xem thú cưng của mình. Lễ tân và quản lý được sửa hồ sơ theo quyền.

Khách có thể đọc ghi chú chăm sóc trong hồ sơ của thú cưng. Quyền thay đổi specialNote thuộc riêng lễ tân.

```mermaid
sequenceDiagram
    actor NguoiDung as Khách / Quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở danh sách rồi chọn thú cưng
    Client->>Server: GET /api/customers/{customerId}/pets
    Server-->>Client: 200 • Danh sách thú cưng
    Client->>Server: GET /api/pets/{id}
    Server-->>Client: 200 • Hồ sơ và ghi chú, hoặc 404 nếu ngoài phạm vi
    Client-->>NguoiDung: Hiển thị hồ sơ theo quyền
    opt Có yêu cầu sửa hồ sơ
        NguoiDung->>Client: Gửi thay đổi
        Client->>Server: PUT /api/pets/{id}
        alt Có trường specialNote và người gọi không phải lễ tân
            Server-->>Client: [Đề xuất] 403 • Từ chối toàn bộ yêu cầu
            Client-->>NguoiDung: Hiển thị hướng dẫn liên hệ lễ tân
        else Người gọi có quyền sửa các trường đã gửi
            Server->>DuLieu: Kiểm tra quyền trên hồ sơ và lưu thay đổi
            Server-->>Client: 201 • Hồ sơ đã cập nhật
            Client-->>NguoiDung: Hiển thị dữ liệu mới
        end
    end
```

Hiện PUT nhận specialNote và kiểm tra quyền theo chủ hồ sơ. Thiết kế mới yêu cầu kiểm tra role và sự xuất hiện của trường trước khi ghi, kể cả specialNote = null hoặc đi kèm trường hợp lệ. Trường note của khách và note của lịch đặt là dữ liệu riêng.

<a id="cus-03"></a>

## CUS-03 · Thêm khách hàng

**Bắt buộc · API đã có**  
**Người tham gia** — Lễ tân, quản lý

Tạo hồ sơ khách ACTIVE cùng mật khẩu để khách có thể đăng nhập bằng số điện thoại.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Nhập họ tên, số điện thoại, mật khẩu rồi nhấn Tạo
    Client->>Server: POST /api/customers
    alt Dữ liệu không hợp lệ
        Server-->>Client: 422 • Lỗi theo trường
        Client-->>NguoiDung: Giữ biểu mẫu để sửa
    else Dữ liệu hợp lệ
        Server->>DuLieu: Tra số điện thoại đã chuẩn hóa
        DuLieu-->>Server: Kết quả kiểm tra trùng
        alt Đã có khách dùng số này
            Server-->>Client: 409 • Số điện thoại đã tồn tại
            Client-->>NguoiDung: Cho phép mở hồ sơ đã có
        else Số điện thoại mới
            Server->>DuLieu: Lưu khách ACTIVE và hash mật khẩu
            Server-->>Client: 201 • Mã khách và hồ sơ
            Client-->>NguoiDung: Mở hồ sơ vừa tạo, cho phép thêm thú cưng
        end
    end
```

Số điện thoại trùng được xử lý bằng `409`. Khi người dùng chọn mở hồ sơ cũ, WPF dùng danh sách của CUS-01 để tìm và mở khách tương ứng.

<a id="cus-04"></a>

## CUS-04 · Thêm thú cưng cho khách

**Bắt buộc · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Tại hồ sơ khách đang hoạt động, tạo thú cưng và nối đúng chủ sở hữu.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn Thêm thú cưng, nhập tên, loài và thông số
    Client->>Server: POST /api/customers/{customerId}/pets
    Server->>Server: Đối chiếu ID trên URL và body, kiểm tra dữ liệu
    alt Có specialNote và người gọi không phải lễ tân
        Server-->>Client: [Đề xuất] 403 • Từ chối tạo và ghi chú
        Client-->>NguoiDung: Hướng dẫn liên hệ lễ tân
    else Yêu cầu hợp lệ và chủ đang ACTIVE
        Server->>DuLieu: Xác minh chủ thú cưng và tạo hồ sơ
        Server-->>Client: 201 • Thú cưng mới
        Client-->>NguoiDung: Thêm thú cưng vào danh sách trên màn hình
    else Sai dữ liệu hoặc khách không hợp lệ
        Server-->>Client: 400 ID lệch hoặc khách INACTIVE / 404 thiếu khách / 422 dữ liệu sai
        Client-->>NguoiDung: Hiển thị lý do và giữ biểu mẫu
    end
```

Body dùng trường `customersId` (có chữ s). ID này phải bằng `customerId` trên URL. API hiện cho cả chủ sở hữu gọi POST, rộng hơn vai trò của CUS-04.

<a id="cus-05"></a>

## CUS-05 · Vô hiệu hóa khách hàng hoặc thú cưng

**Mở rộng · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Ngừng sử dụng một hồ sơ và giữ lại lịch sử booking đã gắn với hồ sơ đó.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn hồ sơ và xác nhận ngừng hoạt động
    alt Vô hiệu hóa khách
        Client->>Server: PUT /api/customers/{id} (status = INACTIVE)
        Server->>DuLieu: Cập nhật trạng thái khách
        Server-->>Client: 201 • Hồ sơ khách INACTIVE
    else Vô hiệu hóa thú cưng
        Client->>Server: PUT /api/pets/{id} (status = INACTIVE)
        Server->>DuLieu: Cập nhật trạng thái thú cưng
        Server-->>Client: 201 • Hồ sơ thú cưng INACTIVE
    end
    Client-->>NguoiDung: Đánh dấu hồ sơ đã ngừng hoạt động
```

Hai PUT hiện chấp nhận `status` qua quyền truy cập chủ sở hữu. Theo use case, chỉ vai trò quản lý hồ sơ được đổi trạng thái. Login kiểm tra ACTIVE. Contract đặt lịch hiện chỉ kiểm tra quan hệ sở hữu, còn thiếu điều kiện ACTIVE của khách và thú cưng.

<a id="cus-06"></a>

## CUS-06 · Đăng nhập tại quầy

**Bắt buộc · API đã có**  
**Người tham gia** — Lễ tân, quản lý

Cùng API đăng nhập nội bộ với nhân viên thực hiện dịch vụ. Sau xác thực, quầy có thể mở màn hình tiếp nhận khách.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn đăng nhập nội bộ, nhập điện thoại và mật khẩu
    Client->>Server: POST /api/auth/staff/login (phone, password)
    Server->>DuLieu: Tra nhân viên ACTIVE trong Operation
    DuLieu-->>Server: Hồ sơ, role và hash
    alt Xác thực và ghi nhận thành công
        Server->>DuLieu: Ghi last_login_at, nâng cấp hash nếu cần
        DuLieu-->>Server: Đã cập nhật một nhân viên
        Server-->>Client: 200 • JWT, thời hạn, role, user
        Client-->>NguoiDung: Mở màn hình tiếp nhận theo role
        Client->>Server: GET /api/customers (Bearer)
        Server-->>Client: 200 • Danh sách khách ACTIVE, hoặc 404 nếu rỗng
        Client-->>NguoiDung: Hiển thị danh sách hoặc trạng thái chưa có khách
    else Từ chối đăng nhập
        Server-->>Client: 400 biểu mẫu sai / 401 thông tin đăng nhập không hợp lệ
        Client-->>NguoiDung: Yêu cầu kiểm tra thông tin
    end
```

Token chứa `sub`, `account_type`, `role` và `staff_id`. Auth xác minh hash qua kho Operation rồi ghi lần đăng nhập trước khi phát JWT. Role lấy từ server. Tài khoản `admin` cũng dùng route này.

<a id="res-01"></a>

## RES-01 · Xem và quản lý danh mục dịch vụ

**Bắt buộc · API đã có**  
**Người tham gia** — Quản lý (đọc và ghi), khách hàng (chỉ xem)

Chọn danh mục, dịch vụ hoặc tùy chọn giá. Mỗi cấp có cùng mẫu thao tác quản lý.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý / Khách
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở danh mục dịch vụ
    Client->>Server: GET /api/categories
    Server-->>Client: 200 • Danh mục và trạng thái
    Client-->>NguoiDung: Hiển thị danh mục
    opt Quản lý chỉnh danh mục, dịch vụ hoặc tùy chọn
        NguoiDung->>Client: Chọn đối tượng và thao tác
        alt Thêm mới
            Client->>Server: POST /api/{tập}
            Server->>DuLieu: Kiểm tra và tạo bản ghi
            Server-->>Client: 201 • Đối tượng mới
        else Chỉnh sửa
            Client->>Server: PUT /api/{tập}/{id}
            Server->>DuLieu: Lưu trường đã sửa
            Server-->>Client: 200 • Đối tượng sau cập nhật
        else Ngừng cung cấp
            Client->>Server: DELETE /api/{tập}/{id}
            Server->>DuLieu: Đặt trạng thái INACTIVE
            Server-->>Client: 204 • Đã ngừng cung cấp
        end
        Client-->>NguoiDung: Cập nhật danh mục hoặc báo lỗi nhập liệu, trùng tên
    end
```

Trong sơ đồ, `{tập}` là `categories`, `services` hoặc `service-options`. Mỗi tập có GET danh sách/chi tiết, POST, PUT, DELETE. Các GET là công khai. Endpoint ghi hiện dùng `StaffOnly`, rộng hơn quyền Manager trong use case.

<a id="res-02"></a>

## RES-02 · Tìm dịch vụ và chọn tùy chọn phù hợp

**Bắt buộc · API đã có**  
**Người tham gia** — Khách hàng, lễ tân, quản lý

Tra cứu theo danh mục và từ khóa. Chọn tùy chọn theo loài, khoảng cân nặng, thời lượng và giá.

```mermaid
sequenceDiagram
    actor NguoiDung as Người tra cứu
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn danh mục và nhập tên dịch vụ
    Client->>Server: GET /api/services?categoryId={id}&keyword={text}
    Server->>DuLieu: Tìm tên dịch vụ trong danh mục
    DuLieu-->>Server: Các dịch vụ phù hợp
    Server-->>Client: 200 • Danh sách kết quả
    alt Có dịch vụ
        Client-->>NguoiDung: Hiển thị danh sách để chọn
        NguoiDung->>Client: Chọn dịch vụ
        Client->>Server: GET /api/services/{serviceId}/options
        Server-->>Client: 200 • Loài, cân nặng, thời lượng, đơn giá, trạng thái
        Client-->>NguoiDung: Hiển thị tùy chọn phù hợp với thú cưng
    else Danh sách rỗng
        Client-->>NguoiDung: Báo chưa tìm thấy, cho đổi bộ lọc
    end
```

GET danh sách hiện có thể trả cả dữ liệu INACTIVE. Màn hình chọn để đặt lịch lọc các cấp ACTIVE. Khi ghi booking, server kiểm tra lại trạng thái và lấy đơn giá qua Resource Contract.

<a id="book-01"></a>

## BOOK-01 · Tạo lịch đặt

**Bắt buộc · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Quầy đã chọn khách, thú cưng, giờ hẹn và ít nhất một tùy chọn dịch vụ qua CUS-01/02 và RES-02.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Customer as Module Customer
    participant Resource as Module Resource
    NguoiDung->>Client: Chọn thú cưng, giờ và dịch vụ, nhấn Đặt lịch
    Client->>Server: POST /api/bookings
    Server->>Customer: PetBelongsToCustomerAsync(petId, customerId)
    Customer-->>Server: Kết quả kiểm tra chủ sở hữu
    Server->>Resource: GetActiveServiceOptionsAsync(serviceOptionIds)
    Resource-->>Server: Các tùy chọn đặt được cùng đơn giá
    alt Đúng chủ và đủ tùy chọn hợp lệ
        Server->>Server: [Đề xuất] Chụp họ tên và số liên hệ của booking
        Server->>Server: Lưu booking PENDING và các dòng dịch vụ
        Server-->>Client: 201 • Mã lịch, trạng thái, chi tiết và giá
        Client-->>NguoiDung: Mở phiếu đặt lịch
    else Không hợp lệ
        Server-->>Client: 400 dữ liệu sai / 403 yêu cầu ngoài phạm vi tài khoản
        Client-->>NguoiDung: Yêu cầu chọn lại khách, thú cưng hoặc dịch vụ
    end
```

Thiết kế mới lưu họ tên và số điện thoại liên hệ vào booking để BOOK-03 có thể thay đổi riêng cho lịch này. WPF gửi `customerId`, `petId`, `bookingTime` ở UTC, `serviceOptionIds` và `note`. Server lưu giá từng tùy chọn vào `booking.booking_details.unit_price`. Phòng và nhân viên được bố trí khi bàn giao ở OPS-11.

<a id="book-02"></a>

## BOOK-02 · Thay đổi dịch vụ trong lịch đặt

**Bắt buộc · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Mã hiện tại cho thay dịch vụ ở PENDING hoặc CONFIRMED. Theo thiết kế mới, quầy chốt danh sách khi bàn giao nhóm phiên. WPF gửi toàn bộ danh sách muốn giữ.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở lịch và chỉnh dịch vụ
    Client->>Server: GET /api/bookings/{id}
    Server-->>Client: 200 • Chi tiết lịch và các tùy chọn đang chọn
    NguoiDung->>Client: Xác nhận danh sách mới
    Client->>Server: PUT /api/bookings/{id}/services (serviceOptionIds)
    Server->>DuLieu: Đọc trạng thái lịch và các dòng hiện tại
    Server->>Server: [Đề xuất] Chặn thay danh sách sau mốc bàn giao
    alt Lịch cho sửa và dữ liệu hợp lệ
        Server->>Server: Kiểm tra chủ thú cưng và các tùy chọn ACTIVE
        Server->>DuLieu: Giữ dòng cũ, bỏ dòng được phép, thêm dòng mới
        Server-->>Client: 200 • Lịch và danh sách dịch vụ mới
        Client-->>NguoiDung: Hiển thị danh sách và giá đã lưu
    else Không thể thay
        Server-->>Client: 409 xung đột trạng thái / 400 danh sách sai
        Client-->>NguoiDung: Giữ lựa chọn để sửa hoặc tải lại lịch
    end
```

Server giữ giá của dòng cũ và lấy giá hiện tại cho dòng thêm mới. Hiện tại các dòng bị bỏ được xóa nếu chưa WORKING/COMPLETED. Theo thiết kế mới, khi đã có phiên, bỏ dịch vụ qua OPS-13 để hủy các phân công và giữ lịch sử, đồng thời cố định tập dòng đã bàn giao. Không thay đổi dịch vụ qua PUT thông tin chung của BOOK-03.

<a id="book-03"></a>

## BOOK-03 · Cập nhật thông tin lịch đặt

**Bắt buộc · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Tách việc sửa giờ hẹn, liên hệ và ghi chú khỏi các thao tác thay dịch vụ, xác nhận và hủy lịch.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở lịch và chọn sửa thông tin
    Client->>Server: GET /api/bookings/{id}
    Server-->>Client: 200 • Giờ hẹn, họ tên, số liên hệ, ghi chú
    NguoiDung->>Client: Sửa thông tin và nhấn Lưu
    Client->>Server: [Đề xuất] PUT /api/bookings/{id} (body thông tin lịch)
    Server->>Server: Chỉ nhận trường cho phép và kiểm tra quyền
    alt Có status, checkIn, checkOut, dịch vụ hoặc ID đổi chủ
        Server-->>Client: 400 • Trường ngoài hợp đồng
    else Lịch PENDING và thông tin hợp lệ
        Server->>DuLieu: Lưu thông tin lịch và bản sao liên hệ
        Server-->>Client: 200 • Booking đã cập nhật
    else Lịch đã tiếp nhận, đóng hoặc dữ liệu sai
        Server-->>Client: 409 sai trạng thái / 400 dữ liệu sai
    end
    Client-->>NguoiDung: Hiển thị lịch mới hoặc lý do cần sửa
```

Body đề xuất gồm bookingTime, contactName, contactPhone, note. Tên và số điện thoại là bản sao liên hệ của riêng booking, khởi tạo từ hồ sơ khách khi tạo lịch. Cần thêm hai trường, xử lý dữ liệu cũ và thay DTO hiện yêu cầu status. WPF cũ và server cần đổi hợp đồng đồng bộ.

<a id="book-04"></a>

## BOOK-04 · Hủy lịch đặt tại quầy

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Quầy hủy lịch chưa bàn giao. Với lịch đã bàn giao, chỉ cho phép hủy khi toàn bộ dòng trong nhóm đã được hủy ở Operation.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn lịch và xác nhận hủy
    Client->>Server: DELETE /api/bookings/{id}
    Server->>DuLieu: Khóa lịch và đọc trạng thái, mốc bàn giao
    alt PENDING hoặc CONFIRMED chưa bàn giao
        Server->>DuLieu: Hủy lịch cùng các dòng dịch vụ và lưu thời điểm
        Server-->>Client: 204 • Đã hủy lịch
    else CONFIRMED đã bàn giao, đủ tập dòng và tất cả đều CANCELLED
        Server->>DuLieu: Hủy mềm booking, giữ lịch sử phiên và dòng đã hủy
        Server-->>Client: 204 • Đã hủy lịch
    else Lặp yêu cầu với lịch đã CANCELLED
        Server-->>Client: 204 • Đã hủy
    else Còn phiên cần xử lý, có dịch vụ đã làm hoặc booking đã đóng
        Server-->>Client: 409 • Lịch đã qua bước cho phép hủy
    end
    Client-->>NguoiDung: Cập nhật danh sách hoặc hiển thị lý do
```

Booking khóa lịch, kiểm tra trạng thái các dòng đã được Operation đồng bộ và ghi `CANCELLED` đúng một lần. Mốc `sessionsCreatedAt` phân biệt lịch chưa bàn giao với nhóm đã bàn giao. Nhóm rỗng hoặc thiếu dòng cần đối soát. Nhóm đã hủy toàn bộ kết thúc ở đây và không lập hóa đơn.

<a id="book-05"></a>

## BOOK-05 · Tìm lịch đặt và mở chi tiết tại quầy

**Bắt buộc · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Tìm booking theo ngày, trạng thái, mã khách hoặc số liên hệ đã lưu trên lịch. Nhân viên phòng mở công việc từ danh sách phiên được giao.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Nhập tiêu chí và nhấn Tìm
    Client->>Server: [Đề xuất] GET /api/bookings?date={date}&status={status}
    Server->>DuLieu: Tìm trong dữ liệu booking và phân trang
    Server-->>Client: 200 • Trang kết quả
    alt Có lịch phù hợp
        Client-->>NguoiDung: Hiển thị lịch, tên khách, giờ hẹn và trạng thái
        NguoiDung->>Client: Chọn một lịch
        Client->>Server: GET /api/bookings/{id}
        Server->>DuLieu: Kiểm tra quyền và đọc booking cùng các dòng dịch vụ
        Server-->>Client: 200 • Thông tin chi tiết
        Client-->>NguoiDung: Mở phiếu đặt lịch
    else Danh sách rỗng
        Client-->>NguoiDung: Hiển thị chưa tìm thấy lịch
    end
```

GET chi tiết theo ID đã có. API tìm kiếm và việc giới hạn cả tìm kiếm lẫn xem chi tiết của màn hình nội bộ cho quầy là thiết kế mới. Với khách tự phục vụ, quyền xem dữ liệu của chính mình vẫn được kiểm tra ở server.

<a id="book-06"></a>

## BOOK-06 · Cập nhật trạng thái booking

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Khi khách đến, quầy xác nhận tiếp nhận. Khi nhóm phiên kết thúc, quầy chốt booking để Billing có thể lập hóa đơn. Hai thao tác dùng cùng một PATCH, mỗi yêu cầu chỉ gửi trường `status`.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn thao tác trên booking
    alt Khách đã đến tiệm
        Client->>Server: PATCH /api/booking/{id} {"status":"confirmed"}
        Server->>DuLieu: Khóa booking và đọc các dòng dịch vụ
        alt PENDING và có dịch vụ hợp lệ
            Server->>DuLieu: Ghi CONFIRMED và thời điểm tiếp nhận
            Server-->>Client: 200 • Booking đã tiếp nhận
        else Đã CONFIRMED
            Server-->>Client: 200 • Trả thời điểm tiếp nhận đã ghi
        else Sai trạng thái hoặc thiếu dịch vụ
            Server-->>Client: 409 • Chưa thể tiếp nhận
        end
    else Quầy chốt nhóm sau bàn giao
        Client->>Server: PATCH /api/booking/{id} {"status":"completed"}
        Server->>DuLieu: Khóa booking, đối chiếu mốc bàn giao và đủ các dòng
        alt CONFIRMED, mọi dòng đã kết thúc và ít nhất một dòng COMPLETED
            Server->>DuLieu: Ghi booking COMPLETED
            Server-->>Client: 200 • Booking đã chốt
        else Đã COMPLETED
            Server-->>Client: 200 • Trả kết quả đã chốt
        else Sai trạng thái, thiếu dòng hoặc còn việc
            Server-->>Client: 409 • Chưa thể chốt
        end
    end
    Client-->>NguoiDung: Hiển thị kết quả hoặc lý do cần xử lý
```

**Gọi cùng [BILL-01](#bill-01).** Khi chốt nhóm, chỉ lập hóa đơn sau khi PATCH `completed` thành công. Nếu mất phản hồi từ Billing, chỉ gửi lại BILL-01. Lần PATCH `confirmed` lúc khách đến kết thúc tại bước tiếp nhận. Hủy lịch qua BOOK-04. Chỉ Billing được yêu cầu `paid` ở BILL-03.

<a id="book-07"></a>

## BOOK-07 · Xem tổng hợp các lịch đặt

**Quy trình con · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý

Mở danh sách booking của quầy theo quyền. Lần tải đầu không cần nhập tiêu chí. Người dùng có thể phân trang hoặc mở phiếu chi tiết.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở màn hình Tất cả lịch đặt
    Client->>Server: [Đề xuất] GET /api/bookings
    Server->>DuLieu: Lấy trang đầu các lịch theo quyền và thứ tự ưu tiên trạng thái
    DuLieu-->>Server: Lịch, khách, thú cưng, giờ hẹn và trạng thái
    Server-->>Client: 200 • items và nextCursor
    Client-->>NguoiDung: Hiển thị danh sách hoặc trạng thái rỗng
    opt Xem trang sau
        NguoiDung->>Client: Chọn Xem thêm
        Client->>Server: [Đề xuất] GET /api/bookings?cursor={nextCursor}
        Server-->>Client: 200 • Trang kế tiếp
        Client-->>NguoiDung: Nối các lịch vào danh sách
    end
    opt Mở chi tiết
        NguoiDung->>Client: Chọn một lịch
        Client->>Server: [Đã có] GET /api/bookings/{id}
        Server-->>Client: 200 • Phiếu đặt lịch
        Client-->>NguoiDung: Hiển thị chi tiết lịch
    end
```

Ưu tiên hiển thị: `completed → confirmed → paid → pending → cancelled`. Server sắp xếp trước khi phân trang, tiếp theo là `bookingTime` tăng dần (giờ trống xếp cuối) và `id` tăng dần. Cursor mang đủ ba khóa sắp xếp. BOOK-05 dùng cùng thứ tự khi có bộ lọc.

<a id="ops-01"></a>

## OPS-01 · Quản lý phòng

**Mở rộng · API đề xuất**  
**Người tham gia** — Quản lý

Khai báo phòng, sửa tên/loại phòng hoặc đưa phòng vào bảo trì. Các route tra cứu theo khoảng giờ đã có. OPS-09 mô tả hợp đồng mới cho cả hiện tại và khoảng giờ.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở danh sách phòng
    Client->>Server: GET /api/room
    Server-->>Client: 200 • Phòng, loại phòng và trạng thái
    NguoiDung->>Client: Chọn thêm phòng hoặc sửa phòng đã có
    alt Phòng mới
        Client->>Server: POST /api/room
        Server->>DuLieu: Ghi tên, loại và trạng thái phòng
        Server-->>Client: 201 • Phòng mới
    else Sửa phòng hoặc chuyển MAINTENANCE
        Client->>Server: PUT /api/room/{id}
        Server->>DuLieu: Kiểm tra các phiên bị ảnh hưởng trước khi lưu
        Server-->>Client: 200 đã cập nhật / 409 nếu còn phiên xung đột
    end
    Client-->>NguoiDung: Hiển thị phòng vừa lưu hoặc yêu cầu bố trí lại phiên
```

Dùng cùng tiền tố `/api/room` của module Operation. Khi đổi trạng thái, thiết kế kiểm tra phiên đang dùng phòng để quản lý có thể sắp xếp lại trước khi ngừng sử dụng.

<a id="ops-02"></a>

## OPS-02 · Xem, thêm, sửa và ngừng sử dụng nhân viên

**Mở rộng · API đề xuất**  
**Người tham gia** — Quản lý

Chọn một hàng để xem chi tiết nhân viên bằng ID. DELETE vô hiệu hóa tài khoản và giữ hồ sơ, lịch sử phân công.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở màn hình nhân viên
    Client->>Server: GET /api/staff
    Server-->>Client: 200 • Danh sách nhân viên
    alt Xem chi tiết
        NguoiDung->>Client: Chọn nhân viên
        Client->>Server: GET /api/staff/{id}
        Server-->>Client: 200 • Hồ sơ, chức danh, vai trò, trạng thái / 404 thiếu hồ sơ
    else Thêm
        Client->>Server: POST /api/staff
        Server-->>Client: 201 • Hồ sơ mới
    else Sửa
        Client->>Server: PUT /api/staff/{id}
        Server-->>Client: 200 • Hồ sơ sau cập nhật
    else Ngừng sử dụng
        NguoiDung->>Client: Xác nhận ngừng sử dụng nhân viên
        Client->>Server: DELETE /api/staff/{id}
        Server->>DuLieu: Kiểm tra phân công, ca và đặt INACTIVE nếu được phép
        Server-->>Client: 204 đã vô hiệu hóa / 409 còn công việc cần bố trí
    end
    Client-->>NguoiDung: Hiển thị kết quả thao tác
```

Khi tạo nhân viên, server kiểm tra trùng tài khoản và lưu hash mật khẩu. Quản lý chỉ được cấp các role trong phạm vi quyền của mình. DELETE trả 409 khi còn phiên hoặc ca cần bố trí lại, trả 204 khi đã INACTIVE. Login nội bộ hiện đã kiểm tra ACTIVE.

<a id="ops-03"></a>

## OPS-03 · Xem toàn bộ ca làm

**Mở rộng · API đề xuất**  
**Người tham gia** — Quản lý

Mở bảng ca theo trang, gồm ca đã xếp, đang làm, hoàn tất và đã hủy.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở bảng ca làm
    Client->>Server: GET /api/staff-shifts
    Server->>DuLieu: Lấy danh sách ca theo trang và quyền
    DuLieu-->>Server: Ngày, giờ, nhân viên, trạng thái
    Server-->>Client: 200 • items và nextCursor
    Client-->>NguoiDung: Hiển thị toàn bộ danh sách theo trang hoặc trạng thái rỗng
    opt Người dùng chuyển trang
        Client->>Server: GET /api/staff-shifts?cursor={nextCursor}
        Server-->>Client: 200 • Trang tiếp theo
        Client-->>NguoiDung: Hiển thị thêm ca
    end
```

Các quy trình OPS-04 đến OPS-08 lần lượt mô tả lọc, tạo, sửa, hủy và ghi nhận vào/kết thúc ca. WORKING là trạng thái đề xuất bổ sung vào StaffShiftStatus và ràng buộc lưu trữ.

<a id="ops-04"></a>

## OPS-04 · Tìm ca làm theo tiêu chí

**Quy trình con · API đề xuất**  
**Người tham gia** — Quản lý

Lọc bảng ca theo ngày, nhân viên hoặc trạng thái. Chỉ gửi các tiêu chí người dùng đã chọn.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn ngày, nhân viên hoặc trạng thái WORKING
    Client->>Server: GET /api/staff-shifts?date={date}&staffId={id}&status={status}
    Server->>DuLieu: Lọc theo các tiêu chí đã gửi
    DuLieu-->>Server: Các ca phù hợp
    Server-->>Client: 200 • Trang kết quả hoặc 400 nếu bộ lọc sai
    Client-->>NguoiDung: Hiển thị các ca tìm được hoặc lỗi bộ lọc
```

Trả 200 với items rỗng khi chưa có ca khớp. Khi bộ lọc thay đổi, WPF bắt đầu lại từ trang đầu.

<a id="ops-05"></a>

## OPS-05 · Tạo ca làm

**Quy trình con · API đề xuất**  
**Người tham gia** — Quản lý

Tạo ca cho nhân viên ACTIVE theo ngày và giờ tại tiệm.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn nhân viên, ngày, giờ bắt đầu và kết thúc
    Client->>Server: POST /api/staff-shifts
    Server->>DuLieu: Kiểm tra nhân viên và các ca giao khoảng
    alt Ca hợp lệ
        Server->>DuLieu: Lưu ca SCHEDULED
        Server-->>Client: 201 • Ca làm mới
    else Dữ liệu sai hoặc trùng lịch
        Server-->>Client: 400 khoảng giờ sai / 409 trùng ca
    end
    Client-->>NguoiDung: Hiển thị kết quả trên lịch ca
```

Ca mới bắt đầu ở SCHEDULED. Server kiểm tra khoảng giờ trên cùng ngày và ca trùng của cùng nhân viên ngay lúc ghi.

<a id="ops-06"></a>

## OPS-06 · Điều chỉnh ca làm

**Quy trình con · API đề xuất**  
**Người tham gia** — Quản lý

Sửa ngày hoặc giờ của ca SCHEDULED và kiểm tra các phân công đang dựa vào ca đó.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở ca đã xếp và sửa ngày hoặc giờ
    Client->>Server: PUT /api/staff-shifts/{id}
    Server->>DuLieu: Kiểm tra trạng thái, ca trùng và các phiên đã giao
    alt SCHEDULED và đáp ứng các phân công
        Server->>DuLieu: Lưu thời gian mới
        Server-->>Client: 200 • Ca đã cập nhật
    else Đã vào ca hoặc có xung đột
        Server-->>Client: 409 • Lý do xung đột
    end
    Client-->>NguoiDung: Hiển thị ca vừa sửa hoặc phiên cần bố trí lại
```

Giữ mốc bắt đầu thực tế của ca WORKING. Khi ca mới khiến nhân viên mất khả dụng cho một phiên đã giao, trả 409 để quản lý bố trí lại.

<a id="ops-07"></a>

## OPS-07 · Hủy ca làm

**Quy trình con · API đề xuất**  
**Người tham gia** — Quản lý

Ngừng sử dụng một ca chưa bắt đầu bằng DELETE. Lịch sử nhân viên vẫn tra cứu được ca đã hủy.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn ca và xác nhận Hủy
    Client->>Server: DELETE /api/staff-shifts/{id}
    Server->>DuLieu: Đọc trạng thái và các phân công bị ảnh hưởng
    alt SCHEDULED đủ điều kiện hoặc đã CANCELLED
        Server->>DuLieu: Đặt CANCELLED nếu đây là lần hủy đầu
        Server-->>Client: 204 • Đã hủy
    else Đang làm, đã hoàn tất hoặc còn phân công
        Server-->>Client: 409 • Ca chưa thể hủy
    end
    Client-->>NguoiDung: Làm mới bảng ca hoặc hiển thị lý do
```

Chỉ hủy ca SCHEDULED không còn phân công cần ca đó. Ca WORKING kết thúc qua quy trình OPS-08 để giữ giờ thực tế.

<a id="ops-08"></a>

## OPS-08 · Ghi nhận vào ca và kết thúc ca

**Quy trình con · API đề xuất**  
**Người tham gia** — Nhân viên của ca, quản lý được phép ghi nhận

Vòng đời ca là SCHEDULED → WORKING → COMPLETED. Từ SCHEDULED cũng có thể chuyển CANCELLED qua DELETE.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân viên / Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Nhấn Vào ca
    Client->>Server: POST /api/staff-shifts/{id}/start
    Server->>DuLieu: Kiểm tra quyền và trạng thái, ghi WORKING cùng giờ vào
    Server-->>Client: 200 • Ca đang làm / 403 sai người / 409 sai trạng thái
    Client-->>NguoiDung: Hiển thị trạng thái ca
    opt Khi kết thúc ca WORKING
        NguoiDung->>Client: Nhấn Kết thúc ca
        Client->>Server: POST /api/staff-shifts/{id}/finish
        Server->>DuLieu: Kiểm tra phiên còn làm, ghi COMPLETED và giờ ra
        Server-->>Client: 200 • Ca hoàn tất / 409 còn phiên đang làm
        Client-->>NguoiDung: Hiển thị kết quả kết thúc ca
    end
```

WORKING phản ánh nhân viên thực sự vào ca. Giờ server ghi nhận một lần cho mỗi mốc. Server kiểm tra người gọi sở hữu ca hoặc có quyền quản lý. Lặp đúng thao tác trả kết quả đã ghi.

<a id="ops-09"></a>

## OPS-09 · Tra cứu phòng hiện tại hoặc theo khoảng giờ

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Tra ngay dùng đồng hồ server. Khi bố trí cho cả dịch vụ, quầy truyền giờ bắt đầu và kết thúc để kiểm tra phòng suốt khoảng đó.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn danh sách phòng trống hoặc không khả dụng
    alt Tra phòng hiện tại
        Client->>Server: GET /api/room/available hoặc /api/room/unavailable
        Server->>Server: Chốt một thời điểm now theo đồng hồ server
        Server->>DuLieu: Xét trạng thái phòng và phiên chiếm phòng tại now
    else Tìm phòng trống theo khoảng yêu cầu
        Client->>Server: GET /api/room/available?startTime={t1}&endTime={t2}
        Server->>DuLieu: Tìm phòng AVAILABLE có thể dùng suốt khoảng từ t1 đến t2
    else Tìm phòng không khả dụng theo khoảng yêu cầu
        Client->>Server: GET /api/room/unavailable?startTime={t1}&endTime={t2}
        Server->>DuLieu: Tìm phòng bị khóa, bảo trì hoặc có phiên giao khoảng
    end
    Server-->>Client: 200 • Danh sách phòng đúng tiêu chí, có thể rỗng
    Client-->>NguoiDung: Hiển thị kết quả tại thời điểm hoặc khoảng đã chọn
```

Hai route mới dùng available/unavailable. Các route hiện có là /api/room/availability và /api/room/unavailability, đều bắt buộc startTime và endTime. Thiếu một mốc hoặc t1 ≥ t2 trả 400. Hai phòng dùng nối tiếp đúng mốc kết thúc không bị tính trùng.

<a id="ops-10"></a>

## OPS-10 · Tra nhân viên rảnh hoặc bận theo thời gian

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Tra hiện tại xét ca WORKING. Tra theo khoảng xét ca SCHEDULED hoặc WORKING bao phủ cả khoảng và phân công có thời gian giao nhau.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn Rảnh/Bận và chế độ tra cứu
    alt Xem tại thời điểm hiện tại
        Client->>Server: GET /api/staff/available hoặc /api/staff/unavailable
        Server->>Server: Lấy giờ server tại lúc nhận yêu cầu
        Server->>DuLieu: Xét ca đang làm và phiên chiếm nhân viên tại thời điểm đó
    else Tra rảnh theo khoảng yêu cầu
        Client->>Server: GET /api/staff/available?startTime={t1}&endTime={t2}
        Server->>DuLieu: Lọc nhân viên ACTIVE, có ca bao phủ và không trùng phân công
    else Tra bận theo khoảng yêu cầu
        Client->>Server: GET /api/staff/unavailable?startTime={t1}&endTime={t2}
        Server->>DuLieu: Tìm phân công đang hiệu lực giao khoảng đã chọn
    end
    Server-->>Client: 200 • Danh sách nhân viên, có thể rỗng / 400 sai khoảng
    Client-->>NguoiDung: Hiển thị nhân viên và điều kiện tra cứu
```

Hai route cũ /api/staff/availability và /api/staff/unavailability yêu cầu cả hai mốc và trả 404 khi rỗng. Hợp đồng mới trả 200 với danh sách rỗng. Bận nghĩa là có phân công xung đột, còn ngoài ca là lý do không đủ điều kiện nhận việc. Thiếu một mốc hoặc khoảng sai trả 400.

<a id="ops-11"></a>

## OPS-11 · Tạo nhóm phiên dịch vụ trong Operation

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Khi bàn giao booking đã tiếp nhận, tạo một phiên WAITING cho mỗi dòng dịch vụ còn hiệu lực. Cả nhóm được ghi trong một giao dịch để danh sách tại quầy luôn có đủ phiên cần theo dõi.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Booking as Module Booking
    NguoiDung->>Client: Bố trí phòng và giờ cho các dòng dịch vụ
    Client->>Server: POST /api/service-sessions (bookingId, items)
    Server->>Booking: Khóa lịch, lấy các dòng còn hiệu lực và kiểm tra CONFIRMED
    Booking-->>Server: Tập dòng dịch vụ cần bàn giao
    alt Gửi lại đúng nhóm đã tạo
        Server-->>Client: 200 • Các mã phiên đã có
    else Khớp đủ dòng và phòng còn khả dụng
        Server->>Server: Tạo mỗi phiên với bookingId và bookingDetailId
        Server->>Booking: Ghi nhận thời điểm bàn giao và cố định danh sách dòng
        Booking-->>Server: Đã ghi trong cùng giao dịch
        Server-->>Client: 201 • Toàn bộ nhóm phiên WAITING
    else Thiếu dòng, sai booking hoặc trùng phòng
        Server-->>Client: 409 • Hủy giao dịch, chưa tạo phiên
    end
    Client-->>NguoiDung: Hiển thị nhóm phiên hoặc cho bố trí lại
```

Body đề xuất có bookingId và items gồm bookingDetailId, roomId, expectedStartAt, expectedFinishAt. Operation lưu booking_id trực tiếp trên mọi phiên. FK trỏ tới booking.booking(id) và FK ghép bảo đảm dòng dịch vụ thuộc đúng booking. Kiểm tra phòng và ghi mốc bàn giao cùng giao dịch với Booking Contract.

<a id="ops-12"></a>

## OPS-12 · Điều chỉnh phiên dịch vụ

**Quy trình con · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Đổi phòng hoặc khoảng giờ dự kiến khi phiên còn WAITING. Đổi nhân viên thuộc quy trình điều chỉnh phân công.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn phiên và sửa phòng hoặc giờ
    Client->>Server: PUT /api/service-sessions/{id}
    Server->>DuLieu: Đọc phiên, các phân công và lịch giao khoảng
    alt WAITING và mọi tài nguyên còn khả dụng
        Server->>DuLieu: Lưu phòng và khoảng giờ mới
        Server-->>Client: 200 • Chi tiết phiên sau cập nhật
    else Đã bắt đầu, hết hiệu lực hoặc có xung đột
        Server-->>Client: 409 • Lý do chưa thể lưu
    end
    Client-->>NguoiDung: Hiển thị phiên mới hoặc đề nghị bố trí lại
```

Operation kiểm tra lại khả dụng phòng và từng người đã nhận việc khi ghi. Khi có xung đột, toàn bộ phiên giữ cấu hình cũ. Việc đổi phòng hoặc giờ không làm phát sinh phiên thứ hai.

<a id="ops-13"></a>

## OPS-13 · Hủy phiên dịch vụ

**Quy trình con · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Hủy riêng một phiên WAITING khi bỏ dịch vụ. Phòng và người thực hiện được giải phóng. Booking giữ trạng thái hiện tại cho tới khi quầy chốt.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Booking as Module Booking
    NguoiDung->>Client: Chọn phiên và xác nhận bỏ dịch vụ
    Client->>Server: DELETE /api/service-sessions/{id}
    alt Phiên WAITING
        Server->>Server: Ghi CANCELLED và hủy các phân công còn hiệu lực
        Server->>Booking: Đặt dòng dịch vụ tương ứng thành CANCELLED
        Booking-->>Server: Đã cập nhật dòng dịch vụ
        Server-->>Client: 204 • Đã hủy phiên
    else Phiên đã hủy
        Server-->>Client: 204 • Đã xử lý trước đó
    else Phiên đã làm hoặc đã hoàn thành
        Server-->>Client: 409 • Phiên đã qua bước cho phép hủy
    end
    Client-->>NguoiDung: Làm mới danh sách phiên
```

Operation cập nhật phiên, phân công và dòng booking qua contract trong cùng giao dịch. Khi đủ tập dòng bàn giao và mọi phiên đã kết thúc, OPS-22 mở BOOK-06 rồi BILL-01 nếu có dịch vụ hoàn thành. Nếu toàn bộ đều hủy, quầy chỉ dùng BOOK-04.

<a id="ops-14"></a>

## OPS-14 · Phân công và tự gửi thông báo lần đầu

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý phân công nhân viên

Khi ghi phân công thành công, server tự gọi nghiệp vụ thông báo đúng một lần cho nhân viên nhận việc. WPF chỉ gửi lệnh phân công.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant ThongBao as Thông báo nội bộ
    NguoiDung->>Client: Chọn phiên WAITING và nhân viên đủ điều kiện
    Client->>Server: POST /api/service-sessions/{id}/assignments (staffId)
    Server->>Server: Kiểm tra ca, xung đột và quyền phân công
    alt Phân công mới
        Server->>Server: Tạo assignment cho nhân viên
        Server->>ThongBao: Tạo thông báo giao việc cho assignment mới
        ThongBao-->>Server: Thông báo chưa đọc đã ghi
        Server-->>Client: 201 • Phân công đã lưu và đã tạo thông báo
    else Gửi lại cùng yêu cầu đã thành công
        Server-->>Client: 200 • Phân công đã có
    else Nhân viên bận hoặc phiên không cho phân công
        Server-->>Client: 409 • Không ghi phân công
    end
    Client-->>NguoiDung: Hiển thị người nhận hoặc lý do xung đột
```

Ghi phân công và thông báo in-app trong cùng giao dịch, với khóa duy nhất theo sự kiện giao việc. Dùng lại cùng nghiệp vụ thông báo khi nhắc thủ công ở OPS-17. Mỗi lần đổi người là một sự kiện giao việc mới.

<a id="ops-15"></a>

## OPS-15 · Điều chỉnh phân công

**Quy trình con · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Quầy đổi người phụ trách khi phiên còn WAITING. Người mới cần thuộc ca phù hợp và rảnh trong toàn bộ khoảng dự kiến.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant ThongBao as Thông báo nội bộ
    NguoiDung->>Client: Chọn phân công và nhân viên thay thế
    Client->>Server: PUT /api/service-sessions/{id}/assignments/{assignmentId}
    Server->>Server: Kiểm tra phiên, liên kết assignment, ca và xung đột
    alt Người mới hợp lệ và khác người cũ
        Server->>Server: Cập nhật phân công, ghi lịch sử đổi người
        Server->>ThongBao: Ghi thông báo giao việc một lần cho người mới
        ThongBao-->>Server: Đã ghi cho đúng người nhận
        Server-->>Client: 200 • Phân công đã đổi
    else Cùng người đang phụ trách
        Server-->>Client: 200 • Phân công hiện tại
    else Không đủ điều kiện
        Server-->>Client: 409 • Giữ nguyên phân công cũ
    end
    Client-->>NguoiDung: Hiển thị người đang phụ trách
```

Lưu lịch sử đổi người, thu hồi quyền thao tác của người cũ và cập nhật danh sách việc. Chỉ tạo thông báo giao việc cho người mới khi người nhận thay đổi thực sự.

<a id="ops-16"></a>

## OPS-16 · Hủy phân công

**Quy trình con · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Rút một nhân viên khỏi phiên còn WAITING. Nếu phiên hết người phụ trách, cần phân công lại trước khi bắt đầu.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn người cần rút và xác nhận
    Client->>Server: DELETE /api/service-sessions/{id}/assignments/{assignmentId}
    Server->>DuLieu: Kiểm tra phân công thuộc phiên và trạng thái hiện tại
    alt WAITING và phân công đang hiệu lực
        Server->>DuLieu: Hủy phân công và giải phóng nhân viên
        Server-->>Client: 204 • Đã hủy
    else Phân công đã hủy
        Server-->>Client: 204 • Đã xử lý
    else Phiên đã bắt đầu hoặc kết thúc
        Server-->>Client: 409 • Phiên đã khóa phân công
    end
    Client-->>NguoiDung: Hiển thị danh sách nhân viên còn được giao
```

Operation vô hiệu hóa phân công và liên kết công việc của người cũ, lưu lịch sử ai đã từng được giao. Nhân viên mở thông báo cũ sẽ được kiểm tra lại quyền theo phân công hiện tại.

<a id="ops-17"></a>

## OPS-17 · Nhắc việc và nhận thông báo tự động

**Quy trình con · API đề xuất**  
**Người tham gia** — Nhân sự có quyền trên phiên gửi nhắc. Nhân viên được giao nhận thông báo.

Lễ tân, quản lý hoặc nhân viên được ủy quyền trên phiên có thể gửi nhắc nhiều lần. Người nhận do server xác định từ phân công còn hiệu lực.

```mermaid
sequenceDiagram
    actor NguoiDung as Người gửi nhắc
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant ClientKhac as WPF nhân viên
    actor NhanVien as Nhân viên
    opt Người có quyền chủ động nhắc việc
        NguoiDung->>Client: Chọn phân công và nhấn Gửi nhắc
        Client->>Server: POST /api/service-sessions/{id}/notifications (assignmentId)
        Server->>Server: Kiểm tra quyền, phiên WAITING và khóa gửi lại
        Server-->>Client: 201 • Thông báo nhắc mới / 200 đã ghi cùng khóa / 403 sai quyền
        Client-->>NguoiDung: Hiển thị kết quả gửi
    end
    par Nhận thông báo nền
        loop Ngay sau đăng nhập và mỗi 30 giây khi còn phiên WPF
            ClientKhac->>Server: GET /api/me/notifications
            Server-->>ClientKhac: 200 • Thông báo của staff_id trong JWT
            ClientKhac-->>NhanVien: Cập nhật số chưa đọc và các thông báo mới
        end
    and Xử lý thao tác trên màn hình
        opt Người nhận mở một thông báo đã tải
            NhanVien->>ClientKhac: Chọn công việc
            ClientKhac->>Server: GET /api/service-sessions/{id}
            Server-->>ClientKhac: 200 • Chi tiết phiên / 403 nếu đã thu hồi phân công
            ClientKhac->>Server: PUT /api/me/notifications/{id}/read
            Server-->>ClientKhac: 204 • Đã đánh dấu đã đọc
        end
    end
```

GET chỉ đọc, POST tạo một lần nhắc mới cho mỗi thao tác có chủ ý. WPF dùng lại Idempotency-Key khi thử lại cùng lần nhấn. Đề xuất thăm dò mỗi 30 giây, dừng khi đăng xuất và bỏ qua nhịp nếu request trước chưa xong.

<a id="ops-18"></a>

## OPS-18 · Bắt đầu dịch vụ được giao

**Bắt buộc · API đề xuất**  
**Người tham gia** — Nhân viên được phân công

Nhân viên mở công việc và bắt đầu khi phiên đang WAITING, đã có phòng và đúng phân công.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân viên
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Booking as Module Booking
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở công việc và nhấn Bắt đầu
    Client->>Server: POST /api/service-sessions/{id}/start
    Server->>DuLieu: Đọc phiên, phòng, phân công và người thao tác
    alt Được giao, có phòng, đang WAITING
        Server->>DuLieu: Chuyển IN_PROGRESS và đặt startedAt
        Server->>Booking: Chuyển dòng dịch vụ sang WORKING trong cùng giao dịch
        Booking-->>Server: Đã đồng bộ dòng dịch vụ
        Server-->>Client: 200 • Phiên đang thực hiện và giờ bắt đầu
        Client-->>NguoiDung: Chuyển công việc sang Đang thực hiện
    else Gửi lại cùng lệnh
        Server-->>Client: 200 • Trạng thái và startedAt đã lưu
        Client-->>NguoiDung: Hiển thị thời gian bắt đầu cũ
    else Sai phân công hoặc điều kiện bắt đầu
        Server-->>Client: 403 sai người / 409 sai trạng thái hoặc thiếu phòng
        Client-->>NguoiDung: Hiển thị lý do, tải lại phiên
    end
```

`staff_id` đến từ JWT. Server đặt `startedAt` một lần. Nếu gửi lại cùng lệnh đã thành công, WPF nhận trạng thái hiện tại và giờ bắt đầu đã ghi.

<a id="ops-19"></a>

## OPS-19 · Lễ tân cập nhật ghi chú chăm sóc

**Mở rộng · API đề xuất**  
**Người tham gia** — Lễ tân ghi nhận yêu cầu của khách hoặc nhân viên chăm sóc

Khách và nhân viên phòng báo nội dung cho lễ tân. Lễ tân kiểm tra đúng thú cưng trước khi cập nhật specialNote.

```mermaid
sequenceDiagram
    actor NguoiDung as Lễ tân
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Customer as Module Customer
    NguoiDung->>Client: Chọn thú cưng và kiểm tra nội dung cần ghi
    Client->>Server: PUT /api/pets/{id}/special-note (specialNote)
    Server->>Server: Xác thực JWT và quyền receptionist
    alt Đúng lễ tân và được truy cập hồ sơ
        Server->>Customer: UpdatePetSpecialNote(petId, nội dung, người ghi)
        Customer->>Customer: Kiểm tra pet ACTIVE, độ dài và lưu ghi chú
        Customer-->>Server: Kết quả cập nhật
        Server-->>Client: 200 • Ghi chú mới / 404 thiếu pet / 409 pet ngừng hoạt động
        Client-->>NguoiDung: Hiển thị ghi chú vừa lưu
    else Tài khoản khách hoặc vai trò khác
        Server-->>Client: 403 • Chỉ lễ tân được sửa ghi chú chăm sóc
        Client-->>NguoiDung: Hiển thị hướng dẫn liên hệ lễ tân
    end
```

Customer sở hữu specialNote. Middleware và nghiệp vụ phải kiểm tra role receptionist ở mọi đường ghi, kể cả PUT thú cưng, trường trong body tạo hồ sơ và lệnh cập nhật theo phiên nếu có. Yêu cầu của người không có quyền bị từ chối trước khi ghi bất kỳ trường nào.

<a id="ops-20"></a>

## OPS-20 · Hoàn tất phiên dịch vụ

**Bắt buộc · API đề xuất**  
**Người tham gia** — Nhân viên được phân công

Nhân viên xác nhận xong phần việc của mình. Lễ tân nhìn thấy thay đổi ở lần làm mới màn hình phiên dịch vụ kế tiếp.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân viên
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Booking as Module Booking
    NguoiDung->>Client: Nhấn Hoàn thành trên công việc được giao
    Client->>Server: POST /api/service-sessions/{id}/complete
    alt Đúng phân công và phiên IN_PROGRESS
        Server->>Server: Lưu phiên COMPLETED và finishedAt
        Server->>Booking: Đặt riêng dòng dịch vụ tương ứng thành COMPLETED
        Booking-->>Server: Kết quả cập nhật dòng dịch vụ
        Server-->>Client: 200 • Phiên đã hoàn thành
    else Lặp lại đúng thao tác đã thành công
        Server-->>Client: 200 • Kết quả và thời điểm đã ghi
    else Sai người hoặc sai trạng thái
        Server-->>Client: 403 sai phân công / 409 sai trạng thái
    end
    Client-->>NguoiDung: Hiển thị kết quả công việc
```

Operation ghi finishedAt một lần và cập nhật dòng booking tương ứng qua contract trong cùng giao dịch. Booking cha tiếp tục CONFIRMED đến khi lễ tân bấm nút chốt. Nếu ghi dòng dịch vụ thất bại, hoàn tác giao dịch để client có thể gửi lại.

<a id="ops-21"></a>

## OPS-21 · Nhân viên đăng nhập và nhận danh sách việc

**Bắt buộc · API kết hợp**  
**Người tham gia** — Nhân viên thực hiện dịch vụ

Nhân viên dùng cùng API với quầy. WPF lấy role trả về để mở màn hình công việc và chỉ tải phiên, thông báo của tài khoản đang đăng nhập.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân viên
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Nhập số điện thoại, mật khẩu và nhấn Đăng nhập
    Client->>Server: POST /api/auth/staff/login (phone, password)
    Server->>DuLieu: Tìm hồ sơ nhân viên ACTIVE
    DuLieu-->>Server: Role và hash mật khẩu
    alt Mật khẩu đúng, ghi nhận đăng nhập thành công
        Server->>DuLieu: Ghi last_login_at, nâng cấp hash nếu cần
        DuLieu-->>Server: Đã cập nhật
        Server-->>Client: 200 • accessToken, expiresAtUtc, role, user
        par Tải công việc bằng Bearer
            Client->>Server: [Đề xuất] GET /api/me/service-sessions
            Server-->>Client: 200 • Các phiên được giao
            Client-->>NguoiDung: Hiển thị các phiên được giao
        and Khởi động theo dõi thông báo bằng Bearer
            loop Ngay khi đăng nhập và mỗi 30 giây đến lúc đăng xuất
                Client->>Server: [Đề xuất] GET /api/me/notifications
                Server-->>Client: 200 • Thông báo của nhân viên
                Client-->>NguoiDung: Cập nhật thông báo chưa đọc
            end
        end
    else Yêu cầu sai hoặc xác thực thất bại
        Server-->>Client: 400 biểu mẫu sai / 401 đăng nhập thất bại
        Client-->>NguoiDung: Giữ màn hình đăng nhập
    end
```

Login đã có. Hai API `/api/me/...` đề xuất suy ra người nhận từ `staff_id`. Đây là cùng vòng thăm dò của OPS-17. Lỗi tải công việc được xử lý ở màn hình công việc, độc lập với kết quả xác thực.

<a id="ops-22"></a>

## OPS-22 · Theo dõi phiên dịch vụ, gom theo booking

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Màn hình lấy các phiên hiện tại từ Operation và hiển thị theo bookingId, gồm cả các phiên đã hoàn thành hoặc hủy thuộc cùng nhóm.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở màn hình Phiên dịch vụ tại quầy
    loop Tải ngay và mỗi 15 giây khi màn hình còn mở
        Client->>Server: GET /api/service-sessions/current
        Server->>DuLieu: Lấy nhóm hiện tại và mọi phiên trong từng nhóm
        DuLieu-->>Server: booking_id, phiên, trạng thái, phòng và người thực hiện
        Server->>Server: Đếm WAITING, IN_PROGRESS, COMPLETED, CANCELLED theo booking_id
        Server-->>Client: 200 • Danh sách nhóm và số phiên theo trạng thái
        alt Đủ tập phiên bàn giao, tất cả kết thúc và có ít nhất một phiên COMPLETED
            Client-->>NguoiDung: Mở thao tác chốt hoặc lập tiếp hóa đơn theo trạng thái booking
        else Đủ tập phiên bàn giao, mọi phiên đều CANCELLED
            Client-->>NguoiDung: Báo nhóm đã kết thúc và mở BOOK-04 để hủy booking
        else Còn phiên đang chờ, đang làm hoặc thiếu phiên bàn giao
            Client-->>NguoiDung: Hiển thị các phiên còn việc và khóa nút
        end
    end
```

Quầy lấy cả nhóm chuyển ca/ngày và phân trang theo bookingId trước khi tải phiên. Đối chiếu đủ tập dòng đã bàn giao. Booking CONFIRMED đủ điều kiện mở BOOK-06 rồi BILL-01. Booking đã COMPLETED nhưng chưa có hóa đơn chỉ mở BILL-01. Tra hóa đơn hiệu lực qua contract Billing để đưa nhóm đã lập hóa đơn ra khỏi hàng chờ. Nhóm hủy toàn bộ rời hàng chờ sau BOOK-04. Chỉ báo một lần cho mỗi nhóm sẵn sàng.

<a id="bill-01"></a>

## BILL-01 · Tạo hóa đơn

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Billing lập hóa đơn cho các dòng dịch vụ đã hoàn thành, theo giá đã lưu trên booking. Mỗi booking có tối đa một hóa đơn hiệu lực.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Booking as Module Booking
    participant Resource as Module Resource
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Lập hóa đơn cho booking đã chốt
    Client->>Server: POST /api/invoices (bookingId)
    Server->>DuLieu: Tìm hóa đơn hiệu lực theo bookingId
    alt Đã có hóa đơn hiệu lực
        Server-->>Client: 200 • Trả hóa đơn hiện có
    else Chưa có hóa đơn
        Server->>Booking: Lấy trạng thái và các dòng COMPLETED cùng đơn giá đã chốt
        Booking-->>Server: Dữ liệu lập hóa đơn
        alt Booking COMPLETED và có dòng đủ điều kiện
            Server->>Resource: Tra mô tả dịch vụ cho từng dòng
            Resource-->>Server: Mô tả cho snapshot
            Server->>DuLieu: Lưu hóa đơn UNPAID và các dòng trong một giao dịch
            Server-->>Client: 201 hóa đơn mới hoặc 200 nếu quầy khác đã tạo
        else Chưa chốt hoặc thiếu dữ liệu tính tiền
            Server-->>Client: 409 • Chưa thể lập hóa đơn
        end
    end
    Client-->>NguoiDung: Mở hóa đơn hoặc hiển thị lý do
```

**Gọi cùng [BOOK-06](#book-06).** Khi chốt nhóm, quầy gọi PATCH `completed` thành công rồi mới POST hóa đơn. Nếu POST lỗi, booking vẫn COMPLETED và có thể thử lại BILL-01. Loại dòng CANCELLED khỏi tiền dịch vụ. Khóa duy nhất theo bookingId cho hóa đơn còn hiệu lực bảo vệ cả khi hai quầy cùng bấm.

<a id="bill-02"></a>

## BILL-02 · Áp dụng giảm giá

**Mở rộng · API đề xuất**  
**Người tham gia** — Quản lý

Quản lý áp dụng số tiền giảm trên hóa đơn UNPAID trước lúc thu tiền. Mức giảm mặc định bằng 0.

```mermaid
sequenceDiagram
    actor NguoiDung as Quản lý
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở hóa đơn và nhập số tiền giảm
    Client->>Server: PUT /api/invoices/{id}/discount (discountAmount)
    Server->>Server: Kiểm tra quyền quản lý
    Server->>DuLieu: Đọc trạng thái và subtotal hiện tại
    alt UNPAID và mức giảm từ 0 đến subtotal
        Server->>DuLieu: Ghi mức giảm và tính lại totalAmount
        Server-->>Client: 200 • Tạm tính, giảm giá và tổng phải thu
        Client-->>NguoiDung: Hiển thị số tiền mới trước khi thanh toán
    else Sai quyền, số tiền hoặc trạng thái
        Server-->>Client: 403 sai quyền / 422 mức giảm sai / 409 hóa đơn đã đóng
        Client-->>NguoiDung: Hiển thị lý do, giữ tổng tiền hiện có
    end
```

Server tính `totalAmount = subtotal - discountAmount`. Chọn giảm theo số tiền, đúng trường `DiscountAmount` của model. Đặt lại cùng mức giảm không làm giảm thêm lần nữa.

<a id="bill-03"></a>

## BILL-03 · Ghi nhận thanh toán

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Quầy ghi nhận toàn bộ số tiền đã nhận bằng tiền mặt hoặc chuyển khoản. Billing gọi contract Booking với mã booking lấy từ hóa đơn. Máy chủ trả biên nhận sau khi cả hai module đã ghi thành công.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant Booking as Module Booking
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Mở hóa đơn cần thu
    Client->>Server: GET /api/invoices/{id}
    Server-->>Client: 200 • Hóa đơn và tổng phải thu
    NguoiDung->>Client: Chọn phương thức, nhập số tiền và xác nhận đã nhận
    Client->>Server: POST /api/invoices/{id}/payments + Idempotency-Key
    Server->>DuLieu: Billing mở giao dịch, khóa hóa đơn và tra khóa chống lặp
    alt Cùng khóa và nội dung với giao dịch đã commit
        Server-->>Client: 200 • Biên nhận đã ghi
    else Hóa đơn UNPAID, khoản thu mới bằng tổng phải thu
        Server->>DuLieu: Ghi Payment SUCCESS, paidAt và Invoice PAID
        Server->>Booking: Contract PATCH /api/booking/{bookingId} {"status":"paid"}
        Booking->>DuLieu: Chuyển COMPLETED sang PAID trong cùng giao dịch
        alt Booking đã ghi PAID cho đúng lần thu
            Booking-->>Server: Đã cập nhật booking
            Server->>DuLieu: Commit giao dịch
            Server-->>Client: 201 • Biên nhận, hóa đơn và booking PAID
        else Booking từ chối hoặc gặp lỗi lưu dữ liệu
            Server->>DuLieu: Rollback cả lần ghi nhận
            Server-->>Client: 409 sai trạng thái / 503 lỗi tạm thời
        end
    else Sai số tiền, xung đột khóa hoặc hóa đơn đã đóng
        Server-->>Client: 422 sai số tiền / 409 xung đột
    end
    Client-->>NguoiDung: Hiển thị biên nhận hoặc kết quả để đối soát
```

Quầy gửi `amount`, `paymentMethod` và `transactionReference` nếu có. Billing truyền `invoice.bookingId` vào nghiệp vụ PATCH `/api/booking/{id}` với `{"status":"paid"}`. Nếu mất phản hồi hoặc gặp lỗi tạm thời, đối soát rồi gửi lại cùng `Idempotency-Key` và nội dung để lấy biên nhận. Lần gửi lại chỉ ghi nhận số tiền đã nhận.

<a id="bill-04"></a>

## BILL-04 · Tìm và xem hóa đơn

**Bắt buộc · API đề xuất**  
**Người tham gia** — Lễ tân, quản lý

Tìm theo mã booking, trạng thái hoặc thời gian. Chi tiết cho biết dịch vụ đã tính tiền, mức giảm và các khoản đã thu.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as Client WPF
    participant Server as Server PetSpa
    participant DuLieu as PostgreSQL
    NguoiDung->>Client: Chọn tiêu chí rồi nhấn Tìm hóa đơn
    Client->>Server: GET /api/invoices?bookingId={id}&status={status}
    Server->>DuLieu: Tìm hóa đơn theo bộ lọc và phân trang
    Server-->>Client: 200 • Trang kết quả
    alt Có hóa đơn
        Client-->>NguoiDung: Hiển thị danh sách
        NguoiDung->>Client: Chọn một hóa đơn
        Client->>Server: GET /api/invoices/{id}
        Server->>DuLieu: Đọc invoice, invoice_item và payment
        Server-->>Client: 200 • Dòng tiền và thanh toán / 404 nếu không còn hồ sơ
        Client-->>NguoiDung: Hiển thị chi tiết, cho phép in khi tải thành công
    else Danh sách rỗng
        Client-->>NguoiDung: Hiển thị chưa tìm thấy và giữ bộ lọc
    end
```

WPF dùng dữ liệu snapshot từ hóa đơn để hiển thị và in. API danh sách trả kết quả phân trang, gồm mã hóa đơn, mã booking, tổng tiền và trạng thái.

<a id="e2e-01"></a>

## E2E-01 · Từ tiếp nhận đến chốt nhóm phiên và thu tiền

**Tổng hợp · API kết hợp**  
**Người tham gia** — Lễ tân, quản lý và nhân viên thực hiện dịch vụ

Lễ tân tiếp nhận, bố trí phiên và chốt nhóm. Nhân viên phòng nhận việc, bắt đầu và kết thúc phiên được giao.

```mermaid
sequenceDiagram
    actor NguoiDung as Nhân sự tại quầy
    participant Client as WPF tại quầy
    participant Server as Server PetSpa
    participant Booking as Module Booking
    participant ClientKhac as WPF nhân viên
    actor NhanVien as Nhân viên
    NguoiDung->>Client: Tạo booking cho khách và thú cưng
    Client->>Server: POST /api/bookings
    Server-->>Client: 201 • Booking PENDING
    NguoiDung->>Client: Khi khách đến, xác nhận tiếp nhận
    Client->>Server: PATCH /api/booking/{id} với body {"status":"confirmed"}
    Server-->>Client: 200 • Booking CONFIRMED
    NguoiDung->>Client: Bố trí phiên rồi phân công nhân viên
    Client->>Server: Tạo nhóm phiên và phân công (OPS-11, OPS-14)
    Server-->>Client: Nhóm WAITING và thông báo giao việc
    NhanVien->>ClientKhac: Mở công việc, bắt đầu rồi hoàn tất phiên
    ClientKhac->>Server: Cập nhật phiên được giao (OPS-18, OPS-20)
    Server-->>ClientKhac: Phiên COMPLETED
    loop Màn hình quầy mở, mỗi 15 giây
        Client->>Server: GET /api/service-sessions/current
        Server-->>Client: Các nhóm phiên theo bookingId
    end
    Client-->>NguoiDung: Báo nhóm đã xong và bật nút Chốt và tạo hóa đơn
    NguoiDung->>Client: Chốt trạng thái completed và tạo hóa đơn
    Client->>Server: PATCH /api/booking/{id} với body {"status":"completed"}
    Server-->>Client: 200 • Booking COMPLETED
    Client->>Server: POST /api/invoices (bookingId của nhóm đã chốt)
    Server-->>Client: 201 hóa đơn mới / 200 hóa đơn đã có
    Client-->>NguoiDung: Hiển thị hóa đơn và số tiền cần thu
    NguoiDung->>Client: Xác nhận đã nhận tiền
    Client->>Server: POST /api/invoices/{id}/payments
    Server->>Booking: Billing gọi contract PATCH /api/booking/{bookingId} {"status":"paid"}
    Booking-->>Server: Booking PAID
    Server-->>Client: 201 • Đã commit, trả biên nhận và trạng thái PAID
```

Màn hình quầy lấy phiên từ Operation theo bookingId. Khi mọi phiên kết thúc và có ít nhất một dịch vụ hoàn thành, WPF báo cho lễ tân và chờ thao tác chủ động. Khi nhóm phiên hoàn tất, quầy gọi BOOK-06 để chốt `COMPLETED` rồi BILL-01 để tạo hóa đơn; đây là hai quy trình riêng được gọi nối tiếp. Sau khi có hóa đơn, thu tiền theo BILL-03; nếu thanh toán thành công thì Billing tiếp tục gọi contract sang Booking để cập nhật trạng thái `PAID`.

## Nguồn đối chiếu

- [UseCase.docx](docs/Base/UseCase.docx) — danh mục, actor, đặc tả, BR01–BR14 và kịch bản demo.
- [Program.cs](src/PetSpa.Api/Program.cs), [AuthEndpoints.cs](src/PetSpa.Modules/Auth/Endpoints/AuthEndpoints.cs), [LoginService.cs](src/PetSpa.Modules/Auth/Application/LoginService.cs) và [chính sách quyền](src/PetSpa.Modules/Auth/Infrastructure/Authorization/AuthorizationRegistration.cs) — route đã map, kết quả đăng nhập và role.
- [Customer](src/PetSpa.Modules/Customer/Endpoints), [Resource](src/PetSpa.Modules/Resource/Endpoints), [BookingService.cs](src/PetSpa.Modules/Booking/Application/Services/BookingService.cs), [OperationModule.cs](src/PetSpa.Modules/Operation/OperationModule.cs) và [Billing](src/PetSpa.Modules/Billing) — đối chiếu endpoint, điều kiện và trạng thái hiện hành.
- [Migration liên kết phiên với booking](src/PetSpa.Modules/Operation/Infrastructure/Persistence/Migrations/20261118155509_LinkServiceSessionToBooking.cs) — thêm khóa ngoại vật lý, điền booking_id cho dữ liệu cũ và chặn sai cặp booking/dòng dịch vụ. Các route mới, mốc bàn giao và thao tác chốt hai bước trong sơ đồ là hợp đồng đề xuất.
- [Enum BookingStatus](src/PetSpa.Modules/Booking/Domain/Enums/BookingStatus.cs) và [cấu hình Booking](src/PetSpa.Modules/Booking/Infrastructure/Persistence/Configurations/BookingConfiguration.cs) — repo chưa khai báo PAID, CHECK còn cho phép CHECKED_IN. Kiểm tra catalog PostgreSQL ở từng môi trường trước khi đồng bộ migration.
