/**
 * HERITAGE & CULINARY MAP CONTROLLER - KIM LONG ECOSTAY
 * Bản đồ số GIS & Dẫn đường du khách: Di tích Cố đô, Làng nghề & Ẩm thực Huế
 */

window.heritageMapController = {
  mapInstance: null,
  locationsData: [],
  markersList: [],
  routePolyline: null,
  routeGlowPolyline: null,
  navMarkerStart: null,
  navMarkerEnd: null,
  navBikeMarker: null,
  bikeAnimInterval: null,
  centerCircle: null,
  streetLayer: null,
  esriStreetLayer: null,
  satelliteLayer: null,
  osmStreetLayer: null,
  labelsLayer: null,
  currentMode: 'street',
  activeNavLocation: null,
  activeTravelMode: 'bike', // 'bike' | 'walk' | 'motor'

  // Tọa độ chuẩn của Kim Long EcoStay
  ecoStayCoords: [16.4560, 107.5495],

  initMap: function (elementId, locations) {
    var container = document.getElementById(elementId);
    if (!container) {
      console.warn("[HeritageMap] Container element #" + elementId + " chưa xuất hiện trong DOM.");
      return false;
    }

    if (typeof L === 'undefined') {
      console.error("[HeritageMap] Thư viện Leaflet L chưa sẵn sàng. Vui lòng kiểm tra leaflet.js.");
      return false;
    }

    this.locationsData = locations || [];

    // Hủy triệt để instance cũ và xóa _leaflet_id trên DOM container để tránh lỗi 'Map container is already initialized'
    if (this.mapInstance) {
      try {
        this.mapInstance.remove();
      } catch (e) {
        console.warn("[HeritageMap] Lỗi khi remove instance cũ:", e);
      }
      this.mapInstance = null;
    }
    if (container._leaflet_id) {
      delete container._leaflet_id;
    }
    this.markersList = [];

    var centerLat = this.ecoStayCoords[0];
    var centerLng = this.ecoStayCoords[1];

    var map = L.map(elementId, {
      center: [centerLat, centerLng],
      zoom: 13,
      scrollWheelZoom: true,
      zoomControl: true,
      fadeAnimation: true
    });
    this.mapInstance = map;

    // =========================================================================
    // 1. CÁC LỚP BẢN ĐỒ SIÊU BỀN VỮNG (CHỐNG MÀN HÌNH TRẮNG & TỰ ĐỘNG DỰ PHÒNG)
    // =========================================================================
    // Lớp 1: CartoDB Voyager - Tươi sáng, tối ưu du lịch, nổi bật sông Hương
    this.streetLayer = L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png', {
      maxZoom: 19,
      subdomains: 'abcd',
      attribution: '&copy; OpenStreetMap &copy; CARTO | Kim Long EcoStay'
    });

    // Lớp 1B (Dự phòng chuẩn quốc tế): OpenStreetMap Standard Tile Layer
    this.osmStreetLayer = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap contributors | Kim Long EcoStay'
    });

    // Lớp 2: Bản đồ giao thông Esri World Street - Chi tiết đường ngõ xóm Huế
    this.esriStreetLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}', {
      maxZoom: 19,
      attribution: 'Tiles &copy; Esri World Street Map | Kim Long EcoStay'
    });

    // Lớp 3: Ảnh vệ tinh thực tế độ nét cao Esri World Imagery
    this.satelliteLayer = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {
      maxZoom: 19,
      attribution: 'Tiles &copy; Esri World Imagery | Kim Long EcoStay'
    });

    // Lớp chú thích tên đường & địa danh trên nền vệ tinh
    this.labelsLayer = L.tileLayer('https://services.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}', {
      maxZoom: 19
    });

    // Cơ chế tự động Failover sang OpenStreetMap nếu CartoDB gặp trục trặc đường truyền
    var self = this;
    var cartoFailed = false;
    this.streetLayer.on('tileerror', function () {
      if (!cartoFailed && self.currentMode === 'street') {
        cartoFailed = true;
        console.warn("[HeritageMap] CartoDB tile gặp sự cố. Kích hoạt cơ chế dự phòng sang OpenStreetMap.");
        if (self.mapInstance && self.mapInstance.hasLayer(self.streetLayer)) {
          self.mapInstance.removeLayer(self.streetLayer);
          self.osmStreetLayer.addTo(self.mapInstance);
          self.osmStreetLayer.bringToBack();
        }
      }
    });

    // Mặc định nạp Street layer
    this.streetLayer.addTo(map);

    // Vòng tròn sinh thái bao quanh Kim Long EcoStay
    this.centerCircle = L.circle([centerLat, centerLng], {
      color: '#166534',
      fillColor: '#22c55e',
      fillOpacity: 0.12,
      radius: 1200
    }).addTo(map);
    this.centerCircle.bindTooltip("Vùng đệm nhà vườn Kim Long ven sông Hương (1.2km)", {
      permanent: false,
      direction: 'top'
    });

    // =========================================================================
    // 2. TẠO MARKER HÌNH ẢNH SỐNG ĐỘNG
    // =========================================================================
    locations.forEach(function (loc, index) {
      var markerIcon = self.createVisualPhotoIcon(loc);

      var marker = L.marker([loc.latitude, loc.longitude], {
        icon: markerIcon,
        title: loc.name,
        zIndexOffset: loc.isEcoStayCenter ? 1000 : 100
      }).addTo(map);

      // Tooltip tên địa điểm
      marker.bindTooltip(`<strong>${loc.name}</strong><br><small style="color:#64748B;">${loc.category} • ${(loc.travelEstimate || '').split('(')[0]}</small>`, {
        direction: 'top',
        offset: [0, -26],
        className: 'custom-map-tooltip'
      });

      // Popup chi tiết kèm nút DẪN ĐƯỜNG TRỰC TIẾP
      var popupContent = `
        <div class="map-popup-card">
          <div class="popup-img-wrap">
            <img src="${loc.imageUrl || '/images/hero_ecostay.jpg'}" alt="${loc.name}" class="popup-img" onerror="this.src='/images/hero_ecostay.jpg'">
            <span class="popup-badge" style="background: ${self.getCategoryColor(loc.category)};">${loc.category}</span>
          </div>
          <div class="popup-body">
            <h4 class="popup-title">${loc.name}</h4>
            <div class="popup-addr"><i class="bi bi-geo-alt-fill text-danger"></i> <strong>Địa chỉ:</strong> ${loc.address}</div>
            <div class="popup-dist"><i class="bi bi-bicycle text-success"></i> <strong>Cự ly:</strong> ${loc.travelEstimate || ''}</div>
            <div class="popup-time-info"><i class="bi bi-clock-history text-primary"></i> <strong>Giờ mở cửa:</strong> ${loc.openingHours}</div>
            <p class="popup-desc">${loc.summary}</p>
            
            <div class="popup-nav-highlight">
              <button class="btn-popup-nav" onclick="window.heritageMapController.navigateFromHomestayTo('${loc.id}', 'bike')">
                <i class="bi bi-signpost-2-fill text-warning"></i> Chỉ đường từ Homestay
              </button>
            </div>

            <div class="popup-actions mt-2">
              <a href="${loc.googleMapsDirectionUrl || loc.googleMapsQueryUrl}" target="_blank" class="btn-popup-maps" title="Bật điều hướng GPS Google Maps">
                <i class="bi bi-google"></i> Google Maps
              </a>
              <button class="btn-popup-select" onclick="window.heritageMapController.focusLocation(${loc.latitude}, ${loc.longitude}, 16)">
                <i class="bi bi-zoom-in"></i> Xem gần
              </button>
            </div>
          </div>
        </div>
      `;

      marker.bindPopup(popupContent, { maxWidth: 330, className: 'custom-leaflet-popup' });
      marker.locData = loc;
      self.markersList.push(marker);
    });

    // InvalidateSize đa kỳ để khử hoàn toàn hiện tượng bản đồ trắng hoặc xám
    [30, 100, 250, 500, 1000, 2000].forEach(function (delay) {
      setTimeout(function () {
        if (self.mapInstance) {
          self.mapInstance.invalidateSize();
        }
      }, delay);
    });

    // Lắng nghe resize cửa sổ & focus tab
    window.addEventListener('resize', function () {
      if (self.mapInstance) {
        self.mapInstance.invalidateSize();
      }
    });
    window.addEventListener('focus', function () {
      if (self.mapInstance) {
        self.mapInstance.invalidateSize();
      }
    });

    // Theo dõi kích thước container bằng ResizeObserver
    if (window.ResizeObserver && container) {
      var ro = new ResizeObserver(function () {
        if (self.mapInstance) {
          self.mapInstance.invalidateSize();
        }
      });
      ro.observe(container);
    }

    // Theo dõi khi bản đồ cuộn vào tầm nhìn người dùng bằng IntersectionObserver
    if (window.IntersectionObserver && container) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting && self.mapInstance) {
            self.mapInstance.invalidateSize();
          }
        });
      }, { threshold: 0.1 });
      io.observe(container);
    }

    return true;
  },

  createVisualPhotoIcon: function (loc) {
    var borderColor = this.getCategoryColor(loc.category);
    var iconClass = loc.iconClass || "bi-geo-alt-fill";

    if (loc.isEcoStayCenter) {
      var homestayHtml = `
        <div class="visual-photo-pin pin-center-homestay">
          <div class="pin-pulse-ring"></div>
          <div class="pin-avatar-frame" style="border-color: #FEF08A;">
            <img src="${loc.imageUrl}" alt="${loc.name}" class="pin-avatar-img" onerror="this.src='/images/logo.jpg'" />
          </div>
          <span class="pin-corner-badge" style="background: #15803D;"><i class="bi bi-house-heart-fill"></i></span>
        </div>
      `;
      return L.divIcon({
        html: homestayHtml,
        className: 'marker-homestay-center',
        iconSize: [46, 46],
        iconAnchor: [23, 23],
        popupAnchor: [0, -24]
      });
    }

    var markerHtml = `
      <div class="visual-photo-pin">
        <div class="pin-avatar-frame" style="border-color: ${borderColor};">
          <img src="${loc.imageUrl}" alt="${loc.name}" class="pin-avatar-img" onerror="this.src='/images/hero_ecostay.jpg'" />
        </div>
        <span class="pin-corner-badge" style="background: ${borderColor};">
          <i class="bi ${iconClass}"></i>
        </span>
        <div class="pin-pin-pointer" style="border-top-color: ${borderColor};"></div>
      </div>
    `;

    return L.divIcon({
      html: markerHtml,
      className: 'marker-visual-photo',
      iconSize: [40, 48],
      iconAnchor: [20, 48],
      popupAnchor: [0, -46]
    });
  },

  getCategoryColor: function (category) {
    switch (category) {
      case "Ẩm Thực Xứ Huế": return "#DC2626"; // Đỏ tươi ẩm thực
      case "Làng Nghề Truyền Thống": return "#EA580C"; // Đỏ cam làng nghề
      case "Quần Thể Di Tích Cố Đô": return "#D97706"; // Vàng hoàng gia
      case "Chùa Cổ & Tâm Linh": return "#7C3AED"; // Tím Huế
      case "Nhà Vườn Di Sản": return "#059669"; // Xanh ngọc
      case "Trụ Sở Homestay": return "#15803D"; // Xanh lá
      default: return "#0A3D2C";
    }
  },

  toggleMapMode: function (mode) {
    if (!this.mapInstance) return;
    this.currentMode = mode;

    // Gỡ tất cả các lớp nền cũ
    var allBaseLayers = [this.streetLayer, this.osmStreetLayer, this.esriStreetLayer, this.satelliteLayer, this.labelsLayer];
    var self = this;
    allBaseLayers.forEach(function (layer) {
      if (layer && self.mapInstance.hasLayer(layer)) {
        self.mapInstance.removeLayer(layer);
      }
    });

    if (mode === 'satellite') {
      if (this.satelliteLayer) this.satelliteLayer.addTo(this.mapInstance);
      if (this.labelsLayer) this.labelsLayer.addTo(this.mapInstance);
      if (this.satelliteLayer) this.satelliteLayer.bringToBack();
    } else if (mode === 'esri_street') {
      if (this.esriStreetLayer) this.esriStreetLayer.addTo(this.mapInstance);
      if (this.esriStreetLayer) this.esriStreetLayer.bringToBack();
    } else {
      // Mặc định 'street' (Carto Voyager du lịch hoặc OSM)
      var targetLayer = this.streetLayer || this.osmStreetLayer;
      if (targetLayer) {
        targetLayer.addTo(this.mapInstance);
        targetLayer.bringToBack();
      }
    }

    [20, 100, 300, 600].forEach(function (d) {
      setTimeout(function () {
        if (self.mapInstance) {
          self.mapInstance.invalidateSize();
        }
      }, d);
    });
  },

  refreshMap: function () {
    if (!this.mapInstance) return;
    var self = this;
    this.clearNavigationLayers();
    this.mapInstance.invalidateSize();
    this.mapInstance.flyTo(this.ecoStayCoords, 13, { duration: 1.0 });
    setTimeout(function () {
      if (self.mapInstance) {
        self.mapInstance.invalidateSize();
      }
    }, 1100);
  },

  focusLocation: function (lat, lng, zoomLevel) {
    if (!this.mapInstance) return;
    zoomLevel = zoomLevel || 16;
    this.mapInstance.flyTo([lat, lng], zoomLevel, {
      duration: 1.2,
      easeLinearity: 0.25
    });

    var target = this.markersList.find(function (m) {
      return Math.abs(m.getLatLng().lat - lat) < 0.0001 &&
             Math.abs(m.getLatLng().lng - lng) < 0.0001;
    });

    if (target) {
      setTimeout(function () {
        target.openPopup();
      }, 700);
    }
  },

  filterMarkers: function (category, query) {
    if (!this.mapInstance) return;
    category = category || "Tất Cả Địa Điểm";
    query = (query || "").toLowerCase().trim();

    var self = this;
    var visibleBounds = [];

    this.markersList.forEach(function (marker) {
      var d = marker.locData;
      var matchCat = (category === "Tất Cả Địa Điểm") ||
                     (category === "Gần Homestay (< 3km)" && d.distanceFromEcoStayKm <= 3.0) ||
                     (d.category === category);

      var matchQuery = true;
      if (query.length > 0) {
        matchQuery = d.name.toLowerCase().includes(query) ||
                     d.address.toLowerCase().includes(query) ||
                     d.category.toLowerCase().includes(query) ||
                     d.summary.toLowerCase().includes(query);
      }

      if (matchCat && matchQuery) {
        if (!self.mapInstance.hasLayer(marker)) {
          marker.addTo(self.mapInstance);
        }
        visibleBounds.push(marker.getLatLng());
      } else {
        if (self.mapInstance.hasLayer(marker)) {
          self.mapInstance.removeLayer(marker);
        }
      }
    });

    if (visibleBounds.length > 0 && category !== "Tất Cả Địa Điểm") {
      var bounds = L.latLngBounds(visibleBounds);
      this.mapInstance.fitBounds(bounds, { padding: [40, 40], maxZoom: 15 });
    }
  },

  // =========================================================================
  // 3. TÍNH NĂNG CHỈ ĐƯỜNG DẪN KHÁCH THỰC TẾ (REAL-TIME GUEST ROUTING)
  // =========================================================================
  navigateFromHomestayTo: function (locationId, travelMode) {
    if (!this.mapInstance) return;

    var loc = this.locationsData.find(function (l) { return l.id === locationId; });
    if (!loc) return;

    this.activeNavLocation = loc;
    this.activeTravelMode = travelMode || 'bike';

    // Xóa tuyến đường chỉ dẫn cũ nếu có
    this.clearNavigationLayers();

    // Tạo các mốc tọa độ tuyến đường thực tế dựa trên mạng lưới đường phố Huế
    var waypoints = this.calculateHueRoadWaypoints(this.ecoStayCoords, [loc.latitude, loc.longitude], loc);

    // 1. Lớp phủ phát sáng (Outer Glow)
    this.routeGlowPolyline = L.polyline(waypoints, {
      color: '#22c55e',
      weight: 12,
      opacity: 0.35,
      lineCap: 'round',
      lineJoin: 'round'
    }).addTo(this.mapInstance);

    // 2. Lớp đường nét đứt di chuyển chính (Inner Route Line)
    var routeColor = (loc.category === "Ẩm Thực Xứ Huế") ? '#DC2626' : '#15803D';
    this.routePolyline = L.polyline(waypoints, {
      color: routeColor,
      weight: 5,
      opacity: 0.95,
      dashArray: '10, 10',
      lineCap: 'round',
      lineJoin: 'round'
    }).addTo(this.mapInstance);

    // 3. Ghim điểm khởi hành đặc biệt (Start Pin: Kim Long EcoStay)
    var startIcon = L.divIcon({
      html: `
        <div class="nav-endpoint-pin nav-start-pin">
          <span class="nav-pin-tag">Xuất phát</span>
          <div class="nav-pin-circle bg-success"><i class="bi bi-house-door-fill text-white"></i></div>
        </div>
      `,
      className: 'nav-endpoint-wrap',
      iconSize: [70, 40],
      iconAnchor: [35, 36]
    });
    this.navMarkerStart = L.marker(waypoints[0], { icon: startIcon, zIndexOffset: 2000 }).addTo(this.mapInstance);

    // 4. Ghim điểm đến đặc biệt (End Pin: Tên địa điểm)
    var endIcon = L.divIcon({
      html: `
        <div class="nav-endpoint-pin nav-end-pin">
          <span class="nav-pin-tag bg-danger">${loc.name.split('(')[0]}</span>
          <div class="nav-pin-circle bg-danger"><i class="bi bi-flag-fill text-white"></i></div>
        </div>
      `,
      className: 'nav-endpoint-wrap',
      iconSize: [120, 40],
      iconAnchor: [60, 36]
    });
    this.navMarkerEnd = L.marker(waypoints[waypoints.length - 1], { icon: endIcon, zIndexOffset: 2000 }).addTo(this.mapInstance);

    // 5. Căn chỉnh khung nhìn hiển thị trọn vẹn tuyến đường
    var bounds = L.latLngBounds(waypoints);
    this.mapInstance.fitBounds(bounds, {
      padding: [70, 70],
      maxZoom: 16
    });

    // 6. Cập nhật Bảng Dẫn Đường Du Khách (Navigation HUD Panel)
    this.updateNavigationHUD(loc, travelMode);

    // 7. Bắt đầu mô phỏng xe đạp di chuyển
    this.animateBikeOnRoute(waypoints);
  },

  calculateHueRoadWaypoints: function (start, end, loc) {
    // start: [16.4560, 107.5495] (Kim Long EcoStay - Ven bờ bắc sông Hương)
    var sLat = start[0], sLng = start[1];
    var eLat = end[0], eLng = end[1];

    var pts = [start];

    // Cung đường Kim Long ven sông Hương
    var ptKimLongStreet = [16.4552, 107.5510];
    pts.push(ptKimLongStreet);

    // Trường hợp 1: Về phía Tây (Chùa Thiên Mụ, Văn Miếu, Huyền Không)
    if (eLng < sLng - 0.003) {
      pts.push([16.4542, 107.5460]); // Ngã ba Thiên Mụ
      if (loc.id === "HERITAGE-THIEN-MU") {
        pts.push([16.4533, 107.5451]);
      } else if (loc.id === "HERITAGE-VAN-MIEU") {
        pts.push([16.4530, 107.5420]);
        pts.push([16.4518, 107.5388]);
      }
    }
    // Trường hợp 2: Qua bờ Nam sông Hương (Thủy Xuân, Phường Đúc, Cà phê muối, Chè Hẻm)
    else if (eLat < 16.4600 && eLng > 107.5600) {
      pts.push([16.4550, 107.5610]); // Đầu cầu Dã Viên bờ Bắc
      pts.push([16.4535, 107.5665]); // Cầu Dã Viên vượt sông Hương
      pts.push([16.4515, 107.5700]); // Đầu cầu Dã Viên bờ Nam (Bùi Thị Xuân)
      
      // Nếu đi Thủy Xuân / Lăng Tự Đức (Tây Nam bờ Nam)
      if (eLat < 16.4450) {
        pts.push([16.4440, 107.5710]); // Đường Điện Biên Phủ
        pts.push([16.4380, 107.5690]); // Huyền Trân Công Chúa
      } else {
        // Đi vào trung tâm bờ Nam (Lê Lợi, Hùng Vương, Nguyễn Lương Bằng)
        pts.push([16.4550, 107.5780]); // Đường Lê Lợi
        pts.push([16.4600, 107.5850]); // Ngã 6 Hùng Vương
      }
    }
    // Trường hợp 3: Đi vào Đại Nội / Hoàng Thành (Bờ Bắc phía Đông)
    else if (eLat > 16.4630 && eLng > 107.5650 && eLng < 107.5900) {
      pts.push([16.4570, 107.5620]); // Đường Kim Long nối Lê Duẩn
      pts.push([16.4640, 107.5710]); // Đường Lê Duẩn (Công viên bờ sông)
      pts.push([16.4680, 107.5760]); // Cửa Quảng Đức / Ngọ Môn
    }
    // Trường hợp 4: Đi Cồn Hến (Hoa Đông)
    else if (loc.id === "FOOD-COM-HEN") {
      pts.push([16.4570, 107.5620]);
      pts.push([16.4650, 107.5750]);
      pts.push([16.4720, 107.5900]); // Trần Hưng Đạo
      pts.push([16.4780, 107.6000]); // Cầu Ưng Bình
      pts.push([16.4820, 107.6045]); // Cồn Hến
    }
    // Trường hợp 5: Các điểm rất gần homestay (Bánh ướt Huyền Anh, Nhà vườn An Hiên, Bến thuyền)
    else if (loc.distanceFromEcoStayKm <= 0.6) {
      pts.push([16.4555, 107.5505]);
    } else {
      // Interpolate trung gian tự nhiên
      var midLat = (sLat + eLat) / 2 + 0.001;
      var midLng = (sLng + eLng) / 2;
      pts.push([midLat, midLng]);
    }

    // Điểm đến cuối cùng
    pts.push(end);
    return pts;
  },

  animateBikeOnRoute: function (waypoints) {
    if (this.bikeAnimInterval) {
      clearInterval(this.bikeAnimInterval);
      this.bikeAnimInterval = null;
    }
    if (this.navBikeMarker) {
      this.mapInstance.removeLayer(this.navBikeMarker);
      this.navBikeMarker = null;
    }

    var bikeIcon = L.divIcon({
      html: `
        <div class="nav-moving-bike">
          <div class="bike-pulse-wave"></div>
          <div class="bike-icon-bubble">🚲</div>
        </div>
      `,
      className: 'nav-bike-wrap',
      iconSize: [36, 36],
      iconAnchor: [18, 18]
    });

    var marker = L.marker(waypoints[0], { icon: bikeIcon, zIndexOffset: 3000 }).addTo(this.mapInstance);
    this.navBikeMarker = marker;

    var curStep = 0;
    var totalSteps = waypoints.length;
    var self = this;

    this.bikeAnimInterval = setInterval(function () {
      curStep++;
      if (curStep >= totalSteps) {
        curStep = 0; // lặp lại chu trình mô phỏng
      }
      marker.setLatLng(waypoints[curStep]);
    }, 1800);
  },

  updateNavigationHUD: function (loc, travelMode) {
    var hud = document.getElementById("guest-navigation-hud");
    if (!hud) return;

    travelMode = travelMode || 'bike';
    var dist = loc.distanceFromEcoStayKm || 0.3;

    // Tính toán thời gian theo phương tiện
    var timeEstimate = "";
    var modeIcon = "bi-bicycle";
    var modeName = "Xe đạp trợ lực Eco-Bike";

    if (travelMode === 'walk') {
      var walkMins = Math.max(2, Math.round(dist * 13));
      timeEstimate = `${walkMins} phút đi bộ`;
      modeIcon = "bi-person-walking";
      modeName = "Đi bộ dạo mát";
    } else if (travelMode === 'motor') {
      var motorMins = Math.max(1, Math.round(dist * 2.2));
      timeEstimate = `${motorMins} phút xe máy / taxi`;
      modeIcon = "bi-scooter";
      modeName = "Xe máy / Ô tô";
    } else {
      var bikeMins = Math.max(1, Math.round(dist * 3.2));
      timeEstimate = `${bikeMins} phút xe đạp`;
      modeIcon = "bi-bicycle";
      modeName = "Xe đạp trợ lực Eco-Bike";
    }

    var googleMapsDir = `https://www.google.com/maps/dir/?api=1&origin=${this.ecoStayCoords[0]},${this.ecoStayCoords[1]}&destination=${loc.latitude},${loc.longitude}&travelmode=${travelMode === 'walk' ? 'walking' : 'bicycling'}`;

    hud.innerHTML = `
      <div class="nav-hud-card shadow-lg">
        <div class="nav-hud-header">
          <div class="d-flex align-items-center gap-2">
            <span class="nav-hud-live-dot"></span>
            <span class="fw-bold text-success text-uppercase" style="font-size: 11px; letter-spacing: 0.5px;">Đang Chỉ Đường Cho Khách</span>
          </div>
          <button type="button" class="btn-close btn-sm" onclick="window.heritageMapController.clearNavigationLayers()" title="Đóng chỉ đường"></button>
        </div>

        <div class="nav-hud-body">
          <div class="d-flex align-items-start gap-3">
            <img src="${loc.imageUrl || '/images/hero_ecostay.jpg'}" alt="${loc.name}" class="nav-hud-thumb" onerror="this.src='/images/hero_ecostay.jpg'">
            <div class="flex-grow-1">
              <h5 class="nav-hud-title">${loc.name}</h5>
              <div class="nav-hud-sub text-muted small"><i class="bi bi-geo-alt-fill text-danger me-1"></i>${loc.address}</div>
              
              <div class="d-flex gap-2 mt-2 flex-wrap">
                <span class="badge bg-success-subtle text-success border border-success-subtle py-1 px-2">
                  <i class="bi bi-pin-map-fill me-1"></i>${dist} km
                </span>
                <span class="badge bg-warning-subtle text-dark border border-warning-subtle py-1 px-2">
                  <i class="bi bi-clock-history me-1"></i>${timeEstimate}
                </span>
                <span class="badge bg-light text-secondary border py-1 px-2">
                  <i class="bi ${modeIcon} me-1"></i>${modeName}
                </span>
              </div>
            </div>
          </div>

          <!-- Bộ chuyển đổi phương tiện nhanh -->
          <div class="nav-mode-selector mt-3">
            <span class="small text-muted me-2">Phương tiện:</span>
            <button class="nav-mode-btn ${travelMode === 'bike' ? 'active' : ''}" onclick="window.heritageMapController.navigateFromHomestayTo('${loc.id}', 'bike')">
              <i class="bi bi-bicycle"></i> Xe đạp Eco-Bike
            </button>
            <button class="nav-mode-btn ${travelMode === 'walk' ? 'active' : ''}" onclick="window.heritageMapController.navigateFromHomestayTo('${loc.id}', 'walk')">
              <i class="bi bi-person-walking"></i> Đi bộ
            </button>
            <button class="nav-mode-btn ${travelMode === 'motor' ? 'active' : ''}" onclick="window.heritageMapController.navigateFromHomestayTo('${loc.id}', 'motor')">
              <i class="bi bi-scooter"></i> Xe máy
            </button>
          </div>

          <!-- Nút hành động mở Google Maps -->
          <div class="d-flex gap-2 mt-3">
            <a href="${googleMapsDir}" target="_blank" class="btn btn-success flex-grow-1 btn-sm fw-bold py-2 rounded-pill shadow-sm d-flex align-items-center justify-content-center gap-2">
              <i class="bi bi-signpost-2-fill fs-6"></i>
              <span>Bật Giọng Nói Dẫn Đường GPS (Google Maps)</span>
            </a>
            <button class="btn btn-outline-secondary btn-sm rounded-pill px-3" onclick="window.heritageMapController.focusLocation(${loc.latitude}, ${loc.longitude}, 16)" title="Phóng to điểm đến">
              <i class="bi bi-geo-alt"></i>
            </button>
          </div>
        </div>
      </div>
    `;
    hud.style.display = "block";
  },

  clearNavigationLayers: function () {
    if (this.routePolyline) {
      this.mapInstance.removeLayer(this.routePolyline);
      this.routePolyline = null;
    }
    if (this.routeGlowPolyline) {
      this.mapInstance.removeLayer(this.routeGlowPolyline);
      this.routeGlowPolyline = null;
    }
    if (this.navMarkerStart) {
      this.mapInstance.removeLayer(this.navMarkerStart);
      this.navMarkerStart = null;
    }
    if (this.navMarkerEnd) {
      this.mapInstance.removeLayer(this.navMarkerEnd);
      this.navMarkerEnd = null;
    }
    if (this.navBikeMarker) {
      this.mapInstance.removeLayer(this.navBikeMarker);
      this.navBikeMarker = null;
    }
    if (this.bikeAnimInterval) {
      clearInterval(this.bikeAnimInterval);
      this.bikeAnimInterval = null;
    }

    var hud = document.getElementById("guest-navigation-hud");
    if (hud) {
      hud.innerHTML = "";
      hud.style.display = "none";
    }
  },

  drawRoute: function (locationIds, allLocations) {
    if (!this.mapInstance) return;
    this.clearNavigationLayers();

    var latlngs = [];
    locationIds.forEach(function (id) {
      var loc = allLocations.find(function (l) { return l.id === id; });
      if (loc) {
        latlngs.push([loc.latitude, loc.longitude]);
      }
    });

    if (latlngs.length > 1) {
      this.routePolyline = L.polyline(latlngs, {
        color: '#15803D',
        weight: 5,
        opacity: 0.9,
        dashArray: '8, 8',
        lineCap: 'round'
      }).addTo(this.mapInstance);

      this.mapInstance.fitBounds(this.routePolyline.getBounds(), {
        padding: [50, 50]
      });
    }
  }
};
