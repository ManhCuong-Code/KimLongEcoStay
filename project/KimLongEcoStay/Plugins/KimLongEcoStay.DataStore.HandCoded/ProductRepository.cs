using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;

namespace KimLongEcoStay.DataStore.HandCoded
{
    public class ProductRepository : IProductRepository, IRoomRepository
    {
        private readonly List<Product> products = new List<Product>();

        public ProductRepository() 
        {
            products = new List<Product>
            {  
                new Product 
                { 
                    Id = 1, 
                    Brand = "Nhà Vườn Hương Sen (Kim Long)", 
                    Name = "Phòng Eco-Nest Ban Mai (KLE-R101)", 
                    Price = 390000, 
                    ImageLink = "/images/room_econest.jpg", 
                    Description = "View trực diện cánh đồng lúa Kim Long & đón bình minh. Ban công kính panorama, đệm organic, xà phòng bồ hòn & dầu gội thảo mộc. Khóa SmartLock IoT tự động cấp mã PIN sau 10s thanh toán." 
                },
                new Product 
                { 
                    Id = 2, 
                    Brand = "Nhà Vườn Hương Sen (Kim Long)", 
                    Name = "Phòng Eco-Nest Hoàng Hôn (KLE-R102)", 
                    Price = 420000, 
                    ImageLink = "/images/room_hoanghon.jpg", 
                    Description = "Góc ngắm hoàng hôn ráng vàng trên sông Hương & rặng tre ngà Kim Long. Bồn tắm thoáng mát bồn sứ cao cấp & tinh dầu sả chanh. Tặng lượt trải nghiệm xe đạp trợ lực điện khám phá chùa Thiên Mụ." 
                },
                new Product 
                { 
                    Id = 3, 
                    Brand = "Nhà Vườn Nguyệt Biều (Kim Long)", 
                    Name = "Garden Suite Cố Đô Thanh Trà (KLE-R201)", 
                    Price = 680000, 
                    ImageLink = "/images/room_gardensuite.jpg", 
                    Description = "Nhà rường kết hợp tiện nghi đương đại dưới bóng vườn bưởi thanh trà 40 năm tuổi. Hiên trà thưởng nguyệt, bồn ngâm chân gỗ pơ-mu thảo mộc, miễn phí 02 xe đạp trợ lực GPS Audio Guide và đặc sản bánh ướt thịt nướng đón tiếp." 
                },
                new Product 
                { 
                    Id = 4, 
                    Brand = "Nhà Vườn Nguyệt Biều (Kim Long)", 
                    Name = "Family Eco-Villa Gắn Kết (KLE-R202)", 
                    Price = 820000, 
                    ImageLink = "/images/hero_ecostay.jpg", 
                    Description = "Biệt thự nhà vườn sinh thái dành cho gia đình 4 khách, không gian rợp bóng cây xanh mát, sân vườn an toàn cho bé chạy nhảy và khám phá nông nghiệp vườn bưởi Thanh Trà." 
                },
                new Product 
                { 
                    Id = 5, 
                    Brand = "Nhà Vườn Nguyệt Biều (Kim Long)", 
                    Name = "Heritage Studio Trầm Mặc (KLE-R203)", 
                    Price = 590000, 
                    ImageLink = "/images/nha_ruong_2.jpg", 
                    Description = "Độc bản văn hóa Cố Đô nhà rường trầm mặc, view hồ sen thanh tịnh & rặng chuối ngự. Bộ ấm trà Cung Đình, bàn cờ tướng & kệ sách lịch sử triều Nguyễn. Trợ giá xe điện thông minh vi vu Đại Nội." 
                }
            };
        }

        public Product? GetProduct(int id)
        {
            return products.FirstOrDefault(p => p.productId == id);
        }

        public IEnumerable<Product> GetProducts(string? filter = null)
        {
            if (string.IsNullOrWhiteSpace(filter)) return products;

            return products.Where(p => !string.IsNullOrEmpty(p.Name) && (p.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) || p.Description.Contains(filter, StringComparison.OrdinalIgnoreCase)));
        }

        public IEnumerable<Room> GetAvailableRooms(int roomTypeId, DateTime checkIn, DateTime checkOut)
        {
            return products.Select(p => new Room { Id = p.Id, Name = p.Name, Brand = p.Brand, Price = p.Price, Description = p.Description, ImageLink = p.ImageLink });
        }
    }
}
