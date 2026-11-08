/**
 * =========================================================================
 * KIM LONG ECOSTAY — ADMIN GIS INTERACTIVE MAP CONTROLLER (adminMap.js)
 * =========================================================================
 * Bộ điều khiển bản đồ GIS tương tác dành riêng cho Cổng Quản Trị Admin.
 *
 * TÍNH NĂNG CHÍNH:
 * 1. Khởi tạo Leaflet bản đồ số tương tác trên #admin-leaflet-map.
 * 2. Cố định vị trí cơ sở Kim Long EcoStay tại tọa độ chuẩn: [16.4560, 107.5495].
 * 3. Hiển thị toàn bộ các điểm đối tác địa phương (ẩm thực, đặc sản, nhà vườn).
 * 4. Chế độ chọn tọa độ (Coordinate Picker): Nhấp vào bản đồ hoặc kéo ghim để
 *    tự động điền Vĩ độ (Latitude) và Kinh độ (Longitude) vào biểu mẫu.
 * 5. Tự động khắc phục lỗi Leaflet xám/trắng khi chuyển tab qua invalidateSize().
 * 6. Phòng vệ đa tầng (Multi-tier Tile Fallback): CartoDB -> OpenStreetMap -> Esri.
 * 7. Tuyệt đối không yêu cầu API Key trả phí, tương thích 100% GitHub Pages & Localhost.
 */

(function(window) {
  'use strict';

  var HOMESTAY_COORDS = [16.4560, 107.5495]; // Tâm Kim Long EcoStay (Huế)
  var DEFAULT_ZOOM = 15;

  var TILE_PROVIDERS = {
    carto: {
      name: 'CartoDB Voyager (Sạch & Rõ nét)',
      url: 'https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png',
      options: {
        attribution: '&copy; <a href="https://carto.com/">CARTO</a> &copy; <a href="https://www.openstreetmap.org/copyright">OSM</a>',
        subdomains: 'abcd',
        maxZoom: 19
      }
    },
    osm: {
      name: 'OpenStreetMap Tiêu Chuẩn',
      url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
      options: {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        subdomains: 'abc',
        maxZoom: 19
      }
    },
    esri: {
      name: 'Esri World Street Map (Dự phòng)',
      url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}',
      options: {
        attribution: '&copy; Esri, DeLorme, NAVTEQ',
        maxZoom: 18
      }
    }
  };

  var AdminMapController = function() {
    this.mapInstance = null;
    this.currentTileLayer = null;
    this.currentProviderKey = 'carto';
    this.markersLayerGroup = null;
    this.pickerMarker = null;
    this.isInitialized = false;
    this.elementId = 'admin-leaflet-map';
    this.dotNetRef = null;
  };

  /**
   * Khởi tạo hoặc lấy lại bản đồ
   */
  AdminMapController.prototype.initMap = function(elementId, locations, dotNetRef) {
    var targetId = elementId || this.elementId;
    this.elementId = targetId;
    if (dotNetRef) {
      this.dotNetRef = dotNetRef;
    }

    var container = document.getElementById(targetId);
    if (!container) {
      // Container chưa render trong DOM
      return false;
    }

    if (typeof L === 'undefined') {
      console.warn('[AdminMap] Thư viện Leaflet chưa sẵn sàng.');
      this.showFallback(true, 'Thư viện bản đồ Leaflet đang được tải. Vui lòng thử lại sau giây lát.');
      return false;
    }

    // Nếu container đã có bản đồ cũ, giải phóng an toàn để tránh lỗi: Map container is already initialized
    if (this.mapInstance) {
      try {
        this.mapInstance.remove();
      } catch(e) {
        console.warn('[AdminMap] Lỗi khi dọn dẹp bản đồ cũ:', e);
      }
      this.mapInstance = null;
    }

    if (container._leaflet_id) {
      delete container._leaflet_id;
    }

    try {
      this.mapInstance = L.map(targetId, {
        center: HOMESTAY_COORDS,
        zoom: DEFAULT_ZOOM,
        zoomControl: true,
        scrollWheelZoom: true,
        attributionControl: true
      });

      // Tạo LayerGroup quản lý markers
      this.markersLayerGroup = L.layerGroup().addTo(this.mapInstance);

      // Nạp lớp gạch (Tile Layer)
      this.setTileLayer(this.currentProviderKey);

      // Thêm sự kiện nhấp chuột chọn tọa độ (Coordinate Picker)
      var self = this;
      this.mapInstance.on('click', function(e) {
        self.handleMapClick(e);
      });

      // Nạp danh sách địa điểm đối tác
      var locs = locations || this.getLocationsData();
      this.reloadMarkers(locs);

      this.isInitialized = true;
      this.showFallback(false);

      // Cập nhật trạng thái
      this.updateStatusBadge('Đã sẵn sàng (' + (locs ? locs.length : 0) + ' địa điểm)');

      // Kích hoạt tính toán lại kích thước sau khi DOM ổn định
      setTimeout(function() {
        if (self.mapInstance) {
          self.mapInstance.invalidateSize();
        }
      }, 200);

      return true;
    } catch(err) {
      console.error('[AdminMap] Lỗi khởi tạo bản đồ:', err);
      this.showFallback(true, 'Không thể khởi tạo bản đồ: ' + err.message);
      return false;
    }
  };

  /**
   * Đổi lớp gạch nền (Tile Layer) có cơ chế dự phòng
   */
  AdminMapController.prototype.setTileLayer = function(providerKey) {
    if (!this.mapInstance) return;

    var prov = TILE_PROVIDERS[providerKey] || TILE_PROVIDERS['carto'];
    this.currentProviderKey = providerKey;

    if (this.currentTileLayer) {
      this.mapInstance.removeLayer(this.currentTileLayer);
    }

    var self = this;
    this.currentTileLayer = L.tileLayer(prov.url, prov.options);

    this.currentTileLayer.on('tileerror', function() {
      console.warn('[AdminMap] Lỗi tải gạch bản đồ từ ' + prov.name + '. Tự động kích hoạt cơ chế dự phòng.');
      self.handleTileError();
    });

    this.currentTileLayer.addTo(this.mapInstance);

    // Cập nhật nút active trên giao diện
    ['osm', 'carto', 'esri'].forEach(function(k) {
      var btn = document.getElementById('btnLayer' + k.charAt(0).toUpperCase() + k.slice(1));
      if (btn) {
        if (k === providerKey) btn.classList.add('active');
        else btn.classList.remove('active');
      }
    });
  };

  /**
   * Tự động chuyển lớp gạch dự phòng khi gặp lỗi
   */
  AdminMapController.prototype.handleTileError = function() {
    if (this.currentProviderKey === 'carto') {
      this.setTileLayer('osm');
    } else if (this.currentProviderKey === 'osm') {
      this.setTileLayer('esri');
    } else {
      this.showFallback(true, 'Máy chủ bản đồ quốc tế đang bận. Đã kích hoạt bản đồ dự phòng Esri.');
    }
  };

  /**
   * Nạp dữ liệu địa điểm đối tác từ bộ lưu trữ
   */
  AdminMapController.prototype.getLocationsData = function() {
    if (typeof getPartnersStore === 'function') {
      return getPartnersStore(true);
    }
    if (window.heritageLocations && Array.isArray(window.heritageLocations)) {
      return window.heritageLocations;
    }
    if (window.DEFAULT_HERITAGE_LOCATIONS && Array.isArray(window.DEFAULT_HERITAGE_LOCATIONS)) {
      return window.DEFAULT_HERITAGE_LOCATIONS;
    }
    return [];
  };

  /**
   * Nạp lại toàn bộ Markers lên bản đồ
   */
  AdminMapController.prototype.reloadMarkers = function(locations) {
    if (!this.mapInstance || !this.markersLayerGroup) return;

    this.markersLayerGroup.clearLayers();
    var list = locations || this.getLocationsData();
    var self = this;

    // 1. Luôn vẽ Marker trung tâm: Kim Long EcoStay
    var ecoMarker = this.createEcoStayCenterMarker();
    if (ecoMarker) {
      this.markersLayerGroup.addLayer(ecoMarker);
    }

    // 2. Vẽ các điểm đối tác
    list.forEach(function(loc) {
      if (!loc || !loc.latitude || !loc.longitude) return;
      if (loc.id === 'KLE-CENTER' || loc.isEcoStayCenter) return; // Đã vẽ ở trên

      var lat = Number(loc.latitude);
      var lng = Number(loc.longitude);
      if (isNaN(lat) || isNaN(lng) || lat < 8 || lat > 24 || lng < 102 || lng > 110.5) return;

      var marker = self.createPartnerMarker(loc, lat, lng);
      if (marker) {
        self.markersLayerGroup.addLayer(marker);
      }
    });

    this.updateStatusBadge(list.length + ' điểm trên bản đồ');
  };

  /**
   * Tạo Marker đặc biệt cho Kim Long EcoStay Center
   */
  AdminMapController.prototype.createEcoStayCenterMarker = function() {
    var icon = L.divIcon({
      className: 'kle-admin-marker-center',
      html: `
        <div style="position: relative; width: 44px; height: 44px; display: flex; align-items: center; justify-content: center;">
          <div style="position: absolute; width: 44px; height: 44px; border-radius: 50%; background: rgba(30,107,69,0.3); animation: klePulseRing 2s infinite ease-out;"></div>
          <div style="width: 34px; height: 34px; border-radius: 50%; background: #14452F; border: 2.5px solid #D4AF37; box-shadow: 0 4px 14px rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; color: #D4AF37; font-size: 16px;">
            <i class="bi bi-flower1"></i>
          </div>
        </div>
      `,
      iconSize: [44, 44],
      iconAnchor: [22, 22]
    });

    var marker = L.marker(HOMESTAY_COORDS, { icon: icon, zIndexOffset: 1000 });
    var popupContent = `
      <div style="font-family: inherit; font-size: 12.5px; min-width: 240px; padding: 4px;">
        <div style="background: #14452F; color: #D4AF37; padding: 6px 10px; border-radius: 8px; margin-bottom: 8px; font-weight: bold; display: flex; align-items: center; gap: 6px;">
          <i class="bi bi-shield-check"></i> TRUNG TÂM KIM LONG ECOSTAY
        </div>
        <div style="font-weight: 700; color: #14452F; font-size: 13.5px; margin-bottom: 4px;">
          Kim Long EcoStay (Cố Đô Huế)
        </div>
        <div style="color: #64748B; font-size: 11.5px; margin-bottom: 6px;">
          <i class="bi bi-geo-alt-fill text-danger me-1"></i>Kiệt 82 Kim Long, Bờ bắc Sông Hương, TP. Huế
        </div>
        <div style="background: #F1F5F9; padding: 4px 8px; border-radius: 6px; font-size: 11px; font-family: monospace; color: #334155; margin-bottom: 8px;">
          Tọa độ: 16.45600° N, 107.54950° E
        </div>
        <div style="display: flex; gap: 4px;">
          <button type="button" class="btn btn-sm btn-success w-100 py-1" style="font-size: 11px; border-radius: 6px;" onclick="window.adminMapController.centerToHomestay()">
            <i class="bi bi-crosshair me-1"></i>Căn Giữa Tâm
          </button>
        </div>
      </div>
    `;

    marker.bindPopup(popupContent, { maxWidth: 300 });
    return marker;
  };

  /**
   * Tạo Marker cho Đối Tác
   */
  AdminMapController.prototype.createPartnerMarker = function(loc, lat, lng) {
    var grp = loc.partnerGroup || loc.category || '';
    var bg = '#15803D';
    var iconClass = 'bi-shop';

    if (grp.includes('ẩm thực') || grp.includes('Ăn')) {
      bg = '#DC2626';
      iconClass = 'bi-egg-fried';
    } else if (grp.includes('Đặc sản')) {
      bg = '#EA580C';
      iconClass = 'bi-basket2-fill';
    } else if (grp.includes('trải nghiệm') || grp.includes('Điểm')) {
      bg = '#D97706';
      iconClass = 'bi-compass-fill';
    }

    var icon = L.divIcon({
      className: 'kle-admin-partner-icon',
      html: `
        <div style="width: 28px; height: 28px; border-radius: 50%; background: ${bg}; border: 2px solid #FFFFFF; box-shadow: 0 3px 10px rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center; color: #FFFFFF; font-size: 12px;" title="${loc.name}">
          <i class="bi ${iconClass}"></i>
        </div>
      `,
      iconSize: [28, 28],
      iconAnchor: [14, 14]
    });

    var marker = L.marker([lat, lng], { icon: icon });
    var self = this;

    var popupContent = `
      <div style="font-family: inherit; font-size: 12px; min-width: 250px; padding: 4px;">
        <div style="font-weight: 700; color: #0F172A; font-size: 13px; margin-bottom: 3px;">
          ${loc.name}
        </div>
        <div style="margin-bottom: 4px;">
          <span style="display: inline-block; background: ${bg}; color: #fff; font-size: 10px; font-weight: 600; padding: 2px 7px; border-radius: 50px;">
            ${grp || 'Đối tác địa phương'}
          </span>
          <span style="font-family: monospace; font-size: 10px; color: #64748B; margin-left: 4px;">${loc.id}</span>
        </div>
        <div style="color: #64748B; font-size: 11px; margin-bottom: 4px;">
          <i class="bi bi-geo-alt me-1 text-danger"></i>${loc.address || 'TP. Huế'}
        </div>
        <div style="background: #F8FAFC; border: 1px dashed #CBD5E1; padding: 3px 6px; border-radius: 6px; font-size: 10.5px; font-family: monospace; color: #334155; margin-bottom: 8px;">
          GPS: ${lat.toFixed(5)}, ${lng.toFixed(5)}
        </div>
        <div style="display: flex; gap: 4px;">
          <button type="button" class="btn btn-sm btn-primary w-100 py-1" style="font-size: 11px; border-radius: 6px;" onclick="window.adminMapController.selectLocationForForm('${loc.id}', ${lat}, ${lng})">
            <i class="bi bi-pencil-square me-1"></i>Chọn Điểm Này
          </button>
        </div>
      </div>
    `;

    marker.bindPopup(popupContent, { maxWidth: 300 });

    marker.on('click', function() {
      if (self.dotNetRef) {
        try {
          self.dotNetRef.invokeMethodAsync('OnLocationSelected', loc.id);
        } catch(e) {}
      }
    });

    return marker;
  };

  /**
   * Xử lý nhấp chuột trên bản đồ để chọn tọa độ
   */
  AdminMapController.prototype.handleMapClick = function(e) {
    if (!e || !e.latlng) return;
    var lat = Number(e.latlng.lat).toFixed(5);
    var lng = Number(e.latlng.lng).toFixed(5);

    // Điền vào ô input
    var latInp = document.getElementById('adminPartnerLatInput');
    var lngInp = document.getElementById('adminPartnerLngInput');
    if (latInp) latInp.value = lat;
    if (lngInp) lngInp.value = lng;

    // Báo cho Blazor Component nếu có
    if (this.dotNetRef) {
      try {
        this.dotNetRef.invokeMethodAsync('OnCoordinatesPicked', Number(lat), Number(lng));
      } catch(err) {}
    }

    // Đặt pin định vị tạm thời
    this.setPickerPin(lat, lng, 'Vị trí đã chọn: ' + lat + ', ' + lng);

    if (typeof showKleToast === 'function') {
      showKleToast('Đã chọn tọa độ: ' + lat + ', ' + lng + ' vào biểu mẫu', 'success');
    }
  };

  /**
   * Đặt ghim chọn vị trí có thể kéo thả
   */
  AdminMapController.prototype.setPickerPin = function(lat, lng, label) {
    if (!this.mapInstance) return;

    var coords = [Number(lat), Number(lng)];

    if (this.pickerMarker) {
      this.pickerMarker.setLatLng(coords);
    } else {
      var icon = L.divIcon({
        className: 'kle-picker-pin',
        html: `
          <div style="width: 32px; height: 32px; border-radius: 50% 50% 50% 0; background: #2563EB; border: 2.5px solid #FFFFFF; transform: rotate(-45deg); box-shadow: 0 4px 15px rgba(37,99,235,0.5); display: flex; align-items: center; justify-content: center; color: #FFFFFF; font-size: 14px;">
            <i class="bi bi-geo-fill" style="transform: rotate(45deg);"></i>
          </div>
        `,
        iconSize: [32, 32],
        iconAnchor: [16, 32]
      });

      var self = this;
      this.pickerMarker = L.marker(coords, { icon: icon, draggable: true, zIndexOffset: 2000 });
      this.pickerMarker.on('dragend', function(event) {
        var position = event.target.getLatLng();
        var nlat = position.lat.toFixed(5);
        var nlng = position.lng.toFixed(5);
        var latInp = document.getElementById('adminPartnerLatInput');
        var lngInp = document.getElementById('adminPartnerLngInput');
        if (latInp) latInp.value = nlat;
        if (lngInp) lngInp.value = nlng;
        if (typeof showKleToast === 'function') {
          showKleToast('Đã dời ghim tới: ' + nlat + ', ' + nlng, 'info');
        }
      });

      this.pickerMarker.addTo(this.mapInstance);
    }

    if (label) {
      this.pickerMarker.bindTooltip(label, { permanent: false, direction: 'top' }).openTooltip();
    }
  };

  /**
   * Chọn một địa điểm đối tác từ danh sách / popup để mở biểu mẫu
   */
  AdminMapController.prototype.selectLocationForForm = function(locId, lat, lng) {
    if (typeof openAdminPartnerForm === 'function') {
      openAdminPartnerForm(locId);
    }
    this.setPickerPin(lat, lng, 'Địa điểm: ' + locId);
    if (this.mapInstance) {
      this.mapInstance.setView([lat, lng], 17, { animate: true });
    }
  };

  /**
   * Căn giữa về Homestay Kim Long EcoStay
   */
  AdminMapController.prototype.centerToHomestay = function() {
    if (!this.mapInstance) return;
    this.mapInstance.flyTo(HOMESTAY_COORDS, 16, { animate: true, duration: 1.0 });
    this.invalidateSize();
  };

  /**
   * Bay mượt tới địa điểm chỉ định trên bản đồ
   */
  AdminMapController.prototype.flyToLocation = function(lat, lng, zoom, label) {
    if (!this.mapInstance) return;
    this.mapInstance.flyTo([lat, lng], zoom || 16, { animate: true, duration: 1.2 });
    this.setPickerPin(lat, lng, label || ('Tọa độ: ' + lat + ', ' + lng));
    this.invalidateSize();
  };

  /**
   * Buộc Leaflet tính toán lại kích thước khung nhìn (Khắc phục lỗi bản đồ xám)
   */
  AdminMapController.prototype.invalidateSize = function() {
    if (this.mapInstance) {
      this.mapInstance.invalidateSize();
    }
  };

  /**
   * Kiểm tra và khởi tạo nếu chưa có, hoặc resize nếu đã có
   */
  AdminMapController.prototype.initOrResize = function() {
    var container = document.getElementById(this.elementId);
    if (!container) return;

    if (!this.isInitialized || !this.mapInstance) {
      this.initMap(this.elementId);
    } else {
      var self = this;
      setTimeout(function() {
        self.invalidateSize();
      }, 100);
    }
  };

  /**
   * Tải lại bản đồ
   */
  AdminMapController.prototype.reloadMap = function() {
    this.isInitialized = false;
    this.initMap(this.elementId);
    if (typeof showKleToast === 'function') {
      showKleToast('Đã tải lại bản đồ số GIS quản trị.', 'info');
    }
  };

  /**
   * Cập nhật nhãn trạng thái bản đồ
   */
  AdminMapController.prototype.updateStatusBadge = function(msg) {
    var el = document.getElementById('adminMapStatusBadge');
    if (el) {
      el.innerHTML = '<i class="bi bi-geo-alt-fill me-1"></i>' + msg;
    }
  };

  /**
   * Hiển thị / Ẩn thông báo dự phòng
   */
  AdminMapController.prototype.showFallback = function(show, msg) {
    var el = document.getElementById('adminMapFallbackNotice');
    if (el) {
      if (show) {
        if (msg) {
          var p = el.querySelector('p');
          if (p) p.textContent = msg;
        }
        el.classList.remove('d-none');
      } else {
        el.classList.add('d-none');
      }
    }
  };

  AdminMapController.prototype.setPickerLocation = function(lat, lng, label) {
    return this.setPickerPin(lat, lng, label);
  };

  AdminMapController.prototype.resize = function() {
    return this.invalidateSize();
  };

  window.adminMapController = new AdminMapController();

  // Tự động gán listener cho Bootstrap Modal hiển thị để invalidateSize
  document.addEventListener('DOMContentLoaded', function() {
    var adminModalEl = document.getElementById('adminPortalModal');
    if (adminModalEl) {
      adminModalEl.addEventListener('shown.bs.modal', function() {
        if (window.adminMapController) {
          window.adminMapController.invalidateSize();
        }
      });
    }
  });

})(window);
