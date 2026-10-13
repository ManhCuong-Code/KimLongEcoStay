using System;
using System.Collections.Generic;
using System.Linq;
using KimLongEcoStay.CoreBusiness.Models;

namespace KimLongEcoStay.CoreBusiness.Services
{
    public static class RoomGalleryProvider
    {
        private static readonly Dictionary<int, List<RoomPhoto>> RoomPhotosMap = new()
        {
            [1] = new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "/images/room_econest.jpg",
                    Caption = "Phòng ngủ Eco-Nest đệm organic tự nhiên, kiến trúc gỗ mộc mạc đón ánh ban mai ấm áp",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "/images/nha_ruong_3.jpg",
                    Caption = "Hiên gỗ nhà rường ngắm vườn bưởi Thanh Trà và hoa cau Kim Long đón nắng sớm",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Hiên Nhà Rường"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "/images/kien_truc_vuon_hue.jpg",
                    Caption = "Bồn ngâm thảo mộc bồ hòn & tinh dầu tràm Thủy Xuân hướng ra cảnh quan sinh thái",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Thảo Mộc Xứ Huế"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "/images/song_huong_hoanghon.jpg",
                    Caption = "Tầm nhìn thoáng đãng hướng về dòng sông Hương êm đềm và rặng tre ngà Kim Long",
                    Category = "Ban Công & View",
                    Tag = "View Sông Hương"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "/images/tour_xedap.jpg",
                    Caption = "Xe đạp trợ lực điện Eco-Bike dạo mát đường làng Kim Long và Chùa Thiên Mụ",
                    Category = "Trải Nghiệm",
                    Tag = "Eco-Bike Trợ Lực"
                }
            },
            [2] = new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "/images/room_hoanghon.jpg",
                    Caption = "Không gian phòng nghỉ đón trọn ráng chiều hoàng hôn rực rỡ bên dòng sông Hương",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "/images/song_huong_hoanghon.jpg",
                    Caption = "Góc ban công ngắm ráng vàng hoàng hôn trên sông Hương và bóng thuyền rồng lướt êm",
                    Category = "Ban Công & View",
                    Tag = "Hoàng Hôn Sông Hương"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "/images/thuyen_rong_hue.jpg",
                    Caption = "Trải nghiệm ngắm thuyền rồng Cố Đô và lắng nghe nhã nhạc ca Huế trên dòng Hương Giang",
                    Category = "Trải Nghiệm",
                    Tag = "Thuyền Rồng Cố Đô"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "/images/nha_ruong_2.jpg",
                    Caption = "Góc ngồi đọc sách & thưởng trà sen Cung Đình bên vách gỗ đố bản trầm ấm",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Thưởng Trà Ráng Chiều"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "/images/smart_tour.jpg",
                    Caption = "Tour di sản thông minh GPS Audio Guide tích hợp thuyết minh 3 ngôn ngữ",
                    Category = "Trải Nghiệm",
                    Tag = "GPS Audio Guide"
                }
            },
            [3] = new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "/images/room_gardensuite.jpg",
                    Caption = "Nhà rường cổ kính nguyên bản xứ Huế dưới bóng vườn bưởi Thanh Trà 40 năm tuổi",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "/images/nha_ruong_1.jpg",
                    Caption = "Cột gỗ mít chạm khắc tinh xảo, vì kèo truyền thống và sập gụ cổ Cố Đô Huế",
                    Category = "Phòng Ngủ",
                    Tag = "Nội Thất Nhà Rường"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "/images/ho_sen_hue.jpg",
                    Caption = "Liệu pháp ngâm chân gỗ pơ-mu với muối thảo dược hoàng cung & hoa sen ngát hương",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Ngâm Chân Hoàng Cung"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "/images/nha_ruong_3.jpg",
                    Caption = "Hiên trà thưởng nguyệt nhìn ra rặng bưởi Thanh Trà trĩu quả thơm hương hoa bưởi",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Vườn Bưởi 40 Năm"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "/images/mon_banhuot.jpg",
                    Caption = "Đặc sản bánh ướt thịt nướng Kim Long trứ danh phục vụ đón tiếp tận hiên phòng",
                    Category = "Trải Nghiệm",
                    Tag = "Đặc Sản Kim Long"
                }
            },
            [4] = new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "/images/hero_ecostay.jpg",
                    Caption = "Khuôn viên nhà vườn sinh thái rộng rãi 1.200m² dành cho cả gia đình sum vầy",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "/images/nha_ruong_1.jpg",
                    Caption = "Không gian gian nhà gỗ truyền thống rộng thoáng, ấm cúng cho gia đình 4 thành viên",
                    Category = "Phòng Khách",
                    Tag = "Không Gian Nhà Rường"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "/images/kien_truc_vuon_hue.jpg",
                    Caption = "Lối dạo rợp bóng cây cổ thụ và thảm cỏ an toàn cho trẻ em khám phá thiên nhiên",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Sân Vườn Trẻ Thơ"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "/images/nha_ruong_2.jpg",
                    Caption = "Bàn trà gỗ mộc mạc nơi cả gia đình quây quần trò chuyện và thưởng thức trà quả Cố Đô",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Gia Đình Quây Quần"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "/images/chua_thien_mu.jpg",
                    Caption = "Cả gia đình cùng đạp xe thăm Chùa Thiên Mụ cổ kính cách homestay chỉ 4 phút",
                    Category = "Trải Nghiệm",
                    Tag = "Chùa Thiên Mụ"
                }
            },
            [5] = new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "/images/nha_ruong_2.jpg",
                    Caption = "Studio độc bản trầm mặc, hòa quyện phong vị triều Nguyễn và sự thanh tịnh Cố Đô",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "/images/nha_ruong_1.jpg",
                    Caption = "Bộ ấm trà Cung Đình cổ, bàn cờ tướng và góc đọc sách văn hóa triều đại",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Bàn Trà Cổ Kính"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "/images/ho_sen_hue.jpg",
                    Caption = "Cửa sổ hướng ra hồ sen thanh tịnh, hoa sen tỏa hương thơm mộc mạc sớm mai",
                    Category = "Ban Công & View",
                    Tag = "Hồ Sen Thanh Tịnh"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "/images/chua_thien_mu.jpg",
                    Caption = "Tầm nhìn hướng tháp Phước Duyên Chùa Thiên Mụ bảng lảng chuông chiều",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Cảnh Sắc Thiên Mụ"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "/images/logo.jpg",
                    Caption = "Dấu ấn biểu trưng di sản văn hóa Kim Long EcoStay Cố Đô Huế",
                    Category = "Trải Nghiệm",
                    Tag = "Dấu Ấn Di Sản"
                }
            }
        };

        public static List<RoomPhoto> GetPhotosByRoomId(int roomId)
        {
            if (RoomPhotosMap.TryGetValue(roomId, out var photos))
            {
                return photos;
            }

            return new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = roomId,
                    Url = "/images/room_econest.jpg",
                    Caption = "Không gian phòng nghỉ sinh thái nhà vườn Kim Long EcoStay Cố Đô Huế",
                    Category = "Phòng Ngủ",
                    Tag = "Toàn Cảnh"
                }
            };
        }

        public static List<RoomPhoto> GetAllGalleryPhotos()
        {
            var list = new List<RoomPhoto>();
            foreach (var kvp in RoomPhotosMap)
            {
                list.AddRange(kvp.Value);
            }
            return list;
        }

        public static List<string> GetAllCategories()
        {
            return new List<string>
            {
                "Tất Cả",
                "Phòng Ngủ",
                "Ban Công & View",
                "Bồn Tắm & Thảo Mộc",
                "Hiên Trà & Vườn",
                "Trải Nghiệm"
            };
        }
    }
}
