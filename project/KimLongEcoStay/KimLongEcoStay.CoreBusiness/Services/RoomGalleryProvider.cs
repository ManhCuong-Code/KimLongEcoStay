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
                    Caption = "Không gian phòng ngủ Eco-Nest đón ánh ban mai ấm áp & giường nệm organic cao cấp",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Ban công kính panorama nhìn thẳng ra cánh đồng lúa xanh ngát và rặng cau Kim Long",
                    Category = "Ban Công & View",
                    Tag = "Bình Minh Cố Đô"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Bồn tắm sứ sinh thái ngâm thảo mộc bồ hòn & tinh dầu sả chanh tự nhiên",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Thư Giãn Thảo Dược"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Hiên trà đón nắng mai rọi bóng hoa cỏ nhà vườn, tĩnh tại và trong lành",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Góc Đón Nắng Sớm"
                },
                new RoomPhoto
                {
                    RoomId = 1,
                    RoomName = "Eco-Nest Ban Mai (KLE-R101)",
                    Url = "/images/tour_xedap.jpg",
                    Caption = "Tặng xe đạp trợ lực điện Eco-Bike dạo mát làng cổ Kim Long và Chùa Thiên Mụ",
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
                    Caption = "Phòng ngủ view lộng gió đón trọn vẹn ráng chiều hoàng hôn rực rỡ bên dòng sông Hương",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "https://images.unsplash.com/photo-1540518614846-7ede433c4ef4?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Ban công hướng sông Hương, ngắm ánh hoàng hôn rọi bóng thuyền rồng lướt êm",
                    Category = "Ban Công & View",
                    Tag = "Hoàng Hôn Sông Hương"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Bồn tắm đá tự nhiên mở rộng tầm mắt ngắm hoàng hôn với xà phòng thảo dược",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Bồn Tắm Đá Cố Đô"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Góc thư giãn chiều tà đọc sách bên ấm trà sen Huế thơm ngát",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Thưởng Trà Ráng Chiều"
                },
                new RoomPhoto
                {
                    RoomId = 2,
                    RoomName = "Eco-Nest Hoàng Hôn (KLE-R102)",
                    Url = "/images/smart_tour.jpg",
                    Caption = "Hệ thống thuyết minh di sản thông minh GPS Audio Guide tích hợp trên mọi nẻo đường",
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
                    Caption = "Nhà rường cổ kết hợp tiện nghi cao cấp dưới bóng vườn bưởi Thanh Trà 40 năm tuổi",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Giường gỗ chạm khắc họa tiết hoa sen Cố Đô tinh xảo, chăn ga lụa tơ tằm mềm mại",
                    Category = "Phòng Ngủ",
                    Tag = "Nội Thất Nhà Rường"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "https://images.unsplash.com/photo-1507652313519-d4e9174996dd?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Bồn ngâm chân gỗ pơ-mu thơm ngát cùng muối thảo mộc hoàng cung giải tỏa mệt mỏi",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Ngâm Chân Hoàng Cung"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Hiên trà thưởng nguyệt nhìn ra rặng bưởi Thanh Trà trĩu quả thơm hương hoa bưởi",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Vườn Bưởi 40 Năm"
                },
                new RoomPhoto
                {
                    RoomId = 3,
                    RoomName = "Garden Suite Cố Đô Thanh Trà (KLE-R201)",
                    Url = "/images/mon_banhuot.jpg",
                    Caption = "Đặc sản bánh ướt thịt nướng Kim Long trứ danh phục vụ tận hiên phòng",
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
                    Url = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Biệt thự vườn sinh thái rộng rãi 2 giường lớn dành cho gia đình 4 người ấm cúng",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Sân vườn cỏ tự nhiên xanh mát an toàn tuyệt đối cho bé chạy nhảy và khám phá thiên nhiên",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Sân Vườn Trẻ Thơ"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Phòng sinh hoạt chung gia đình tiện nghi với bàn trà mây tre đan tinh tế",
                    Category = "Phòng Khách",
                    Tag = "Không Gian Sinh Hoạt"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "https://images.unsplash.com/photo-1540541338287-41700207dee6?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Bàn tiệc ăn uống ngoài trời sum họp cả nhà rộn rã tiếng cười giữa vườn hoa",
                    Category = "Ban Công & View",
                    Tag = "Tiệc Trà Ngoài Trời"
                },
                new RoomPhoto
                {
                    RoomId = 4,
                    RoomName = "Family Eco-Villa Gắn Kết (KLE-R202)",
                    Url = "/images/hero_ecostay.jpg",
                    Caption = "Khuôn viên sinh thái rộng hơn 1.500m² với thảm cỏ và rặng cây xanh mát",
                    Category = "Trải Nghiệm",
                    Tag = "Khuôn Viên Sinh Thái"
                }
            },
            [5] = new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "https://images.unsplash.com/photo-1595526114035-0d45ed16cfbf?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Studio độc bản trầm mặc, hòa quyện phong vị cung đình và nét tối giản hiện đại",
                    Category = "Phòng Ngủ",
                    Tag = "Góc Toàn Cảnh"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Bộ ấm chén trà Cung Đình cổ, bàn cờ tướng và góc đọc sách văn hóa triều Nguyễn",
                    Category = "Hiên Trà & Vườn",
                    Tag = "Bàn Trà Cổ Kính"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "https://images.unsplash.com/photo-1509233725247-49e657c54213?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Cửa sổ nhìn ra hồ súng thanh tịnh, hoa súng nở sớm thơm mát hương thảo mộc",
                    Category = "Ban Công & View",
                    Tag = "Hồ Súng Thanh Tịnh"
                },
                new RoomPhoto
                {
                    RoomId = 5,
                    RoomName = "Heritage Studio Trầm Mặc (KLE-R203)",
                    Url = "https://images.unsplash.com/photo-1552321554-5fefe8c9ef14?auto=format&fit=crop&w=1200&q=85",
                    Caption = "Phòng tắm ốp sỏi đá tự nhiên Cố Đô, vòi sen tắm mưa nhiệt đới thư giãn",
                    Category = "Bồn Tắm & Thảo Mộc",
                    Tag = "Phòng Tắm Sỏi Đá"
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

            // Fallback default photos
            return new List<RoomPhoto>
            {
                new RoomPhoto
                {
                    RoomId = roomId,
                    Url = "/images/room_econest.jpg",
                    Caption = "Không gian phòng nghỉ sinh thái Kim Long EcoStay",
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
