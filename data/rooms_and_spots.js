/**
 * DỮ LIỆU SẢN PHẨM MẪU (MVP) - KIM LONG ECOSTAY
 * Đề án môn học: Khởi nghiệp & Đổi mới sáng tạo (KN&DMST) - Đại học Huế
 */

const ECOSTAY_DATA = {
  projectInfo: {
    name: "Kim Long EcoStay",
    company: "Công ty Cổ phần Kim Long EcoStay (Kim Long EcoStay JSC)",
    slogan: "Trải nghiệm văn hóa • Gắn kết thiên nhiên",
    location: "Phường Kim Long, TP. Huế (ven bờ bắc sông Hương, cạnh Chùa Thiên Mụ)",
    hotline: "0905.888.666",
    email: "contact@kimlongecostay.vn",
    website: "www.kimlongecostay.vn",
    establishedYear: 2026,
    mvpScale: "02 Căn nhà vườn mẫu (05 Phòng ngủ sinh thái khép kín)",
    currentStage: "Giai đoạn Thử nghiệm Khả dụng Tối thiểu (MVP Stage)"
  },

  // 1. DANH SÁCH 5 PHÒNG NGỦ MẪU THUỘC 2 CĂN NHÀ VƯỜN
  rooms: [
    {
      id: "KLE-R101",
      gardenHouse: "Nhà Vườn Hương Sen (Căn mẫu số 01)",
      name: "Phòng Eco-Nest Ban Mai",
      category: "Eco-Nest (Gen Z & Sống Chậm)",
      price: 390000,
      originalPrice: 480000,
      area: "24m²",
      capacity: "1 - 2 Khách",
      view: "View trực diện cánh đồng lúa Kim Long & Đón bình minh",
      bedType: "01 Giường đôi Queen Size (Gỗ thông tự nhiên, đệm organic)",
      smartLockId: "LOCK-KL-101",
      defaultPin: "686899",
      features: [
        "Khóa điện tử thông minh IoT (Mở cửa bằng PIN/QR 10 giây)",
        "Ban công kính panorama ngắm đồng lúa & hít gió sông Hương",
        "Bộ vệ sinh sinh thái: Xà phòng bồ hòn & dầu gội thảo mộc Huế",
        "Miễn phí 01 xe đạp trợ lực điện thông minh kèm giá đỡ điện thoại",
        "Trà sen cung đình & nước khoáng thủy tinh miễn phí mỗi ngày",
        "Wi-Fi mesh tốc độ cao & Bàn làm việc chill cho dân Digital Nomad"
      ],
      description: "Được thiết kế tối giản mộc mạc dành riêng cho người trẻ và dân văn phòng tìm kiếm kỳ nghỉ 'chữa lành'. Sáng sớm kéo rèm đón từng vạt nắng rọi trên đồng lúa xanh ngát, thưởng thức ly trà sen thơm dịu và làm chủ kỳ nghỉ riêng tư tuyệt đối.",
      badge: "Phổ biến nhất",
      imageUrl: "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=800&q=80"
    },
    {
      id: "KLE-R102",
      gardenHouse: "Nhà Vườn Hương Sen (Căn mẫu số 01)",
      name: "Phòng Eco-Nest Hoàng Hôn",
      category: "Eco-Nest (Gen Z & Sống Chậm)",
      price: 420000,
      originalPrice: 500000,
      area: "26m²",
      capacity: "1 - 2 Khách",
      view: "View hướng hoàng hôn sông Hương & Rặng tre ngà",
      bedType: "01 Giường đôi Queen Size (Gỗ mộc, rèm thô tự nhiên)",
      smartLockId: "LOCK-KL-102",
      defaultPin: "123888",
      features: [
        "Khóa thông minh không chạm nhận phòng trong 10 giây",
        "Cửa sổ góc ngắm hoàng hôn ráng vàng trên sông Hương",
        "Phòng tắm mở thoáng mát với bồn sứ cao cấp & tinh dầu sả chanh",
        "Xe đạp trợ lực điện xanh dạo phố Chùa Thiên Mụ",
        "Hệ thống đèn ngủ ánh vàng tự động kích hoạt khi mở cửa"
      ],
      description: "Căn phòng sở hữu góc nhìn đắt giá nhất khi ráng chiều buông xuống dòng sông Hương và cánh đồng lúa Kim Long. Không gian tĩnh tại, thoang thoảng hương bưởi thanh trà nở rộ trong vườn.",
      badge: "View Hoàng Hôn",
      imageUrl: "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=800&q=80"
    },
    {
      id: "KLE-R201",
      gardenHouse: "Nhà Vườn Nguyệt Biều (Căn mẫu số 02)",
      name: "Garden Suite Cố Đô Thanh Trà",
      category: "Garden Suite (Du khách Quốc tế & Gia đình)",
      price: 680000,
      originalPrice: 850000,
      area: "42m²",
      capacity: "2 - 3 Khách (Phù hợp gia đình nhỏ hoặc cặp đôi)",
      view: "View toàn cảnh vườn cây ăn quả cổ thụ & Lối dạo lát gạch Bát Tràng",
      bedType: "01 Giường King Size + 01 Sofa Bed đọc sách",
      smartLockId: "LOCK-KL-201",
      defaultPin: "779922",
      features: [
        "Khóa thông minh mở cửa 1 chạm hoặc quét mã QR qua điện thoại",
        "Khu vực hiên trà thưởng nguyệt phong cách Cố đô Huế",
        "Bồn ngâm chân thảo mộc bằng gỗ pơ-mu thơm tự nhiên",
        "Miễn phí 02 xe đạp trợ lực điện cao cấp tích hợp GPS Audio Guide",
        "Tặng 01 phần bánh ướt thịt nướng than hoa Kim Long chào đón",
        "Tủ lạnh mini tích hợp nông sản sạch theo mùa từ vườn nhà"
      ],
      description: "Không gian mang đậm dấu ấn kiến trúc nhà rường Huế kết hợp tiện nghi đương đại. Nằm ẩn mình dưới tán bưởi thanh trà hơn 40 năm tuổi, mang lại trải nghiệm nghỉ dưỡng văn hóa sâu lắng và bình yên đích thực.",
      badge: "Sang trọng Cố đô",
      imageUrl: "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80"
    },
    {
      id: "KLE-R202",
      gardenHouse: "Nhà Vườn Nguyệt Biều (Căn mẫu số 02)",
      name: "Family Eco-Villa Gắn Kết",
      category: "Family Suite (Gia đình Trẻ)",
      price: 820000,
      originalPrice: 990000,
      area: "52m²",
      capacity: "3 - 4 Khách (Bố mẹ & 1-2 con nhỏ)",
      view: "View trực diện sân vườn cỏ tự nhiên an toàn cho trẻ nhỏ & Luống rau sạch",
      bedType: "02 Giường đôi cỡ lớn (Nệm sinh học, chất liệu kháng khuẩn)",
      smartLockId: "LOCK-KL-202",
      defaultPin: "998833",
      features: [
        "Hệ thống Smart Lock kiểm soát ra vào độc lập 24/7",
        "Khoảng sân riêng biệt rợp bóng cây cho trẻ em vui chơi an toàn",
        "Bộ kit trải nghiệm nông nghiệp cho bé: Học hái quả, tưới rau",
        "Miễn phí xe đạp gia đình (có ghế an toàn cho em bé)",
        "Bữa sáng đặc sản Huế giao tận phòng nóng hổi"
      ],
      description: "Lựa chọn hoàn hảo cho các gia đình trẻ cuối tuần muốn đưa con rời xa màn hình điện thoại, hòa mình vào thiên nhiên, biết hạt lúa nảy mầm và cây trái đơm hoa trong môi trường thuần khiết.",
      badge: "Dành cho Gia đình",
      imageUrl: "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=800&q=80"
    },
    {
      id: "KLE-R203",
      gardenHouse: "Nhà Vườn Nguyệt Biều (Căn mẫu số 02)",
      name: "Heritage Studio Trầm Mặc",
      category: "Heritage Studio (Trải nghiệm Văn hóa Độc bản)",
      price: 590000,
      originalPrice: 720000,
      area: "32m²",
      capacity: "2 Khách",
      view: "View hồ cá hoa súng & Rặng chuối ngự ven bờ",
      bedType: "01 Giường Queen Size thủ công mỹ nghệ Huế",
      smartLockId: "LOCK-KL-203",
      defaultPin: "369852",
      features: [
        "Khóa điện tử ngoài trời chống nước chuẩn IP66 cao cấp",
        "Bộ ấm trà Cung Đình và bộ cờ tướng thư giãn",
        "Kệ sách giới thiệu lịch sử các chúa Nguyễn và Cố đô Huế",
        "Trợ giá 50% xe máy điện thông minh đi khám phá Đại Nội Huế",
        "Dịch vụ gọi món nóng sốt giao trong 15 phút từ các lò gia truyền"
      ],
      description: "Căn phòng hòa quyện giữa nét cổ kính của di sản và sự mộc mạc của làng quê Kim Long. Nơi du khách có thể thả hồn vào từng trang sách hay ngồi nhâm nhi chén trà sen bên hồ súng thanh tịnh.",
      badge: "Độc bản Văn hóa",
      imageUrl: "https://images.unsplash.com/photo-1595526114035-0d45ed16cfbf?auto=format&fit=crop&w=800&q=80"
    }
  ],

  // 2. DANH SÁCH 8 ĐIỂM DI TÍCH & DANH THẮNG TRONG TOUR GPS AUDIO GUIDE
  heritageSpots: [
    {
      id: "SPOT-01",
      name: "Chùa Thiên Mụ (Linh Mụ Tự)",
      category: "Di tích Lịch sử - Tâm linh",
      distance: "850m từ Homestay (3 phút đạp xe)",
      audioDuration: "2 phút 45 giây",
      lat: 16.4533,
      lng: 107.5451,
      image: "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=800&q=80",
      intro: "Ngôi cổ tự đệ nhất xứ Đàng Trong gắn liền với huyền tích bà tiên áo đỏ thời chúa Nguyễn Hoàng năm 1601.",
      narrations: {
        vi: "Chào mừng bạn đến với Chùa Thiên Mụ, biểu tượng tâm linh bất diệt của Cố đô Huế soi bóng bên dòng sông Hương thơ mộng. Tương truyền vào năm 1601, khi Tiên chúa Nguyễn Hoàng vào trấn thủ xứ Thuận Hóa, ngài đích thân đi xem xét địa thế. Khi đến ngọn đồi nhỏ hình dáng như con rồng quay đầu nhìn lại, người dân địa phương kể rằng ban đêm thường có một bà lão áo đỏ xuất hiện nói rằng: 'Rồi đây sẽ có bậc chân chúa đến dựng chùa để tụ khí thiêng, gom long mạch cho muôn đời'. Chúa Nguyễn Hoàng mừng rỡ liền cho khởi dựng ngôi chùa, đặt tên là Thiên Mụ Tự. Tháp Phước Duyên hình bát giác 7 tầng cao 21 mét được xây dựng dưới thời vua Thiệu Trị, mỗi tầng thờ một đức Phật, cùng tiếng chuông chùa ngân vang sớm chiều đã trở thành linh hồn sâu thẳm của xứ Huế.",
        en: "Welcome to Thien Mu Pagoda, the timeless spiritual symbol of Hue perched gracefully on Ha Khe hill overlooking the Perfume River. Founded in 1601 by the first Nguyen Lord, Nguyen Hoang, following the legend of a celestial lady in red prophecy. Its iconic 7-storey octagonal Phuoc Duyen tower rises 21 meters into the sky, representing each rebirth of Buddha.",
        fr: "Bienvenue à la pagode Thien Mu, le symbole spirituel intemporel de Hué bordant la rivière des Parfums. Fondée en 1601 par le seigneur Nguyen Hoang selon la prophétie d'une dame céleste vêtue de rouge. Sa majestueuse tour octogonale Phuoc Duyen de sept étages s'élève à 21 mètres au-dessus des collines verdoyantes."
      }
    },
    {
      id: "SPOT-02",
      name: "Nhà Vườn An Hiên",
      category: "Kiến trúc Nhà vườn Huế",
      distance: "450m từ Homestay (2 phút đi bộ)",
      audioDuration: "2 phút 15 giây",
      lat: 16.4552,
      lng: 107.5495,
      image: "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=800&q=80",
      intro: "Tuyệt tác nhà vườn xứ Huế với cổng vòm cổ kính, hàng cây mận quân rợp bóng và ngôi nhà rường 3 gian 2 chái trác tuyệt.",
      narrations: {
        vi: "Bạn đang đứng trước cổng Nhà vườn An Hiên, một trong những ngôi nhà vườn tiêu biểu và nguyên vẹn nhất của mảnh đất Kim Long. Được xây dựng từ cuối thế kỷ 19, ban đầu là phủ đệ của công chúa con vua Dục Đức. Bước qua cổng tam quan rêu phong, du khách bước đi dưới vòm cây mận quân và mơ màng trong làn hương ngào ngạt của hoa quỳnh, hoa sứ. Ở trung tâm là ngôi nhà rường ba gian hai chái dựng hoàn toàn bằng gỗ mít và gỗ lim, trạm trổ hoa văn tinh xảo không dùng một chiếc đinh sắt nào. An Hiên không chỉ là một chốn ở, mà là triết lý sống hòa hợp trọn vẹn giữa con người, vũ trụ và thiên nhiên thảo mộc xứ Huế.",
        en: "You are standing before An Hien Garden House, the quintessence of Hue's traditional garden house architecture in Kim Long village. Dating back to the late 19th century, it features an ancient curved stone gate, an enchanting apricot walkway, and a magnificent 3-bay wooden ruong house built entirely without iron nails.",
        fr: "Vous voici devant la maison-jardin An Hien, chef-d'œuvre de l'architecture traditionnelle de Hué à Kim Long. Érigée à la fin du 19ème siècle, elle charme par sa porte d'entrée séculaire, ses allées ombragées et sa maison rường traditionnelle en bois sculpté."
      }
    },
    {
      id: "SPOT-03",
      name: "Văn Miếu Huế (Văn Thánh)",
      category: "Di tích Cố đô Triều Nguyễn",
      distance: "1.4km từ Homestay (5 phút đạp xe)",
      audioDuration: "2 phút 30 giây",
      lat: 16.4518,
      lng: 107.5388,
      image: "https://images.unsplash.com/photo-1548013146-72479768bada?auto=format&fit=crop&w=800&q=80",
      intro: "Nơi tôn vinh đạo học triều Nguyễn với 32 tấm bia tiến sĩ khắc tên các bậc hiền tài trên lưng rùa đá hướng ra sông Hương.",
      narrations: {
        vi: "Văn Miếu Huế, hay còn gọi là Văn Thánh, được vua Gia Long cho khởi công xây dựng vào năm 1808 trên đồi cao sát bờ sông Hương thuộc địa phận Kim Long. Đây là thánh đường Nho học lớn nhất của vương triều nhà Nguyễn, nơi thờ đức Khổng Tử và lưu danh các vị đại khoa hiền triết. Điểm đặc sắc nhất nơi đây là hai dãy nhà bia gồm 32 tấm bia tiến sĩ bằng đá thanh, khắc tên 293 vị tiến sĩ từ khoa thi Hương đầu tiên đến các khoa thi Đình thời Nguyễn, ghi dấu truyền thống hiếu học ngàn năm của dân tộc.",
        en: "Hue Temple of Literature, or Van Mieu, built in 1808 by Emperor Gia Long, stands solemnly by the Perfume River in Kim Long. It houses 32 monumental stone stelae mounted on tortoise backs, honoring 293 doctoral scholars of the Nguyen Dynasty.",
        fr: "Le Temple de la Littérature de Hué, édifié en 1808 sous le règne de Gia Long, rend hommage aux grands érudits de la dynastie des Nguyen avec ses 32 stèles de docteurs portées par des tortues sacrées en pierre."
      }
    },
    {
      id: "SPOT-04",
      name: "Bến Thuyền Kim Long & Bờ Bắc Sông Hương",
      category: "Cảnh quan & Lối sống Bản địa",
      distance: "300m từ Homestay (1 phút đi bộ)",
      audioDuration: "1 phút 50 giây",
      lat: 16.4548,
      lng: 107.5521,
      image: "https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80",
      intro: "Khúc sông êm ả nơi các chúa Nguyễn từng chọn đặt thủ phủ Đàng Trong, điểm ngắm hoàng hôn buông đẹp nhất xứ Cố đô.",
      narrations: {
        vi: "Bến thuyền Kim Long là nơi lưu giữ ký ức về thời kỳ hoàng kim của Đàng Trong. Vào thế kỷ 17, chúa Nguyễn Phúc Lan đã dời thủ phủ về Kim Long, biến nơi này thành một đô thị phồn thịnh với tàu thuyền buôn bán sầm uất. Ngày nay, bến thuyền trở lại vẻ bình yên vốn có, là nơi du khách có thể thả bước chân trần ngắm ráng chiều vàng óng ả dát lên mặt nước sông Hương và nghe tiếng chèo khua nhè nhẹ của ngư dân.",
        en: "Kim Long Wharf holds historical memories of the 17th-century capital of the Nguyen Lords. Today, it offers a serene escape where you can witness breathtaking sunsets reflecting golden hues across the Perfume River.",
        fr: "L'embarcadère de Kim Long garde la mémoire de l'ancienne capitale des seigneurs Nguyen au XVIIe siècle, offrant aujourd'hui un coucher de soleil spectaculaire sur les eaux calmes de la rivière des Parfums."
      }
    },
    {
      id: "SPOT-05",
      name: "Lò Bánh Ướt Thịt Nướng Gia Truyền Huyền Anh",
      category: "Di sản Ẩm thực Kim Long",
      distance: "250m từ Homestay (Liên kết giao 15 phút)",
      audioDuration: "2 phút 00 giây",
      lat: 16.4560,
      lng: 107.5510,
      image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
      intro: "Lò bánh nổi tiếng bậc nhất Kim Long với món thịt heo nướng than hoa thơm lừng và nước chấm ruốc tương ớt gia truyền.",
      narrations: {
        vi: "Kim Long từ xa xưa đã nức tiếng trong câu ca dao: 'Kim Long có gái mỹ miều / Có con sông nhỏ sớm chiều đón đưa'. Nhưng điều làm níu chân du khách muôn phương chính là món bánh ướt thịt nướng gia truyền. Lát bánh tráng mỏng mềm mướt như lụa tráng từ gạo bột mới, cuộn cùng lát thịt ba chỉ ướp sả mè nướng trên bếp than hoa đỏ rực, ăn kèm rau thơm tươi hái tại vườn và chấm chén tương đậu mè bùi ngậy gia truyền đậm đà.",
        en: "Famous for the historic folk saying praising Kim Long's beauty and food, this local bakery crafts Hue's most iconic grilled pork wrapped in fresh steamed rice paper sheets, dipped in savory sesame peanut sauce.",
        fr: "Célèbre adresse gastronomique de Kim Long confectionnant de délicates galettes de riz à la vapeur enroulant du porc grillé à la braise parfumée à la citronnelle, servies avec une sauce onctueuse maison."
      }
    },
    {
      id: "SPOT-06",
      name: "Cánh Đồng Lúa Sinh Thái Ven Sông Hương",
      category: "Cảnh quan Nông nghiệp Kim Long",
      distance: "Ngay trước thềm phòng ngủ Homestay",
      audioDuration: "1 phút 40 giây",
      lat: 16.4540,
      lng: 107.5480,
      image: "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=800&q=80",
      intro: "Không gian xanh nông nghiệp trù phú được Kim Long EcoStay cam kết bảo tồn nguyên vẹn trước làn sóng đô thị hóa.",
      narrations: {
        vi: "Bạn đang nhìn ngắm cánh đồng lúa xanh ngút ngàn của phường Kim Long, lá phổi xanh quý báu của thành phố Huế. Mỗi bông lúa nơi đây được nuôi dưỡng bằng phù sa màu mỡ sông Hương và giọt mồ hôi tảo tần của bà con nông dân. Kim Long EcoStay áp dụng mô hình thuê khoán cố định và bao tiêu nông sản để cùng bà con giữ lấy những mảng xanh thuần nông này, tạo nên không gian chữa lành tuyệt vời cho du khách.",
        en: "The lush green rice paddy right outside your bedroom window is nurtured by the fertile silt of the Perfume River. Kim Long EcoStay actively partners with local farmers to preserve this agrarian heritage.",
        fr: "La rizière verdoyante s'étendant devant votre chambre est nourrie par les alluvions fertiles du fleuve. Kim Long EcoStay s'engage aux côtés des agriculteurs locaux pour protéger ce patrimoine agraire vivant."
      }
    },
    {
      id: "SPOT-07",
      name: "Vườn Bưởi Thanh Trà Làng Cổ",
      category: "Nông nghiệp & Sản vật Tiến Vua",
      distance: "600m từ Homestay",
      audioDuration: "2 phút 10 giây",
      lat: 16.4525,
      lng: 107.5435,
      image: "https://images.unsplash.com/photo-1596435707700-67600ab6640b?auto=format&fit=crop&w=800&q=80",
      intro: "Giống bưởi đặc sản tiến vua thời nhà Nguyễn với tép bưởi mọng nước, hương thơm thanh khiết như nắng thu Cố đô.",
      narrations: {
        vi: "Thanh trà là đệ nhất danh quả của xứ Huế từng được tuyển chọn tiến dâng lên các vị hoàng đế triều Nguyễn. Khác với các giống bưởi thông thường, bưởi thanh trà Kim Long có quả nhỏ thon, vỏ mỏng vàng ươm, tép bưởi óng ánh như hạt ngọc, cắn vào vị ngọt thanh mát tan dần đầu lưỡi. Du khách lưu trú tại homestay có thể tự tay tham gia thu hoạch quả cùng nông dân vào vụ thu hái tháng 8, tháng 9 hằng năm.",
        en: "Thanh Tra pomelo was the royal fruit served exclusively to Nguyen Dynasty emperors. Smaller and exceptionally fragrant, its translucent golden segments burst with refreshing sweetness upon the first taste.",
        fr: "Le pamplemousse Thanh Tra était le fruit royal par excellence offert aux empereurs de Hué. Sa chair translucide et sucrée dégage un arôme subtil et raffiné, emblème des vergers traditionnels de Kim Long."
      }
    },
    {
      id: "SPOT-08",
      name: "Chùa Huyền Không Sơn Thượng",
      category: "Thiền viện Rừng thông",
      distance: "6.5km từ Homestay (Có tuyến xe máy điện trợ giá)",
      audioDuration: "2 phút 20 giây",
      lat: 16.4680,
      lng: 107.5120,
      image: "https://images.unsplash.com/photo-1544816155-12df9643f363?auto=format&fit=crop&w=800&q=80",
      intro: "Ngôi thiền viện Nam tông ẩn mình giữa đại ngàn rừng thông tĩnh mịch, chốn thanh tịnh cho tâm hồn được gột rửa muộn phiền.",
      narrations: {
        vi: "Nằm nép mình giữa thung lũng rừng thông bạt ngàn của thôn Chầm, Chùa Huyền Không Sơn Thượng là chốn ẩn dật thoát tục hiếm có. Không gian thiền môn nơi đây hòa quyện giữa tiếng thông reo vi vu, hồ nước hoa súng nở tím ngắt và những câu thơ thư pháp thâm trầm khắc trên vách đá. Một hành trình bằng xe máy điện từ Kim Long EcoStay lên đây sẽ đưa du khách trở về với sự tĩnh tại tuyệt đối của tâm hồn.",
        en: "Tucked deep inside the pine forests of Cham valley, Huyen Khong Son Thuong Zen Monastery is an ethereal sanctuary where whispering pine breezes, lotus ponds, and Zen calligraphy restore spiritual peace.",
        fr: "Niché au cœur d'une forêt de pins paisible, le monastère Zen Huyen Khong Son Thuong est un havre de paix hors du temps où la nature et la spiritualité s'unissent en parfaite harmonie."
      }
    },
    {
      id: "SPOT-09",
      name: "Làng Hương Thủy Xuân (Làng Nghề Truyền Thống)",
      category: "Làng Nghề Truyền Thống",
      distance: "4.2km từ Homestay (12 phút đạp xe)",
      audioDuration: "2 phút 15 giây",
      lat: 16.4358,
      lng: 107.5681,
      image: "https://images.unsplash.com/photo-1548013146-72479768bada?auto=format&fit=crop&w=800&q=80",
      intro: "Làng nghề làm hương trầm và nón lá hơn 700 năm tuổi với những đóa chông hương rực rỡ sắc màu xòe như hoa.",
      narrations: {
        vi: "Chào mừng bạn đến với Làng hương Thủy Xuân, làng nghề thủ công nổi tiếng bậc nhất Cố đô Huế dưới chân đồi Vọng Cảnh. Nơi đây từng chuyên cung cấp hương trầm thơm ngát cho triều đình và phủ chúa. Du khách có thể tự tay se nén hương trầm thơm và chiêm ngưỡng nghệ thuật chằm nón bài thơ.",
        en: "Welcome to Thuy Xuan Incense Village, a 700-year-old craft village famous for vibrant displays of aromatic incense sticks and traditional conical hats.",
        fr: "Bienvenue au village d'encens de Thuy Xuan, réputé pour ses bouquets d'encens multicolores parfumés et son artisanat séculaire de chapeaux coniques."
      }
    },
    {
      id: "SPOT-10",
      name: "Đại Nội Huế (Hoàng Thành & Tử Cấm Thành)",
      category: "Quần Thể Di Tích Cố Đô",
      distance: "2.8km từ Homestay (8 phút đạp xe)",
      audioDuration: "3 phút 10 giây",
      lat: 16.4697,
      lng: 107.5779,
      image: "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=800&q=80",
      intro: "Trung tâm quyền lực tối cao của 13 vị hoàng đế triều Nguyễn (1802 - 1945), Di sản Văn hóa Thế giới UNESCO.",
      narrations: {
        vi: "Đại Nội Huế là biểu tượng lịch sử huy hoàng của triều đại phong kiến cuối cùng Việt Nam. Được bao bọc bởi Kinh thành đồ sộ, Hoàng Thành và Tử Cấm Thành sở hữu hơn 100 công trình cung đình tráng lệ như Cửa Ngọ Môn, Điện Thái Hòa, Thế Miếu và Nhà hát Duyệt Thị Đường.",
        en: "The Imperial City of Hue, a UNESCO World Heritage site, served as the majestic political center of the Nguyen Dynasty with over a hundred royal palaces and shrines.",
        fr: "La Cité Impériale de Hué, classée au patrimoine mondial de l'UNESCO, était le cœur politique de la dernière dynastie impériale du Vietnam."
      }
    },
    {
      id: "SPOT-11",
      name: "Làng Tranh Dân Gian Làng Sình",
      category: "Làng Nghề Truyền Thống",
      distance: "7.5km từ Homestay (20 phút du thuyền/xe)",
      audioDuration: "2 phút 20 giây",
      lat: 16.5036,
      lng: 107.5992,
      image: "https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80",
      intro: "Dòng tranh mộc bản cổ truyền 400 năm in trên giấy điệp óng ánh phục vụ tín ngưỡng dân gian và hội vật đầu xuân.",
      narrations: {
        vi: "Tranh dân gian Làng Sình là một trong ba dòng tranh mộc bản cổ kính nhất Việt Nam. Nghệ nhân in tranh trên giấy mộc quét nước điệp vỏ sò lấp lánh rồi tô màu tự nhiên từ lá cây, vỏ ốc, mang đậm triết lý cầu an và đời sống tâm linh xứ Huế.",
        en: "Sinh Village woodblock painting is a 400-year-old folk art printed on shimmering seashell paper, reflecting the spiritual and cultural heartbeat of ancient Hue.",
        fr: "Les estampes populaires du village de Sinh perpétuent un art graphique de plus de 400 ans imprimé sur du papier nacre naturel."
      }
    },
    {
      id: "SPOT-12",
      name: "Làng Hoa Giấy Thanh Tiên",
      category: "Làng Nghề Truyền Thống",
      distance: "7.8km từ Homestay (22 phút đi xe)",
      audioDuration: "2 phút 05 giây",
      lat: 16.5019,
      lng: 107.6033,
      image: "https://images.unsplash.com/photo-1500382017468-9049fed747ef?auto=format&fit=crop&w=800&q=80",
      intro: "Nghệ thuật làm hoa sen và hoa cúng bằng giấy thủ công hơn 300 năm tuổi với phẩm màu thảo mộc tự nhiên.",
      narrations: {
        vi: "Làng hoa giấy Thanh Tiên nức tiếng với những bông sen giấy thanh nhã được tạo tác hoàn toàn thủ công. Từ nan tre, giấy mộc và phẩm màu vỏ cây tự nhiên, bàn tay khéo léo của các nghệ nhân đã thổi hồn vào những cánh hoa sen vĩnh cửu không bao giờ tàn phai.",
        en: "Thanh Tien paper flower village has preserved the delicate art of crafting handmade paper lotus blossoms for royal shrines and ancestors for over three centuries.",
        fr: "Le village de Thanh Tiên perpétue depuis 300 ans la fabrication minutieuse de fleurs de lotus en papier teintées avec des pigments végétaux naturels."
      }
    },
    {
      id: "SPOT-13",
      name: "Làng Đúc Đồng Phường Đúc",
      category: "Làng Nghề Truyền Thống",
      distance: "2.5km từ Homestay (7 phút đạp xe)",
      audioDuration: "2 phút 10 giây",
      lat: 16.4522,
      lng: 107.5667,
      image: "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=800&q=80",
      intro: "Nơi các nghệ nhân tạo tác Cửu Đỉnh, Đại Hồng Chung chùa Thiên Mụ và 9 khẩu Thần Công uy nghiêm thời Nguyễn.",
      narrations: {
        vi: "Phường Đúc là cái nôi của nghề đúc đồng tinh xảo miền Trung khởi nghiệp từ thế kỷ 17. Các nghệ nhân nơi đây đã đúc nên những bảo vật quốc gia như Cửu Đỉnh tại Đại Nội và quả đại hồng chung 2 tấn ngân vang tại chùa Thiên Mụ.",
        en: "Phuong Duc Bronze Casting Village dates back to the 17th century, celebrated for casting royal treasures including the Nine Dynastic Urns and Thien Mu's giant bell.",
        fr: "Le village de fonderie de bronze de Phuong Duc a façonné depuis le XVIIe siècle les plus beaux trésors nationaux dont les Neuf Urnes Dynastiques."
      }
    },
    {
      id: "SPOT-FOOD-01",
      name: "Bún Bò Huế Mụ Rơi (Nguyễn Du)",
      category: "Ẩm Thực Xứ Huế",
      distance: "4.8km từ Homestay (14 phút đi xe)",
      audioDuration: "2 phút 05 giây",
      lat: 16.4765,
      lng: 107.5878,
      image: "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?auto=format&fit=crop&w=800&q=80",
      intro: "Quán bún bò Huế lâu năm nức tiếng với nước dùng hầm xương ngọt thanh, thơm ngát mùi sả và ruốc Cố đô.",
      narrations: {
        vi: "Bún bò Mụ Rơi trên đường Nguyễn Du là một trong những quán bún bò chuẩn vị Huế xưa được người dân địa phương vô cùng yêu thích. Nồi nước dùng trong veo nhưng đậm đà, vị cay the nhè nhẹ hòa quyện cùng mùi sả và ruốc Huế đặc trưng. Từng tô bún đầy đặn với thịt bắp bò hoa giòn sần sật, chả cua quết tay vàng ươm và tiết mềm mọng nước.",
        en: "Mu Roi's spicy beef noodle soup is celebrated for its deeply flavorful broth infused with lemongrass, shrimp paste, and tender beef shank.",
        fr: "Le Bun Bo de Mu Roi est une institution culinaire à Hué, renommé pour son bouillon parfumé à la citronnelle et ses boulettes de crabe faites maison."
      }
    },
    {
      id: "SPOT-FOOD-02",
      name: "Bánh Bèo - Nậm - Lọc Bà Đỏ",
      category: "Ẩm Thực Xứ Huế",
      distance: "4.2km từ Homestay (12 phút đi xe)",
      audioDuration: "2 phút 10 giây",
      lat: 16.4712,
      lng: 107.5895,
      image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
      intro: "Thiên đường các loại bánh truyền thống Huế với bánh bèo chén tôm cháy giòn rụm, bánh lọc gói lá chuối trong veo.",
      narrations: {
        vi: "Quán Bà Đỏ trên đường Nguyễn Bỉnh Khiêm là điểm dừng chân ẩm thực không thể bỏ lỡ của tín đồ mê bánh Huế. Những chén bánh bèo nhỏ nhắn trắng ngần rắc tôm chấy đỏ cam và tóp mỡ giòn rụm, bánh nậm mềm mịn thơm mùi lá chuối và bánh lọc bọc tôm sông giòn ngọt ăn cùng nước mắm ớt cay xé lưỡi.",
        en: "Ba Do is the benchmark for traditional Hue rice cakes: steamed savory rice cakes topped with crushed shrimp, tapioca dumplings with river prawns, and flat rice cakes wrapped in banana leaves.",
        fr: "Ba Do est l'adresse incontournable pour déguster les délicieux gâteaux traditionnels cuits à la vapeur de Hué arrosés de sauce nuoc-mâm pimentée."
      }
    },
    {
      id: "SPOT-FOOD-03",
      name: "Cơm Hến & Bún Hến Hoa Đông (Cồn Hến)",
      category: "Ẩm Thực Xứ Huế",
      distance: "5.8km từ Homestay (16 phút đi xe)",
      audioDuration: "2 phút 15 giây",
      lat: 16.4820,
      lng: 107.6045,
      image: "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?auto=format&fit=crop&w=800&q=80",
      intro: "Quán cơm hến lâu đời bậc nhất giữa ốc đảo Cồn Hến - cái nôi khai sinh món hến trứ danh xứ Huế.",
      narrations: {
        vi: "Hoa Đông nằm ngay trên cồn cát nhỏ giữa dòng sông Hương thơ mộng, nơi khai sinh ra món cơm hến - món ăn mộc mạc mà chứa đựng đủ đầy âm hưởng triết lý ẩm thực Huế: đủ chua cay mặn ngọt bùi chát, ăn kèm nước luộc hến nóng hổi bốc khói cay nồng.",
        en: "Hoa Dong on Con Hen island is the historic birthplace of baby basket clam rice, a colorful culinary masterpiece balancing fiery chilies, fresh herbs, crispy pork cracklings, and hot clam broth.",
        fr: "Situé sur l'île paisible de Cồn Hến, Hoa Đông est le berceau du Com Hen, plat emblématique composé de riz aux palourdes et herbes aromatiques."
      }
    },
    {
      id: "SPOT-FOOD-04",
      name: "Chè Hẻm Cố Đô (Bột Lọc Heo Quay)",
      category: "Ẩm Thực Xứ Huế",
      distance: "4.5km từ Homestay (13 phút đi xe)",
      audioDuration: "2 phút 00 giây",
      lat: 16.4635,
      lng: 107.5912,
      image: "https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=800&q=80",
      intro: "Ngôi quán chè nép mình trong con hẻm số 1 Hùng Vương với món chè bột lọc heo quay độc nhất vô nhị.",
      narrations: {
        vi: "Quán Chè Hẻm Hùng Vương là một huyền thoại của ẩm thực đường phố Huế hơn nửa thế kỷ qua. Món độc đáo nhất là chè bột lọc bọc thịt heo quay: viên bột lọc trong veo dẻo dai bọc lấy miếng heo quay giòn bì mằn mặn, nấu cùng nước đường phèn gừng ấm nồng tạo nên sự bùng nổ hương vị khó quên.",
        en: "Che Hem Hung Vuong is renowned for Hue's most intriguing dessert: tapioca sweet dumplings stuffed with crispy roast pork swimming in ginger syrup.",
        fr: "Une petite ruelle abrite ce salon de desserts réputé pour sa soupe sucrée unique aux perles de tapioca farcies au porc rôti caramélisé."
      }
    },
    {
      id: "SPOT-FOOD-05",
      name: "Cà Phê Muối Xứ Huế (Nguyễn Lương Bằng)",
      category: "Ẩm Thực Xứ Huế",
      distance: "3.8km từ Homestay (10 phút đi xe)",
      audioDuration: "1 phút 50 giây",
      lat: 16.4590,
      lng: 107.5840,
      image: "https://images.unsplash.com/photo-1509042239860-f550ce710b93?auto=format&fit=crop&w=800&q=80",
      intro: "Cái nôi sáng tạo nên thức uống Cà phê muối trứ danh lan tỏa khắp Việt Nam trong khuôn viên sân vườn yên bình.",
      narrations: {
        vi: "Số 10 Nguyễn Lương Bằng chính là địa chỉ đầu tiên khai sinh ra món Cà phê muối trứ danh của xứ Huế vào năm 2010. Vị đắng nồng của cà phê phin truyền thống hòa quyện cùng lớp kem béo mặn mòi của muối tinh và sữa đặc thơm lừng, tạo nên thức uống gây thương nhớ sâu sắc cho du khách.",
        en: "The original birthplace of Hue's famous Salted Cream Coffee at 10 Nguyen Luong Bang, balancing traditional robusta drip coffee with velvet salty cream.",
        fr: "Le berceau historique du café salé de Hué où la puissance du café filtre se marie à la douceur onctueuse d'une crème salée délicate."
      }
    },
    {
      id: "SPOT-14",
      name: "Đồi Vọng Cảnh (Ngắm Hoàng Hôn Sông Hương)",
      category: "Quần Thể Di Tích Cố Đô",
      distance: "4.6km từ Homestay (14 phút đi xe)",
      audioDuration: "2 phút 25 giây",
      lat: 16.4258,
      lng: 107.5620,
      image: "https://images.unsplash.com/photo-1506744038136-46273834b3fb?auto=format&fit=crop&w=800&q=80",
      intro: "Điểm ngắm hoàng hôn triệu đô ôm trọn khúc cua đẹp nhất của dòng sông Hương và rặng thông xanh ngút ngàn.",
      narrations: {
        vi: "Đồi Vọng Cảnh từng là nơi các vua triều Nguyễn thường ngự cảnh nghỉ chân để thưởng ngoạn vẻ đẹp sông Hương và lăng tẩm hoàng tộc. Đứng từ đỉnh đồi nhìn xuống, sông Hương như dải lụa mềm uốn lượn qua các vườn bưởi Thanh Trà Thủy Biều, rặng thông reo rì rào trong gió chiều ráng đỏ.",
        en: "Vong Canh Hill offers the most breathtaking panoramic view of the Perfume River curving gracefully beneath whispering pine forests.",
        fr: "La colline de Vong Canh offre un panorama spectaculaire sur les méandres de la rivière des Parfums bordée de forêts de pins séculaires."
      }
    },
    {
      id: "SPOT-15",
      name: "Cầu Trường Tiền (Biểu Tượng 127 Năm Sông Hương)",
      category: "Quần Thể Di Tích Cố Đô",
      distance: "3.9km từ Homestay (12 phút đi xe)",
      audioDuration: "2 phút 15 giây",
      lat: 16.4682,
      lng: 107.5878,
      image: "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=800&q=80",
      intro: "Cây cầu thép huyền thoại sáu vài mười hai nhịp bắc qua sông Hương — chứng nhân lịch sử hơn một thế kỷ của Cố đô.",
      narrations: {
        vi: "Cầu Trường Tiền được khánh thành năm 1899 dưới thời vua Thành Thái do hãng Eiffel thiết kế. Cây cầu đã đi vào thi ca nhạc họa như một biểu tượng bất tử của xứ Huế mộng mơ, lung linh rực rỡ ánh đèn đổi màu mỗi khi đêm về.",
        en: "Truong Tien Bridge, designed by Eiffel in 1899, is the beloved historical landmark spanning the Perfume River.",
        fr: "Le pont Truong Tien, chef-d'œuvre métallique conçu par les ateliers Eiffel en 1899, est le symbole poétique de Hué."
      }
    },
    {
      id: "SPOT-16",
      name: "Chợ Đông Ba (Khu Chợ Truyền Thống 125 Năm)",
      category: "Quần Thể Di Tích Cố Đô",
      distance: "4.1km từ Homestay (13 phút đi xe)",
      audioDuration: "2 phút 20 giây",
      lat: 16.4718,
      lng: 107.5872,
      image: "https://images.unsplash.com/photo-1548013146-72479768bada?auto=format&fit=crop&w=800&q=80",
      intro: "Khu chợ sầm uất bậc nhất Cố đô — thiên đường mua sắm đặc sản mắm Huế, nón bài thơ và ẩm thực dân dã.",
      narrations: {
        vi: "Chợ Đông Ba là nơi tụ hội tinh hoa ẩm thực đường phố, các quầy vải may áo dài, nón lá bài thơ, mè xửng, tôm chua và nhịp sống giao thương buôn bán chân chất của người Huế.",
        en: "Dong Ba Market is the vibrant beating heart of Hue commerce and street food culture for more than a century.",
        fr: "Le marché Dong Ba est le cœur battant du commerce traditionnel et de la gastronomie populaire de Hué."
      }
    },
    {
      id: "SPOT-FOOD-06",
      name: "Bánh Ép Huế Chị Huệ (Đặc Sản Đường Phố Nóng Hổi)",
      category: "Ẩm Thực Xứ Huế",
      distance: "4.3km từ Homestay (13 phút đi xe)",
      audioDuration: "1 phút 50 giây",
      lat: 16.4410,
      lng: 107.5670,
      image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=800&q=80",
      intro: "Món ăn vặt số 1 của giới trẻ Cố đô với miếng bánh ép dẻo dai nóng hổi kẹp pate, trứng và thịt băm.",
      narrations: {
        vi: "Bột lọc tươi được ép giòn tan giữa hai tấm gang đúc đỏ rực than hoa, kẹp trứng, pate thơm nức, cuốn cùng rau răm, dưa leo và chấm chén nước mắm ớt tỏi sền sệt bùng nổ vị giác.",
        en: "Banh Ep is Hue's ultimate crispy street snack: freshly pressed tapioca batter with minced pork, egg, and pate served with spicy sweet fish sauce.",
        fr: "Banh Ep est la galette de tapioca croustillante préférée des jeunes de Hué, garnie de porc haché, d'œuf et de pâté."
      }
    },
    {
      id: "SPOT-FOOD-07",
      name: "Bánh Khoái Hoàng Gia Lạc Thiện (Cửa Thượng Tứ)",
      category: "Ẩm Thực Xứ Huế",
      distance: "3.8km từ Homestay (11 phút đi xe)",
      audioDuration: "2 phút 10 giây",
      lat: 16.4715,
      lng: 107.5835,
      image: "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=800&q=80",
      intro: "Bánh khoái hoàng cung chiên giòn rụm kẹp tôm sông tươi rói, chấm nước lèo gan đậu phộng béo bùi bí truyền.",
      narrations: {
        vi: "Thương hiệu bánh khoái Lạc Thiện hơn 60 năm nổi tiếng với chiếc bánh vàng ruộm giòn tan, ăn kèm vả chát, khế chua, chuối chát và chấm chén nước lèo gan heo đậu phộng bí truyền ba đời.",
        en: "Lac Thien's royal crispy pancake is famous for its crunchy golden shell packed with river shrimp and pork, dipped in decadent peanut-liver sauce.",
        fr: "Les fameuses crêpes croustillantes royales de Lac Thien sont servies avec une sauce onctueuse au foie et aux cacahuètes."
      }
    }
  ],

  // 3. MENU ẨM THỰC BẢN ĐỊA GIAO 15 PHÚT & NÔNG SẢN BAO TIÊU
  foodAndFarmMenu: [
    {
      id: "FOOD-01",
      name: "Bánh Ướt Thịt Nướng Kim Long",
      type: "Đặc sản Gia Truyền (Giao 15 phút)",
      price: 45000,
      partner: "Lò Bánh Ướt Huyền Anh (Cách 250m)",
      time: "15 phút",
      desc: "Thịt heo ba chỉ ướp gia vị sả mè nướng trên than hoa đỏ rực, cuốn bánh ướt bột gạo mềm mướt kèm rau thơm Kim Long, chấm sốt tương ớt đậm đà.",
      badge: "Món Chữ Ký (Signature)",
      image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
    },
    {
      id: "FOOD-02",
      name: "Chè Hạt Sen Nhãn Lồng Cố Đô",
      type: "Đặc sản Gia Truyền (Giao 15 phút)",
      price: 35000,
      partner: "Nghệ nhân Chè Cung Đình Kim Long",
      time: "15 phút",
      desc: "Hạt sen hồ Tịnh Tâm bùi ngậy bọc trong cùi nhãn lồng giòn ngọt, nấu cùng đường phèn thanh mát và vài sợi gừng ấm nồng.",
      badge: "Mát Lành Chữa Lành",
      image: "https://images.unsplash.com/photo-1551024709-8f23befc6f87?auto=format&fit=crop&w=400&q=80"
    },
    {
      id: "FOOD-03",
      name: "Mứt Gừng Kim Long Ấm Nồng (Hộp 250g)",
      type: "Quà Tặng Lưu Niệm Bản Địa",
      price: 55000,
      partner: "Hợp tác xã Làm Mứt Truyền thống Kim Long",
      time: "Có sẵn tại phòng",
      desc: "Làm từ củ gừng sẻ bản địa cay đậm, lát mỏng phủ lớp đường tuyết mịn. Rất thích hợp uống trà ngắm mưa Huế.",
      badge: "Quà Tặng Bản Địa",
      image: "https://images.unsplash.com/photo-1587735243615-c03f25aaff15?auto=format&fit=crop&w=400&q=80"
    },
    {
      id: "FARM-01",
      name: "Bưởi Thanh Trà Kim Long Hái Tại Vườn",
      type: "Nông Sản Bao Tiêu Sạch",
      price: 45000,
      partner: "Vườn Nông Dân Bác Hai (Hộ liên kết căn số 01)",
      time: "Hái tươi ngay sau khi đặt",
      desc: "Quả bưởi thanh trà tiến vua hái trực tiếp từ cây trong vườn homestay. Tép mọng nước, vị ngọt thanh mát, không thuốc kích thích.",
      badge: "Bao Tiêu 100% Giá Cao",
      image: "https://images.unsplash.com/photo-1596435707700-67600ab6640b?auto=format&fit=crop&w=400&q=80"
    },
    {
      id: "FARM-02",
      name: "Rổ Rau Thơm Hữu Cơ Kim Long (1kg)",
      type: "Nông Sản Bao Tiêu Sạch",
      price: 25000,
      partner: "Hộ Nông Dân Canh Tác Liền Kề",
      time: "Giao trong ngày",
      desc: "Bao gồm xà lách, tía tô, kinh giới, rau thơm tưới bằng nước phù sa sông Hương. Khách lưu trú có thể tự tay hái cùng bà con.",
      badge: "Hữu Cơ 100%",
      image: "https://images.unsplash.com/photo-1540420773420-3366772f4999?auto=format&fit=crop&w=400&q=80"
    },
    {
      id: "FOOD-04",
      name: "Ấm Trà Sen Cung Đình Đón Bình Minh",
      type: "Dịch Vụ Trải Nghiệm Thư Thái",
      price: 30000,
      partner: "Vườn Thảo Mộc Homestay",
      time: "10 phút",
      desc: "Trà ướp hoa sen bách diệp tự nhiên, dùng kèm mứt sen trần và ấm trà gốm đất nung truyền thống.",
      badge: "Miễn Phí Khách Ở",
      image: "https://images.unsplash.com/photo-1576092768241-dec231879fc3?auto=format&fit=crop&w=400&q=80"
    }
  ],

  // 4. BẢNG SỐ LIỆU VẬN HÀNH DÀNH CHO CỔNG CHỦ VƯỜN & ADMIN DASHBOARD
  operationalMetrics: {
    fixedLeasePerHouse: "10.000.000 VNĐ / tháng / căn",
    totalHouses: 2,
    totalRooms: 5,
    farmerPartners: [
      {
        farmerName: "Bác Nguyễn Văn Hải (Bác Hai)",
        house: "Nhà Vườn Hương Sen (Căn 01 - 2 phòng ngủ)",
        landArea: "1.200m² vườn cây & ruộng lúa liên kề",
        leaseStatus: "Đã chi trả tháng hiện tại (10.000.000đ vào ngày 01)",
        doubleIncome: "20.000.000đ/tháng (10tr tiền thuê + 10tr lương buồng phòng & vườn)",
        agriSupport: "Đã cấp 100% men vi sinh EM & bao tiêu 180kg bưởi thanh trà"
      },
      {
        farmerName: "Bác Trần Thị Lệ (Bác Ba Lệ)",
        house: "Nhà Vườn Nguyệt Biều (Căn 02 - 3 phòng ngủ)",
        landArea: "1.650m² nhà rường truyền thống & ao súng",
        leaseStatus: "Đã chi trả tháng hiện tại (10.000.000đ vào ngày 01)",
        doubleIncome: "20.000.000đ/tháng (10tr tiền thuê + 10tr lương nhân sự vận hành)",
        agriSupport: "Cam kết bao tiêu rau màu sạch và hỗ trợ giống vụ Đông Xuân"
      }
    ],
    sopHousekeeping8Steps: [
      { step: 1, title: "Thu gom & phân loại rác sinh thái (hữu cơ ủ phân bón, không ni-lông 1 lần)" },
      { step: 2, title: "Tháo giặt ga gối bằng nước giặt sinh học sả chanh tự ủ, phơi nắng đồng lúa" },
      { step: 3, title: "Lau bụi cửa kính panorama & bàn ghế gỗ mộc bằng khăn vi sợi microfiber" },
      { step: 4, title: "Cọ rửa nhà tắm bằng dung dịch enzym bồ hòn lên men tự nhiên (không hóa chất tẩy)" },
      { step: 5, title: "Trải ga giường trắng phẳng góc 45 độ, đệm organic thông thoáng thơm nắng" },
      { step: 6, title: "Bổ sung đồ tiêu hao thảo mộc: xà phòng bồ hòn, dầu gội tràm, trà sen, nước chai thủy tinh" },
      { step: 7, title: "Đốt đèn xông tinh dầu tràm quế Cố đô, kiểm tra điều hòa và tín hiệu Wi-Fi mesh" },
      { step: 8, title: "Kiểm tra mức pin khóa Smart Lock (nếu < 20% thay ngay), kích hoạt mã PIN mới" }
    ],
    mvpFinancialSummary: {
      totalCapex: "750.000.000 VNĐ (Vốn đầu tư ban đầu MVP 5 phòng mẫu)",
      monthlyFixedOpex: "28.000.000 VNĐ / tháng (Định phí hoạt động FC: Thù lao 15M, Mkt 8M, AWS/IoT 2.5M, Khấu hao 2.5M)",
      directCostPerNight: "325.500 VNĐ / đêm phòng (Biến phí VC: chia sẻ chủ vườn 30% 205.5k, giặt ủi/điện nước 80k, công dọn 40k)",
      adrAvgPrice: "685.000 VNĐ / đêm phòng (Giá bán bình quân ADR)",
      breakEvenRooms: "63.0 đêm / tháng (Hòa vốn hợp nhất: 2.10 phòng/ngày ~ 42.0% công suất)",
      currentMonthOccupancy: "60.0% (Mục tiêu Năm 1: Doanh thu 1.303.050.000 VNĐ, Lợi nhuận ròng 317.856.000 VNĐ)"
    }
  }
};

// Xuất dữ liệu cho môi trường trình duyệt
if (typeof window !== "undefined") {
  window.ECOSTAY_DATA = ECOSTAY_DATA;
}
