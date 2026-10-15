using System;
using System.Collections.Generic;
using System.Linq;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public static class HeritageMapProvider
    {
        private static readonly List<HeritageLocation> _locations = new()
        {
            // =========================================================================
            // ĐIỂM TRUNG TÂM / HOMESTAY XUẤT PHÁT
            // =========================================================================
            new HeritageLocation
            {
                Id = "KLE-CENTER",
                Name = "Kim Long EcoStay (Tâm Điểm Xuất Phát)",
                Category = "Trụ Sở Homestay",
                Address = "Khu vực Nhà vườn Kim Long, Ven Sông Hương, Phường Kim Long, TP. Huế",
                Latitude = 16.4560,
                Longitude = 107.5495,
                DistanceFromEcoStayKm = 0.0,
                TravelEstimate = "Điểm khởi hành tour xe đạp & nhận phòng SmartLock",
                ImageUrl = "/images/nha_ruong_1.jpg",
                Summary = "Mô hình Homestay Sinh Thái Nhà Vườn & Du Lịch Di Sản Thông Minh ven sông Hương.",
                Description = "Tọa lạc tại vùng lõi đệ nhất nhà vườn Kim Long, Kim Long EcoStay kết nối trực tiếp với 2 căn nhà vườn mẫu, cung cấp đội xe đạp trợ lực điện Eco-Bike và thiết bị thuyết minh GPS Audio Guide tự động miễn phí cho du khách.",
                ExperienceHighlights = new()
                {
                    "Nhận phòng tự động 10 giây với khóa SmartLock ngoài trời IP66",
                    "Đội 10 xe đạp trợ lực điện Eco-Bike kèm giá đỡ điện thoại dẫn đường",
                    "Thưởng trà sen Cung Đình dưới bóng vườn bưởi Thanh Trà 40 năm",
                    "Thưởng thức bánh ướt thịt nướng Kim Long than hoa đỏ rực giao tận bàn 15 phút"
                },
                OpeningHours = "Mở cửa 24/7 (Smart Check-in không chạm)",
                TicketInfo = "Dịch vụ độc quyền cho khách lưu trú & trải nghiệm",
                IsEcoStayCenter = true,
                BadgeColor = "success",
                IconClass = "bi-house-heart-fill"
            },

            // =========================================================================
            // I. CÁC KHU LÀNG NGHỀ TRUYỀN THỐNG XỨ HUẾ
            // =========================================================================
            new HeritageLocation
            {
                Id = "CRAFT-THUY-XUAN",
                Name = "Làng Hương Thủy Xuân (Hương Trầm & Nón Lá)",
                Category = "Làng Nghề Truyền Thống",
                Address = "Đường Huyền Trân Công Chúa, Phường Thủy Xuân, TP. Huế (Dưới chân đồi Vọng Cảnh)",
                Latitude = 16.4358,
                Longitude = 107.5681,
                DistanceFromEcoStayKm = 4.2,
                TravelEstimate = "4.2 km (~12 phút đạp xe Eco-Bike)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Làng nghề làm hương trầm và nón lá hơn 700 năm tuổi với những đóa chông hương rực rỡ sắc màu.",
                Description = "Làng hương Thủy Xuân là một trong những làng nghề lâu đời và nổi tiếng nhất Cố đô. Du khách đến đây sẽ bị mê hoặc bởi hàng ngàn bó hương trầm, quế, tùng xòe bung như những đóa hoa rực rỡ dưới nắng vàng. Nơi đây từng chuyên cung cấp hương trầm thơm ngát cho triều đình và phủ chúa.",
                ExperienceHighlights = new()
                {
                    "Trải nghiệm tự tay nhồi bột, se nén hương trầm cổ truyền cùng nghệ nhân",
                    "Không gian 'check-in' nghệ thuật rực rỡ sắc màu bậc nhất Cố đô Huế",
                    "Chiêm ngưỡng các nghệ nhân chằm nón bài thơ với hoa văn tinh xảo",
                    "Mua sắm nén hương trầm sạch, tinh dầu sả chanh và nón lá Huế làm quà"
                },
                OpeningHours = "07:00 - 18:30 hàng ngày",
                TicketInfo = "Miễn phí tham quan & trải nghiệm (Ủng hộ mua sản phẩm)",
                BadgeColor = "warning",
                IconClass = "bi-flower2"
            },

            new HeritageLocation
            {
                Id = "CRAFT-KIM-LONG",
                Name = "Làng Bưởi Thanh Trà & Bánh Ướt Thịt Nướng Kim Long",
                Category = "Làng Nghề Truyền Thống",
                Address = "Tuyến đường Kim Long & Kiệt 82 Kim Long, Phường Kim Long, TP. Huế",
                Latitude = 16.4565,
                Longitude = 107.5512,
                DistanceFromEcoStayKm = 0.3,
                TravelEstimate = "300m (~1 phút đi bộ từ phòng ngủ)",
                ImageUrl = "/images/nha_ruong_3.jpg",
                Summary = "Thủ phủ đệ nhất bưởi tiến vua Thanh Trà và làng ẩm thực bánh ướt thịt nướng gia truyền 300 năm.",
                Description = "Vùng đất trù phú Kim Long ven bờ bắc sông Hương được thiên nhiên ưu đãi lớp phù sa màu mỡ bồi đắp hàng ngàn năm, tạo nên giống bưởi Thanh Trà tép vàng mọng nước, thơm dịu chuyên tiến vua. Song hành cùng đó là các lò tráng bánh ướt thịt nướng than hoa nức tiếng trong câu ca dao: 'Kim Long có gái mỹ miều...'.",
                ExperienceHighlights = new()
                {
                    "Dạo bước dưới tán vườn bưởi cổ thụ 40 năm tuổi sai trĩu quả",
                    "Thưởng thức đĩa bánh ướt thịt nướng than hoa nóng hổi vừa thổi vừa ăn",
                    "Nếm thử gỏi tép bưởi Thanh Trà trộn tôm sông mực một nắng thơm nức",
                    "Trải nghiệm tráng bánh ướt mỏng như lụa trên nồi hơi vải truyền thống"
                },
                OpeningHours = "06:30 - 21:00 hàng ngày",
                TicketInfo = "Tham quan tự do • Món ăn từ 35.000 VNĐ/suất",
                BadgeColor = "danger",
                IconClass = "bi-egg-fried"
            },

            new HeritageLocation
            {
                Id = "CRAFT-LANG-SINH",
                Name = "Làng Tranh Dân Gian Làng Sình",
                Category = "Làng Nghề Truyền Thống",
                Address = "Làng Lại Ân (Làng Sình), Xã Phú Mậu, TP. Huế (Ven ngã ba Sình)",
                Latitude = 16.5036,
                Longitude = 107.5992,
                DistanceFromEcoStayKm = 7.5,
                TravelEstimate = "7.5 km (~20 phút đi xe hoặc du thuyền sông Hương)",
                ImageUrl = "/images/thuyen_rong_hue.jpg",
                Summary = "Dòng tranh mộc bản cổ xưa hơn 400 năm in trên giấy điệp phục vụ đời sống tín ngưỡng và tâm linh dân gian.",
                Description = "Cùng với tranh Đông Hồ và Hàng Trống, tranh Làng Sình là một trong ba dòng tranh dân gian trứ danh của Việt Nam. Điểm độc đáo của tranh Sình là kỹ thuật in mộc bản trên giấy mộc quét nước điệp vỏ sò óng ánh, sau đó nghệ nhân dùng bút lông chấm nét màu tự nhiên từ lá cây, hoa hòe và tro rơm.",
                ExperienceHighlights = new()
                {
                    "Gặp gỡ nghệ nhân Kỳ Hữu Phước — bàn tay vàng gìn giữ mộc bản tranh Sình",
                    "Tự tay dập mộc bản gỗ cổ và dùng chổi lông quét mực in bức tranh của riêng mình",
                    "Tìm hiểu ý nghĩa các bức tranh thế mạng, tranh 12 con giáp và tranh thờ cầu an",
                    "Kết hợp tham quan hội vật Làng Sình truyền thống vào mùng 10 tháng Giêng"
                },
                OpeningHours = "08:00 - 17:30",
                TicketInfo = "Miễn phí tham quan • Trải nghiệm in tranh: 20.000 - 30.000 VNĐ",
                BadgeColor = "warning",
                IconClass = "bi-palette-fill"
            },

            new HeritageLocation
            {
                Id = "CRAFT-THANH-TIEN",
                Name = "Làng Hoa Giấy Thanh Tiên",
                Category = "Làng Nghề Truyền Thống",
                Address = "Thôn Thanh Tiên, Xã Phú Mậu, Thành phố Huế (Liền kề Làng Sình)",
                Latitude = 16.5019,
                Longitude = 107.6033,
                DistanceFromEcoStayKm = 7.8,
                TravelEstimate = "7.8 km (~22 phút đạp xe hoặc kết hợp thuyền rồng)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Nghệ thuật làm hoa sen và hoa cúng bằng giấy thủ công hơn 300 năm tuổi nức tiếng xứ Huế.",
                Description = "Ra đời từ thế kỷ 18 thời các chúa Nguyễn, hoa giấy Thanh Tiên gắn liền với triết lý thờ cúng tổ tiên của người Việt. Bằng đôi bàn tay khéo léo, các nghệ nhân chẻ tre, vót nan, nhuộm giấy bằng phẩm màu thảo mộc tự nhiên và dập nếp xếp cánh tạo nên những đóa hoa sen hồng, sen trắng thanh khiết như hoa thật.",
                ExperienceHighlights = new()
                {
                    "Trải nghiệm tự tay làm đóa hoa sen giấy dưới sự hướng dẫn của nghệ nhân",
                    "Tìm hiểu bí quyết pha chế phẩm màu từ vỏ cây, củ nghệ và trái mồng tơi",
                    "Chiêm ngưỡng cây bông hoa giấy khổng lồ rực rỡ ngày Tết Cố đô",
                    "Mang về nhà những đóa sen giấy thanh nhã không bao giờ tàn phai"
                },
                OpeningHours = "07:30 - 18:00",
                TicketInfo = "Miễn phí tham quan • Trải nghiệm làm hoa: 30.000 VNĐ",
                BadgeColor = "warning",
                IconClass = "bi-flower1"
            },

            new HeritageLocation
            {
                Id = "CRAFT-PHUONG-DUC",
                Name = "Làng Đúc Đồng Phường Đúc",
                Category = "Làng Nghề Truyền Thống",
                Address = "Đường Bùi Thị Xuân, Phường Đúc, TP. Huế (Bờ nam sông Hương)",
                Latitude = 16.4522,
                Longitude = 107.5667,
                DistanceFromEcoStayKm = 2.5,
                TravelEstimate = "2.5 km (~7 phút đạp xe qua cầu Dã Viên)",
                ImageUrl = "/images/kien_truc_vuon_hue.jpg",
                Summary = "Nơi các nghệ nhân đúc đồng tài ba tạo tác Cửu Đỉnh, Đại Hồng Chung và 9 khẩu Thần Công uy nghiêm thời Nguyễn.",
                Description = "Làng nghề đúc đồng Phường Đúc có lịch sử khởi nghiệp từ thế kỷ 17 dưới thời chúa Nguyễn Hoàng do cụ tổ Nguyễn Văn Lương sáng lập. Trải qua hàng trăm năm, các thợ đúc đồng tài hoa nơi đây đã đúc nên những bảo vật quốc gia vô giá như Cửu Đỉnh (đặt tại Thế Miếu), quả chuông đồng nặng 2 tấn tại Chùa Thiên Mụ và tượng các bậc danh nhân.",
                ExperienceHighlights = new()
                {
                    "Tận mắt chứng kiến lò đúc đồng đỏ rực và kỹ thuật đổ khuôn đất sét bí truyền",
                    "Ngắm nhìn các sản phẩm lư hương, đỉnh đồng, chuông khánh với độ vang thanh thoát",
                    "Tìm hiểu về kỹ thuật đúc Cửu Đỉnh triều Nguyễn — kiệt tác đỉnh cao của nghề đồng Việt",
                    "Mua các sản phẩm lưu niệm bằng đồng tinh xảo, chuông gió phong thủy"
                },
                OpeningHours = "08:00 - 17:00",
                TicketInfo = "Tham quan miễn phí tại các xưởng đúc đồng nghệ nhân",
                BadgeColor = "warning",
                IconClass = "bi-bell-fill"
            },

            new HeritageLocation
            {
                Id = "CRAFT-TAY-HO",
                Name = "Làng Nón Lá Tây Hồ (Nón Bài Thơ Huế)",
                Category = "Làng Nghề Truyền Thống",
                Address = "Thôn Tây Hồ, Xã Phú Hồ, Huyện Phú Vang, Thừa Thiên Huế (Ven dòng sông Như Ý)",
                Latitude = 16.4392,
                Longitude = 107.6631,
                DistanceFromEcoStayKm = 11.2,
                TravelEstimate = "11.2 km (~25 phút đi xe)",
                ImageUrl = "/images/room_hoanghon.jpg",
                Summary = "Cái nôi khai sinh ra chiếc nón bài thơ trứ danh — biểu tượng duyên dáng của người con gái Huế.",
                Description = "Nón lá Tây Hồ nổi tiếng với kỹ thuật ủi lá, chằm nón khéo léo và sáng tạo độc đáo: chèn những câu thơ lục bát và hình ảnh cầu Tràng Tiền, chùa Thiên Mụ vào giữa hai lớp lá nón. Chỉ khi soi chiếc nón lên ánh nắng ban mai hay ánh đèn, bài thơ và bức tranh xứ Huế mới hiện lên lung linh.",
                ExperienceHighlights = new()
                {
                    "Xem nghệ nhân thực hiện 15 công đoạn chằm nón: từ phơi lá, ủi phẳng đến chằm chỉ cước",
                    "Trải nghiệm tự tay xếp lá và viết những dòng chữ kỷ niệm vào nón bài thơ",
                    "Đội thử nón bài thơ truyền thống chụp ảnh lưu niệm cùng áo dài tím Huế",
                    "Mua nón bài thơ chính gốc Tây Hồ với độ bền và nét thanh thoát đặc trưng"
                },
                OpeningHours = "07:00 - 18:00",
                TicketInfo = "Tham quan miễn phí • Nón bài thơ từ 60.000 - 120.000 VNĐ/chiếc",
                BadgeColor = "warning",
                IconClass = "bi-umbrella-fill"
            },

            new HeritageLocation
            {
                Id = "CRAFT-PHUOC-TICH",
                Name = "Làng Cổ & Gốm Đất Nung Phước Tích",
                Category = "Làng Nghề Truyền Thống",
                Address = "Làng cổ Phước Tích, Xã Phong Hòa, Huyện Phong Điền, Tỉnh Thừa Thiên Huế (Bên sông Ô Lâu)",
                Latitude = 16.6853,
                Longitude = 107.3197,
                DistanceFromEcoStayKm = 38.5,
                TravelEstimate = "38.5 km (~45 phút ô tô / tour xe sinh thái trong ngày)",
                ImageUrl = "/images/nha_ruong_1.jpg",
                Summary = "Ngôi làng cổ thứ hai của Việt Nam sau Đường Lâm với nghề làm gốm đất nung tiến vua hơn 500 năm tuổi.",
                Description = "Được thành lập từ năm 1470, Phước Tích ôm ấp trong mình quần thể hơn 20 ngôi nhà rường cổ từ 100 đến 200 năm tuổi cùng rặng thị cổ thụ 500 năm. Nghề gốm Phước Tích nổi tiếng với đồ gốm thô đất sét tự nhiên nung bằng củi, đặc biệt là chiếc 'om ngự' chuyên dùng để nấu cơm dâng lên các hoàng đế triều Nguyễn.",
                ExperienceHighlights = new()
                {
                    "Dạo bước trên những con đường lát gạch cổ rêu phong và ngắm nhìn cổng chè tàu xanh mướt",
                    "Khám phá các ngôi nhà rường cổ kính với hệ rui mè, cột gỗ chạm khắc hình hoa sen, rồng phượng",
                    "Trải nghiệm chuốt gốm trên bàn xoay thủ công cùng nghệ nhân Lương Thanh Hiền",
                    "Thưởng ngoạn cảnh sắc bình yên của khúc sông Ô Lâu bao bọc ba mặt làng cổ"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "Vé tham quan làng cổ: 30.000 VNĐ/người",
                BadgeColor = "warning",
                IconClass = "bi-cup-straw"
            },

            new HeritageLocation
            {
                Id = "CRAFT-BAO-LA",
                Name = "Làng Mây Tre Đan Bao La",
                Category = "Làng Nghề Truyền Thống",
                Address = "Thôn Bao La, Xã Quảng Phú, Huyện Quảng Điền, Thừa Thiên Huế (Ven bờ sông Bồ)",
                Latitude = 16.5583,
                Longitude = 107.4917,
                DistanceFromEcoStayKm = 14.5,
                TravelEstimate = "14.5 km (~30 phút đạp xe / đi xe máy)",
                ImageUrl = "/images/kien_truc_vuon_hue.jpg",
                Summary = "Làng nghề đan lát mây tre hơn 600 năm tuổi với các sản phẩm thủ công mỹ nghệ sinh thái xuất khẩu quốc tế.",
                Description = "Ra đời từ thời chúa Nguyễn Phúc Chu, làng Bao La nổi tiếng khắp xứ Đàng Trong nhờ sự dẻo dai, tỉ mỉ trong từng đường nan tre vót mịn. Từ những vật dụng dân dã như thúng, mủng, rổ, rá, ngày nay các nghệ nhân đã nâng tầm thành những tác phẩm đèn mây, khay đựng hoa quả, giỏ quà thân thiện môi trường.",
                ExperienceHighlights = new()
                {
                    "Chiêm ngưỡng các nghệ nhân vót nan tre mỏng tang như sợi chỉ và đan hoa văn chữ Vạn",
                    "Trải nghiệm tự tay đan chiếc khay tre nhỏ hoặc lẵng hoa mang về làm kỷ niệm",
                    "Khám phá Hợp tác xã Mây tre đan Bao La — hình mẫu phát triển kinh tế xanh làng nghề",
                    "Mua sắm các vật dụng sinh thái 100% tre tự nhiên thay thế đồ nhựa"
                },
                OpeningHours = "07:30 - 17:00",
                TicketInfo = "Tham quan miễn phí",
                BadgeColor = "warning",
                IconClass = "bi-basket2-fill"
            },

            // =========================================================================
            // II. QUẦN THỂ DI TÍCH CỐ ĐÔ HUẾ (DI SẢN THẾ GIỚI UNESCO)
            // =========================================================================
            new HeritageLocation
            {
                Id = "HERITAGE-DAI-NOI",
                Name = "Đại Nội Huế (Hoàng Thành & Tử Cấm Thành)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Đường 23 Tháng 8, Phường Thuận Hòa, Thành phố Huế",
                Latitude = 16.4697,
                Longitude = 107.5779,
                DistanceFromEcoStayKm = 2.8,
                TravelEstimate = "2.8 km (~8 phút đạp xe Eco-Bike)",
                ImageUrl = "/images/kien_truc_vuon_hue.jpg",
                Summary = "Trung tâm quyền lực tối cao của 13 vị hoàng đế triều Nguyễn (1802 - 1945), Di sản Văn hóa Thế giới UNESCO.",
                Description = "Quần thể di tích Cố đô Huế được UNESCO vinh danh năm 1993, trong đó Đại Nội là hạt nhân quan trọng nhất. Được bao bọc bởi Kinh thành đồ sộ chu vi gần 10km, Hoàng Thành và Tử Cấm Thành sở hữu hơn 100 công trình kiến trúc cung đình tuyệt mỹ: Ngọ Môn sừng sững, Điện Thái Hòa dát vàng, Cung Diên Thọ thanh tịnh, Nhà hát Duyệt Thị Đường cổ xưa nhất Đông Nam Á.",
                ExperienceHighlights = new()
                {
                    "Check-in trước Cửa Ngọ Môn và Cột Cờ Kỳ Đài ngắm toàn cảnh sông Hương",
                    "Chiêm ngưỡng ngai vàng vua Nguyễn và kiến trúc gỗ sơn son thếp vàng Điện Thái Hòa",
                    "Thưởng thức Nhã nhạc Cung đình Huế (Di sản Phi vật thể thế giới) tại Duyệt Thị Đường",
                    "Dạo bước trên những hành lang đỏ son và hồ sen thơm ngát trong Tử Cấm Thành"
                },
                OpeningHours = "07:30 - 17:30 (Mở cửa ban đêm vào các dịp lễ hội)",
                TicketInfo = "200.000 VNĐ/người lớn • 40.000 VNĐ/trẻ em",
                BadgeColor = "primary",
                IconClass = "bi-bank2"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-THIEN-MU",
                Name = "Chùa Thiên Mụ (Linh Mụ Tự)",
                Category = "Chùa Cổ & Tâm Linh",
                Address = "Đồi Hà Khê, Đường Nguyễn Phúc Nguyên, Phường Kim Long, TP. Huế",
                Latitude = 16.4533,
                Longitude = 107.5451,
                DistanceFromEcoStayKm = 0.85,
                TravelEstimate = "850m (~3 phút đạp xe Eco-Bike)",
                ImageUrl = "/images/chua_thien_mu.jpg",
                Summary = "Đệ nhất cổ tự xứ Đàng Trong (1601), biểu tượng tâm linh bất diệt của Cố đô Huế bên dòng sông Hương.",
                Description = "Được khởi lập từ năm 1601 bởi Tiên chúa Nguyễn Hoàng sau lời sấm truyền kỳ bí của bà tiên áo đỏ trên đồi Hà Khê. Chùa Thiên Mụ nổi bật với Tháp Phước Duyên hình bát giác 7 tầng cao 21 mét, tiếng chuông chùa ngân vang sớm chiều đã đi vào thơ ca nhạc họa xứ Huế.",
                ExperienceHighlights = new()
                {
                    "Ngắm tháp Phước Duyên 7 tầng và bia đá thời chúa Nguyễn Phúc Chu",
                    "Chiêm bái Đại Hồng Chung nặng hơn 2 tấn đúc năm 1710 với âm vang vang xa hàng dặm",
                    "Viếng chiếc xe hơi cổ của Hòa thượng Thích Quảng Đức — chứng tích lịch sử năm 1963",
                    "Ngắm hoàng hôn buông lơi dát vàng mặt nước sông Hương tuyệt đẹp từ sân chùa"
                },
                OpeningHours = "06:30 - 18:30 hàng ngày",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "info",
                IconClass = "bi-peace"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-TU-DUC",
                Name = "Lăng Tự Đức (Khiêm Lăng)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Thôn Thượng Ba, Phường Thủy Xuân, TP. Huế (Cạnh Làng Hương)",
                Latitude = 16.4331,
                Longitude = 107.5658,
                DistanceFromEcoStayKm = 4.5,
                TravelEstimate = "4.5 km (~15 phút đạp xe Eco-Bike)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Tuyệt tác lăng tẩm mang phong cách thi ca sơn thủy hữu tình bậc nhất của vương triều nhà Nguyễn.",
                Description = "Được xây dựng từ năm 1864 đến 1867, Khiêm Lăng phản ánh tâm hồn thi sĩ u buồn và tinh tế của vua Tự Đức. Toàn bộ gần 50 công trình trong lăng đều có chữ 'Khiêm' trong tên gọi. Không gian hòa quyện giữa hồ sen Lưu Khiêm, nhà hóng mát Xung Khiêm Tạ, nhà hát cổ Minh Khiêm Đường và đồi thông rì rào.",
                ExperienceHighlights = new()
                {
                    "Dừng chân tại Xung Khiêm Tạ ngắm hoa súng nở và đàn cá bơi lội dưới hồ Lưu Khiêm",
                    "Chiêm ngưỡng nhà hát cổ Minh Khiêm Đường — nơi nhà vua từng thưởng trà xem ca vũ",
                    "Đọc tấm bia Khiêm Cung Ký bằng đá thanh nặng 20 tấn do chính vua Tự Đức tự soạn",
                    "Tận hưởng bầu không khí trong lành, an yên rợp bóng thông cổ thụ"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "150.000 VNĐ/người lớn • 30.000 VNĐ/trẻ em",
                BadgeColor = "primary",
                IconClass = "bi-water"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-KHAI-DINH",
                Name = "Lăng Khải Định (Ứng Lăng)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Sườn núi Châu Chữ, Xã Thủy Bằng, Thành phố Huế",
                Latitude = 16.3986,
                Longitude = 107.5904,
                DistanceFromEcoStayKm = 8.5,
                TravelEstimate = "8.5 km (~20 phút đi ô tô hoặc xe máy)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Đỉnh cao nghệ thuật ghép sành sứ và thủy tinh tinh xảo bậc nhất thời Nguyễn, giao thoa Đông - Tây.",
                Description = "Được xây dựng kéo dài suốt 11 năm (1920 - 1931), Ứng Lăng tuy có diện tích khiêm tốn nhưng đòi hỏi công phu tạo tác tột cùng. Bước vào Cung Thiên Định, du khách sẽ choáng ngợp trước những bức phù điêu sành sứ lộng lẫy, bức bích họa 'Cửu Long Ẩn Vân' trên trần điện và pho tượng đồng mạ vàng vua Khải Định đúc tại Pháp.",
                ExperienceHighlights = new()
                {
                    "Vượt qua 127 bậc tam cấp bằng đá uy nghiêm dẫn lên đỉnh đồi Châu Chữ",
                    "Ngắm hàng tượng quan văn, quan võ, voi ngựa hộ lăng tạc bằng đá thanh sống động",
                    "Chiêm ngưỡng nghệ thuật khảm sành sứ đỉnh cao với hàng triệu mảnh gốm sứ rực rỡ",
                    "Bức họa 9 con rồng ẩn hiện trong mây vẽ bằng chân độc nhất vô nhị trên trần Cung Thiên Định"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "150.000 VNĐ/người lớn • 30.000 VNĐ/trẻ em",
                BadgeColor = "primary",
                IconClass = "bi-gem"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-MINH-MANG",
                Name = "Lăng Minh Mạng (Hiếu Lăng)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Núi Cẩm Kê, Xã Hương Thọ, TP. Huế (Gần ngã ba Tuần)",
                Latitude = 16.3889,
                Longitude = 107.5706,
                DistanceFromEcoStayKm = 9.2,
                TravelEstimate = "9.2 km (~22 phút đi xe)",
                ImageUrl = "/images/kien_truc_vuon_hue.jpg",
                Summary = "Quần thể lăng tẩm uy nghiêm với bố cục đăng đối tuyệt đối, chuẩn mực phong thủy Nho giáo.",
                Description = "Được vua Thiệu Trị hoàn thành năm 1843, Hiếu Lăng là một tổng thể kiến trúc gồm 40 công trình lớn nhỏ được bố trí đăng đối nghiêm ngặt trên một trục thần đạo dài 700m. Lăng tựa lưng vào núi, hướng nhìn ra sông, soi bóng xuống hai nửa hồ Trừng Minh uốn lượn như trăng lưỡi liềm.",
                ExperienceHighlights = new()
                {
                    "Dạo bước trên cầu Thông Minh Chính Trực bắc qua hồ sen ngát hương",
                    "Chiêm ngưỡng Cổng Đại Hồng Môn chỉ mở duy nhất một lần khi rước thi hài vua",
                    "Ngắm kiến trúc Điện Sùng Ân với 14 ô chữ Hán khắc thơ văn uyên bác của vua Minh Mạng",
                    "Cảm nhận sự tĩnh mịch, trang nghiêm và đăng đối tuyệt mỹ giữa thiên nhiên và bàn tay con người"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "150.000 VNĐ/người lớn • 30.000 VNĐ/trẻ em",
                BadgeColor = "primary",
                IconClass = "bi-compass"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-AN-HIEN",
                Name = "Nhà Vườn An Hiên (Đệ Nhất Nhà Vườn Xứ Huế)",
                Category = "Nhà Vườn Di Sản",
                Address = "58 Nguyễn Phúc Nguyên, Phường Kim Long, TP. Huế",
                Latitude = 16.4552,
                Longitude = 107.5495,
                DistanceFromEcoStayKm = 0.45,
                TravelEstimate = "450m (~2 phút đi bộ từ Kim Long EcoStay)",
                ImageUrl = "/images/nha_ruong_1.jpg",
                Summary = "Tuyệt tác nhà vườn mẫu mực tiêu biểu nhất của Cố đô với cổng vòm cổ kính và ngôi nhà rường lim 3 gian 2 chái.",
                Description = "An Hiên ban đầu là phủ đệ của công chúa con vua Dục Đức xây dựng cuối thế kỷ 19. Khuôn viên rộng gần 5.000m² nổi bật với cổng tam quan rêu phong, con đường mận quân rợp bóng mát, hồ nước phủ đầy hoa súng và ngôi nhà rường gỗ lim tinh xảo không dùng một chiếc đinh sắt nào.",
                ExperienceHighlights = new()
                {
                    "Dạo bước dưới vòm cây mận quân rợp bóng mát và hít hà hương hoa quỳnh hoa sứ",
                    "Tìm hiểu triết lý phong thủy nhà vườn Huế: cổng, bình phong, hòn non bộ, nhà rường",
                    "Thưởng trà sen Cung Đình và lắng nghe giai điệu ca Huế truyền thống trong sân vườn",
                    "Khám phá các bức hoành phi, câu đối cổ thếp vàng của các bậc đại khoa danh gia"
                },
                OpeningHours = "07:30 - 18:00",
                TicketInfo = "50.000 VNĐ/người (Bao gồm hướng dẫn viên tại điểm)",
                BadgeColor = "success",
                IconClass = "bi-tree-fill"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-VAN-MIEU",
                Name = "Văn Miếu Huế (Văn Thánh)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Đường Văn Thánh, Phường Hương Hồ, TP. Huế (Bờ sông Hương cạnh Kim Long)",
                Latitude = 16.4518,
                Longitude = 107.5388,
                DistanceFromEcoStayKm = 1.4,
                TravelEstimate = "1.4 km (~5 phút đạp xe Eco-Bike)",
                ImageUrl = "/images/nha_ruong_2.jpg",
                Summary = "Thánh đường Nho học triều Nguyễn với 32 tấm bia tiến sĩ khắc tên 293 vị đại khoa hiền tài của đất nước.",
                Description = "Văn Miếu Huế được vua Gia Long khởi công năm 1808 trên ngọn đồi cao sát bờ sông Hương thuộc phường Kim Long. Đây là nơi tôn vinh đạo học, thờ Khổng Tử và lưu danh các bậc hiền tài. Điểm đặc sắc nhất là hai dãy nhà bia gồm 32 tấm bia đá thanh rùa cõng bia hướng về sông Hương thơ mộng.",
                ExperienceHighlights = new()
                {
                    "Đọc tên các vị tiến sĩ triều Nguyễn khắc trên bia đá rùa cõng thiêng liêng",
                    "Chiêm ngưỡng Cổng Văn Miếu cổ kính rêu phong nhìn thẳng ra bờ sông Hương",
                    "Tìm hiểu về khoa cử thời phong kiến và truyền thống hiếu học ngàn đời của dân tộc",
                    "Không gian thanh tịnh, trầm mặc, rất ít du khách thương mại hóa"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "primary",
                IconClass = "bi-mortarboard-fill"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-CUNG-AN-DINH",
                Name = "Cung An Định (Viên Ngọc Tân Cổ Điển)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "179 Phan Đình Phùng, Phường Phú Nhuận, Thành phố Huế (Ven sông An Cựu)",
                Latitude = 16.4578,
                Longitude = 107.5936,
                DistanceFromEcoStayKm = 4.0,
                TravelEstimate = "4.0 km (~12 phút đạp xe / đi xe máy)",
                ImageUrl = "/images/room_hoanghon.jpg",
                Summary = "Lâu đài mùa hè phong cách tân cổ điển châu Âu của vua Khải Định và nơi gắn bó với Hoàng hậu Nam Phương.",
                Description = "Được khởi dựng năm 1917, Cung An Định là sự kết hợp tuyệt vời giữa kiến trúc La Mã - Pháp với hoa văn trang trí cung đình Huế. Tòa lâu đài Khải Tường Lâu 3 tầng nguy nga nổi tiếng với 6 bức tranh tường vẽ bằng sơn dầu miêu tả các lăng tẩm hoàng đế triều Nguyễn và gắn liền với cuộc đời thăng trầm của Đức Từ Cung và Nam Phương Hoàng Hậu.",
                ExperienceHighlights = new()
                {
                    "Ngắm kiến trúc mặt tiền lâu đài Khải Tường Lâu lộng lẫy phong cách Phục Hưng",
                    "Chiêm ngưỡng 6 bức bích họa sơn dầu quý giá về phong cảnh lăng tẩm Huế tại đại sảnh",
                    "Thăm Đình Bát Giác cổ kính và khuôn viên vườn hoa hoàng gia ven sông An Cựu",
                    "Tìm hiểu các kỷ vật và cuộc đời phong sương của Hoàng hậu Nam Phương"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "50.000 VNĐ/người lớn",
                BadgeColor = "primary",
                IconClass = "bi-building-fill-check"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-HON-CHEN",
                Name = "Điện Hòn Chén (Huệ Nam Điện)",
                Category = "Chùa Cổ & Tâm Linh",
                Address = "Núi Ngọc Trản, Thôn Hải Cát, Xã Hương Thọ, TP. Huế (Bên bờ sông Hương)",
                Latitude = 16.4258,
                Longitude = 107.5417,
                DistanceFromEcoStayKm = 5.8,
                TravelEstimate = "5.8 km (Thuận tiện đi thuyền rồng dọc sông Hương từ Kim Long)",
                ImageUrl = "/images/thuyen_rong_hue.jpg",
                Summary = "Quần thể di tích tâm linh thờ Mẫu Thiên Y A Na trên vách núi Ngọc Trản soi bóng khúc sông Hương hùng vĩ.",
                Description = "Điện Hòn Chén giữ vị trí độc nhất vô nhị trong đời sống tâm linh xứ Huế, kết hợp hài hòa giữa tín ngưỡng dân gian thờ Mẫu của người Chăm - Việt và nghi lễ cung đình triều Nguyễn. Hàng năm vào tháng Ba và tháng Bảy âm lịch, lễ hội rước sắc trên sông Hương thu hút hàng vạn tín đồ áo mão rực rỡ chèo thuyền trẩy hội.",
                ExperienceHighlights = new()
                {
                    "Trải nghiệm đi thuyền rồng từ bến Kim Long lướt sóng ngắm sông Hương đến chân vách núi Ngọc Trản",
                    "Chiêm bái đền Minh Kính Đài với các pho tượng thờ Mẫu uy nghi và đồ thờ pháp khí cổ xưa",
                    "Hòa mình vào không khí lễ hội rước Thánh Mẫu rộn rã cờ hoa, hát văn hầu đồng truyền thống",
                    "Ngắm trọn khúc cua cong uốn lượn đẹp nhất của dòng sông Hương từ mỏm núi đá cao"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "50.000 VNĐ/người lớn (Chưa bao gồm tiền thuyền)",
                BadgeColor = "info",
                IconClass = "bi-clouds-fill"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-HO-QUYEN",
                Name = "Đấu Trường Hổ Quyền & Điện Voi Ré",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Kiệt 168 Bùi Thị Xuân, Phường Thủy Biều, TP. Huế (Bờ nam đối diện Kim Long)",
                Latitude = 16.4442,
                Longitude = 107.5531,
                DistanceFromEcoStayKm = 2.2,
                TravelEstimate = "2.2 km (~6 phút đạp xe qua cầu Bạch Hổ / Dã Viên)",
                ImageUrl = "/images/kien_truc_vuon_hue.jpg",
                Summary = "Đấu trường sinh tử giữa voi và hổ thời nhà Nguyễn — chứng tích đấu trường lộ thiên duy nhất ở châu Á.",
                Description = "Xây dựng năm 1830 dưới thời vua Minh Mạng, Hổ Quyền là đấu trường lộ thiên hình tròn bằng gạch vồ kiên cố, nơi tổ chức các trận tử chiến giữa voi (tượng trưng cho sức mạnh chính nghĩa của triều đình) và hổ (tượng trưng cho cái ác). Liền kề đó là Điện Voi Ré — ngôi đền thờ các thần dũng tướng voi chiến trung thành của quân đội hoàng gia.",
                ExperienceHighlights = new()
                {
                    "Dạo bước trên khán đài dành cho vua và các hoàng thân ngắm xuống lòng đấu trường",
                    "Quan sát 5 chuồng cọp với then cài gỗ lim và dấu móng vuốt cọp còn hằn trên vách đá",
                    "Thăm Điện Voi Ré với hai bức tượng voi chiến đúc bằng đá và các mộ voi linh thiêng",
                    "Tìm hiểu về nghệ thuật huấn luyện thớt voi chiến tinh nhuệ của quân đội triều Nguyễn"
                },
                OpeningHours = "07:30 - 17:30",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "primary",
                IconClass = "bi-shield-shaded"
            },

            new HeritageLocation
            {
                Id = "HERITAGE-TU-HIEU",
                Name = "Chùa Từ Hiếu (Ngôi Chùa Thái Giám & Rừng Thông)",
                Category = "Chùa Cổ & Tâm Linh",
                Address = "Thôn Dương Xuân Thượng, Phường Thủy Xuân, TP. Huế (Gần Làng Hương)",
                Latitude = 16.4385,
                Longitude = 107.5742,
                DistanceFromEcoStayKm = 4.8,
                TravelEstimate = "4.8 km (~15 phút đạp xe Eco-Bike)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Ngôi cổ tự thanh tịnh nép mình giữa rừng thông bạt ngàn, nơi lưu giữ nghĩa trang thái giám triều Nguyễn và gắn liền với Thiền sư Thích Nhất Hạnh.",
                Description = "Được khai sơn từ năm 1843, Chùa Từ Hiếu nổi tiếng với câu chuyện hiếu thảo của nhà sư Nhất Định nuôi mẹ già ốm nặng. Về sau, các quan thái giám triều Nguyễn đã quyên góp tiền của để đại trùng tu chùa làm nơi nương tựa tuổi già và làm nghĩa trang an nghỉ cho mình. Đây cũng là nơi Thiền sư Thích Nhất Hạnh xuất gia tu học và an dưỡng những năm tháng cuối đời.",
                ExperienceHighlights = new()
                {
                    "Bách bộ dưới rặng thông reo rì rào và ngắm đàn cá tung tăng dưới hồ bán nguyệt phủ hoa súng",
                    "Viếng nghĩa trang thái giám độc nhất vô nhị ở Việt Nam với hơn 20 ngôi mộ cổ",
                    "Chiêm bái gian phòng thiền và nơi tĩnh dưỡng của Thiền sư Thích Nhất Hạnh",
                    "Hít thở không khí thanh tịnh, thiền hành và thưởng thức cơm chay thanh đạm nhà chùa"
                },
                OpeningHours = "06:00 - 18:00",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "info",
                IconClass = "bi-brightness-high-fill"
            },

            // =========================================================================
            // III. ĐỊA ĐIỂM ẨM THỰC TRUYỀN THỐNG & ĐẶC SẢN XỨ HUẾ
            // =========================================================================
            new HeritageLocation
            {
                Id = "FOOD-BANH-UOT",
                Name = "Bánh Ướt Thịt Nướng Huyền Anh (Đệ Nhất Kim Long)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "50 Kim Long, Phường Kim Long, TP. Huế (Cách Homestay 250m)",
                Latitude = 16.4565,
                Longitude = 107.5512,
                DistanceFromEcoStayKm = 0.25,
                TravelEstimate = "250m (~1 phút đi bộ • Giao nóng tận phòng 15 phút)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Quán bánh ướt thịt nướng gia truyền lừng danh nhất xứ Huế, thơm nức hương sả mè than hoa.",
                Description = "Được vinh danh trong cẩm nang ẩm thực uy tín và nức tiếng trong câu ca dao xứ Huế. Từng lát bánh ướt tráng mỏng mềm mướt như lụa cuốn thịt ba chỉ ướp gia vị nướng trên than hoa đỏ rực, chấm nước tương đậu mè bùi béo gia truyền.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức suất bánh ướt thịt nướng than hoa nóng hổi vừa nướng xong",
                    "Thử món bánh ướt tôm chua thịt luộc chấm ruốc Huế cay xé lưỡi",
                    "Khám phá kỹ thuật tráng bánh ướt mỏng tang trên nồi hơi vải truyền thống",
                    "Kim Long EcoStay liên kết độc quyền giao nóng tận phòng chỉ trong 15 phút"
                },
                OpeningHours = "08:30 - 21:00 hàng ngày",
                TicketInfo = "Món ăn từ 35.000 - 65.000 VNĐ/suất",
                BadgeColor = "danger",
                IconClass = "bi-egg-fried"
            },
            new HeritageLocation
            {
                Id = "FOOD-BUN-BO",
                Name = "Bún Bò Huế Mụ Rơi (Cổ Truyền Cố Đô)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "38 Trần Cao Vân, Phường Phú Hội, TP. Huế",
                Latitude = 16.4632,
                Longitude = 107.5956,
                DistanceFromEcoStayKm = 4.8,
                TravelEstimate = "4.8 km (~14 phút xe đạp Eco-Bike / taxi)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Quán bún bò Huế gia truyền với nước dùng xương hầm thanh ngọt và hương sả ruốc quyến rũ.",
                Description = "Nước dùng bún bò được hầm từ xương bò và bắp bò suốt 12 tiếng, thơm lừng mùi sả tươi và mắm ruốc Huế đặc trưng. Tô bún đầy đặn với thịt bắp hoa giòn sần sật, chả cua quết tay vàng ươm, khoanh giò heo mềm rục ăn kèm rau sống hoa chuối bắp.",
                ExperienceHighlights = new()
                {
                    "Nếm thử nước dùng nguyên bản thơm cay chuẩn vị Cố đô Huế",
                    "Viên chả cua bể quết tay giòn ngọt bùi thơm nức tiếng",
                    "Ăn kèm ớt sa tế ngâm giấm tỏi và dĩa rau bắp chuối tươi non",
                    "Điểm ăn sáng quen thuộc hàng chục năm của người dân địa phương"
                },
                OpeningHours = "06:00 - 10:30 (Thường hết sớm)",
                TicketInfo = "Từ 40.000 - 60.000 VNĐ/tô",
                BadgeColor = "danger",
                IconClass = "bi-cup-hot-fill"
            },
            new HeritageLocation
            {
                Id = "FOOD-BANH-BEO",
                Name = "Quán Bánh Bèo - Nậm - Lọc Bà Đỏ",
                Category = "Ẩm Thực Xứ Huế",
                Address = "08 Nguyễn Bỉnh Khiêm, Phường Phú Cát, TP. Huế",
                Latitude = 16.4746,
                Longitude = 107.5901,
                DistanceFromEcoStayKm = 4.2,
                TravelEstimate = "4.2 km (~12 phút đạp xe)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Địa chỉ thưởng thức trọn vẹn tinh hoa khay bánh bèo chén, bánh nậm tôm thịt và bánh bột lọc Huế.",
                Description = "Quán Bà Đỏ là thương hiệu bánh Huế truyền thống hơn 50 năm tuổi. Từng chén bánh bèo mỏng tang xoáy tròn rắc tôm chấy đỏ au và tóp mỡ giòn rụm, bánh nậm thơm nức mùi lá chuối và bánh bột lọc trong suốt nhìn thấy rõ con tôm sông rim đậm đà.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức mâm thập cẩm: bánh bèo, bánh nậm, bánh lọc, bánh ram ít, chả tôm",
                    "Chan nước mắm ớt xanh ngọt nhẹ pha theo công thức hoàng gia",
                    "Quan sát các o, các mệ thoăn thoắt gói bánh nậm bằng lá chuối tươi",
                    "Đóng gói bánh lọc sống hút chân không mang về làm quà biếu chuẩn vị"
                },
                OpeningHours = "08:00 - 21:30 hàng ngày",
                TicketInfo = "Từ 40.000 - 80.000 VNĐ/phần",
                BadgeColor = "danger",
                IconClass = "bi-basket-fill"
            },
            new HeritageLocation
            {
                Id = "FOOD-COM-HEN",
                Name = "Cơm Hến & Bún Hến Hoa Đông (Đặc Sản Cồn Hến)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "64 Kiệt 7 Ưng Bình, Thôn Cồn Hến, Phường Vỹ Dạ, TP. Huế",
                Latitude = 16.4715,
                Longitude = 107.6085,
                DistanceFromEcoStayKm = 5.8,
                TravelEstimate = "5.8 km (~18 phút đạp xe qua cầu Đập Đá & Cồn Hến)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Thủ phủ Cơm hến xứ Cố đô bên dòng sông Hương với vị cay nồng xé lưỡi và nước hến ngọt lịm.",
                Description = "Cồn Hến là bãi bồi giữa sông Hương sản sinh ra loài hến nhỏ nhưng thịt ngọt đậm đà nhất xứ Huế. Món cơm hến kết hợp 14 thứ nguyên liệu: hến xào, cơm nguội, bắp chuối, rau thơm, đậu phộng rang, da heo chiên giòn, ruốc ớt cay xè và chén nước luộc hến nóng hổi bốc khói.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức tô cơm hến hoặc bún hến đúng điệu của người Huế xưa",
                    "Cảm nhận vị bùi béo của đậu phộng, giòn của da heo và cay nồng của ớt xiêm",
                    "Húp ngụm nước canh hến trong veo ngọt lành xua tan cái nóng",
                    "Ngắm khung cảnh êm đềm thôn Vỹ Dạ và rặng bắp xanh mướt giữa lòng sông"
                },
                OpeningHours = "06:30 - 20:00 hàng ngày",
                TicketInfo = "Bình dân chỉ từ 15.000 - 25.000 VNĐ/tô",
                BadgeColor = "danger",
                IconClass = "bi-fire"
            },
            new HeritageLocation
            {
                Id = "FOOD-CHE-HEM",
                Name = "Chè Hẻm Cố Đô (Chè Bột Lọc Heo Quay & Sen Nhãn)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "Số 1 Kiệt 29 Hùng Vương, Phường Phú Hội, TP. Huế",
                Latitude = 16.4651,
                Longitude = 107.5928,
                DistanceFromEcoStayKm = 4.5,
                TravelEstimate = "4.5 km (~13 phút đạp xe)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Quán chè cổ xưa nức tiếng với món chè bột lọc bọc heo quay mặn ngọt độc nhất vô nhị xứ Huế.",
                Description = "Ẩm thực chè Huế có hơn 36 món chè cung đình và dân dã. Quán Chè Hẻm ẩn mình sâu trong con ngõ nhỏ đường Hùng Vương đã tồn tại hơn 40 năm. Món chè bột lọc heo quay với viên bột lọc trong suốt bọc thịt ba chỉ quay giòn giòn, ăn cùng nước đường phèn gừng ấm nồng tạo nên sự bùng nổ vị giác độc đáo.",
                ExperienceHighlights = new()
                {
                    "Nếm thử món chè bột lọc bọc thịt heo quay trứ danh không nơi nào có",
                    "Thưởng thức chè hạt sen nhãn lồng thanh tao, ngọt dịu hồ Tịnh Tâm",
                    "Thử chè môn sáp vàng, chè đậu ngự, chè bắp non ngọt mát",
                    "Quán mở cửa đến đêm muộn, điểm dừng chân thư giãn lý tưởng"
                },
                OpeningHours = "10:30 - 22:00 hàng ngày",
                TicketInfo = "Từ 15.000 - 20.000 VNĐ/ly",
                BadgeColor = "danger",
                IconClass = "bi-cup-straw"
            },
            new HeritageLocation
            {
                Id = "FOOD-CA-PHE-MUOI",
                Name = "Quán Cà Phê Muối Xứ Huế (Số 10 Nguyễn Lương Bằng)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "10 Nguyễn Lương Bằng, Phường Phú Nhuận, TP. Huế",
                Latitude = 16.4558,
                Longitude = 107.5886,
                DistanceFromEcoStayKm = 3.8,
                TravelEstimate = "3.8 km (~11 phút đạp xe)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Nơi khởi nguồn sáng tạo món Cà phê muối lừng danh khắp cả nước của đôi vợ chồng cố đô năm 2010.",
                Description = "Món cà phê muối ra đời năm 2010 tại căn nhà vườn yên tĩnh số 10 Nguyễn Lương Bằng. Vị đắng đậm của cà phê phin truyền thống được làm dịu bởi lớp kem sữa muối béo mặn nhẹ và sữa đặc ngọt ngào, tạo nên thức uống làm say đắm hàng triệu du khách khi đến Huế.",
                ExperienceHighlights = new()
                {
                    "Uống ly cà phê muối nguyên bản thơm ngậy tại chính nơi sáng tạo ra món này",
                    "Thư giãn trong không gian sân vườn Huế yên tĩnh rợp bóng cây xanh",
                    "Tìm hiểu câu chuyện khởi nghiệp sáng tạo gắn với văn hóa ẩm thực Cố đô",
                    "Thưởng thức cà phê đá xay muối và các loại trà hoa thảo mộc Huế"
                },
                OpeningHours = "06:30 - 22:00 hàng ngày",
                TicketInfo = "Từ 20.000 - 35.000 VNĐ/ly",
                BadgeColor = "danger",
                IconClass = "bi-cup-fill"
            },
            new HeritageLocation
            {
                Id = "FOOD-BANH-CANH",
                Name = "Bánh Canh Nam Phổ Thúy (Làng Cổ Nam Phổ)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "16 Phạm Hồng Thái, Phường Vĩnh Ninh, TP. Huế",
                Latitude = 16.4612,
                Longitude = 107.5895,
                DistanceFromEcoStayKm = 3.9,
                TravelEstimate = "3.9 km (~12 phút đạp xe)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Món quà chiều dân dã với nước súp sánh sệt màu đỏ gạch tôm cua độc đáo của làng cổ Nam Phổ.",
                Description = "Bánh canh Nam Phổ có nguồn gốc từ làng Nam Phổ huyện Phú Vang từ hàng trăm năm trước. Khác với bánh canh sợi thông thường, sợi bánh canh bột gạo được nấu nhuyễn sánh sệt cùng nước hầm tôm thịt và màu đỏ gạch tự nhiên của tôm đất giã nhuyễn, rắc thêm hành hoa thái nhỏ và ớt xanh cay nồng.",
                ExperienceHighlights = new()
                {
                    "Món quà chiều trứ danh của các gánh hàng rong xứ Huế xưa",
                    "Nước bánh canh đỏ au óng ánh vị ngọt đậm đà từ thịt tôm đồng",
                    "Thưởng thức cùng chả quết dai giòn và nước mắm ớt chỉ thiên cay nồng",
                    "Món ăn nhẹ nhàng dễ tiêu, chuẩn vị cố đô"
                },
                OpeningHours = "12:30 - 18:30 hàng ngày",
                TicketInfo = "Chỉ từ 20.000 - 30.000 VNĐ/tô",
                BadgeColor = "danger",
                IconClass = "bi-soup"
            },
            new HeritageLocation
            {
                Id = "FOOD-COM-CHAY",
                Name = "Cơm Chay Thanh Liễu (Ẩm Thực Chay Thiền Môn)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "50 Nguyễn Công Trứ, Phường Phú Hội, TP. Huế",
                Latitude = 16.4628,
                Longitude = 107.5982,
                DistanceFromEcoStayKm = 5.0,
                TravelEstimate = "5.0 km (~15 phút đạp xe)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Ẩm thực chay thanh tịnh, tinh hoa văn hóa ẩm thực Phật giáo xứ Huế với hơn 50 món ăn bổ dưỡng.",
                Description = "Huế là thủ phủ Phật giáo với truyền thống ăn chay lâu đời từ chốn hoàng tộc triều Nguyễn đến thường dân. Quán chay Thanh Liễu nổi tiếng với các món cơm chay, bún bò chay, lẩu nấm và chả giò sen được chế biến tinh tế từ rau củ, nấm tươi và thảo mộc thiên nhiên thanh lành.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức mâm cơm chay thanh đạm với các món đậu hũ non sốt nấm, canh chua hoa chuối",
                    "Chiêm ngưỡng nghệ thuật tạo hình món ăn chay tỉ mỉ như món mặn của nghệ nhân Huế",
                    "Bữa ăn thanh lọc cơ thể và tìm về sự bình yên cho tâm hồn",
                    "Giá cả cực kỳ bình dân, đón tiếp niềm nở chuẩn người Huế"
                },
                OpeningHours = "07:00 - 20:30 hàng ngày",
                TicketInfo = "Từ 25.000 - 50.000 VNĐ/món",
                BadgeColor = "danger",
                IconClass = "bi-flower3"
            },

            // =========================================================================
            // IV. DANH THẮNG & ĐIỂM ĐẾN VĂN HÓA BỔ SUNG
            // =========================================================================
            new HeritageLocation
            {
                Id = "HERITAGE-DOI-VONG-CANH",
                Name = "Đồi Vọng Cảnh (Điểm Ngắm Khúc Quanh Sông Hương Đẹp Nhất)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Đường Huyền Trân Công Chúa, Phường Thủy Biều, TP. Huế",
                Latitude = 16.4258,
                Longitude = 107.5620,
                DistanceFromEcoStayKm = 4.6,
                TravelEstimate = "4.6 km (~14 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Điểm ngắm hoàng hôn triệu đô ôm trọn khúc cua đẹp nhất của dòng sông Hương và rặng thông xanh biếc.",
                Description = "Đồi Vọng Cảnh từng là nơi các vua triều Nguyễn thường ngự cảnh nghỉ chân để thưởng ngoạn vẻ đẹp sông Hương và lăng tẩm hoàng tộc. Đứng từ đỉnh đồi nhìn xuống, sông Hương như dải lụa mềm uốn lượn qua các vườn bưởi Thanh Trà Thủy Biều, rặng thông reo rì rào trong gió chiều ráng đỏ.",
                ExperienceHighlights = new()
                {
                    "Ngắm hoàng hôn ráng vàng buông xuống khúc quanh ngoạn mục của dòng sông Hương",
                    "Thư giãn hít thở bầu không khí rừng thông trong lành, tĩnh mịch",
                    "Ghé thăm các lô cốt quân sự lịch sử thời chiến tranh",
                    "Chụp những bức ảnh check-in thiên nhiên thơ mộng chuẩn chất Huế"
                },
                OpeningHours = "Mở cửa tự do 24/7 (Đẹp nhất từ 16:30 - 18:00)",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "warning",
                IconClass = "bi-sunset-fill"
            },
            new HeritageLocation
            {
                Id = "HERITAGE-CAU-TRUONG-TIEN",
                Name = "Cầu Trường Tiền (Biểu Tượng 127 Năm Sông Hương)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Bắc qua sông Hương nối đường Lê Lợi và đường Trần Hưng Đạo, TP. Huế",
                Latitude = 16.4682,
                Longitude = 107.5878,
                DistanceFromEcoStayKm = 3.9,
                TravelEstimate = "3.9 km (~12 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Cây cầu thép huyền thoại sáu vài mười hai nhịp bắc qua sông Hương — chứng nhân lịch sử hơn một thế kỷ của Cố đô.",
                Description = "Được khánh thành năm 1899 dưới thời vua Thành Thái do hãng Eiffel (Pháp) thiết kế, Cầu Trường Tiền đã đi vào thi ca nhạc họa như một biểu tượng trường tồn của xứ Huế. Đêm về, hệ thống đèn đổi màu rực rỡ soi bóng xuống mặt nước lung linh huyền ảo.",
                ExperienceHighlights = new()
                {
                    "Đi dạo trên lối đi bộ riêng biệt ngắm trọn vẹn dòng sông Hương êm đềm",
                    "Chiêm ngưỡng hoa phượng đỏ rực rỡ hai đầu cầu vào mùa hè",
                    "Ngắm cầu đổi màu lung linh rực rỡ vào các buổi tối cuối tuần",
                    "Thưởng thức kem Tràng Tiền hoặc bánh mì đêm chân cầu"
                },
                OpeningHours = "Mở cửa 24/7 (Phố đi bộ cuối tuần cấm xe cơ giới)",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "warning",
                IconClass = "bi-bridge"
            },
            new HeritageLocation
            {
                Id = "HERITAGE-CHO-DONG-BA",
                Name = "Chợ Đông Ba (Khu Chợ Truyền Thống 125 Năm Tuổi)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Số 02 Trần Hưng Đạo, Phường Phú Hòa, TP. Huế (Bờ Bắc sông Hương)",
                Latitude = 16.4718,
                Longitude = 107.5872,
                DistanceFromEcoStayKm = 4.1,
                TravelEstimate = "4.1 km (~13 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/nha_ruong_2.jpg",
                Summary = "Khu chợ sầm uất bậc nhất Cố đô — thiên đường mua sắm đặc sản mắm Huế, nón bài thơ và ẩm thực dân dã.",
                Description = "Chợ Đông Ba hình thành từ thời vua Gia Long và dời về vị trí hiện nay năm 1899 dưới thời vua Thành Thái. Chợ có kiến trúc 3 tầng hình chuông úp, là nơi tụ hội tinh hoa ẩm thực đường phố, các quầy vải may áo dài, nón lá bài thơ, mè xửng, tôm chua và các món chè cung đình thơm ngon.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức bún nghệ xào lòng heo, bánh lọc trần, nem lụi ngay tại tầng trệt",
                    "Mua sắm đặc sản mè xửng, tôm chua Trọng Tín, nón lá bài thơ làm quà",
                    "Trải nghiệm nhịp sống giao thương buôn bán chân chất của các mệ, các o xứ Huế",
                    "Khám phá các gian hàng vải lụa gấm may áo dài cổ truyền"
                },
                OpeningHours = "05:00 - 19:30 hàng ngày",
                TicketInfo = "Tham quan mua sắm tự do",
                BadgeColor = "warning",
                IconClass = "bi-shop"
            },
            new HeritageLocation
            {
                Id = "HERITAGE-QUOC-HOC",
                Name = "Trường Quốc Học Huế (Ngôi Trường Cổ Kính 1896 Ven Sông Hương)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "12 Lê Lợi, Phường Vĩnh Ninh, TP. Huế (Đối diện Đài Tưởng Niệm)",
                Latitude = 16.4630,
                Longitude = 107.5835,
                DistanceFromEcoStayKm = 3.5,
                TravelEstimate = "3.5 km (~10 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/kien_truc_vuon_hue.jpg",
                Summary = "Ngôi trường trung học đầu tiên tại Huế xây dựng năm 1896 mang sắc đỏ mận cổ kính và kiến trúc giao thoa Pháp - Việt.",
                Description = "Quốc Học Huế là cái nôi ươm mầm nhiều danh nhân lỗi lạc của dân tộc, tiêu biểu là Chủ tịch Hồ Chí Minh (Nguyễn Tất Thành), Đại tướng Võ Nguyên Giáp. Khuôn viên rợp bóng cây điệp anh đào nở hoa hồng rực rỡ vào mùa xuân dưới nền tường đỏ mận cổ kính.",
                ExperienceHighlights = new()
                {
                    "Chiêm ngưỡng kiến trúc tân cổ điển Pháp kết hợp hoa văn cung đình Huế",
                    "Chụp ảnh lưu niệm tại hàng cây điệp anh đào mùa hoa nở rộ",
                    "Thăm tượng đài thời trẻ của Chủ tịch Hồ Chí Minh trong sân trường",
                    "Dạo bước trên cung đường rợp bóng cây xanh đẹp nhất xứ Huế đường Lê Lợi"
                },
                OpeningHours = "Tham quan ngoài giờ học & cuối tuần (07:00 - 17:30)",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "warning",
                IconClass = "bi-mortarboard-fill"
            },
            new HeritageLocation
            {
                Id = "HERITAGE-LANG-GIA-LONG",
                Name = "Lăng Vua Gia Long (Thiên Thọ Lăng Giữa 36 Ngọn Núi)",
                Category = "Quần Thể Di Tích Cố Đô",
                Address = "Xã Hương Thọ, TP. Huế (Nằm trên ngọn đồi Thiên Thọ ven sông Tả Trạch)",
                Latitude = 16.3980,
                Longitude = 107.5950,
                DistanceFromEcoStayKm = 9.2,
                TravelEstimate = "9.2 km (~25 phút đi xe máy / thuyền rồng)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Nơi an nghỉ của vị hoàng đế sáng lập triều Nguyễn giữa bức tranh thiên nhiên sơn thủy hoang sơ hùng vĩ nhất.",
                Description = "Lăng Gia Long là quần thể lăng tẩm tĩnh mịch và rộng lớn nhất với 36 ngọn núi bao quanh. Điểm đặc biệt nhất là hai ngôi mộ đá nguyên khối của vua Gia Long và Thừa Thiên Cao Hoàng hậu được đặt song táng bên nhau, thể hiện tình nghĩa vợ chồng son sắt cùng trải qua bao gian nan khởi nghiệp.",
                ExperienceHighlights = new()
                {
                    "Ngắm bức tranh phong thủy tuyệt mỹ giữa đại ngàn thông reo và hồ sen bán nguyệt",
                    "Chiêm bái khu mộ song táng bằng đá thanh cổ kính nguyên vẹn",
                    "Đi qua cầu phao nối hai bờ sông Tả Trạch hoang sơ",
                    "Tận hưởng không gian tĩnh lặng thoát tục tuyệt đối"
                },
                OpeningHours = "07:30 - 17:00 hàng ngày",
                TicketInfo = "150.000 VNĐ/người lớn (Vé Trung tâm BTDTCĐ Huế)",
                BadgeColor = "warning",
                IconClass = "bi-bank"
            },

            // =========================================================================
            // V. ĐỊA ĐIỂM ẨM THỰC TRỨ DANH BỔ SUNG
            // =========================================================================
            new HeritageLocation
            {
                Id = "FOOD-BANH-EP",
                Name = "Bánh Ép Huế Chị Huệ (Đặc Sản Đường Phố Nóng Hổi)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "116 Lê Ngô Cát, Phường Thủy Xuân, TP. Huế (Gần Làng Hương)",
                Latitude = 16.4410,
                Longitude = 107.5670,
                DistanceFromEcoStayKm = 4.3,
                TravelEstimate = "4.3 km (~13 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Món ăn vặt số 1 của giới trẻ Cố đô với miếng bánh ép dẻo dai nóng hổi kẹp pate, trứng và thịt băm.",
                Description = "Bột lọc tươi được vo viên kẹp thịt mỡ, hành lá rồi ép giòn tan giữa hai tấm gang đúc đỏ rực lửa than hoa. Bánh được đập thêm trứng gà, phết pate thơm nức, cuốn cùng rau răm, dưa leo chua ngọt và chấm chén nước mắm ớt tỏi sền sệt bùng nổ hương vị.",
                ExperienceHighlights = new()
                {
                    "Ăn từng chiếc bánh ép vừa mới lấy ra từ khuôn gang nóng hổi",
                    "Chấm đẫm chén nước mắm ớt chua cay thơm nức mùi ớt Huế",
                    "Uống ly trà sữa thái xanh hoặc nước sâm dứa mát lạnh",
                    "Món quà chiều giá cực kỳ sinh viên chỉ từ 4.000 - 6.000 VNĐ/chiếc"
                },
                OpeningHours = "13:30 - 21:00 hàng ngày",
                TicketInfo = "Từ 20.000 - 40.000 VNĐ/người ăn no",
                BadgeColor = "danger",
                IconClass = "bi-fire"
            },
            new HeritageLocation
            {
                Id = "FOOD-BANH-KHOAI",
                Name = "Bánh Khoái Hoàng Gia Lạc Thiện (Cửa Thượng Tứ)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "06 Đinh Tiên Hoàng, Phường Đông Ba, TP. Huế (Cửa Thượng Tứ Đại Nội)",
                Latitude = 16.4715,
                Longitude = 107.5835,
                DistanceFromEcoStayKm = 3.8,
                TravelEstimate = "3.8 km (~11 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Thương hiệu bánh khoái cung đình hơn 60 năm với vỏ bánh giòn rụm và chén nước lèo đậu phụng gan heo bí truyền.",
                Description = "Bánh khoái được đổ trên chảo gang nhỏ, bột gạo pha nghệ vàng óng cuộn tôm sông tươi rói, thịt ba chỉ, nấm mèo và giá đỗ. Điểm cốt lõi làm nên danh tiếng Lạc Thiện chính là chén nước lèo nâu óng nấu từ gan heo, thịt nạc giã nhuyễn, mè và đậu phụng thơm ngậy bùi béo.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức chiếc bánh khoái vàng ruộm giòn tan nóng hổi vừa bưng ra",
                    "Cuộn cùng trái vả chát, chuối chát, khế chua và rau thơm xanh non",
                    "Chấm chén nước lèo gan đậu bí truyền 3 đời",
                    "Gặp gỡ gia đình cụ Lạc Thiện với nghệ thuật mở nắp chai bia độc đáo"
                },
                OpeningHours = "09:00 - 21:30 hàng ngày",
                TicketInfo = "Từ 35.000 - 55.000 VNĐ/cái",
                BadgeColor = "danger",
                IconClass = "bi-pie-chart-fill"
            },
            new HeritageLocation
            {
                Id = "FOOD-CHE-MO-TON-DICH",
                Name = "Chè Mợ Tôn Đích (Chè Heo Quay Cửa Thượng Tứ)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "20 Đinh Tiên Hoàng, Phường Đông Ba, TP. Huế (Trước Cửa Thượng Tứ)",
                Latitude = 16.4725,
                Longitude = 107.5828,
                DistanceFromEcoStayKm = 3.7,
                TravelEstimate = "3.7 km (~11 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Quán chè đêm sầm uất nhất Cố đô trước cổng thành với hơn 20 nồi chè cung đình nghi ngút khói.",
                Description = "Mỗi khi hoàng hôn buông xuống, quán chè Mợ Tôn Đích lại sáng rực đèn trước cổng thành Thượng Tứ. Khách chen chân thưởng thức từng ly chè bột lọc bọc heo quay vỏ trong suốt, chè khoai tía tím lịm, chè sen long nhãn hồ Tịnh Tâm bùi ngọt thơm thảo.",
                ExperienceHighlights = new()
                {
                    "Nếm thử chè bột lọc heo quay chuẩn gốc gia truyền",
                    "Thưởng thức ly chè khoai tía sánh mịn màu tím mộng mơ xứ Huế",
                    "Ngồi ngắm cổng thành Cố đô về đêm lung linh ánh đèn",
                    "Mở cửa tới khuya, điểm hẹn hò thú vị cho du khách"
                },
                OpeningHours = "16:00 - 23:00 hàng ngày",
                TicketInfo = "Đồng giá từ 15.000 - 25.000 VNĐ/ly",
                BadgeColor = "danger",
                IconClass = "bi-cup-straw"
            },
            new HeritageLocation
            {
                Id = "FOOD-BANH-MI-TRUONG-TIEN",
                Name = "Bánh Mì Đêm Cầu Trường Tiền (Bánh Mì O Tho)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "14 Trần Hưng Đạo, Phường Phú Hòa, TP. Huế (Đầu cầu Trường Tiền bờ Bắc)",
                Latitude = 16.4695,
                Longitude = 107.5865,
                DistanceFromEcoStayKm = 4.0,
                TravelEstimate = "4.0 km (~12 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/mon_banhuot.jpg",
                Summary = "Xe bánh mì đêm huyền thoại bên chân cầu Trường Tiền nướng than hoa giòn rụm với nước sốt đậm đà cay nồng.",
                Description = "Được mệnh danh là 'cứu tinh về đêm' của người dân và du khách Cố đô. Ổ bánh mì nhỏ thon được hơ trên bếp than hoa ấm giòn, nhân ngập tràn thịt xíu nạc mềm, pate gan béo ngậy, chả lụa, nem chua, chan nước sốt thịt kho sánh đặc và ớt rim Huế cay xé lưỡi.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức ổ bánh mì bột lọc giòn rụm chỉ có ở xứ Huế",
                    "Nhâm nhi ổ bánh mì nóng hổi ngắm sông Hương về đêm tĩnh lặng",
                    "Nếm thử vị ớt rim cay nồng đặc trưng khó quên",
                    "Xe bánh mì tấp nập khách từ chập tối đến tận 2 - 3 giờ sáng"
                },
                OpeningHours = "17:30 - 02:00 sáng hôm sau",
                TicketInfo = "Chỉ từ 15.000 - 25.000 VNĐ/ổ",
                BadgeColor = "danger",
                IconClass = "bi-egg-fill"
            },
            new HeritageLocation
            {
                Id = "FOOD-CA-PHE-RUNG-THONG",
                Name = "Cà Phê Rừng Thông Sunset (View Đồi Vọng Cảnh)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "Khu vực Đồi Vọng Cảnh, Đường Huyền Trân Công Chúa, Phường Thủy Biều, TP. Huế",
                Latitude = 16.4270,
                Longitude = 107.5615,
                DistanceFromEcoStayKm = 4.7,
                TravelEstimate = "4.7 km (~14 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Quán cà phê gỗ mộc giữa đồi thông thơ mộng ngắm hoàng hôn buông xuống dòng sông Hương.",
                Description = "Nép mình dưới tán thông già lộng gió, quán được thiết kế bằng gỗ mộc mạc hòa mình vào thiên nhiên. Đây là địa điểm dừng chân lý tưởng sau hành trình đạp xe từ Kim Long EcoStay lên Đồi Vọng Cảnh để nhâm nhi ly trà hoa sen hoặc cà phê muối đậm đà.",
                ExperienceHighlights = new()
                {
                    "Ngồi ghế bành gỗ ngắm mặt trời đỏ ối lặn dần sau rặng núi Kim Phụng",
                    "Thưởng thức cà phê muối đá xay và bánh ngọt trà xanh thơm dịu",
                    "Lắng nghe tiếng thông reo và tiếng chuông chùa xa xăm thanh bình",
                    "Góc check-in 'chữa lành' được yêu thích nhất của du khách"
                },
                OpeningHours = "06:30 - 22:00 hàng ngày",
                TicketInfo = "Đồ uống từ 25.000 - 45.000 VNĐ",
                BadgeColor = "danger",
                IconClass = "bi-cup-hot"
            },
            new HeritageLocation
            {
                Id = "FOOD-NHA-HANG-Y-THAO",
                Name = "Nhà Hàng Ẩm Thực Cung Đình Y Thảo Garden (Nhà Vườn Vỹ Dạ)",
                Category = "Ẩm Thực Xứ Huế",
                Address = "03 Thạch Hãn, Phường Thuận Hòa, TP. Huế (Trong Kinh Thành)",
                Latitude = 16.4710,
                Longitude = 107.5750,
                DistanceFromEcoStayKm = 3.2,
                TravelEstimate = "3.2 km (~9 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/nha_ruong_1.jpg",
                Summary = "Không gian nhà rường cổ kính phục vụ đại tiệc ẩm thực Cung đình triều Nguyễn được chạm khắc công phu.",
                Description = "Y Thảo Garden tọa lạc trong một khuôn viên nhà rường Huế cổ kính với hoa trái sum suê. Nơi đây tái hiện nghệ thuật ẩm thực yến tiệc hoàng cung với các món chả công, nem phụng được cắt tỉa từ củ quả sống động như tác phẩm điêu khắc nghệ thuật.",
                ExperienceHighlights = new()
                {
                    "Thưởng thức set menu Cung đình triều Nguyễn: Nem phụng hoàng, Chả công hoàng gia",
                    "Trải nghiệm bữa ăn quý phái trong không gian nhà rường gỗ chạm trổ hoa văn",
                    "Tham gia lớp học nấu ăn truyền thống Huế do nghệ nhân trực tiếp giảng dạy",
                    "Chiêm ngưỡng bộ sưu tập gốm sứ cổ triều Nguyễn trưng bày tại nhà vườn"
                },
                OpeningHours = "10:30 - 21:00 hàng ngày",
                TicketInfo = "Set menu từ 250.000 - 550.000 VNĐ/khách",
                BadgeColor = "danger",
                IconClass = "bi-award-fill"
            },

            // =========================================================================
            // VI. CHÙA CỔ & LÀNG NGHỀ DI SẢN BỔ SUNG
            // =========================================================================
            new HeritageLocation
            {
                Id = "HERITAGE-CHUA-DIEU-DE",
                Name = "Chùa Diệu Đế (Đệ Tam Quốc Tự & Bích Họa Long Vân Khế Hội)",
                Category = "Chùa Cổ & Tâm Linh",
                Address = "110 Bạch Đằng, Phường Phú Cát, TP. Huế (Bờ sông Đông Ba)",
                Latitude = 16.4760,
                Longitude = 107.5880,
                DistanceFromEcoStayKm = 4.4,
                TravelEstimate = "4.4 km (~13 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/ho_sen_hue.jpg",
                Summary = "Ngôi Quốc tự thứ ba của triều Nguyễn với bức tranh trần 'Cửu Long Ẩn Hiện' lớn và độc đáo nhất Việt Nam.",
                Description = "Được vua Thiệu Trị sắc phong là Quốc tự vào năm 1844, Chùa Diệu Đế nằm tĩnh lặng bên dòng sông Đông Ba rợp bóng cây cổ thụ. Điểm trác tuyệt nhất tại chánh điện là bức đại bích họa 'Long Vân Khế Hội' vẽ 9 con rồng ẩn hiện trong mây uốn lượn trên trần và các cột trụ do nghệ nhân cung đình vẽ hoàn toàn bằng bột màu tự nhiên.",
                ExperienceHighlights = new()
                {
                    "Ngửa mặt chiêm ngưỡng kiệt tác bích họa 9 con rồng 'Long Vân Khế Hội' trăm tuổi",
                    "Ngắm cây bồ đề cổ thụ sum suê xòe bóng mát trước sân chùa thanh tịnh",
                    "Tìm hiểu về lịch sử Phật giáo gắn liền với phong trào đấu tranh bảo vệ di sản Huế",
                    "Dạo bước trên phố cổ Bạch Đằng ngắm những dãy nhà cổ kính thời đầu thế kỷ 20"
                },
                OpeningHours = "06:00 - 18:30 hàng ngày",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "info",
                IconClass = "bi-brightness-high"
            },
            new HeritageLocation
            {
                Id = "HERITAGE-CHUA-BAO-QUOC",
                Name = "Chùa Báo Quốc & Giếng Hàm Long (Cổ Tự 350 Năm)",
                Category = "Chùa Cổ & Tâm Linh",
                Address = "Đường Báo Quốc, Phường Phường Đúc, TP. Huế (Trên đồi Hàm Long)",
                Latitude = 16.4520,
                Longitude = 107.5760,
                DistanceFromEcoStayKm = 3.1,
                TravelEstimate = "3.1 km (~9 phút xe đạp Eco-Bike)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Một trong những ngôi tổ đình cổ kính bậc nhất Huế với giếng nước ngọc Hàm Long mát lành từng dâng tiến chúa Nguyễn.",
                Description = "Được Thiền sư Giác Phong khai sơn vào cuối thế kỷ 17 thời chúa Nguyễn Phúc Chu. Chùa tọa lạc trên ngọn đồi Hàm Long yên tĩnh rợp bóng cây đại cổ thụ. Dưới chân đồi có Giếng Hàm Long với mạch nước ngầm trong vắt ngọt lịm quanh năm, từng được chúa Nguyễn cắt cử quân lính bảo vệ để lấy nước tiến cung pha trà.",
                ExperienceHighlights = new()
                {
                    "Uống ngụm nước giếng ngọc Hàm Long mát lành trong vắt huyền thoại",
                    "Chiêm bái tháp mộ cổ của các bậc cao tăng tiền bối triều Nguyễn",
                    "Thư thái ngắm cảnh thành phố Huế từ trên sườn đồi Hàm Long tĩnh mịch",
                    "Tìm hiểu về cái nôi đào tạo Phật học lớn nhất miền Trung"
                },
                OpeningHours = "05:30 - 18:00 hàng ngày",
                TicketInfo = "Miễn phí tham quan",
                BadgeColor = "info",
                IconClass = "bi-flower1"
            },
            new HeritageLocation
            {
                Id = "CRAFT-NON-TAY-HO",
                Name = "Làng Nón Lá Tây Hồ (Cái Nôi Nón Bài Thơ Xứ Huế)",
                Category = "Làng Nghề Truyền Thống",
                Address = "Xã Phú Hồ, Huyện Phú Vang, Tỉnh Thừa Thiên Huế (Ven sông Như Ý)",
                Latitude = 16.4420,
                Longitude = 107.6250,
                DistanceFromEcoStayKm = 7.8,
                TravelEstimate = "7.8 km (~22 phút đi xe máy / ô tô)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Làng nghề thủ công hàng trăm năm tuổi sáng tạo nên chiếc Nón Bài Thơ mỏng manh e ấp của người con gái Huế.",
                Description = "Nghệ nhân Tây Hồ đã sáng tạo nên chiếc nón bài thơ độc đáo bằng cách chèn những câu thơ lục bát và hình vẽ chùa Thiên Mụ, cầu Trường Tiền vào giữa hai lớp lá gồi trắng muốt. Soi chiếc nón lên ánh nắng ban mai, bài thơ và hình ảnh hiện lên huyền ảo như một tuyệt tác nghệ thuật.",
                ExperienceHighlights = new()
                {
                    "Học cách ủi lá gồi bằng lưỡi cày nóng đỏ và chằm từng đường kim mũi cước",
                    "Tự tay chèn câu thơ yêu thích của mình vào giữa hai lớp nón bài thơ",
                    "Mua những chiếc nón lá trắng tinh khôi về làm quà lưu niệm duyên dáng",
                    "Ngắm khung cảnh làng quê thanh bình bên dòng sông Như Ý uốn lượn"
                },
                OpeningHours = "07:30 - 17:30 hàng ngày",
                TicketInfo = "Miễn phí tham quan làng nghề",
                BadgeColor = "warning",
                IconClass = "bi-umbrella-fill"
            },
            new HeritageLocation
            {
                Id = "CRAFT-LANG-CHUON",
                Name = "Làng Rượu Chuồn & Chợ Nổi Đầm Chuồn (Đầm Phá Tam Giang)",
                Category = "Làng Nghề Truyền Thống",
                Address = "Xã Phú An, Huyện Phú Vang, Tỉnh Thừa Thiên Huế (Hệ đầm phá Tam Giang)",
                Latitude = 16.4880,
                Longitude = 107.6580,
                DistanceFromEcoStayKm = 11.5,
                TravelEstimate = "11.5 km (~28 phút đi xe máy / ô tô)",
                ImageUrl = "/images/song_huong_hoanghon.jpg",
                Summary = "Làng nấu rượu gạo nếp tiến vua nức tiếng và chợ nổi ẩm thực thủy hải sản giữa đầm phá Tam Giang mênh mông.",
                Description = "Rượu Làng Chuồn được nấu từ gạo nếp than và nguồn nước giếng làng ngọt thanh ủ với men thảo mộc gia truyền 36 vị thuốc bắc thơm nồng ấm bụng. Liền kề là Đầm Chuồn thuộc hệ đầm phá Tam Giang lớn nhất Đông Nam Á, nơi du khách có thể đi thuyền ngắm hoàng hôn đỏ rực và ăn hải sản tươi sống trên nhà chồ nổi.",
                ExperienceHighlights = new()
                {
                    "Tìm hiểu quy trình chưng cất rượu gạo nếp men thuốc bắc tiến vua truyền thống",
                    "Đi thuyền sup hoặc đò gỗ ngắm hoàng hôn ráng chiều kỳ ảo trên đầm phá Tam Giang",
                    "Thưởng thức bánh khoái cá kình tươi rói nhảy tanh tách vừa đánh bắt",
                    "Ăn tôm đất xào khế, cua rạm rang muối tại các nhà chồ nổi trên mặt nước"
                },
                OpeningHours = "06:00 - 20:00 hàng ngày",
                TicketInfo = "Tham quan tự do • Đi thuyền từ 50.000 VNĐ/người",
                BadgeColor = "warning",
                IconClass = "bi-water"
            }
        };

        private static readonly List<HeritageTourRoute> _routes = new()
        {
            new HeritageTourRoute
            {
                Id = "ROUTE-RIVER",
                Title = "Tuyến 1: Cung Đường Di Sản Ven Sông Hương & Nhà Vườn Kim Long",
                Subtitle = "Thư thái đạp xe Eco-Bike đón gió sông Hương, chiêm ngưỡng nhà vườn và cổ tự 400 năm",
                Duration = "Khoảng 2 - 3 giờ (5.5 km)",
                Transport = "Xe đạp trợ lực điện Eco-Bike (Miễn phí từ Kim Long EcoStay)",
                Description = "Khởi hành từ Kim Long EcoStay ➔ Nhà Vườn An Hiên (thăm nhà rường) ➔ Chùa Thiên Mụ (ngắm Tháp Phước Duyên) ➔ Văn Miếu Huế (32 bia tiến sĩ) ➔ Lò Bánh Ướt Kim Long (thưởng thức bánh ướt thịt nướng than hoa).",
                StopLocationIds = new() { "KLE-CENTER", "HERITAGE-AN-HIEN", "HERITAGE-THIEN-MU", "HERITAGE-VAN-MIEU", "CRAFT-KIM-LONG" },
                Badge = "Tour Xe Đạp Phổ Biến Nhất"
            },
            new HeritageTourRoute
            {
                Id = "ROUTE-CRAFTS",
                Title = "Tuyến 2: Khám Phá Làng Nghề Thủ Công & Lăng Tẩm Thi Ca",
                Subtitle = "Check-in làng hương ngập tràn sắc màu, trải nghiệm làm nón lá và chiêm ngưỡng kiến trúc hoàng gia",
                Duration = "Khoảng 3.5 - 4.5 giờ (9.0 km)",
                Transport = "Xe đạp Eco-Bike hoặc xe máy / ô tô",
                Description = "Kim Long EcoStay ➔ Cầu Dã Viên ➔ Làng Đúc Đồng Phường Đúc ➔ Làng Hương Thủy Xuân (tự tay se hương) ➔ Lăng Tự Đức (thơ mộng hồ sen) ➔ Chùa Từ Hiếu (rừng thông thái giám).",
                StopLocationIds = new() { "KLE-CENTER", "CRAFT-PHUONG-DUC", "CRAFT-THUY-XUAN", "HERITAGE-TU-DUC", "HERITAGE-TU-HIEU" },
                Badge = "Trải Nghiệm Thủ Công Nghệ Thuật"
            },
            new HeritageTourRoute
            {
                Id = "ROUTE-ROYAL",
                Title = "Tuyến 3: Trái Tim Hoàng Gia & Thủ Phủ Mộc Bản Làng Sình",
                Subtitle = "Tìm về cội nguồn vương triều nhà Nguyễn và dòng tranh khắc gỗ dân gian 4 thế kỷ",
                Duration = "Khoảng 4 - 5 giờ (16 km)",
                Transport = "Kết hợp xe đạp / ô tô hoặc du thuyền rồng sông Hương",
                Description = "Kim Long EcoStay ➔ Đại Nội Huế (Ngọ Môn & Tử Cấm Thành) ➔ Cung An Định (kiến trúc tân cổ điển) ➔ Làng Tranh Làng Sình (in tranh mộc bản) ➔ Làng Hoa Giấy Thanh Tiên (xếp cánh hoa sen).",
                StopLocationIds = new() { "KLE-CENTER", "HERITAGE-DAI-NOI", "HERITAGE-CUNG-AN-DINH", "CRAFT-LANG-SINH", "CRAFT-THANH-TIEN" },
                Badge = "Văn Hóa Cung Đình & Dân Gian"
            },
            new HeritageTourRoute
            {
                Id = "ROUTE-FOOD",
                Title = "Tuyến 4: Thiên Đường Ẩm Thực & Món Ngon Xứ Huế",
                Subtitle = "Hành trình vị giác thưởng thức bánh ướt thịt nướng, bún bò, chè bột lọc heo quay và cà phê muối",
                Duration = "Khoảng 3 - 4 giờ (8.5 km)",
                Transport = "Xe đạp trợ lực điện Eco-Bike hoặc xe máy dạo phố",
                Description = "Kim Long EcoStay ➔ Bánh ướt Huyền Anh Kim Long (250m) ➔ Bánh bèo nậm lọc Bà Đỏ ➔ Cà phê muối Nguyễn Lương Bằng ➔ Chè Hẻm Hùng Vương (chè bột lọc heo quay) ➔ Bún bò Huế Mụ Rơi.",
                StopLocationIds = new() { "KLE-CENTER", "FOOD-BANH-UOT", "FOOD-BANH-BEO", "FOOD-CA-PHE-MUOI", "FOOD-CHE-HEM", "FOOD-BUN-BO" },
                Badge = "Tour Vị Giác Cố Đô"
            },
            new HeritageTourRoute
            {
                Id = "ROUTE-SUNSET",
                Title = "Tuyến 5: Chiều Tà Sông Hương & Hoàng Hôn Đồi Vọng Cảnh",
                Subtitle = "Đón hoàng hôn rực rỡ trên khúc quanh sông Hương, viếng chùa Từ Hiếu và cà phê ngắm cảnh",
                Duration = "Khoảng 3 giờ (10.5 km)",
                Transport = "Xe đạp trợ lực điện Eco-Bike hoặc xe máy",
                Description = "Kim Long EcoStay ➔ Chùa Thiên Mụ ➔ Cầu Dã Viên ➔ Làng Hương Thủy Xuân ➔ Đồi Vọng Cảnh (ngắm hoàng hôn triệu đô) ➔ Cà Phê Rừng Thông Sunset.",
                StopLocationIds = new() { "KLE-CENTER", "HERITAGE-THIEN-MU", "CRAFT-THUY-XUAN", "HERITAGE-DOI-VONG-CANH", "FOOD-CA-PHE-RUNG-THONG" },
                Badge = "Tour Hoàng Hôn Triệu Đô"
            }
        };

        public static List<HeritageLocation> GetAllLocations() => _locations;

        public static List<HeritageLocation> GetCraftVillages() => 
            _locations.Where(l => l.Category == "Làng Nghề Truyền Thống").ToList();

        public static List<HeritageLocation> GetMonumentSites() => 
            _locations.Where(l => l.Category == "Quần Thể Di Tích Cố Đô").ToList();

        public static List<HeritageLocation> GetFoodSpots() => 
            _locations.Where(l => l.Category == "Ẩm Thực Xứ Huế").ToList();

        public static List<HeritageLocation> GetNearbyLocations(double maxKm = 3.0) => 
            _locations.Where(l => l.DistanceFromEcoStayKm <= maxKm).OrderBy(l => l.DistanceFromEcoStayKm).ToList();

        public static List<string> GetAllCategories() => new()
        {
            "Tất Cả Địa Điểm",
            "Quần Thể Di Tích Cố Đô",
            "Làng Nghề Truyền Thống",
            "Ẩm Thực Xứ Huế",
            "Chùa Cổ & Tâm Linh",
            "Nhà Vườn Di Sản",
            "Gần Homestay (< 3km)"
        };

        public static List<HeritageTourRoute> GetSuggestedRoutes() => _routes;
    }
}
