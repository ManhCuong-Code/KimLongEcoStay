/**
 * =========================================================================
 * KIM LONG ECOSTAY — SECTIONS INTERACTIVE HANDLER & SMOOTH NAVIGATION
 * =========================================================================
 * Cung cấp logic điều hướng fragment mượt mà (smooth scroll), đồng bộ URL hash,
 * quản lý tương tác 3 khu vực:
 * 1. #specialties (Sản phẩm & Đặc sản Kim Long)
 * 2. #royal-tea (Trải nghiệm Trà Cung Đình)
 * 3. #local-partners (Mạng lưới đối tác địa phương)
 */

(function (window, document) {
  'use strict';

  // =========================================================================
  // 1. ĐIỀU HƯỚNG MƯỢT MÀ VÀ ĐỒNG BỘ HASH URL (SMOOTH SCROLL NAVIGATION)
  // =========================================================================

  /**
   * Cuộn mượt đến phần tử mục tiêu có tính bù trừ chiều cao của Navbar cố định
   * @param {string} id - ID của phần tử trong DOM (có thể có hoặc không có dấu #)
   * @param {boolean} updateHistory - Có cập nhật URL hash trong lịch sử trình duyệt hay không
   */
  window.scrollToSection = function (id, updateHistory) {
    if (!id) return;
    var cleanId = id.replace(/^#/, '');
    var el = document.getElementById(cleanId);

    if (el) {
      var nav = document.querySelector('.kle-navbar') || document.querySelector('.navbar');
      var navHeight = nav ? nav.offsetHeight : 80;
      var rect = el.getBoundingClientRect();
      var targetPosition = rect.top + window.pageYOffset - (navHeight + 16);

      window.scrollTo({
        top: Math.max(0, targetPosition),
        behavior: 'smooth'
      });

      if (updateHistory !== false && window.history && window.history.pushState) {
        if (window.location.hash !== '#' + cleanId) {
          window.history.pushState(null, '', window.location.pathname + '#' + cleanId);
        }
      }
    } else {
      // Nếu không tìm thấy trên trang hiện tại, chuyển hướng về trang chủ kèm hash
      var pathname = window.location.pathname;
      var isHome = pathname === '/' || pathname.endsWith('/index.html') || pathname.indexOf('/KimLongEcoStay') === 0;
      if (!isHome) {
        window.location.href = '/' + '#' + cleanId;
      }
    }
  };

  /**
   * Khởi tạo bộ lắng nghe điều hướng URL hash (Hashchange, Popstate & Initial Load)
   */
  window.initHashNavigation = function () {
    function handleCurrentHash(isInitial) {
      var hash = window.location.hash;
      if (hash && hash.length > 1) {
        var targetId = hash.substring(1);
        var attempts = 0;
        var maxAttempts = 15;

        function tryScroll() {
          var el = document.getElementById(targetId);
          if (el) {
            window.scrollToSection(targetId, false);
          } else if (attempts < maxAttempts) {
            attempts++;
            setTimeout(tryScroll, 120);
          }
        }

        if (isInitial) {
          setTimeout(tryScroll, 180);
        } else {
          tryScroll();
        }
      }
    }

    window.addEventListener('hashchange', function () {
      handleCurrentHash(false);
    });

    window.addEventListener('popstate', function () {
      handleCurrentHash(false);
    });

    if (document.readyState === 'complete' || document.readyState === 'interactive') {
      handleCurrentHash(true);
    } else {
      window.addEventListener('DOMContentLoaded', function () {
        handleCurrentHash(true);
      });
    }

    window.addEventListener('load', function () {
      handleCurrentHash(true);
    });
  };

  // =========================================================================
  // 2. DỮ LIỆU ĐẶC TẢ 10 ĐỐI TÁC ĐỊA PHƯƠNG KIM LONG (LOCAL PARTNERS DATA)
  // =========================================================================
  var LOCAL_PARTNERS = [
    {
      id: 'PARTNER-NV-HUONG-SEN',
      name: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      category: 'Nhà Vườn & Homestay',
      partnerGroup: 'Nhà vườn và homestay',
      address: 'Kiệt 104 Kim Long, Phường Kim Long, TP. Huế',
      phone: '0934.567.891',
      distanceKm: 0.3,
      imageUrl: 'images/nha_ruong_1.jpg',
      hours: '07:00 - 21:00',
      highlights: ['Bưởi Thanh Trà', 'Hồ Sen Trắng Tịnh Tâm', 'Trà Sen Cung Đình'],
      summary: 'Nhà vườn kiểu mẫu với 2 phòng nghỉ sinh thái, hồ sen trắng và rặng bưởi Thanh Trà 40 năm tuổi.',
      desc: 'Hộ liên kết nòng cốt trong chuỗi nhà vườn Kim Long EcoStay. Cung cấp không gian thưởng trà sen tao nhã, bưởi Thanh Trà chín cây và trải nghiệm ngâm chân thảo mộc.'
    },
    {
      id: 'PARTNER-NV-NGUYET-BIEU',
      name: 'Nhà Vườn Sinh Thái Nguyệt Biều',
      category: 'Nhà Vườn & Homestay',
      partnerGroup: 'Nhà vườn và homestay',
      address: 'Phường Nguyệt Biều, TP. Huế (Bờ nam sông Hương)',
      phone: '0914.234.567',
      distanceKm: 1.8,
      imageUrl: 'images/nha_ruong_3.jpg',
      hours: '08:00 - 20:30',
      highlights: ['Vườn Bưởi Thanh Trà Cổ Thụ', 'Cơm Quê Nhà Vườn', 'Đạp Xe Sinh Thái'],
      summary: 'Điểm đến miệt vườn sông Hương nổi tiếng với giống bưởi Thanh Trà ngon nhất xứ Huế.',
      desc: 'Cung cấp 3 căn phòng mẫu và các sản phẩm nông nghiệp sinh thái tuần hoàn. Phối hợp tổ chức tour xe đạp trợ lực điện Eco-Bike băng qua cồn Dã Viên sang Nguyệt Biều.'
    },
    {
      id: 'PARTNER-BANH-UOT-HUYEN-ANH',
      name: 'Bánh Ướt Thịt Nướng Huyền Anh',
      category: 'Quán Ăn & Ẩm Thực',
      partnerGroup: 'Quán ăn và ẩm thực',
      address: '50 Kim Long, Phường Kim Long, TP. Huế',
      phone: '0905.789.012',
      distanceKm: 0.4,
      imageUrl: 'images/mon_banhuot.jpg',
      hours: '08:30 - 20:00',
      highlights: ['Bánh Ướt Thịt Nướng', 'Bún Thịt Nướng Kim Long', 'Nước Chấm Gia Truyền'],
      summary: 'Quán ăn trứ danh Kim Long với công thức ướp thịt nướng than hoa trên 50 năm.',
      desc: 'Đối tác chiến lược cung ứng ẩm thực giao tận buồng phòng cho khách lưu trú Kim Long EcoStay trong 15-20 phút. Đảm bảo nóng hổi và chuẩn vị Cố đô.'
    },
    {
      id: 'PARTNER-ME-XUNG-THIEN-HUONG',
      name: 'Cơ Sở Mè Xửng Thiên Hương',
      category: 'Đặc Sản Truyền Thống',
      partnerGroup: 'Đặc sản và sản phẩm địa phương',
      address: '20 Chi Lăng, TP. Huế',
      phone: '0234.352.289',
      distanceKm: 2.5,
      imageUrl: 'images/qua_mexung.jpg',
      hours: '07:30 - 21:00',
      highlights: ['Mè Xửng Giòn Cung Đình', 'Mè Xửng Dẻo', 'Kẹo Cau Truyền Thống'],
      summary: 'Thương hiệu mè xửng lâu đời nhất Cố đô Huế với công thức mật mía và mè rang hảo hạng.',
      desc: 'Cung cấp các gói quà tặng đóng hộp sang trọng phục vụ khách du lịch tại Kim Long EcoStay kèm chứng nhận OCOP 4 sao.'
    },
    {
      id: 'PARTNER-TRA-DUC-PHUONG',
      name: 'Trà Cung Đình Đức Phượng',
      category: 'Đồ Uống & Trà',
      partnerGroup: 'Đặc sản và sản phẩm địa phương',
      address: '24 Nguyễn Huệ, TP. Huế',
      phone: '0905.345.678',
      distanceKm: 2.2,
      imageUrl: 'images/tra_cung_dinh.jpg',
      hours: '08:00 - 21:30',
      highlights: ['Trà Cung Đình 16 Vị', 'Trà Quý Phi', 'Trà Hoàng Triều'],
      summary: 'Thương hiệu trà thảo mộc Cung Đình số 1 xứ Huế được phục chế theo điển lễ triều Nguyễn.',
      desc: 'Cung cấp nguyên liệu thảo mộc cho toàn bộ 4 gói trải nghiệm thưởng trà tại Kim Long EcoStay và bán lẻ các gói trà túi lọc, trà hãm cao cấp.'
    },
    {
      id: 'PARTNER-TINH-DAU-THANH-LOAN',
      name: 'Tinh Dầu Tràm Cố Đô Thanh Loan',
      category: 'Đặc Sản & Quà Lưu Niệm',
      partnerGroup: 'Đặc sản và sản phẩm địa phương',
      address: 'Đường Kim Long, Phường Kim Long, TP. Huế',
      phone: '0978.123.456',
      distanceKm: 0.6,
      imageUrl: 'images/qua_tinhdautram.jpg',
      hours: '07:00 - 20:00',
      highlights: ['Tinh Dầu Tràm Gió 100%', 'Muối Ngâm Chân Pơ-Mu', 'Dầu Massage Thảo Mộc'],
      summary: 'Cơ sở chưng cất thủ công truyền thống tinh dầu tràm tự nhiên xứ Huế.',
      desc: 'Cung cấp tinh dầu xông phòng khử khuẩn và muối ngâm chân thảo mộc cho toàn bộ các căn phòng sinh thái của Kim Long EcoStay.'
    },
    {
      id: 'PARTNER-NHA-RUONG-AN-HIEN',
      name: 'Nhà Rường Cổ An Hiên',
      category: 'Điểm Đến Di Sản',
      partnerGroup: 'Điểm trải nghiệm',
      address: '58 Kim Long, Phường Kim Long, TP. Huế',
      phone: '0234.352.345',
      distanceKm: 0.5,
      imageUrl: 'images/nha_ruong_2.jpg',
      hours: '07:30 - 17:30',
      highlights: ['Nhà Rường 3 Gian 2 Chái', 'Vườn Cây Trái 4 Mùa', 'Cổng Vòm Mận Đỏ'],
      summary: 'Di tích nhà rường tiêu biểu nhất của nghệ thuật kiến trúc nhà vườn Huế.',
      desc: 'Điểm dừng chân số 1 trên tuyến tour xe đạp trợ lực điện Eco-Bike xuất phát từ Kim Long EcoStay với thiết bị GPS Audio Guide tự động thuyết minh.'
    },
    {
      id: 'PARTNER-LANG-THUY-XUAN',
      name: 'Làng Hương & Nón Lá Thủy Xuân',
      category: 'Làng Nghề Truyền Thống',
      partnerGroup: 'Điểm trải nghiệm',
      address: 'Đường Huyền Trân Công Chúa, TP. Huế',
      phone: '0989.456.789',
      distanceKm: 3.8,
      imageUrl: 'images/song_huong_hoanghon.jpg',
      hours: '07:00 - 19:00',
      highlights: ['Chân Hương Rực Rỡ', 'Nón Bài Thơ Huế', 'Trầm Hương Tự Nhiên'],
      summary: 'Làng nghề làm hương trầm và chằm nón lá nổi tiếng hàng trăm năm ven chân đồi Vọng Cảnh.',
      desc: 'Liên kết trải nghiệm văn hóa: khách Kim Long EcoStay được tặng phiếu trải nghiệm tự tay se nón lá và làm thẻ hương trầm truyền thống.'
    },
    {
      id: 'PARTNER-BANH-BA-DO',
      name: 'Cơ Sở Bánh Bèo - Nậm - Lọc Bà Đỏ',
      category: 'Quán Ăn & Ẩm Thực',
      partnerGroup: 'Quán ăn và ẩm thực',
      address: '8 Nguyễn Bỉnh Khiêm, TP. Huế',
      phone: '0234.354.123',
      distanceKm: 2.9,
      imageUrl: 'images/mon_banhbeonamloc.jpg',
      hours: '08:00 - 21:00',
      highlights: ['Bánh Bèo Tôm Chấy', 'Bánh Nậm Lá Dong', 'Bánh Lọc Tôm Thịt'],
      summary: 'Quán bánh Huế gia truyền ba đời nức tiếng khắp trong và ngoài nước.',
      desc: 'Cung cấp các set bánh khai vị và mẹt điểm tâm Huế giao tận buồng phòng trong các khung giờ ăn sáng, ăn trưa và ăn xế.'
    },
    {
      id: 'PARTNER-TRA-SEN-TINH-TAM',
      name: 'Trà Sen & Hạt Sen Hồ Tịnh Tâm',
      category: 'Đặc Sản Nhà Vườn',
      partnerGroup: 'Đặc sản và sản phẩm địa phương',
      address: 'Khu vực Đinh Tiên Hoàng, TP. Huế',
      phone: '0935.678.901',
      distanceKm: 2.7,
      imageUrl: 'images/ho_sen_hue.jpg',
      hours: '06:00 - 18:30',
      highlights: ['Hạt Sen Tươi Bóc Vỏ', 'Trà Ướp Bông Sen Sống', 'Tim Sen Sấy Khô'],
      summary: 'Vựa sen ngự hoàng gia nổi tiếng từ thời các vua Nguyễn với hương sen thanh khiết.',
      desc: 'Cung cấp sen tươi và bông sen ướp trà trực tiếp cho các buổi thưởng trà hoàng gia tại Kim Long EcoStay.'
    }
  ];

  // =========================================================================
  // 3. DỮ LIỆU ĐẶC TẢ 30 SẢN PHẨM & ĐẶC SẢN (SPECIALTIES PRODUCTS DATA)
  // =========================================================================
  var SPECIALTY_PRODUCTS = [
    // A. ĐẶC SẢN HUẾ
    {
      id: 'SP001',
      name: 'Mè Xửng Giòn Cung Đình Thiên Hương',
      category: 'Đặc sản Huế',
      price: 45000,
      stock: 40,
      unit: 'hộp 300g',
      img: 'images/qua_mexung.jpg',
      origin: '20 Chi Lăng, TP. Huế',
      partnerId: 'PARTNER-ME-XUNG-THIEN-HUONG',
      partnerName: 'Cơ Sở Mè Xửng Thiên Hương',
      desc: 'Bánh mè xửng giòn vàng ruộm kẹp bánh tráng nướng mè, mạch nha dẻo ngọt và đậu phộng bùi béo chuẩn vị Cung Đình.',
      featured: true
    },
    {
      id: 'SP002',
      name: 'Mè Xửng Dẻo Thượng Hạng Gói Lá Chuối',
      category: 'Đặc sản Huế',
      price: 50000,
      stock: 35,
      unit: 'gói 500g',
      img: 'images/qua_mexung.jpg',
      origin: '20 Chi Lăng, TP. Huế',
      partnerId: 'PARTNER-ME-XUNG-THIEN-HUONG',
      partnerName: 'Cơ Sở Mè Xửng Thiên Hương',
      desc: 'Mè xửng dẻo mềm óng ánh, vị ngọt thanh tao của đường mía và hương thơm lừng của mè rang chín tới.',
      featured: false
    },
    {
      id: 'SP003',
      name: 'Mứt Gừng Kim Long Cổ Truyền Xứ Huế',
      category: 'Đặc sản Huế',
      price: 65000,
      stock: 28,
      unit: 'hộp 250g',
      img: 'images/qua_mutgung.jpg',
      origin: 'Làng mứt gừng cổ truyền Kim Long, TP. Huế',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Gừng đồi củ nhỏ cay nồng, xắt lát mỏng đều, sên đường cát trắng giòn cay đượm nồng khi thưởng thức cùng trà sen.',
      featured: true
    },
    {
      id: 'SP004',
      name: 'Mắm Tôm Chua Huế Thượng Hạng',
      category: 'Đặc sản Huế',
      price: 85000,
      stock: 30,
      unit: 'hũ 500g',
      img: 'images/mon_tomchua.jpg',
      origin: 'Phường Kim Long, TP. Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Tôm đất đầm phá tươi sống ủ chín men rượu, ớt chỉ thiên và củ riềng cay nồng, ăn kèm thịt luộc bánh tráng ngon trứ danh.',
      featured: true
    },
    {
      id: 'SP005',
      name: 'Mắm Ruốc Huế Chuẩn Vị Nấu Bún Bò',
      category: 'Đặc sản Huế',
      price: 55000,
      stock: 45,
      unit: 'hũ 400g',
      img: 'images/mon_mamruoc.jpg',
      origin: 'Cửa biển Thuận An, Thừa Thiên Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Ruốc biển tươi phơi sương ủ chum sành truyền thống, màu nâu tím mịn màng, thơm nồng đậm đà dùng nêm bún bò hoặc xào thịt.',
      featured: false
    },
    {
      id: 'SP006',
      name: 'Nem Chua & Tré Huế Gia Truyền',
      category: 'Đặc sản Huế',
      price: 90000,
      stock: 20,
      unit: 'xâu 10 chiếc',
      img: 'images/mon_nemchua_tre.jpg',
      origin: 'Đường Kim Long, TP. Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Nem chua gói lá ổi lên men tự nhiên và tré rơm giòn bùi vị riềng tỏi mè rang, món nhắm hảo hạng đất Thần Kinh.',
      featured: false
    },
    {
      id: 'SP007',
      name: 'Bánh Đậu Xanh Trái Cây Tiến Vua',
      category: 'Đặc sản Huế',
      price: 75000,
      stock: 25,
      unit: 'hộp 16 chiếc',
      img: 'images/mon_banhkhoai.jpg',
      origin: 'Cơ sở bánh truyền thống Kim Long, TP. Huế',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Bánh đậu xanh nặn hình trái cây tinh xảo, bọc lớp rau câu trong suốt sáng bóng, hương vị bùi béo thanh tao.',
      featured: true
    },
    {
      id: 'SP008',
      name: 'Kẹo Cau Kim Long Truyền Thống',
      category: 'Đặc sản Huế',
      price: 35000,
      stock: 50,
      unit: 'gói 200g',
      img: 'images/qua_mexung.jpg',
      origin: 'Làng kẹo Kim Long, TP. Huế',
      partnerId: 'PARTNER-ME-XUNG-THIEN-HUONG',
      partnerName: 'Cơ Sở Mè Xửng Thiên Hương',
      desc: 'Kẹo ngậm hình múi cau xứ Huế với phần ruột vàng óng từ đường và phần vỏ trắng tinh từ bột gạo, mộc mạc và thanh vị.',
      featured: false
    },

    // B. SẢN PHẨM NHÀ VƯỜN & NÔNG SẢN
    {
      id: 'SP009',
      name: 'Bưởi Thanh Trà Kim Long Tuyển Chọn',
      category: 'Sản phẩm nhà vườn',
      price: 95000,
      stock: 35,
      unit: 'kg (quả ~1kg)',
      img: 'images/nongsan_thanhtra.jpg',
      origin: 'Vườn cây ăn quả cổ thụ Phường Kim Long, TP. Huế',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Giống bưởi Thanh Trà tiến vua đặc hữu vùng phù sa sông Hương: tép ráo mọng nước, vị ngọt thanh mát và hương thơm tinh khiết.',
      featured: true
    },
    {
      id: 'SP010',
      name: 'Giỏ Rau Xanh Hữu Cơ Nhà Vườn Theo Mùa',
      category: 'Sản phẩm nhà vườn',
      price: 45000,
      stock: 25,
      unit: 'giỏ 1kg',
      img: 'images/nongsan_rauxanh.jpg',
      origin: 'Vườn sinh thái Kim Long, TP. Huế',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Rau cải non, xà lách, rau thơm, quả vả Huế tươi và rau má canh tác hữu cơ tại các hộ nhà vườn liên kết.',
      featured: false
    },
    {
      id: 'SP011',
      name: 'Hạt Sen Tươi Hồ Tịnh Tâm Bóc Vỏ',
      category: 'Sản phẩm nhà vườn',
      price: 135000,
      stock: 20,
      unit: 'gói 500g',
      img: 'images/ho_sen_hue.jpg',
      origin: 'Hồ Tịnh Tâm, TP. Huế',
      partnerId: 'PARTNER-TRA-SEN-TINH-TAM',
      partnerName: 'Trà Sen & Hạt Sen Hồ Tịnh Tâm',
      desc: 'Hạt sen trắng tươi bóc vỏ thông tâm trong ngày, hạt tròn đều, khi nấu chè hay hầm canh đều bở tơi ngát hương.',
      featured: true
    },
    {
      id: 'SP012',
      name: 'Trái Cây Miệt Vườn Sinh Thái Kim Long',
      category: 'Sản phẩm nhà vườn',
      price: 50000,
      stock: 28,
      unit: 'phần 1.5kg',
      img: 'images/nongsan_traicay.jpg',
      origin: 'Vườn sinh thái Nguyệt Biều & Kim Long',
      partnerId: 'PARTNER-NV-NGUYET-BIEU',
      partnerName: 'Nhà Vườn Sinh Thái Nguyệt Biều',
      desc: 'Chuối ngự tiến vua, ổi sẻ giòn ngọt, măng cụt hoặc thanh long ruột đỏ chín cây thu hái tự nhiên.',
      featured: false
    },
    {
      id: 'SP013',
      name: 'Nấm Bào Ngư & Nông Sản Củ Quả Sạch',
      category: 'Sản phẩm nhà vườn',
      price: 45000,
      stock: 30,
      unit: 'phần 500g',
      img: 'images/nongsan_cuqua.jpg',
      origin: 'Vườn sinh thái Kim Long, TP. Huế',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Vả Huế tươi giòn, khoai mỡ tím, bí đỏ hữu cơ và nấm bào ngư tươi thu hoạch mỗi buổi sớm.',
      featured: false
    },
    {
      id: 'SP014',
      name: 'Giỏ Quà Nông Sản Sinh Thái Kim Long',
      category: 'Sản phẩm nhà vườn',
      price: 190000,
      stock: 18,
      unit: 'giỏ mây tre đan',
      img: 'images/gio_nongsan_sach.jpg',
      origin: 'Mạng lưới nhà vườn liên kết Kim Long',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Giỏ quà tinh tế gồm 1 quả Thanh Trà Kim Long, 1 bó rau sạch hữu cơ, 500g củ quả sạch và thảo mộc thơm phòng.',
      featured: true
    },

    // C. ĐỒ UỐNG VÀ TRÀ CUNG ĐÌNH
    {
      id: 'SP015',
      name: 'Trà Sen Cung Đình Đức Phượng Thượng Hạng',
      category: 'Đồ uống và trà',
      price: 120000,
      stock: 30,
      unit: 'hộp 250g',
      img: 'images/qua_trasen.jpg',
      origin: '24 Nguyễn Huệ, TP. Huế',
      partnerId: 'PARTNER-TRA-DUC-PHUONG',
      partnerName: 'Trà Cung Đình Đức Phượng',
      desc: 'Trà xanh Tân Cương ướp hoa sen Tịnh Tâm tự nhiên qua 7 lần ngậm hương, hậu vị ngọt sâu lắng.',
      featured: true
    },
    {
      id: 'SP016',
      name: 'Trà Cung Đình Huế 16 Vị Thảo Mộc',
      category: 'Đồ uống và trà',
      price: 85000,
      stock: 50,
      unit: 'gói 500g',
      img: 'images/tra_cung_dinh.jpg',
      origin: '24 Nguyễn Huệ, TP. Huế',
      partnerId: 'PARTNER-TRA-DUC-PHUONG',
      partnerName: 'Trà Cung Đình Đức Phượng',
      desc: 'Bí quyết thảo mộc hoàng gia: atiso, cúc hoa, kỷ tử, hoa hồi, cam thảo, tim sen giúp thanh nhiệt, ngủ ngon và bồi bổ sức khỏe.',
      featured: true
    },
    {
      id: 'SP017',
      name: 'Nước Ép Bưởi Thanh Trà Tươi Nguyên Chất',
      category: 'Đồ uống và trà',
      price: 40000,
      stock: 40,
      unit: 'chai 330ml',
      img: 'images/nuoc_thanhtra.jpg',
      origin: 'Quầy bar sinh thái Kim Long EcoStay',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Bưởi Thanh Trà Kim Long ép lạnh tươi mới giữ trọn vitamin C tự nhiên, vị chua ngọt thanh tao giải nhiệt ngày hè.',
      featured: false
    },
    {
      id: 'SP018',
      name: 'Nước Chanh Sả Gừng Thảo Mộc Nhà Vườn',
      category: 'Đồ uống và trà',
      price: 35000,
      stock: 40,
      unit: 'chai 350ml',
      img: 'images/nuoc_thaomoc.jpg',
      origin: 'Quầy bar sinh thái Kim Long EcoStay',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Chanh vườn, sả cây tươi và gừng Kim Long hữu cơ nấu đường phèn thanh nhẹ, ấm bụng và tăng cường đề kháng.',
      featured: false
    },
    {
      id: 'SP019',
      name: 'Trà Atiso Đỏ Hữu Cơ Ướp Mật Ong Rừng',
      category: 'Đồ uống và trà',
      price: 45000,
      stock: 25,
      unit: 'ly 400ml',
      img: 'images/tra_thaomoc.jpg',
      origin: 'Quầy bar sinh thái Kim Long EcoStay',
      partnerId: 'PARTNER-NV-NGUYET-BIEU',
      partnerName: 'Nhà Vườn Sinh Thái Nguyệt Biều',
      desc: 'Hoa atiso đỏ thu hái từ vườn sinh thái, ngâm chua ngọt tự nhiên hòa quyện cùng mật ong hoa bưởi dịu nhẹ.',
      featured: false
    },

    // D. ẨM THỰC HUẾ GIAO TẬN BUỒNG PHÒNG (15-30 PHÚT)
    {
      id: 'SP020',
      name: 'Bún Bò Huế Kim Long Chuẩn Vị Nước Dùng',
      category: 'Ẩm thực Huế',
      price: 55000,
      stock: 30,
      unit: 'tô đầy đủ (Giao nóng)',
      img: 'images/mon_bunbo.jpg',
      origin: 'Phường Kim Long, TP. Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Bún sợi to, nạm bò mềm, chả cua Huế thơm nức, huyết và móng giò ninh cùng sả cây và mắm ruốc truyền thống.',
      featured: true
    },
    {
      id: 'SP021',
      name: 'Cơm Hến Kim Long Kèm Canh Ruốc Nóng',
      category: 'Ẩm thực Huế',
      price: 40000,
      stock: 35,
      unit: 'phần đầy đủ (Giao nóng)',
      img: 'images/mon_comhen.jpg',
      origin: 'Phường Kim Long, TP. Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Hến xào đậm vị, tóp mỡ giòn rụm, môn ngọt bắp chuối thái mịn ăn cùng ớt sa tế cay xé lưỡi và bát nước luộc hến thanh mát.',
      featured: true
    },
    {
      id: 'SP022',
      name: 'Mẹt Bánh Ướt Thịt Nướng Huyền Anh Đặc Biệt',
      category: 'Ẩm thực Huế',
      price: 65000,
      stock: 35,
      unit: 'mẹt 4 cuốn lớn',
      img: 'images/mon_banhuot.jpg',
      origin: '50 Kim Long, TP. Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Bánh ướt tráng mỏng cuốn rau thơm tươi và thịt ba chỉ nướng than hoa thơm lừng chấm tương đậu mè béo ngậy.',
      featured: true
    },
    {
      id: 'SP023',
      name: 'Mẹt Bánh Huế Thập Cẩm (Bèo - Nậm - Lọc Bà Đỏ)',
      category: 'Ẩm thực Huế',
      price: 70000,
      stock: 25,
      unit: 'mẹt 18 chiếc',
      img: 'images/mon_banhbeonamloc.jpg',
      origin: '8 Nguyễn Bỉnh Khiêm, TP. Huế',
      partnerId: 'PARTNER-BANH-BA-DO',
      partnerName: 'Cơ Sở Bánh Bèo - Nậm - Lọc Bà Đỏ',
      desc: '10 chén bánh bèo tôm chấy da heo giòn, 4 bánh nậm lá dong mềm mịn và 4 bánh lọc tôm thịt trong veo chấm mắm ớt xanh.',
      featured: true
    },
    {
      id: 'SP024',
      name: 'Bánh Khoái Hoàng Cung Nhân Tôm Thịt Giá Đỗ',
      category: 'Ẩm thực Huế',
      price: 50000,
      stock: 20,
      unit: 'phần 2 chiếc giòn rụm',
      img: 'images/mon_banhkhoai.jpg',
      origin: 'Quán bánh truyền thống Kim Long',
      partnerId: 'PARTNER-BANH-BA-DO',
      partnerName: 'Cơ Sở Bánh Bèo - Nậm - Lọc Bà Đỏ',
      desc: 'Bánh khoái chiên giòn vàng ươm nhân tôm tươi, thịt nạc và giá đỗ, chấm nước lèo gan đậu phộng đậm đà.',
      featured: false
    },
    {
      id: 'SP025',
      name: 'Nem Lụi Nướng Sả Cuốn Bánh Tráng',
      category: 'Ẩm thực Huế',
      price: 60000,
      stock: 25,
      unit: 'phần 6 cây nướng nóng',
      img: 'images/mon_nemlui.jpg',
      origin: 'Phường Kim Long, TP. Huế',
      partnerId: 'PARTNER-BANH-UOT-HUYEN-ANH',
      partnerName: 'Bánh Ướt Thịt Nướng Huyền Anh',
      desc: 'Nem lụi bọc cọng sả tươi nướng trên than hoa đượm mùi, ăn kèm rau sống vườn nhà, dưa chuột và nước lèo gan bùi thơm.',
      featured: false
    },

    // E. QUÀ LƯU NIỆM, TRẢI NGHIỆM & THẢO MỘC
    {
      id: 'SP026',
      name: 'Tinh Dầu Tràm Gió Nguyên Chất Thanh Loan',
      category: 'Quà lưu niệm',
      price: 150000,
      stock: 30,
      unit: 'chai thủy tinh 100ml',
      img: 'images/qua_tinhdautram.jpg',
      origin: 'Đường Kim Long, TP. Huế',
      partnerId: 'PARTNER-TINH-DAU-THANH-LOAN',
      partnerName: 'Tinh Dầu Tràm Cố Đô Thanh Loan',
      desc: 'Chưng cất 100% từ lá tràm gió tự nhiên, mùi thơm ấm áp dịu nhẹ, giữ ấm cơ thể, chống cảm lạnh và xua đuổi muỗi.',
      featured: true
    },
    {
      id: 'SP027',
      name: 'Nón Bài Thơ Làng Nghề Thủy Xuân',
      category: 'Quà lưu niệm',
      price: 95000,
      stock: 20,
      unit: 'chiếc',
      img: 'images/song_huong_hoanghon.jpg',
      origin: 'Làng nghề Thủy Xuân, TP. Huế',
      partnerId: 'PARTNER-LANG-THUY-XUAN',
      partnerName: 'Làng Hương & Nón Lá Thủy Xuân',
      desc: 'Nón lá chằm thủ công tỉ mỉ bằng lá cọ mỏng, soi lên ánh sáng hiện rõ các câu thơ tình xứ Huế và hình ảnh cầu Trường Tiền.',
      featured: false
    },
    {
      id: 'SP028',
      name: 'Muối Thảo Mộc Ngâm Chân Bồn Gỗ Pơ-Mu',
      category: 'Quà lưu niệm',
      price: 75000,
      stock: 40,
      unit: 'hũ 400g',
      img: 'images/qua_tinhdautram.jpg',
      origin: 'Đường Kim Long, TP. Huế',
      partnerId: 'PARTNER-TINH-DAU-THANH-LOAN',
      partnerName: 'Tinh Dầu Tràm Cố Đô Thanh Loan',
      desc: 'Muối khoáng biển hầm thảo mộc: gừng, quế, ngải cứu, thiên niên kiện và tinh dầu tràm giúp lưu thông khí huyết và ngủ sâu giấc.',
      featured: true
    },
    {
      id: 'SP029',
      name: 'Trầm Hương Thư Giãn Tự Nhiên Thủy Xuân',
      category: 'Quà lưu niệm',
      price: 180000,
      stock: 25,
      unit: 'hộp 40 nụ trầm cao cấp',
      img: 'images/song_huong_hoanghon.jpg',
      origin: 'Làng nghề Thủy Xuân, TP. Huế',
      partnerId: 'PARTNER-LANG-THUY-XUAN',
      partnerName: 'Làng Hương & Nón Lá Thủy Xuân',
      desc: 'Nụ trầm hương thiên nhiên không hóa chất, khói thơm dịu ngọt thanh lọc không khí và mang lại cảm giác bình yên thư thái.',
      featured: false
    },
    {
      id: 'SP030',
      name: 'Combo Quà Tặng Di Sản Kim Long EcoStay',
      category: 'Quà lưu niệm',
      price: 320000,
      stock: 15,
      unit: 'hộp quà sang trọng',
      img: 'images/qua_combokimlong.jpg',
      origin: 'Kim Long EcoStay Center',
      partnerId: 'PARTNER-NV-HUONG-SEN',
      partnerName: 'Nhà Vườn Sinh Thái Hương Sen Kim Long',
      desc: 'Bộ quà tặng cao cấp gồm: 1 hộp Trà Cung Đình 16 vị, 1 hộp mè xửng giòn Thiên Hương, 1 chai tinh dầu tràm Thanh Loan và thiệp chúc mừng.',
      featured: true
    }
  ];

  // =========================================================================
  // 4. DỮ LIỆU ĐẶC TẢ 4 GÓI TRÀ CUNG ĐÌNH (ROYAL TEA PACKAGES DATA)
  // =========================================================================
  var ROYAL_TEA_PACKAGES = {
    'TEA-HOMESTAY': {
      code: 'TEA-HOMESTAY',
      name: 'Thưởng Trà Cung Đình Tại Homestay',
      price: 85000,
      priceType: 'người',
      maxCapacity: 8,
      duration: '45 phút',
      img: 'images/nha_ruong_2.jpg',
      schedule: '08:30 • 15:30 • 19:30',
      story: 'Trải nghiệm thưởng trà truyền thống trong không gian thanh tịnh của nhà rường Kim Long. Quý khách được phục vụ ấm trà sen Tịnh Tâm hoặc thảo mộc 16 vị Cung Đình hãm trong ấm đất nung, kèm mứt gừng cay nồng cổ truyền xứ Huế.',
      ingredients: [
        'Hoa sen trắng Tịnh Tâm ướp trà Tân Cương',
        'Thảo mộc 16 vị: cúc chi, kỷ tử, táo đỏ, cam thảo, cỏ ngọt...',
        'Nước giếng đá tinh khiết đun sôi trên bếp than nhẹ',
        'Mứt gừng Kim Long sên đường cát giòn cay'
      ],
      protocol: 'Rót trà 3 tuần, thưởng hương hoa sen, nhấp ngụm đầu chậm rãi để cảm nhận vị chát thanh dịu rồi ngọt hậu sâu lắng.',
      cakesIncluded: 'Mứt gừng lát Kim Long & Kẹo cau mộc xứ Huế.'
    },
    'TEA-CAKES': {
      code: 'TEA-CAKES',
      name: 'Trải Nghiệm Thưởng Trà Kết Hợp Bánh Huế',
      price: 135000,
      priceType: 'người',
      maxCapacity: 12,
      duration: '60 phút',
      img: 'images/mon_banhkhoai.jpg',
      schedule: '09:00 • 15:00',
      story: 'Sự kết hợp hoàn hảo giữa văn hóa ẩm trà Cung Đình và nghệ thuật làm bánh tiến vua tinh xảo của các nghệ nhân Kim Long.',
      ingredients: [
        'Trà Hoàng Triều thảo mộc cao cấp',
        'Bánh đậu xanh trái cây nặn thủ công tỉ mỉ',
        'Bánh sen nướng giòn xốp thơm lừng',
        'Mè xửng giòn mè rang Thiên Hương'
      ],
      protocol: 'Thưởng bánh trước khi nhấp trà để vị bùi ngọt của bánh hòa quyện nâng tầm hương thơm thảo mộc.',
      cakesIncluded: 'Mẹt bánh tiến vua 4 loại: Đậu xanh trái cây, Bánh sen nướng, Mè xửng giòn, Mứt gừng sấy dẻo.'
    },
    'TEA-COUPLE': {
      code: 'TEA-COUPLE',
      name: 'Gói Thưởng Trà Cặp Đôi & Nhóm Bạn',
      price: 250000,
      priceType: 'gói (2-4 người)',
      maxCapacity: 4,
      duration: '75 phút',
      img: 'images/nha_ruong_1.jpg',
      schedule: 'Linh hoạt theo yêu cầu hẹn trước',
      story: 'Bàn trà riêng tư bài trí lãng mạn giữa vườn bưởi Thanh Trà hoặc chòi vọng cảnh ngắm hoàng hôn sông Hương, thắp nến hoa sen lung linh.',
      ingredients: [
        '2 ấm trà tùy chọn (Trà Sen Tịnh Tâm & Trà Quý Phi)',
        'Khay bánh Huế nghệ nhân đặc biệt',
        'Hoa sen tươi trang trí bàn trà',
        'Đèn lồng thắp sáng buổi tối'
      ],
      protocol: 'Không gian riêng tư tuyệt đối, nhân viên phục vụ sẵn sàng hỗ trợ chụp ảnh kỷ niệm phong cách Cố đô.',
      cakesIncluded: 'Khay bánh hoa đăng cao cấp & đĩa trái cây tươi vườn nhà theo mùa.'
    },
    'TEA-CULTURE': {
      code: 'TEA-CULTURE',
      name: 'Gói Trải Nghiệm Văn Hóa Huế Kết Hợp Giới Thiệu Trà',
      price: 390000,
      priceType: 'gói trải nghiệm',
      maxCapacity: 6,
      duration: '90 phút',
      img: 'images/song_huong_hoanghon.jpg',
      schedule: '15:30 - 17:00 hàng ngày',
      story: 'Workshop chuyên sâu cùng nghệ nhân văn hóa Huế: lắng nghe lịch sử trà đạo triều Nguyễn, học kỹ thuật ướp trà sen sống và tự tay pha ấm trà chuẩn mực hoàng gia.',
      ingredients: [
        'Bông sen tươi Hồ Tịnh Tâm để thực hành ướp trà',
        'Bộ đồ trà tử sa cao cấp',
        'Nguyên liệu 16 vị thảo mộc chuẩn ngự y',
        'Chứng nhận trải nghiệm văn hóa Cố Đô từ Kim Long EcoStay'
      ],
      protocol: 'Nghệ nhân trực tiếp hướng dẫn nghi thức bái trà, hãm trà, dâng trà và luận đàm thi ca di sản.',
      cakesIncluded: 'Trọn bộ điểm tâm Cung Đình và tặng 1 túi trà sen thành phẩm do chính tay bạn ướp.'
    }
  };

  // =========================================================================
  // 5. LOGIC HIỂN THỊ & BỘ LỌC ĐẶC SẢN (SPECIALTIES RENDER & FILTER)
  // =========================================================================
  var currentCategory = 'ALL';
  var currentPage = 1;
  var ITEMS_PER_PAGE = 8;

  window.setSpecialtyCategory = function (cat, btnEl) {
    currentCategory = cat;
    currentPage = 1;

    // Cập nhật class active cho nút lọc
    var filterBtns = document.querySelectorAll('.kle-spec-filter-btn');
    filterBtns.forEach(function (btn) {
      btn.classList.remove('active', 'btn-success');
      btn.classList.add('btn-outline-secondary');
    });

    if (btnEl) {
      btnEl.classList.remove('btn-outline-secondary');
      btnEl.classList.add('active', 'btn-success');
    }

    window.filterSpecialties();
  };

  window.filterSpecialties = function () {
    var searchInput = document.getElementById('specialtySearchInput');
    var partnerSelect = document.getElementById('specialtyPartnerSelect');
    var priceFilter = document.getElementById('specialtyPriceFilter');
    var sortSelect = document.getElementById('specialtySortSelect');
    var grid = document.getElementById('specialtiesGrid');
    var countEl = document.getElementById('specialtyResultsCount');

    if (!grid) return;

    var query = (searchInput ? searchInput.value : '').toLowerCase().trim();
    var partnerId = partnerSelect ? partnerSelect.value : 'ALL';
    var priceRange = priceFilter ? priceFilter.value : 'ALL';
    var sortOrder = sortSelect ? sortSelect.value : 'featured';

    var filtered = SPECIALTY_PRODUCTS.filter(function (prod) {
      // 1. Lọc theo danh mục
      var matchCat = true;
      if (currentCategory !== 'ALL') {
        matchCat = prod.category.toLowerCase().includes(currentCategory.toLowerCase()) ||
                   (currentCategory === 'Đặc sản Huế' && prod.category === 'Đặc sản Huế') ||
                   (currentCategory === 'Sản phẩm nhà vườn' && prod.category === 'Sản phẩm nhà vườn') ||
                   (currentCategory === 'Đồ uống và trà' && prod.category === 'Đồ uống và trà') ||
                   (currentCategory === 'Ẩm thực Huế' && prod.category === 'Ẩm thực Huế') ||
                   (currentCategory === 'Quà lưu niệm' && prod.category === 'Quà lưu niệm');
      }

      // 2. Lọc theo đối tác
      var matchPartner = (partnerId === 'ALL' || prod.partnerId === partnerId);

      // 3. Lọc theo giá
      var matchPrice = true;
      if (priceRange === 'under50') matchPrice = prod.price < 50000;
      else if (priceRange === '50to100') matchPrice = (prod.price >= 50000 && prod.price <= 100000);
      else if (priceRange === '100to200') matchPrice = (prod.price > 100000 && prod.price <= 200000);
      else if (priceRange === 'above200') matchPrice = prod.price > 200000;

      // 4. Lọc theo từ khóa tìm kiếm
      var matchQuery = !query ||
        prod.name.toLowerCase().includes(query) ||
        prod.desc.toLowerCase().includes(query) ||
        prod.category.toLowerCase().includes(query) ||
        (prod.partnerName && prod.partnerName.toLowerCase().includes(query));

      return matchCat && matchPartner && matchPrice && matchQuery;
    });

    // Sắp xếp
    if (sortOrder === 'price-asc') {
      filtered.sort(function (a, b) { return a.price - b.price; });
    } else if (sortOrder === 'price-desc') {
      filtered.sort(function (a, b) { return b.price - a.price; });
    } else if (sortOrder === 'name-asc') {
      filtered.sort(function (a, b) { return a.name.localeCompare(b.name, 'vi'); });
    } else {
      // Mặc định: sản phẩm nổi bật lên trước
      filtered.sort(function (a, b) { return (b.featured ? 1 : 0) - (a.featured ? 1 : 0); });
    }

    if (countEl) {
      countEl.innerHTML = 'Hiển thị <strong>' + filtered.length + '</strong> / ' + SPECIALTY_PRODUCTS.length + ' sản phẩm';
    }

    // Phân trang
    var totalPages = Math.ceil(filtered.length / ITEMS_PER_PAGE) || 1;
    if (currentPage > totalPages) currentPage = totalPages;
    var startIdx = (currentPage - 1) * ITEMS_PER_PAGE;
    var pageItems = filtered.slice(startIdx, startIdx + ITEMS_PER_PAGE);

    if (pageItems.length === 0) {
      grid.innerHTML = '<div class="col-12 text-center py-5 bg-white rounded-4 border text-muted">' +
        '<i class="bi bi-basket2 fs-1 text-muted d-block mb-2"></i>' +
        '<h5 class="text-dark fw-bold">Không tìm thấy sản phẩm hoặc món ăn phù hợp</h5>' +
        '<p class="small text-secondary">Vui lòng thử tìm với từ khóa khác hoặc bấm nút "Tất Cả (30)"</p>' +
        '</div>';
      renderPagination(0, 1);
      return;
    }

    grid.innerHTML = pageItems.map(function (p) {
      var priceFormatted = p.price.toLocaleString('vi-VN') + ' đ';
      return `
        <div class="col-xl-3 col-lg-4 col-md-6">
          <div class="card h-100 border-0 rounded-4 shadow-sm overflow-hidden specialty-product-card bg-white d-flex flex-column" style="transition: transform 0.2s ease, box-shadow 0.2s ease;">
            <div class="position-relative" style="height: 180px; overflow: hidden; cursor: pointer;" onclick="window.openProductDetailModal('${p.id}')">
              <img src="${p.img}" alt="${p.name}" class="w-100 h-100" style="object-fit: cover; transition: transform 0.3s ease;" onerror="this.src='images/mon_banhuot.jpg'" loading="lazy" />
              <span class="badge position-absolute top-0 start-0 m-3 rounded-pill px-2.5 py-1 shadow-sm fw-bold text-white" style="font-size: 11px; background: rgba(20, 69, 47, 0.85);">
                ${p.category}
              </span>
              <span class="badge position-absolute top-0 end-0 m-3 rounded-pill px-2 py-1 shadow-sm bg-success text-white" style="font-size: 11px;">
                Còn hàng (${p.stock})
              </span>
            </div>
            <div class="p-3 d-flex flex-column flex-grow-1">
              <div class="small text-muted mb-1 text-truncate" title="${p.partnerName}">
                <i class="bi bi-shop text-success me-1"></i>${p.partnerName}
              </div>
              <h6 class="fw-bold text-dark font-serif mb-2" style="font-size: 15px; cursor: pointer; line-height: 1.4;" onclick="window.openProductDetailModal('${p.id}')" title="${p.name}">
                ${p.name}
              </h6>
              <p class="text-muted small mb-3 flex-grow-1 text-truncate-2" style="font-size: 12.5px; line-height: 1.5;">
                ${p.desc}
              </p>
              <div class="d-flex align-items-baseline justify-content-between pt-2 border-top mt-auto mb-2">
                <div>
                  <span class="fs-5 fw-bold text-success font-monospace">${priceFormatted}</span>
                  <small class="text-muted">/${p.unit}</small>
                </div>
              </div>
              <div class="d-flex gap-2">
                <button type="button" class="btn btn-sm btn-outline-secondary rounded-pill px-2 flex-grow-1" onclick="window.openProductDetailModal('${p.id}')" title="Xem chi tiết sản phẩm">
                  <i class="bi bi-eye me-1"></i>Chi tiết
                </button>
                <button type="button" class="btn btn-sm btn-success rounded-pill px-2 flex-grow-1 fw-bold shadow-sm" onclick="window.openRoomDeliveryModal('${p.id}')" title="Đặt giao món tận phòng trong 15-30 phút">
                  <i class="bi bi-box-seam me-1"></i>Đặt Giao
                </button>
              </div>
            </div>
          </div>
        </div>
      `;
    }).join('');

    renderPagination(totalPages, currentPage);
  };

  function renderPagination(totalPages, activePage) {
    var pEl = document.getElementById('specialtiesPagination');
    if (!pEl) return;
    if (totalPages <= 1) {
      pEl.innerHTML = '';
      return;
    }

    var html = '<div class="btn-group rounded-pill border bg-white p-1 shadow-sm" role="group">';
    for (var i = 1; i <= totalPages; i++) {
      var isAct = (i === activePage);
      html += '<button type="button" class="btn btn-sm rounded-pill px-3 fw-bold ' +
        (isAct ? 'btn-success text-white' : 'btn-light text-dark') +
        '" onclick="window.goToSpecialtyPage(' + i + ')">' + i + '</button>';
    }
    html += '</div>';
    pEl.innerHTML = html;
  }

  window.goToSpecialtyPage = function (page) {
    currentPage = page;
    window.filterSpecialties();
    var sEl = document.getElementById('specialties');
    if (sEl) {
      window.scrollToSection('specialties', false);
    }
  };

  // =========================================================================
  // 6. MODAL CHI TIẾT SẢN PHẨM & ĐẶT GIAO PHÒNG (PRODUCT & DELIVERY MODALS)
  // =========================================================================

  window.openProductDetailModal = function (id) {
    var p = SPECIALTY_PRODUCTS.find(function (item) { return item.id === id; });
    if (!p) return;

    var titleEl = document.getElementById('productDetailModalTitle');
    var catEl = document.getElementById('productDetailModalCategory');
    var bodyEl = document.getElementById('productDetailModalBody');
    var cartBtn = document.getElementById('productDetailCartBtn');
    var deliverBtn = document.getElementById('productDetailDeliverBtn');

    if (titleEl) titleEl.textContent = p.name;
    if (catEl) catEl.textContent = p.category + ' • Cung cấp bởi ' + p.partnerName;

    if (bodyEl) {
      bodyEl.innerHTML = `
        <div class="row g-4">
          <div class="col-md-5">
            <div class="rounded-4 overflow-hidden border shadow-sm" style="max-height: 280px;">
              <img src="${p.img}" alt="${p.name}" class="w-100 h-100" style="object-fit: cover;" onerror="this.src='images/mon_banhuot.jpg'" />
            </div>
            <div class="mt-3 p-2 bg-light rounded-3 text-center border">
              <span class="fs-4 fw-bold text-success font-monospace">${p.price.toLocaleString('vi-VN')} đ</span>
              <small class="text-muted d-block">Đơn vị: ${p.unit}</small>
            </div>
          </div>
          <div class="col-md-7">
            <div class="badge bg-success bg-opacity-10 text-success border border-success rounded-pill px-3 py-1 mb-2">
              <i class="bi bi-shield-check me-1"></i>Sản Phẩm Xác Thực OCOP Huế
            </div>
            <h5 class="fw-bold font-serif text-dark mb-2">${p.name}</h5>
            <p class="text-muted" style="line-height: 1.7;">${p.desc}</p>
            
            <div class="d-flex flex-column gap-2 p-3 bg-light rounded-3 border mb-3 small">
              <div><i class="bi bi-shop text-success me-2"></i><strong>Đối tác cung cấp:</strong> ${p.partnerName}</div>
              <div><i class="bi bi-geo-alt-fill text-danger me-2"></i><strong>Địa chỉ xuất xứ:</strong> ${p.origin}</div>
              <div><i class="bi bi-box2 text-primary me-2"></i><strong>Tình trạng hàng:</strong> Còn ${p.stock} ${p.unit} sẵn sàng</div>
              <div><i class="bi bi-truck text-warning me-2"></i><strong>Phục vụ phòng:</strong> Giao nóng tận phòng trong 15 - 30 phút</div>
            </div>

            <div class="alert alert-warning d-flex align-items-center gap-2 py-2 px-3 rounded-3 small mb-0">
              <i class="bi bi-info-circle-fill text-warning fs-5"></i>
              <div>Khách lưu trú tại Kim Long EcoStay có thể đặt món ăn nóng sốt giao tận cửa buồng phòng hoặc thanh toán khi nhận hàng.</div>
            </div>
          </div>
        </div>
      `;
    }

    if (cartBtn) {
      cartBtn.onclick = function () {
        alert('Đã thêm "' + p.name + '" vào giỏ hàng thành công!');
        var modalEl = document.getElementById('productDetailModal');
        if (modalEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(modalEl).hide();
        }
      };
    }

    if (deliverBtn) {
      deliverBtn.onclick = function () {
        var modalEl = document.getElementById('productDetailModal');
        if (modalEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(modalEl).hide();
        }
        window.openRoomDeliveryModal(p.id);
      };
    }

    var mEl = document.getElementById('productDetailModal');
    if (mEl && window.bootstrap) {
      bootstrap.Modal.getOrCreateInstance(mEl).show();
    }
  };

  window.openRoomDeliveryModal = function (id) {
    var p = SPECIALTY_PRODUCTS.find(function (item) { return item.id === id; }) || SPECIALTY_PRODUCTS[0];
    if (!p) return;

    var prodIdEl = document.getElementById('deliveryProductId');
    var prodNameEl = document.getElementById('deliveryProductName');
    var unitPriceEl = document.getElementById('deliveryUnitPrice');
    var displayNameEl = document.getElementById('deliveryDisplayProdName');
    var displaySupplierEl = document.getElementById('deliveryDisplaySupplier');
    var displayPriceEl = document.getElementById('deliveryDisplayPrice');
    var qtyEl = document.getElementById('deliveryQuantity');

    if (prodIdEl) prodIdEl.value = p.id;
    if (prodNameEl) prodNameEl.value = p.name;
    if (unitPriceEl) unitPriceEl.value = p.price;
    if (displayNameEl) displayNameEl.textContent = p.name;
    if (displaySupplierEl) displaySupplierEl.textContent = 'Cung cấp bởi: ' + p.partnerName;
    if (displayPriceEl) displayPriceEl.textContent = p.price.toLocaleString('vi-VN') + ' đ / ' + p.unit;
    if (qtyEl) qtyEl.value = 1;

    window.calculateDeliveryTotal();

    var mEl = document.getElementById('roomDeliveryModal');
    if (mEl && window.bootstrap) {
      bootstrap.Modal.getOrCreateInstance(mEl).show();
    }
  };

  window.calculateDeliveryTotal = function () {
    var unitPrice = parseInt(document.getElementById('deliveryUnitPrice')?.value || 0);
    var qty = parseInt(document.getElementById('deliveryQuantity')?.value || 1);
    var total = unitPrice * qty;
    var totalDisplayEl = document.getElementById('deliveryTotalDisplay');
    if (totalDisplayEl) {
      totalDisplayEl.textContent = total.toLocaleString('vi-VN') + ' đ';
    }
  };

  window.handleRoomDeliverySubmit = function (e) {
    if (e && e.preventDefault) e.preventDefault();

    var prodId = document.getElementById('deliveryProductId')?.value || '';
    var prodName = document.getElementById('deliveryProductName')?.value || '';
    var unitPrice = parseInt(document.getElementById('deliveryUnitPrice')?.value || 0);
    var room = document.getElementById('deliveryRoomNumber')?.value || '101';
    var qty = parseInt(document.getElementById('deliveryQuantity')?.value || 1);
    var custName = document.getElementById('deliveryCustomerName')?.value || 'Khách Quý';
    var phone = document.getElementById('deliveryPhone')?.value || '';
    var paymentMethod = document.getElementById('deliveryPaymentMethod')?.value || 'CASH_ROOM';
    var timeMode = document.getElementById('deliveryTimeMode')?.value || 'ASAP';
    var notes = document.getElementById('deliveryNotes')?.value || '';
    var total = unitPrice * qty;
    var orderCode = 'DELIV-' + Math.floor(100000 + Math.random() * 900000);

    var submitBtn = document.getElementById('deliverySubmitBtn');
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang tiếp nhận...';
    }

    var payload = {
      orderCode: orderCode,
      roomNumber: room,
      customerName: custName,
      phone: phone,
      productId: prodId,
      productName: prodName,
      quantity: qty,
      unitPrice: unitPrice,
      totalAmount: total,
      paymentMethod: paymentMethod,
      timeSlot: timeMode,
      specialRequests: notes
    };

    fetch('/api/public/room-delivery', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
      .then(function (res) { return res.json(); })
      .then(function (data) {
        var mEl = document.getElementById('roomDeliveryModal');
        if (mEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(mEl).hide();
        }
        var codeEl = document.getElementById('deliverySuccessCode');
        if (codeEl) codeEl.textContent = data.orderCode || orderCode;
        var succEl = document.getElementById('deliverySuccessModal');
        if (succEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(succEl).show();
        }
      })
      .catch(function () {
        var mEl = document.getElementById('roomDeliveryModal');
        if (mEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(mEl).hide();
        }
        var codeEl = document.getElementById('deliverySuccessCode');
        if (codeEl) codeEl.textContent = orderCode;
        var succEl = document.getElementById('deliverySuccessModal');
        if (succEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(succEl).show();
        }
      })
      .finally(function () {
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.innerHTML = '<i class="bi bi-send-check-fill me-1"></i>Xác Nhận Đặt Món';
        }
      });
  };

  // =========================================================================
  // 7. LOGIC ROYAL TEA (MODALS & API SUBMISSION)
  // =========================================================================

  window.openTeaDetailModal = function (code) {
    var tea = ROYAL_TEA_PACKAGES[code] || ROYAL_TEA_PACKAGES['TEA-HOMESTAY'];
    if (!tea) return;

    var titleEl = document.getElementById('teaDetailModalTitle');
    var bodyEl = document.getElementById('teaDetailModalBody');
    var bookBtn = document.getElementById('teaDetailModalBookBtn');

    if (titleEl) titleEl.textContent = tea.name;

    if (bodyEl) {
      bodyEl.innerHTML = `
        <div class="row g-4">
          <div class="col-md-5">
            <div class="rounded-4 overflow-hidden border shadow-sm" style="max-height: 260px;">
              <img src="${tea.img}" alt="${tea.name}" class="w-100 h-100" style="object-fit: cover;" onerror="this.src='images/hero_ecostay.jpg'" />
            </div>
            <div class="mt-3 p-3 bg-light rounded-3 text-center border">
              <span class="fs-4 fw-bold text-success font-monospace">${tea.price.toLocaleString('vi-VN')} đ</span>
              <small class="text-muted d-block">/${tea.priceType}</small>
              <span class="badge bg-success bg-opacity-25 text-success border border-success rounded-pill px-3 py-1 mt-2">
                <i class="bi bi-clock me-1"></i>Thời lượng: ${tea.duration}
              </span>
            </div>
          </div>
          <div class="col-md-7">
            <h5 class="fw-bold font-serif text-dark mb-2">${tea.name}</h5>
            <p class="text-muted" style="line-height: 1.7;">${tea.story}</p>
            
            <div class="mb-3">
              <strong class="text-success small fw-bold text-uppercase d-block mb-1">
                <i class="bi bi-flower1 me-1"></i>Nguyên liệu trà &amp; thảo mộc:
              </strong>
              <ul class="small text-secondary mb-0 ps-3" style="line-height: 1.6;">
                ${tea.ingredients.map(function (ing) { return '<li>' + ing + '</li>'; }).join('')}
              </ul>
            </div>

            <div class="mb-3">
              <strong class="text-warning small fw-bold text-uppercase d-block mb-1">
                <i class="bi bi-egg-fried me-1"></i>Bánh &amp; mứt đi kèm:
              </strong>
              <div class="small text-dark p-2 bg-light rounded-3 border">${tea.cakesIncluded}</div>
            </div>

            <div class="mb-3">
              <strong class="text-primary small fw-bold text-uppercase d-block mb-1">
                <i class="bi bi-journal-text me-1"></i>Nghi thức &amp; không gian:
              </strong>
              <div class="small text-muted">${tea.protocol}</div>
            </div>

            <div class="p-2 bg-light rounded-3 border small text-muted">
              <i class="bi bi-info-circle me-1 text-warning"></i><strong>Lịch phục vụ:</strong> ${tea.schedule}
            </div>
          </div>
        </div>
      `;
    }

    if (bookBtn) {
      bookBtn.onclick = function () {
        var modalEl = document.getElementById('teaDetailModal');
        if (modalEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(modalEl).hide();
        }
        window.openTeaBookingModal(tea.code, tea.name, tea.price, tea.priceType, tea.maxCapacity);
      };
    }

    var mEl = document.getElementById('teaDetailModal');
    if (mEl && window.bootstrap) {
      bootstrap.Modal.getOrCreateInstance(mEl).show();
    }
  };

  window.openTeaBookingModal = function (serviceCode, optName, optPrice, optPriceType, optMaxCapacity) {
    var tea = ROYAL_TEA_PACKAGES[serviceCode] || {
      code: serviceCode || 'TEA-HOMESTAY',
      name: optName || 'Thưởng Trà Cung Đình Tại Homestay',
      price: optPrice || 85000,
      priceType: optPriceType || 'người',
      maxCapacity: optMaxCapacity || 8
    };

    var codeEl = document.getElementById('teaServiceCode');
    var nameEl = document.getElementById('teaServiceName');
    var priceEl = document.getElementById('teaServiceUnitPrice');
    var priceTypeEl = document.getElementById('teaServicePriceType');
    var maxCapEl = document.getElementById('teaServiceMaxCapacity');
    var cardTitleEl = document.getElementById('teaModalCardTitle');
    var subTitleEl = document.getElementById('teaModalSelectedTitle');
    var priceBadgeEl = document.getElementById('teaModalSelectedPrice');
    var capBadgeEl = document.getElementById('teaModalSelectedCapacity');
    var dateInput = document.getElementById('teaBookingDate');

    if (codeEl) codeEl.value = tea.code;
    if (nameEl) nameEl.value = tea.name;
    if (priceEl) priceEl.value = tea.price;
    if (priceTypeEl) priceTypeEl.value = tea.priceType;
    if (maxCapEl) maxCapEl.value = tea.maxCapacity;
    if (cardTitleEl) cardTitleEl.textContent = tea.name;
    if (subTitleEl) subTitleEl.textContent = 'Gói: ' + tea.name;
    if (priceBadgeEl) priceBadgeEl.textContent = tea.price.toLocaleString('vi-VN') + ' đ / ' + tea.priceType;
    if (capBadgeEl) capBadgeEl.textContent = 'Sức chứa tối đa: ' + tea.maxCapacity + ' khách';

    if (dateInput && !dateInput.value) {
      var tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      dateInput.value = tomorrow.toISOString().split('T')[0];
    }

    window.calculateTeaBookingTotal();

    var mEl = document.getElementById('teaBookingModal');
    if (mEl && window.bootstrap) {
      bootstrap.Modal.getOrCreateInstance(mEl).show();
    }
  };

  window.calculateTeaBookingTotal = function () {
    var unitPrice = parseInt(document.getElementById('teaServiceUnitPrice')?.value || 85000);
    var priceType = document.getElementById('teaServicePriceType')?.value || 'người';
    var guests = parseInt(document.getElementById('teaGuestsCount')?.value || 2);
    var maxCap = parseInt(document.getElementById('teaServiceMaxCapacity')?.value || 8);

    if (guests > maxCap) {
      alert('Sức chứa tối đa của không gian trà này là ' + maxCap + ' khách. Hệ thống tự động đặt ' + maxCap + ' khách.');
      guests = maxCap;
      var gInput = document.getElementById('teaGuestsCount');
      if (gInput) gInput.value = maxCap;
    }

    var isPackage = priceType.includes('gói') || priceType.includes('trải nghiệm');
    var total = isPackage ? unitPrice : (unitPrice * guests);

    var totalDisplayEl = document.getElementById('teaBookingTotalDisplay');
    if (totalDisplayEl) {
      totalDisplayEl.textContent = total.toLocaleString('vi-VN') + ' đ';
    }
  };

  window.handleTeaBookingSubmit = function (e) {
    if (e && e.preventDefault) e.preventDefault();

    var code = document.getElementById('teaServiceCode')?.value || 'TEA-HOMESTAY';
    var name = document.getElementById('teaServiceName')?.value || 'Thưởng Trà Cung Đình Tại Homestay';
    var date = document.getElementById('teaBookingDate')?.value || '';
    var timeSlot = document.getElementById('teaTimeSlot')?.value || '15:30 - 16:30';
    var guests = parseInt(document.getElementById('teaGuestsCount')?.value || 2);
    var room = document.getElementById('teaRoomNumber')?.value || '';
    var customerName = document.getElementById('teaCustomerName')?.value || 'Khách Quý';
    var phone = document.getElementById('teaCustomerPhone')?.value || '';
    var email = document.getElementById('teaCustomerEmail')?.value || '';
    var notes = document.getElementById('teaNotes')?.value || '';
    var unitPrice = parseInt(document.getElementById('teaServiceUnitPrice')?.value || 85000);
    var priceType = document.getElementById('teaServicePriceType')?.value || 'người';
    var isPackage = priceType.includes('gói') || priceType.includes('trải nghiệm');
    var total = isPackage ? unitPrice : (unitPrice * guests);
    var bookingCode = 'TEA-' + Math.floor(100000 + Math.random() * 900000);

    var submitBtn = document.getElementById('teaSubmitBtn');
    if (submitBtn) {
      submitBtn.disabled = true;
      submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Đang ghi nhận...';
    }

    var payload = {
      bookingCode: bookingCode,
      serviceCode: code,
      serviceName: name,
      customerName: customerName,
      phone: phone,
      email: email,
      date: date,
      timeSlot: timeSlot,
      guests: guests,
      total: total,
      notes: (room ? '[Phòng ' + room + '] ' : '') + notes
    };

    fetch('/api/public/tea-booking', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload)
    })
      .then(function (res) { return res.json(); })
      .then(function (data) {
        var mEl = document.getElementById('teaBookingModal');
        if (mEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(mEl).hide();
        }
        var codeEl = document.getElementById('teaSuccessCode');
        if (codeEl) codeEl.textContent = data.bookingCode || bookingCode;
        var succEl = document.getElementById('teaSuccessModal');
        if (succEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(succEl).show();
        }
      })
      .catch(function () {
        var mEl = document.getElementById('teaBookingModal');
        if (mEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(mEl).hide();
        }
        var codeEl = document.getElementById('teaSuccessCode');
        if (codeEl) codeEl.textContent = bookingCode;
        var succEl = document.getElementById('teaSuccessModal');
        if (succEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(succEl).show();
        }
      })
      .finally(function () {
        if (submitBtn) {
          submitBtn.disabled = false;
          submitBtn.innerHTML = '<i class="bi bi-check-circle-fill me-1"></i>Xác Nhận Đặt Chỗ';
        }
      });
  };

  // =========================================================================
  // 8. LOGIC LOCAL PARTNERS (DIRECTORY & DETAIL MODAL)
  // =========================================================================

  window.renderPartnerDirectory = function () {
    var grid = document.getElementById('partnerDirectoryGrid');
    if (!grid) return;

    var grpBadgeMap = {
      'Nhà vườn và homestay': { bg: '#15803D', icon: 'bi-house-heart-fill' },
      'Quán ăn và ẩm thực': { bg: '#DC2626', icon: 'bi-egg-fried' },
      'Đặc sản và sản phẩm địa phương': { bg: '#EA580C', icon: 'bi-basket2-fill' },
      'Điểm trải nghiệm': { bg: '#D97706', icon: 'bi-compass-fill' }
    };

    grid.innerHTML = LOCAL_PARTNERS.map(function (p) {
      var badgeInfo = grpBadgeMap[p.partnerGroup] || { bg: '#15803D', icon: 'bi-shop' };
      var distText = p.distanceKm < 1 ? (p.distanceKm * 1000) + 'm' : p.distanceKm + ' km';

      return `
        <div class="col-xl-4 col-lg-6 col-md-6">
          <div class="card h-100 border rounded-4 shadow-sm bg-white overflow-hidden d-flex flex-column" style="transition: transform 0.2s ease, box-shadow 0.2s ease;">
            <div class="position-relative" style="height: 180px; overflow: hidden; cursor: pointer;" onclick="window.openPartnerDetailModal('${p.id}')">
              <img src="${p.imageUrl}" alt="${p.name}" class="w-100 h-100" style="object-fit: cover;" onerror="this.src='images/nha_ruong_1.jpg'" loading="lazy" />
              <span class="badge position-absolute top-0 start-0 m-3 rounded-pill px-2.5 py-1 shadow-sm text-white" style="background: ${badgeInfo.bg}; font-size: 11px;">
                <i class="bi ${badgeInfo.icon} me-1"></i>${p.category}
              </span>
              <span class="badge position-absolute bottom-0 end-0 m-3 rounded-pill px-2.5 py-1 shadow-sm bg-dark bg-opacity-75 text-warning font-monospace" style="font-size: 11px;">
                <i class="bi bi-bicycle me-1"></i>${distText} từ homestay
              </span>
            </div>
            <div class="p-3 d-flex flex-column flex-grow-1">
              <h5 class="fw-bold font-serif text-dark mb-1" style="font-size: 17px; cursor: pointer;" onclick="window.openPartnerDetailModal('${p.id}')">
                ${p.name}
              </h5>
              <div class="small text-muted mb-2 text-truncate" title="${p.address}">
                <i class="bi bi-geo-alt-fill text-danger me-1"></i>${p.address}
              </div>
              <p class="text-muted small mb-3 flex-grow-1" style="font-size: 13px; line-height: 1.6;">
                ${p.summary}
              </p>
              
              <div class="d-flex gap-1 flex-wrap mb-3">
                ${p.highlights.map(function (hl) {
                  return '<span class="badge bg-light text-secondary border rounded-pill" style="font-size: 10.5px;">' + hl + '</span>';
                }).join('')}
              </div>

              <div class="d-flex align-items-center justify-content-between pt-2 border-top mt-auto gap-2">
                <button type="button" class="btn btn-sm btn-outline-secondary rounded-pill px-3" onclick="window.openPartnerDetailModal('${p.id}')">
                  <i class="bi bi-info-circle me-1"></i>Chi tiết
                </button>
                <button type="button" class="btn btn-sm btn-outline-success rounded-pill px-3 fw-bold" onclick="window.filterByPartner('${p.id}')">
                  <i class="bi bi-basket2 me-1"></i>Xem sản phẩm
                </button>
                <a href="tel:${p.phone.replace(/[^0-9]/g, '')}" class="btn btn-sm btn-success rounded-pill px-2.5" title="Gọi đối tác ${p.phone}">
                  <i class="bi bi-telephone-fill"></i>
                </a>
              </div>
            </div>
          </div>
        </div>
      `;
    }).join('');
  };

  window.openPartnerDetailModal = function (partnerId) {
    var p = LOCAL_PARTNERS.find(function (item) { return item.id === partnerId; });
    if (!p) return;

    var nameEl = document.getElementById('partnerDetailModalName');
    var catEl = document.getElementById('partnerDetailModalCategory');
    var bodyEl = document.getElementById('partnerDetailModalBody');
    var callBtn = document.getElementById('partnerDetailModalCallBtn');
    var viewProdsBtn = document.getElementById('partnerDetailModalViewProdsBtn');

    if (nameEl) nameEl.textContent = p.name;
    if (catEl) catEl.textContent = p.category + ' • Cách Kim Long EcoStay ' + (p.distanceKm < 1 ? (p.distanceKm * 1000) + 'm' : p.distanceKm + ' km');

    // Lấy các sản phẩm được cung cấp bởi đối tác này
    var linkedProds = SPECIALTY_PRODUCTS.filter(function (sp) { return sp.partnerId === p.id; });

    if (bodyEl) {
      bodyEl.innerHTML = `
        <div class="row g-4 mb-3">
          <div class="col-md-5">
            <div class="rounded-4 overflow-hidden border shadow-sm" style="max-height: 240px;">
              <img src="${p.imageUrl}" alt="${p.name}" class="w-100 h-100" style="object-fit: cover;" onerror="this.src='images/nha_ruong_1.jpg'" />
            </div>
            <div class="mt-3 p-3 bg-light rounded-3 border small">
              <div><i class="bi bi-clock text-primary me-2"></i><strong>Giờ mở cửa:</strong> ${p.hours}</div>
              <div><i class="bi bi-telephone-fill text-success me-2"></i><strong>Hotline / Zalo:</strong> <a href="tel:${p.phone.replace(/[^0-9]/g, '')}" class="fw-bold text-success">${p.phone}</a></div>
              <div><i class="bi bi-geo-alt-fill text-danger me-2"></i><strong>Địa chỉ:</strong> ${p.address}</div>
            </div>
          </div>
          <div class="col-md-7">
            <div class="badge bg-success bg-opacity-25 text-success border border-success rounded-pill px-3 py-1 mb-2">
              <i class="bi bi-check-circle-fill me-1"></i>Hộ Liên Kết Xác Thực Kim Long EcoStay
            </div>
            <h5 class="fw-bold font-serif text-dark mb-2">${p.name}</h5>
            <p class="text-muted" style="line-height: 1.7;">${p.desc}</p>
            
            <div class="mb-3">
              <strong class="text-dark small fw-bold text-uppercase d-block mb-1">
                <i class="bi bi-stars text-warning me-1"></i>Trải nghiệm &amp; Điểm nổi bật:
              </strong>
              <div class="d-flex gap-1 flex-wrap">
                ${p.highlights.map(function (hl) {
                  return '<span class="badge bg-light text-success border rounded-pill px-2.5 py-1">' + hl + '</span>';
                }).join('')}
              </div>
            </div>

            <div class="p-2 bg-light rounded-3 border small">
              <i class="bi bi-bicycle text-success me-1"></i><strong>Chỉ đường từ homestay:</strong> Quý khách có thể mượn xe đạp trợ lực điện Eco-Bike tại quầy lễ tân để đạp sang trải nghiệm trong ${Math.round(p.distanceKm * 4)} phút.
            </div>
          </div>
        </div>

        <!-- Danh sách sản phẩm của đối tác -->
        <div class="border-top pt-3">
          <h6 class="fw-bold font-serif text-dark mb-2">
            <i class="bi bi-basket2-fill text-success me-1"></i>Sản phẩm &amp; Món ăn do đối tác cung cấp (${linkedProds.length}):
          </h6>
          ${linkedProds.length === 0 ? '<p class="text-muted small">Đối tác hiện tập trung vào dịch vụ tham quan và trải nghiệm văn hóa.</p>' : `
            <div class="row g-2">
              ${linkedProds.map(function (lp) {
                return `
                  <div class="col-md-6">
                    <div class="d-flex align-items-center gap-2 p-2 bg-light rounded-3 border" style="cursor: pointer;" onclick="var m = bootstrap.Modal.getInstance(document.getElementById('partnerDetailModal')); if (m) m.hide(); window.openProductDetailModal('${lp.id}');">
                      <img src="${lp.img}" alt="${lp.name}" style="width: 50px; height: 50px; object-fit: cover; border-radius: 8px;" onerror="this.src='images/mon_banhuot.jpg'" />
                      <div class="overflow-hidden flex-grow-1">
                        <div class="fw-bold text-dark text-truncate small">${lp.name}</div>
                        <span class="text-success font-monospace small fw-bold">${lp.price.toLocaleString('vi-VN')} đ</span>
                      </div>
                      <i class="bi bi-chevron-right text-muted small"></i>
                    </div>
                  </div>
                `;
              }).join('')}
            </div>
          `}
        </div>
      `;
    }

    if (callBtn) {
      callBtn.href = 'tel:' + p.phone.replace(/[^0-9]/g, '');
    }

    if (viewProdsBtn) {
      viewProdsBtn.onclick = function () {
        var modalEl = document.getElementById('partnerDetailModal');
        if (modalEl && window.bootstrap) {
          bootstrap.Modal.getOrCreateInstance(modalEl).hide();
        }
        window.filterByPartner(p.id);
      };
    }

    var mEl = document.getElementById('partnerDetailModal');
    if (mEl && window.bootstrap) {
      bootstrap.Modal.getOrCreateInstance(mEl).show();
    }
  };

  window.filterByPartner = function (partnerId) {
    var pSelect = document.getElementById('specialtyPartnerSelect');
    if (pSelect) {
      pSelect.value = partnerId;
    }
    window.scrollToSection('specialties', true);
    window.filterSpecialties();
  };

  // Tự động nạp danh sách đối tác vào Dropdown bộ lọc của Specialties
  function populatePartnerDropdown() {
    var pSelect = document.getElementById('specialtyPartnerSelect');
    if (pSelect) {
      var current = pSelect.value || 'ALL';
      var opts = '<option value="ALL">Tất cả đối tác cung cấp (10)</option>';
      LOCAL_PARTNERS.forEach(function (pt) {
        opts += '<option value="' + pt.id + '">' + pt.name + '</option>';
      });
      pSelect.innerHTML = opts;
      pSelect.value = current;
    }
  }

  // =========================================================================
  // 9. KHỞI TẠO TỔNG THỂ (AUTO-INITIALIZATION ON PAGE LOAD / BLANK STATE)
  // =========================================================================

  function initAllSections() {
    populatePartnerDropdown();
    window.filterSpecialties();
    window.renderPartnerDirectory();
    window.initHashNavigation();
  }

  if (document.readyState === 'complete' || document.readyState === 'interactive') {
    setTimeout(initAllSections, 150);
  } else {
    document.addEventListener('DOMContentLoaded', function () {
      setTimeout(initAllSections, 150);
    });
  }

  // Dự phòng khi chuyển trang Blazor hoặc Ajax
  window.initAllSections = initAllSections;

})(window, document);

