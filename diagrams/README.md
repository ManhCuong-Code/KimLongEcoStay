# HỒ SƠ THIẾT KẾ UML & ĐỐI CHIẾU TIÊU CHÍ CHẤM THI CAPSTONE (ECO2415)
## Dự Án: Kim Long EcoStay — Nền Tảng Đặt Phòng Nhà Vườn Sinh Thái Kim Long

Tài liệu này chuẩn hóa toàn bộ các sơ đồ **PlantUML** theo đúng **Rubric chấm Đồ án Capstone ECO2415** (thang điểm 100) cho **Dự án Kim Long EcoStay**.

---

## 📋 BẢNG ĐỐI CHIẾU SƠ ĐỒ UML VỚI PHIẾU CHẤM CHI TIẾT (MỤC 12 BAREM)

| Mã KT | Tiêu chí chấm thi | Điểm tối đa | Thể hiện trong Sơ đồ PlantUML & Kiến trúc |
|---|---|:---:|---|
| **K1.1** | Đủ 4 project; `CoreBusiness` không tham chiếu project nào; `UseCases` chỉ tham chiếu `CoreBusiness`. | **8đ** | **Class Diagram**: 4 package độc lập thể hiện đúng 4 project: `CoreBusiness`, `UseCases`, `Plugins.DataStore.SQL`, `WebApp`. |
| **K1.2** | Interface repository nằm ở `UseCases`, Plugin cài đặt; file `.razor` inject interface, không inject lớp cụ thể. | **7đ** | **Class Diagram & Sequence Diagram**: `IRoomRepository`, `IBookingRepository` đặt ở tầng `UseCases`; `CheckoutBookingPage.razor` chỉ `@inject ICreateBookingUseCase`. |
| **K1.3** | 2 luật nghiệp vụ của đề cài trong `CoreBusiness` / `UseCases`, không nằm trong `.razor`. | **6đ** | **Class & Sequence & Activity Diagram**: `OverbookingRule` (Luật 1) và `PricingAndCancellationRule` (Luật 2) nằm trọn trong `CoreBusiness`. |
| **K1.4** | DI đăng ký đủ; giỏ/lựa chọn tạm dùng `Scoped`. | **4đ** | **Class Diagram & Sequence Diagram**: `BookingCartState` được khai báo là Scoped Service lưu phòng & khoảng ngày chọn. |
| **K2.1** | Từ 5 bảng, 3NF, có khóa ngoại; ít nhất 1 cặp header-line. | **8đ** | **Class Diagram**: 6 thực thể (`RoomType`, `Room`, `RatePlan`, `Guest`, `Booking`, `BookingNight`), trong đó `Booking` – `BookingNight` là cặp Header–LineItem. |
| **K2.2** | Cặp header–line ghi trong 1 transaction. | **6đ** | **Sequence Diagram 1**: `SqlBookingRepository` mở `BeginTransaction()`, lưu `Booking` và duyệt danh sách `BookingNight` trước khi `Commit()`. |
| **K2.3** | Mọi truy vấn tham số hóa, không nối chuỗi SQL. | **5đ** | **Sequence Diagram 1**: Truy vấn `@RoomId`, `@CheckIn`, `@CheckOut` qua Dapper trong `SqlBookingRepository`. |
| **K2.4** | Luồng giao dịch chính chạy end-to-end trên SQL Server. | **6đ** | **Sequence Diagram 1**: Luồng đặt phòng hoàn chỉnh từ WebApp -> UseCases -> CoreBusiness -> Plugin SQL -> SQL Server. |
| **K3.1** | Ít nhất 3 component tái sử dụng có `[Parameter]` / `EventCallback`. | **6đ** | **Class Diagram**: `SmartPasteModalComponent`, `RoomCardComponent`, `PriceBadgeComponent`. |
| **K3.2** | `EditForm` + `DataAnnotations`; lỗi nhập liệu hiển thị cạnh ô nhập. | **5đ** | **Sequence Diagram 1 & 2**: Form đặt phòng dùng `EditForm` bắt lỗi hợp lệ dữ liệu khách. |
| **K3.3** | Ca vi phạm 2 luật nghiệp vụ hiện thông báo thân thiện, không văng lỗi. | **4đ** | **Activity Diagram**: Khi vi phạm trùng phòng hoặc hủy sát giờ, ném Domain Exception và UI hiển thị cảnh báo thân thiện. |
| **K4.1 & K4.2** | Đăng nhập/đăng xuất 2 vai trò; trang quản trị được bảo vệ `[Authorize]`. | **9đ** | **Use Case & Class Diagram**: Phân quyền Khách hàng vs Host/Admin (`[Authorize]`). |
| **K4.3** | Mở rộng AI: đạt đủ mục 10 (IChatClient trong UseCase riêng, AI chỉ gợi ý, người dùng xác nhận trước khi lưu). | **6đ** | **Sequence Diagram 2 (AI SmartPaste)**: `ExtractBookingFromMessageUseCase` inject `IChatClient`, bóc tách tin nhắn Zalo, lễ tân xác nhận trên `EditForm` trước khi ghi CSDL. |
| **K5.1** | Use Case, Class, Sequence (PlantUML) khớp tên lớp và luồng trong code. | **6đ** | Toàn bộ 5 sơ đồ PlantUML khớp 100% tên Class, Interface, Method, Entity và luồng chạy. |
| **K5.2** | Vấn đáp: chỉ đúng dòng cài luật và giải thích Dependency Rule. | **6đ** | Bản giải thích chi tiết vị trí cài 2 luật nghiệp vụ và quy tắc phụ thuộc Clean Architecture. |

---

## 📂 DANH MỤC CÁC TỆP SƠ ĐỒ (.PUML)

1. [use_case_diagram.puml](file:///c:/Users/TUF/Desktop/KimLongEcoStay/diagrams/use_case_diagram.puml)
   - Sơ đồ Ca sử dụng chuẩn hóa: Khách hàng (User) & Chủ homestay (Admin) + VietQR + AI Service.
2. [class_diagram.puml](file:///c:/Users/TUF/Desktop/KimLongEcoStay/diagrams/class_diagram.puml)
   - Sơ đồ Lớp 4 tầng Clean Architecture: `CoreBusiness`, `UseCases`, `Plugins.DataStore.SQL`, `WebApp`.
3. [sequence_booking.puml](file:///c:/Users/TUF/Desktop/KimLongEcoStay/diagrams/sequence_booking.puml)
   - Sơ đồ Tuần tự Luồng giao dịch chính (End-to-End): Kiểm tra phòng trống, tính tiền từng đêm và lưu Header-Line trong 1 Transaction.
4. [sequence_ai_smartpaste.puml](file:///c:/Users/TUF/Desktop/KimLongEcoStay/diagrams/sequence_ai_smartpaste.puml)
   - Sơ đồ Tuần tự Mở rộng AI SmartPaste: Chuẩn mục 10 của Barem (UseCase riêng, `IChatClient`, người dùng xác nhận).
5. [activity_diagram.puml](file:///c:/Users/TUF/Desktop/KimLongEcoStay/diagrams/activity_diagram.puml)
   - Sơ đồ Hoạt động: Quy trình đặt phòng và luồng kiểm soát 2 luật nghiệp vụ.
6. [state_machine_diagram.puml](file:///c:/Users/TUF/Desktop/KimLongEcoStay/diagrams/state_machine_diagram.puml)
   - Sơ đồ Trạng thái: Vòng đời đơn `Booking` & buồng phòng `Room`, nhánh hủy cọc theo luật 48 giờ.
