# 🌿 HỆ THỐNG ĐẶT PHÒNG TRỰC TUYẾN KIM LONG ECOSTAY

---

## 🎓 THÔNG TIN HỌC VIÊN & ĐỀ TÀI

* **Họ và tên sinh viên:** **Trần Viết Mạnh Cường**
* **Mã sinh viên (MSV):** **23K4080003**
* **Ngành đào tạo:** **Tin học kinh tế**
* **Khoa:** **Hệ thống thông tin kinh tế**
* **Học phần:** **Lập trình ứng dụng Web**
* **Tên đề tài:** **Nền tảng đặt phòng trực tuyến Kim Long EcoStay (Mô hình Homestay Nhà Vườn sinh thái Kim Long - Cố Đô Huế)**
* **Repository GitHub:** [https://github.com/ManhCuong-Code/KimLongEcoStay](https://github.com/ManhCuong-Code/KimLongEcoStay)

---

## 📖 1. TỔNG QUAN ĐỀ TÀI

### 1.1. Bối cảnh & Vấn đề thực tế
Kim Long (TP. Huế) nổi tiếng với hệ thống nhà vườn di sản truyền thống và không gian xanh ven bờ sông Hương. Hiện nay, đa số các chủ homestay nhà vườn tại Huế đều phụ thuộc hoàn toàn vào các sàn đại lý du lịch trực tuyến (OTA) như Booking.com hay Agoda, dẫn tới nhiều bất cập:
* **Chi phí hoa hồng cao:** Bị cắt phế từ **15% đến 25%** tổng doanh thu mỗi tháng.
* **Giam giữ dòng tiền:** Tiền phòng bị sàn trung gian giữ từ 15 đến 30 ngày mới giải ngân.
* **Mất dữ liệu khách hàng:** Không có thông tin liên hệ trực tiếp để tư vấn và chăm sóc khách quen.

### 1.2. Mục tiêu dự án
Xây dựng một nền tảng Web đặt phòng trực tuyến độc lập giúp cơ sở Kim Long EcoStay:
1. **Tự chủ kinh doanh:** Giữ trọn 100% doanh thu, tiền cọc 50% thanh toán trực tiếp qua mã **VietQR** vào tài khoản ngân hàng.
2. **Kiểm soát buồng phòng tuyệt đối:** Loại bỏ 100% rủi ro trùng phòng (Overbooking) nhờ thuật toán quản lý theo từng đêm lưu trú (`NightDate`).
3. **Chính sách giá & hủy cọc linh hoạt:** Áp dụng giá theo ngày trong tuần (Dynamic Pricing) và luật hủy phòng trước 48 giờ minh bạch.

---

## 🛠️ 2. KIẾN TRÚC & CÔNG NGHỆ ÁP DỤNG

Hệ thống được thiết kế theo chuẩn **Clean Architecture 4 tầng** (Onion Architecture), tách biệt hoàn toàn giữa Core Nghiệp Vụ, Use Cases, Hạ tầng CSDL và Giao diện người dùng:

| Tầng Kiến Trúc | Dự án trong Solution | Công Nghệ & Vai Trò |
|---|---|---|
| **1. Core Business** | `KimLongEcoStay.CoreBusiness` | Domain Entities (`Room`, `Booking`, `Guest`, `RatePlan`...) & Business Rules (Độc lập, 0 tham chiếu). |
| **2. Use Cases** | `KimLongEcoStay.UseCases` | Chứa Application Services & Plugin Interfaces (`IRoomRepository`, `IBookingRepository`...). |
| **3. Infrastructure / Plugins** | `Plugins/KimLongEcoStay.DataStore.SQL.Dapper`<br>`Plugins/KimLongEcoStay.ShoppingCart.LocalStorage`<br>`Plugins/KimLongEcoStay.StateStore.DI` | Cài đặt truy xuất CSDL SQL Server với **Dapper ORM**, lưu giao dịch Header-Line trong 1 Transaction, quản lý State giỏ hàng. |
| **4. Presentation / Web Host** | `KimLongEcoStay.Web`<br>`KimLongEcoStay.Web.CustomerPortal`<br>`KimLongEcoStay.Web.AdminPortal`<br>`KimLongEcoStay.Web.Common` | Giao diện **Blazor Web App (.NET 10 LTS)** với tương tác Server Interactive SPA, Cookie Authentication & Authorization. |

---

## 📊 3. MÔ HÌNH HÓA HỆ THỐNG BẰNG UML (UNIFIED MODELING LANGUAGE)

### 3.1. Sơ Đồ Ca Sử Dụng Tổng Thể (Use Case Diagram)

```mermaid
flowchart LR
    subgraph Actors["Tác Nhân Hệ Thống"]
        Guest(("Khách Hàng<br>(Guest)"))
        Host(("Chủ Homestay / Lễ Tân<br>(Host/Admin)"))
        Bank(("Hệ Thống VietQR<br>(External)"))
    end

    subgraph ClientPortal["Phân Hệ Khách Hàng (Customer Portal)"]
        UC01(["UC01: Tìm kiếm buồng phòng theo ngày"])
        UC02(["UC02: Xem chi tiết phòng & giá từng đêm"])
        UC03(["UC03: Chọn phòng vào Giỏ tạm (BookingCartState)"])
        UC04(["UC04: Đặt phòng trực tuyến (Place Booking)"])
        UC05(["UC05: Kiểm tra phòng trống [LUẬT 1: Overbooking]"])
        UC06(["UC06: Tự động tính tiền = Σ giá đêm [LUẬT 2]"])
        UC07(["UC07: Thanh toán cọc 50% qua VietQR"])
    end

    subgraph AdminPortal["Phân Hệ Quản Trị & Vận Hành (Host Portal)"]
        UC08(["UC08: Đăng nhập hệ thống (Cookie Auth)"])
        UC09(["UC09: Quản lý buồng phòng (Room & RoomType)"])
        UC10(["UC10: Quản lý bảng giá linh hoạt (RatePlan)"])
        UC11(["UC11: Tiếp đón khách nhận phòng (Check-in 14h)"])
        UC12(["UC12: Xử lý khách trả phòng (Check-out 12h)"])
        UC13(["UC13: Quản lý danh sách đơn & xác nhận cọc"])
    end

    Guest --> UC01
    Guest --> UC02
    Guest --> UC03
    Guest --> UC04

    UC04 -.->|include| UC05
    UC04 -.->|include| UC06
    UC04 -.->|include| UC07
    UC07 --> Bank

    Host --> UC08
    Host --> UC09
    Host --> UC10
    Host --> UC11
    Host --> UC12
    Host --> UC13
```

---

### 3.2. Sơ Đồ Lớp 4 Tầng (Class Diagram - Clean Architecture)

```mermaid
classDiagram
    direction TB

    namespace CoreBusiness {
        class RoomType {
            +int Id
            +string TypeName
            +string Description
            +int MaxGuests
            +decimal BasePrice
            +string Amenities
            +string ImageUrl
        }

        class Room {
            +int Id
            +string RoomNumber
            +int RoomTypeId
            +RoomStatus Status
            +bool IsActive
            +CanCheckIn(date) bool
            +CanCheckOut(date) bool
        }

        class RatePlan {
            +int Id
            +int RoomTypeId
            +DayOfWeek DayOfWeek
            +decimal Price
            +bool IsWeekend
            +decimal HolidaySurcharge
        }

        class Guest {
            +int Id
            +string FullName
            +string PhoneNumber
            +string Email
            +string IdentityCard
        }

        class Booking {
            +int Id
            +string BookingCode
            +int GuestId
            +DateTime CheckInDate
            +DateTime CheckOutDate
            +int TotalGuests
            +decimal TotalAmount
            +decimal DepositAmount
            +BookingStatus Status
            +DateTime CreatedAt
            +DateTime? CancelledAt
            +decimal? CancellationFee
        }

        class BookingNight {
            +int Id
            +int BookingId
            +int RoomId
            +DateTime NightDate
            +decimal Price
        }

        class BookingService {
            +ValidateBooking(Booking booking) bool
            +ValidateOverbooking(int roomId, DateTime in, DateTime out) bool
        }
    }

    namespace UseCases {
        class IRoomRepository {
            <<Interface>>
            +GetProduct(int id) Product
            +GetProducts(string filter) IEnumerable~Product~
            +GetAvailableRooms(int typeId, DateTime in, DateTime out) IEnumerable~Room~
        }

        class IBookingRepository {
            <<Interface>>
            +CreateOrder(Order order) int
            +GetBookingByCode(string code) Booking
        }

        class ISearchRoomUseCase {
            <<Interface>>
            +Execute(string filter) IEnumerable~Product~
        }

        class IPlaceBookingUseCase {
            <<Interface>>
            +Execute(Order order) Task~string~
        }
    }

    namespace Infrastructure_Dapper {
        class DapperRoomRepository {
            -IDataAccess _dataAccess
            +GetProduct(int id) Product
            +GetProducts(string filter) IEnumerable~Product~
        }

        class DapperBookingRepository {
            -IDataAccess _dataAccess
            +CreateOrder(Order order) int
            +GetBookingByCode(string code) Booking
        }
    }

    RoomType "1" *-- "0..*" Room : Contains
    RoomType "1" *-- "0..*" RatePlan : Prices
    Guest "1" o-- "0..*" Booking : Places
    Booking "1" *-- "1..*" BookingNight : Header-LineItem
    Room "1" o-- "0..*" BookingNight : Assigned

    BookingService ..> Booking : Validates
    DapperRoomRepository ..|> IRoomRepository : Implements
    DapperBookingRepository ..|> IBookingRepository : Implements
    IPlaceBookingUseCase ..> IBookingRepository : Depends
```

---

### 3.3. Sơ Đồ Tuần Tự Luồng Giao Dịch Đặt Phòng (Sequence Diagram - End to End Booking)

Minh họa quy trình đặt phòng, kiểm tra 2 luật nghiệp vụ và lưu giao dịch Header - LineItem trong **cùng 1 Transaction**:

```mermaid
sequenceDiagram
    autonumber
    actor User as Khách Hàng (Guest)
    participant UI as CheckoutComponent (Blazor)
    participant Cart as BookingCartState (Scoped)
    participant UC as PlaceBookingUseCase (UseCases)
    participant Rule as BookingService (Core Rules)
    participant Repo as DapperBookingRepository (Plugin SQL)
    participant DB as SQL Server (KimLongEcoStayDB)

    User->>UI: Điền thông tin (Họ tên, SĐT) & Nhấn "Xác Nhận Đặt Phòng"
    UI->>Cart: Lấy thông tin phòng & ngày lưu trú đã chọn
    Cart-->>UI: Trả về: Phòng KL-101, Check-in: 15/10, Check-out: 17/10 (2 đêm)

    UI->>UC: Execute(bookingOrder)
    
    Note over UC,Rule: 1. Kiểm tra Luật 1 (Chống Overbooking)
    UC->>Rule: ValidateOverbooking(roomId=101, 15/10, 17/10)
    Rule-->>UC: Hợp lệ (Không có khách ở trùng đêm)

    Note over UC,Rule: 2. Kiểm tra Luật 2 (Tính tiền từng đêm: Đêm 1: 700k + Đêm 2: 850k = 1.550.000đ)
    UC->>Rule: ValidateCreateOrder(order)
    Rule-->>UC: Dữ liệu đơn & tiền cọc 50% (775.000đ) hợp lệ

    Note over UC,DB: 3. Ghi Header - LineItem trong 1 Transaction (K2.2)
    UC->>Repo: CreateOrder(bookingOrder)
    Repo->>DB: BEGIN TRANSACTION
    Repo->>DB: INSERT INTO Bookings (Header: Mã đơn, Khách, Tổng tiền, Cọc...)
    DB-->>Repo: Sinh mới BookingId = #102
    
    loop Từng đêm lưu trú (LineItems)
        Repo->>DB: INSERT INTO BookingNights (LineItem: BookingId, RoomId, NightDate, Price)
    end
    
    Repo->>DB: COMMIT TRANSACTION
    DB-->>Repo: Xác nhận lưu thành công
    Repo-->>UC: Trả về Mã đơn #BK20261015-01
    UC-->>UI: Trả về kết quả thành công
    UI->>Cart: EmptyAsync() (Xóa giỏ chọn tạm)
    UI-->>User: Hiển thị màn hình thành công kèm Mã VietQR chuyển cọc 50%!
```

---

### 3.4. Sơ Đồ Tuần Tự Xác Thực & Phân Quyền (Sequence Diagram - Authentication & RBAC Authorization)

Minh họa cơ chế phân quyền chặt chẽ: Khách vãng lai tự do đặt phòng không cần tài khoản, còn Nhân viên / Quản trị bắt buộc đăng nhập để truy cập `/admin`:

```mermaid
sequenceDiagram
    autonumber
    actor User as Người Dùng (Khách / Nhân Viên)
    participant UI as Login / TopNavbar (Blazor)
    participant AuthEP as Minimal API (/authenticate)
    participant UserRepo as UserRepository (Dapper)
    participant DB as SQL Server (KimLongEcoStayDB)
    participant Cookie as CookieAuthenticationHandler
    participant AdminUI as AdminPortal ([Authorize])

    alt Luồng 1: Khách vãng lai truy cập đặt phòng
        User->>UI: Truy cập "/" hoặc "/products"
        UI-->>User: Cho phép tự do xem phòng & đặt phòng (Không yêu cầu đăng nhập)
    else Luồng 2: Nhân viên đăng nhập để kiểm tra hệ thống
        User->>UI: Nhập tài khoản "letan" / "123456" tại "/login"
        UI->>AuthEP: POST /authenticate (Username, Password)
        AuthEP->>UserRepo: AuthenticateAsync("letan", "123456")
        UserRepo->>DB: SELECT * FROM dbo.AppUsers WHERE Username='letan' AND IsActive=1
        DB-->>UserRepo: Trả về thông tin: FullName="Lễ Tân Homestay", Role="Staff"
        UserRepo-->>AuthEP: Trả về AppUser Entity
        AuthEP->>Cookie: SignInAsync (Claims: Name="Lễ Tân", Role="Staff")
        Cookie-->>UI: Cấp phát Cookie "KimLongEcoStay.Auth.Cookie"
        AuthEP-->>UI: Redirect 302 -> "/admin"
        UI->>AdminUI: Điều hướng vào "/admin"
        AdminUI-->>User: Mở giao diện Quản Trị: Kiểm tra Đơn & Buồng phòng thành công!
    else Luồng 3: Khách vãng lai hoặc tài khoản không có quyền cố truy cập "/admin"
        User->>AdminUI: Truy cập trực tiếp URL "/admin"
        AdminUI->>Cookie: Kiểm tra quyền hạn (Require Role: Staff/Admin)
        Cookie-->>AdminUI: Từ chối (Chưa đăng nhập hoặc không đủ quyền)
        AdminUI-->>User: Kích hoạt AuthorizeRouteView -> Hiển thị cảnh báo & Yêu cầu đăng nhập Nhân viên
    end
```

---

### 3.5. Sơ Đồ Tuần Tự Nhân Viên Kiểm Tra & Duyệt Đơn Đặt Phòng (Sequence Diagram - Staff Order Processing & Room Inspection)

Minh họa thao tác của nhân viên lễ tân khi đăng nhập hệ thống để kiểm tra đơn đặt phòng và bảng trạng thái buồng phòng:

```mermaid
sequenceDiagram
    autonumber
    actor Staff as Nhân Viên Tiếp Tân (Staff)
    participant AdminPage as AdminPortal.razor
    participant Outstanding as OutstandingOrdersComponent
    participant UC_View as ViewOutstandingOrdersUseCase
    participant UC_Process as ProcessOrderUseCase
    participant OrderRepo as OrderRepository (Dapper)
    participant DB as SQL Server (KimLongEcoStayDB)
    participant RoomTab as RoomInspectionComponent

    Staff->>AdminPage: Mở phân hệ Quản Trị (/admin)
    AdminPage->>Outstanding: Kích hoạt Tab "Đơn Chờ Xử Lý"
    Outstanding->>UC_View: Execute()
    UC_View->>OrderRepo: GetOrders(processed = false)
    OrderRepo->>DB: SELECT * FROM Orders WHERE DateProcessed IS NULL
    DB-->>OrderRepo: Danh sách đơn đặt phòng mới của khách
    OrderRepo-->>UC_View: Trả về List<Order>
    UC_View-->>Outstanding: Hiển thị bảng đơn hàng & thông tin cọc 50%
    
    Staff->>Outstanding: Nhấn "Duyệt Đơn" (Order #102)
    Outstanding->>UC_Process: Execute(orderId = 102)
    UC_Process->>OrderRepo: ProcessOrder(orderId = 102)
    OrderRepo->>DB: UPDATE Orders SET DateProcessed = GETDATE() WHERE OrderId = 102
    DB-->>OrderRepo: Xác nhận cập nhật (1 row affected)
    OrderRepo-->>UC_Process: Hoàn tất xử lý
    UC_Process-->>Outstanding: Thông báo duyệt đơn thành công
    Outstanding-->>Staff: Cập nhật danh sách đơn (Đơn chuyển sang Đã Xử Lý)

    Staff->>AdminPage: Chuyển sang Tab "Kiểm Tra Buồng Phòng"
    AdminPage->>RoomTab: Tải dữ liệu 5 buồng phòng (KL-101 đến KL-301)
    RoomTab-->>Staff: Hiển thị trạng thái buồng: Sẵn Sàng / Đang Có Khách / Đang Dọn
    Staff->>RoomTab: Cập nhật phòng KL-101 -> "Đang Có Khách (Check-in)"
    RoomTab-->>Staff: Lưu trạng thái phòng thành công
```

---

### 3.6. Sơ Đồ Tuần Tự Hủy Phòng & Tính Phí Phạt 48H (Sequence Diagram - Cancellation & 48H Penalty Calculation)

Minh họa thuật toán tự động tính phí phạt hủy phòng theo Luật Nghiệp Vụ 2:

```mermaid
sequenceDiagram
    autonumber
    actor Guest as Khách Hàng (Guest)
    actor Staff as Nhân Viên / Lễ Tân
    participant System as BookingService (Core Business)
    participant DB as SQL Server (KimLongEcoStayDB)

    Guest->>Staff: Yêu cầu hủy đơn đặt phòng #BK20261015-01
    Staff->>System: ProcessCancellation(BookingId, RequestTime)
    System->>DB: Lấy thông tin đơn đặt & danh sách đêm (BookingNights)
    DB-->>System: CheckInDate = 15/10 14h00, Đêm 1: 700.000đ, Đã cọc: 775.000đ
    
    Note over System: Kiểm tra Luật Nghiệp Vụ 2 (Chính sách 48 Giờ)
    System->>System: Thời gian còn lại = CheckInDate - RequestTime
    
    alt Hủy trước giờ Check-in >= 48 giờ (Đúng hạn)
        System->>System: Phạt = 0đ | Hoàn lại = 100% Tiền cọc (775.000đ)
        System->>DB: UPDATE Bookings SET Status = 'CANCELLED', CancellationFee = 0
        System->>DB: Xóa giữ chỗ trong BookingNights (Giải phóng phòng)
        DB-->>System: Xác nhận cập nhật thành công
        System-->>Staff: Thông báo: Hủy hợp lệ, hoàn trả khách 775.000đ
    else Hủy trước giờ Check-in < 48 giờ (Hủy gấp)
        System->>System: Phạt = Giá 1 đêm đầu tiên (700.000đ)
        System->>System: Hoàn lại = Tiền cọc - Phí phạt = 775.000đ - 700.000đ = 75.000đ
        System->>DB: UPDATE Bookings SET Status = 'CANCELLED', CancellationFee = 700000
        System->>DB: Xóa giữ chỗ trong BookingNights (Giải phóng phòng đón khách mới)
        DB-->>System: Xác nhận cập nhật thành công
        System-->>Staff: Thông báo: Phạt 700.000đ bù lỗ phòng trống, hoàn lại 75.000đ cho khách
    end
    Staff-->>Guest: Gửi thông báo kết quả xử lý hủy phòng
```

---

### 3.7. Sơ Đồ Chuyển Trạng Thái Đơn Đặt & Buồng Phòng (State Machine Diagram)

```mermaid
stateDiagram-v2
    [*] --> PENDING_DEPOSIT : Khách tạo yêu cầu đặt phòng (Giữ chỗ 30 phút)
    
    PENDING_DEPOSIT --> CANCELLED_EXPIRED : Quá 30 phút không nhận được tiền cọc
    CANCELLED_EXPIRED --> [*]

    PENDING_DEPOSIT --> CONFIRMED : Khách thanh toán cọc 50% qua VietQR
    
    state "Kiểm Tra Thời Điểm Hủy (Luật 2)" as CancelChoice <<choice>>
    CONFIRMED --> CancelChoice : Khách gửi yêu cầu hủy đơn
    CancelChoice --> CANCELLED_FULL_REFUND : Hủy trước Check-in >= 48h (Hoàn 100% cọc)
    CancelChoice --> CANCELLED_PENALTY : Hủy trước Check-in < 48h (Phạt 1 đêm đầu tiên)
    CANCELLED_FULL_REFUND --> [*]
    CANCELLED_PENALTY --> [*]

    CONFIRMED --> CHECKED_IN : Lễ tân đón tiếp khách (14h00 nhận phòng)
    CHECKED_IN --> CHECKED_OUT : Lễ tân trả phòng (12h00) & thu 50% còn lại
    CHECKED_OUT --> [*]
```

---

### 3.8. Sơ Đồ Hoạt Động Quy Trình Đặt Phòng (Activity Diagram)

```mermaid
flowchart TD
    Start([Khách chọn phòng & ngày lưu trú]) --> InputDates[Nhập ngày Check-in & Check-out]
    InputDates --> CheckOverbooking{Kiểm tra Luật 1:<br>Đêm phòng có bị trùng?}
    
    CheckOverbooking -- "Có (Trùng đêm)" --> ErrorConflict[Báo lỗi: Phòng đã có người đặt trong thời gian này!]
    ErrorConflict --> InputDates

    CheckOverbooking -- "Không (Trống lịch)" --> CalcPrice[Tính tổng tiền = Σ giá từng đêm theo RatePlan]
    CalcPrice --> CalcDeposit[Tính tiền cọc 50% = Tổng tiền x 0.5]
    CalcDeposit --> FillGuestInfo[Khách điền thông tin Họ tên, SĐT, Email]
    FillGuestInfo --> SubmitBooking[Bấm xác nhận đặt phòng]
    
    SubmitBooking --> TransactionDB[(Lưu Header Bookings & LineItems BookingNights trong 1 Transaction)]
    TransactionDB --> ShowVietQR[Hiển thị mã VietQR chuyển khoản cọc 50%]
    ShowVietQR --> WaitDeposit{Khách chuyển tiền cọc?}
    
    WaitDeposit -- "Không (Quá 30p)" --> AutoCancel[Hủy đơn & Giải phóng phòng]
    WaitDeposit -- "Có (Xác nhận cọc)" --> ConfirmBooking[Đơn chuyển trạng thái CONFIRMED]
    ConfirmBooking --> ReceptionCheckIn[Lễ tân đón tiếp lúc 14h00 Check-in]
    ReceptionCheckIn --> ReceptionCheckOut[Lễ tân trả phòng lúc 12h00 Check-out & chuyển dọn phòng]
    ReceptionCheckOut --> End([Kết thúc chu trình lưu trú])
```

---

### 3.9. Sơ Đồ Triển Khai Hệ Thống (Deployment Diagram)

```mermaid
flowchart TB
    subgraph ClientDevice["1. Thiết Bị Người Dùng (Client Tier)"]
        Browser["Trình Duyệt Web (Chrome / Edge / Safari / Mobile)<br>• Blazor Web App (Interactive Server Mode)<br>• Kết nối thời gian thực SignalR WebSocket<br>• Lưu trữ LocalStorage giỏ phòng"]
    end

    subgraph AppServer["2. Máy Chủ Ứng Dụng (Application Tier - Kestrel Server)"]
        subgraph WebHost["KimLongEcoStay.Web (.NET 10 LTS)"]
            AuthMiddleware["Cookie Authentication & Authorization Middleware"]
            SignalRHub["Blazor Server Circuit Hub"]
            MinimalAPI["Minimal API Endpoints: /authenticate, /logout"]
        end
        subgraph LogicLayers["Tầng Nghiệp Vụ & Dữ Liệu"]
            UseCasesModule["KimLongEcoStay.UseCases"]
            CoreModule["KimLongEcoStay.CoreBusiness"]
            DapperPlugin["Dapper Micro-ORM Plugin"]
        end
    end

    subgraph DBServer["3. Máy Chủ Cơ Sở Dữ Liệu (Database Tier)"]
        SQLServer[("Microsoft SQL Server 2022 / SQLEXPRESS<br>• CSDL: KimLongEcoStayDB (Chuẩn 3NF)<br>• Kết nối TCP/IP Port 1433<br>• Bảo toàn giao dịch ACID")]
    end

    subgraph ExternalServices["4. Dịch Vụ Thanh Toán Bên Ngoài (External Gateway)"]
        VietQRAPI["Hệ Thống Thanh Toán Ngân Hàng VietQR Gateway<br>(Sinh mã QR chuyển khoản tiền cọc 50%)"]
    end

    Browser <-->|WebSocket / HTTPS: 7082| WebHost
    WebHost --> LogicLayers
    DapperPlugin <-->|ADO.NET / SqlClient Connection String| SQLServer
    Browser -.->|Quét mã thanh toán trực tiếp| VietQRAPI
```

---

### 3.10. Sơ Đồ Thành Phần Kiến Trúc Phần Mềm (Component Diagram - Clean Architecture)

```mermaid
flowchart TD
    subgraph Presentation["Tầng Giao Diện (Presentation Layer)"]
        Web["KimLongEcoStay.Web (Host Project)"]
        CustomerPortal["KimLongEcoStay.Web.CustomerPortal<br>(Tìm phòng, Chi tiết phòng, Đặt phòng vãng lai)"]
        AdminPortal["KimLongEcoStay.Web.AdminPortal<br>(Kiểm tra đơn, Duyệt cọc, Bảng buồng phòng)"]
        Common["KimLongEcoStay.Web.Common<br>(Layout, Controls dùng chung)"]
    end

    subgraph AppUseCases["Tầng Ứng Dụng (Use Cases Layer)"]
        UC["KimLongEcoStay.UseCases<br>• SearchRoomUseCase, PlaceBookingUseCase<br>• ViewOutstandingOrdersUseCase, ProcessOrderUseCase"]
        Interfaces["Plugin Interfaces<br>• IRoomRepository, IBookingRepository<br>• IOrderRepository, IUserRepository"]
    end

    subgraph Domain["Tầng Cốt Lõi (Core Business Layer)"]
        Entities["Domain Models<br>• Room, RoomType, RatePlan<br>• Booking, BookingNight, Guest, AppUser"]
        DomainServices["Domain Services & Rules<br>• BookingService (Luật 1: Chống Overbooking)<br>• OrderService (Luật 2: Tính tiền & Phạt 48h)"]
    end

    subgraph Infrastructure["Tầng Hạ Tầng & Tiện Ích (Plugins Layer)"]
        DapperRepo["KimLongEcoStay.DataStore.SQL.Dapper<br>(Truy xuất CSDL SQL Server với 1 Transaction)"]
        LocalStorage["KimLongEcoStay.ShoppingCart.LocalStorage"]
        StateStore["KimLongEcoStay.StateStore.DI"]
        HandCoded["KimLongEcoStay.DataStore.HandCoded"]
    end

    Web --> CustomerPortal
    Web --> AdminPortal
    Web --> Common
    CustomerPortal --> UC
    AdminPortal --> UC

    UC --> Domain
    UC --> Interfaces

    DapperRepo ..|> Interfaces
    LocalStorage ..|> Interfaces
    StateStore ..|> Interfaces
    HandCoded ..|> Interfaces

    DapperRepo --> Domain
```

---

### 3.11. Sơ Đồ Phân Quyền Chi Tiết Theo Vai Trò (Role-Based Access Control Use Case Diagram)

```mermaid
flowchart LR
    subgraph Actors["Các Tác Nhân"]
        GuestUser(("Khách Hàng Vãng Lai<br>(Không Cần Đăng Nhập)"))
        MemberUser(("Khách Thành Viên<br>(Customer)"))
        StaffUser(("Nhân Viên Lễ Tân<br>(Staff)"))
        AdminUser(("Quản Trị Viên<br>(Admin)"))
    end

    subgraph PublicFeatures["Chức Năng Công Khai (Khách Tự Do)"]
        F1["Tìm kiếm phòng theo ngày Check-in/out"]
        F2["Xem ảnh, giá & tiện ích buồng phòng"]
        F3["Đặt phòng nhanh & Nhận mã QR cọc 50%"]
    end

    subgraph MemberFeatures["Chức Năng Thành Viên"]
        F4["Đăng nhập tài khoản khách hàng"]
        F5["Tự động điền thông tin khi đặt phòng"]
    end

    subgraph StaffFeatures["Chức Năng Nhân Viên (Bắt Buộc Đăng Nhập)"]
        F6["Đăng nhập hệ thống quản trị (/login)"]
        F7["Kiểm tra danh sách đơn đặt phòng mới"]
        F8["Kiểm tra & duyệt thanh toán cọc"]
        F9["Kiểm tra bảng trạng thái buồng phòng"]
        F10["Thực hiện thủ tục Check-in / Check-out"]
    end

    subgraph AdminFeatures["Chức Năng Quản Trị Cấp Cao"]
        F11["Cấu hình bảng giá linh hoạt theo thứ (RatePlans)"]
        F12["Quản lý phân quyền tài khoản người dùng"]
    end

    GuestUser --> F1
    GuestUser --> F2
    GuestUser --> F3

    MemberUser --> F1
    MemberUser --> F2
    MemberUser --> F3
    MemberUser --> F4
    MemberUser --> F5

    StaffUser --> F6
    StaffUser --> F7
    StaffUser --> F8
    StaffUser --> F9
    StaffUser --> F10

    AdminUser --> StaffFeatures
    AdminUser --> F11
    AdminUser --> F12
```

---

## ⚖️ 4. HAI LUẬT NGHIỆP VỤ CỐT LÕI

* **Luật Nghiệp Vụ 1 (Chống Overbooking):**
  * Quản lý trạng thái lưu trú theo từng đêm (`NightDate` từ 14h00 ngày $D$ đến 12h00 ngày $D+1$).
  * Quy tắc: Khách cũ trả phòng ngày $D$ (12h00) và khách mới nhận phòng ngày $D$ (14h00) là **hoàn toàn hợp lệ**, không xung đột lịch.
* **Luật Nghiệp Vụ 2 (Dynamic Pricing & Chính Sách Phạt Hủy 48H):**
  * Tổng tiền thuê phòng tự động tính:
    $$\text{TotalAmount} = \sum_{i=1}^{N} \text{Giá đêm}_i$$
    *(Giá ngày thường Thứ 2 - Thứ 5 thấp hơn giá cuối tuần Thứ 6 - Chủ Nhật theo `RatePlans`)*.
  * Tiền cọc yêu cầu: $\text{DepositAmount} = 50\% \times \text{TotalAmount}$.
  * Chính sách hủy cọc:
    * Hủy trước $\ge 48$ tiếng trước giờ Check-in: Hoàn 100% tiền cọc.
    * Hủy gấp $< 48$ tiếng trước giờ Check-in: Khấu trừ phí phạt tương đương **giá của 1 đêm đầu tiên** (`CancellationFee`) để bù lỗ phòng trống.

---

## 🔐 5. CẤU TRÚC XÁC THỰC & PHÂN QUYỀN (AUTHENTICATION & AUTHORIZATION)

Hệ thống được thiết kế theo mô hình phân quyền chặt chẽ đáp ứng tiêu chí **K4.1 & K4.2** trong barem đánh giá:

| Đối Tượng Người Dùng | Yêu Cầu Đăng Nhập | Quyền Hạn & Chức Năng Trên Hệ Thống |
|---|---|---|
| **Khách Hàng Vãng Lai (Guest)** | ❌ **Không cần đăng nhập** | Tự do tìm kiếm buồng phòng, xem chi tiết phòng, chọn phòng vào giỏ và hoàn tất đặt cọc trực tuyến chỉ với việc nhập thông tin liên lạc (Họ tên, SĐT, Email). |
| **Khách Hàng Thành Viên (Customer)** | 💡 **Tùy chọn đăng nhập** | Đăng nhập tài khoản (`khachhang` / `123456`) để tự động điền sẵn thông tin khi đặt phòng và lưu vết lịch sử giao dịch cá nhân. |
| **Nhân Viên Tiếp Tân (Staff / Receptionist)** | 🔒 **BẮT BUỘC đăng nhập** | Truy cập phân hệ `/admin` (`[Authorize(Roles = "Staff,Admin")]`) để **kiểm tra danh sách đơn đặt phòng**, duyệt cọc, kiểm tra bảng trạng thái buồng phòng (Sẵn sàng / Đang có khách / Dọn phòng). |
| **Quản Trị Viên (Admin / Host)** | 🔒 **BẮT BUỘC đăng nhập** | Toàn quyền kiểm tra, cấu hình bảng giá `RatePlans`, duyệt đơn và quản lý toàn bộ hệ thống. |

### Danh Sách Tài Khoản Thử Nghiệm Hệ Thống (Mật khẩu chung: `123456`)
* 🛡️ **Nhân viên / Lễ tân:** Username: `letan` | Role: `Staff` *(Dành riêng cho nhân viên đăng nhập để kiểm tra hệ thống)*
* 👑 **Quản trị viên:** Username: `admin` | Role: `Admin` *(Toàn quyền quản trị)*
* 👤 **Khách hàng thân thiết:** Username: `khachhang` | Role: `Customer` *(Tài khoản khách hàng tùy chọn)*

---

## 🗄️ 6. CƠ SỞ DỮ LIỆU SQL SERVER (CHUẨN 3NF)

Kịch bản CSDL lưu tại: [database/schema.sql](database/schema.sql)

```mermaid
erDiagram
    RoomTypes ||--o{ Rooms : "has"
    RoomTypes ||--o{ RatePlans : "prices"
    Guests ||--o{ Bookings : "places"
    Bookings ||--|{ BookingNights : "consists_of (Header-Line)"
    Rooms ||--o{ BookingNights : "assigned_to"

    RoomTypes {
        int Id PK
        nvarchar TypeName UK
        decimal BasePrice
        int MaxGuests
        nvarchar Amenities
    }

    Rooms {
        int Id PK
        varchar RoomNumber UK
        int RoomTypeId FK
        varchar Status
    }

    RatePlans {
        int Id PK
        int RoomTypeId FK
        tinyint DayOfWeek
        decimal Price
        bit IsWeekend
    }

    Guests {
        int Id PK
        nvarchar FullName
        varchar PhoneNumber
        varchar Email
        varchar IdentityCard
    }

    Bookings {
        int Id PK
        varchar BookingCode UK
        int GuestId FK
        date CheckInDate
        date CheckOutDate
        decimal TotalAmount
        decimal DepositAmount
        varchar Status
    }

    BookingNights {
        int Id PK
        int BookingId FK
        int RoomId FK
        date NightDate
        decimal Price
    }
```

### 6.1. Danh Mục 5 Buồng Phòng Thực Tế & Chính Sách Giá (Kim Long EcoStay MVP)

| Mã Phòng | Tên Loại Phòng | Cơ Sở Nhà Vườn | Giá T2-T5 (VNĐ/đêm) | Giá T6-CN (VNĐ/đêm) | Sức Chứa | Tiện Ích Độc Bản & Công Nghệ |
|:---|:---|:---|:---:|:---:|:---:|:---|
| **KLE-R101** | **Eco-Nest Ban Mai** | Nhà Vườn Hương Sen | 390.000 | 480.000 | 2 khách | Ban công kính panorama view đồng lúa, đệm organic, SmartLock IoT 10s |
| **KLE-R102** | **Eco-Nest Hoàng Hôn** | Nhà Vườn Hương Sen | 420.000 | 500.000 | 2 khách | View hoàng hôn sông Hương, bồn tắm sứ, tinh dầu sả chanh, xe đạp trợ lực |
| **KLE-R201** | **Garden Suite Cố Đô Thanh Trà** | Nhà Vườn Nguyệt Biều | 680.000 | 850.000 | 3 khách | Vườn bưởi 40 năm, bồn ngâm chân thảo mộc, 02 xe đạp trợ lực GPS Audio Guide, bánh ướt đón tiếp |
| **KLE-R202** | **Family Eco-Villa Gắn Kết** | Nhà Vườn Nguyệt Biều | 820.000 | 990.000 | 4 khách | Biệt thự gia đình, sân vườn cỏ an toàn cho bé, kit nông nghiệp học hái quả tưới rau |
| **KLE-R203** | **Heritage Studio Trầm Mặc** | Nhà Vườn Nguyệt Biều | 590.000 | 720.000 | 2 khách | View hồ súng & chuối ngự, ấm trà Cung Đình, kệ sách triều Nguyễn, trợ giá 50% xe máy điện |

---

## 🚀 7. HƯỚNG DẪN KHỞI CHẠY DỰ ÁN

### Yêu Cầu Môi Trường
* **.NET SDK:** 10.0 trở lên
* **Hệ Quản trị CSDL:** Microsoft SQL Server 2019/2022 hoặc SQL Server Express (`.\SQLEXPRESS`)

### Bước 1: Khởi tạo Cơ sở Dữ liệu
Chạy tệp SQL kịch bản trực tiếp bằng `sqlcmd` hoặc mở bằng SQL Server Management Studio (SSMS):
```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -i "database/schema.sql"
```

### Bước 2: Cấu hình Chuỗi Kết Nối
Kiểm tra cấu hình trong `project/KimLongEcoStay/KimLongEcoStay.Web/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=KimLongEcoStayDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### Bước 3: Biên dịch & Chạy Website
```powershell
# Chuyển vào thư mục solution
cd project/KimLongEcoStay

# Biên dịch toàn bộ 10 dự án thành phần
dotnet build KimLongEcoStay.slnx

# Khởi chạy Website Blazor
dotnet run --project KimLongEcoStay.Web/KimLongEcoStay.Web.csproj
```
Truy cập trình duyệt tại địa chỉ: `https://localhost:7082` (hoặc cổng hiển thị trên console).