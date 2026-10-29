/**
 * HERITAGE & PARTNER NETWORK MAP CONTROLLER - KIM LONG ECOSTAY
 * Bản đồ số GIS Nhà vườn, Mạng lưới Đối tác Địa phương, Ẩm thực, Đặc sản & Điểm trải nghiệm
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
  userLocationMarker: null,
  userLocationCircle: null,
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
  activeCategoryFilter: 'Tất Cả Địa Điểm',
  activeSearchQuery: '',
  activeServiceFilter: 'ALL',

  // Tọa độ chuẩn của Kim Long EcoStay
  ecoStayCoords: [16.4560, 107.5495],

  // =========================================================================
  // BỘ ĐỊNH NGHĨA CÁC NHÀ CUNG CẤP TILE BẢN ĐỒ (TILE PROVIDERS REGISTRY)
  // Tuyệt đối không dùng endpoint yêu cầu API Key nếu chưa có cấu hình.
  // =========================================================================
  TILE_PROVIDERS: {
    osmStandard: {
      id: 'osmStandard',
      name: 'OpenStreetMap Tiêu Chuẩn',
      url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
      options: {
        maxZoom: 19,
        attribution: '&copy; OpenStreetMap contributors | Kim Long EcoStay'
      }
    },
    googleStreet: {
      id: 'googleStreet',
      name: 'Google Maps Du Lịch',
      url: 'https://mt{s}.google.com/vt/lyrs=m&x={x}&y={y}&z={z}',
      options: {
        maxZoom: 20,
        subdomains: ['0', '1', '2', '3'],
        attribution: '&copy; OpenStreetMap & Google Maps | Kim Long EcoStay'
      }
    },
    esriStreet: {
      id: 'esriStreet',
      name: 'Esri World Street Map (Giao Thông)',
      url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}',
      options: {
        maxZoom: 19,
        attribution: 'Tiles &copy; Esri World Street Map | Kim Long EcoStay'
      }
    },
    esriSatellite: {
      id: 'esriSatellite',
      name: 'Esri World Imagery (Vệ Tinh)',
      url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
      options: {
        maxZoom: 19,
        attribution: 'Tiles &copy; Esri World Imagery | Kim Long EcoStay'
      }
    },
    esriLabels: {
      id: 'esriLabels',
      name: 'Esri World Boundaries & Places',
      url: 'https://services.arcgisonline.com/ArcGIS/rest/services/Reference/World_Boundaries_and_Places/MapServer/tile/{z}/{y}/{x}',
      options: {
        maxZoom: 19
      }
    }
  },

  currentElementId: null,
  activeBaseLayers: {},
  failedProviders: {},

  createSafeTileLayer: function(providerConfig, onFailover) {
    var layer = L.tileLayer(providerConfig.url, providerConfig.options);
    var failedCount = 0;
    var hasHandled = false;

    layer.on('tileerror', function() {
      failedCount++;
      if (failedCount >= 3 && !hasHandled) {
        hasHandled = true;
        console.warn("[HeritageMap] Tile provider '" + providerConfig.name + "' gặp lỗi tải tile liên tiếp (" + failedCount + " tiles). Chuyển sang nguồn dự phòng.");
        if (typeof onFailover === 'function') {
          onFailover(providerConfig);
        }
      }
    });

    layer.on('tileload', function(e) {
      if (e.tile && (e.tile.naturalWidth < 16 || e.tile.naturalHeight < 16)) {
        failedCount++;
        if (failedCount >= 3 && !hasHandled) {
          hasHandled = true;
          console.warn("[HeritageMap] Tile provider '" + providerConfig.name + "' trả về kích thước không hợp lệ. Chuyển sang nguồn dự phòng.");
          if (typeof onFailover === 'function') {
            onFailover(providerConfig);
          }
        }
      }
    });

    return layer;
  },

  showFallbackNotice: function(msg, isSuccess) {
    var container = document.getElementById(this.currentElementId || 'heritage-leaflet-map');
    if (!container) return;
    var existing = container.querySelector('.map-tile-fallback-notice');
    if (existing) existing.remove();

    var notice = document.createElement('div');
    notice.className = 'map-tile-fallback-notice';
    var borderColor = isSuccess ? 'rgba(34,197,94,0.45)' : 'rgba(254,240,138,0.35)';
    var textColor = isSuccess ? '#BBF7D0' : '#FEF08A';
    var icon = isSuccess ? 'bi-check-circle-fill text-success' : 'bi-info-circle-fill text-warning';
    notice.style.cssText = 'position:absolute; bottom:16px; left:16px; right:16px; z-index:1000; background:rgba(15,23,42,0.9); color:' + textColor + '; padding:10px 16px; border-radius:12px; font-size:13px; font-weight:600; text-align:center; backdrop-filter:blur(6px); border:1px solid ' + borderColor + '; pointer-events:none; box-shadow:0 4px 16px rgba(0,0,0,0.25);';
    notice.innerHTML = '<i class="bi ' + icon + ' me-2"></i>' + msg;
    container.appendChild(notice);
  },

  clearFallbackNotice: function() {
    var container = document.getElementById(this.currentElementId || 'heritage-leaflet-map');
    if (!container) return;
    var existing = container.querySelector('.map-tile-fallback-notice');
    if (existing) existing.remove();
  },

  initMap: function (elementId, locations) {
    this.currentElementId = elementId;
    var container = document.getElementById(elementId);
    if (!container) {
      console.warn("[HeritageMap] Container element #" + elementId + " chưa xuất hiện trong DOM.");
      return false;
    }

    if (typeof L === 'undefined') {
      console.error("[HeritageMap] Thư viện Leaflet L chưa sẵn sàng. Vui lòng kiểm tra leaflet.js.");
      this.showFallbackNotice("Không thể tải thư viện bản đồ Leaflet. Vui lòng kiểm tra kết nối mạng hoặc xem danh sách đối tác bên cạnh.");
      return false;
    }

    var activeLocations = (locations || (window.getKlePartners ? window.getKlePartners(false) : window.heritageLocations) || []).filter(function (loc) {
      return loc && loc.isActive !== false;
    });
    this.locationsData = activeLocations;

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
    this.failedProviders = {};

    var centerLat = this.ecoStayCoords[0];
    var centerLng = this.ecoStayCoords[1];

    var map = L.map(elementId, {
      center: [centerLat, centerLng],
      zoom: 14,
      scrollWheelZoom: true,
      zoomControl: true,
      fadeAnimation: true
    });
    this.mapInstance = map;

    var self = this;

    // Lớp 1: Google Street / OpenStreetMap Hybrid
    this.streetLayer = this.createSafeTileLayer(this.TILE_PROVIDERS.googleStreet, function() {
      self.handleStreetFailover();
    });

    // Lớp 2: OpenStreetMap Standard
    this.osmStreetLayer = this.createSafeTileLayer(this.TILE_PROVIDERS.osmStandard, function() {
      self.handleOsmFailover();
    });

    // Lớp 3: Esri World Street Map
    this.esriStreetLayer = this.createSafeTileLayer(this.TILE_PROVIDERS.esriStreet, function() {
      self.handleEsriFailover();
    });

    // Lớp 4: Esri World Imagery Satellite
    this.satelliteLayer = this.createSafeTileLayer(this.TILE_PROVIDERS.esriSatellite, function() {
      console.warn("[HeritageMap] Vệ tinh Esri gặp lỗi đường truyền.");
    });

    this.labelsLayer = L.tileLayer(this.TILE_PROVIDERS.esriLabels.url, this.TILE_PROVIDERS.esriLabels.options);

    // Mặc định nạp Street layer
    this.streetLayer.addTo(map);
    this.streetLayer.bringToBack();

    // Vòng tròn sinh thái bao quanh Kim Long EcoStay
    this.centerCircle = L.circle([centerLat, centerLng], {
      color: '#166534',
      fillColor: '#22c55e',
      fillOpacity: 0.10,
      radius: 1000
    }).addTo(map);
    this.centerCircle.bindTooltip("Vùng lõi liên kết Nhà vườn & Đối tác Kim Long (Bán kính 1km)", {
      permanent: false,
      direction: 'top'
    });

    this.renderMarkersOnMap(activeLocations);

    [30, 100, 250, 500, 1000, 2000].forEach(function (delay) {
      setTimeout(function () {
        if (self.mapInstance) {
          self.mapInstance.invalidateSize();
        }
      }, delay);
    });

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

    if (window.ResizeObserver && container) {
      var ro = new ResizeObserver(function () {
        if (self.mapInstance) {
          self.mapInstance.invalidateSize();
        }
      });
      ro.observe(container);
    }

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

  renderMarkersOnMap: function (locations) {
    if (!this.mapInstance) return;
    var self = this;

    // Xóa marker cũ
    this.markersList.forEach(function (m) {
      if (self.mapInstance.hasLayer(m)) {
        self.mapInstance.removeLayer(m);
      }
    });
    this.markersList = [];

    (locations || []).forEach(function (loc) {
      if (!loc || loc.isActive === false) return;
      var lat = parseFloat(loc.latitude);
      var lng = parseFloat(loc.longitude);
      if (isNaN(lat) || isNaN(lng)) return;

      var markerIcon = self.createVisualPhotoIcon(loc);
      var marker = L.marker([lat, lng], {
        icon: markerIcon,
        title: loc.name,
        zIndexOffset: loc.isEcoStayCenter ? 1000 : (loc.isPartner ? 400 : 100)
      }).addTo(self.mapInstance);

      var groupLabel = loc.partnerGroup || (window.inferPartnerGroup ? window.inferPartnerGroup(loc) : loc.category);
      marker.bindTooltip('<strong>' + loc.name + '</strong><br><small style="color:#64748B;">' + groupLabel + ' • ' + ((loc.travelEstimate || '').split('(')[0]) + '</small>', {
        direction: 'top',
        offset: [0, -26],
        className: 'custom-map-tooltip'
      });

      var popupContent = self.buildPopupHtml(loc);
      marker.bindPopup(popupContent, { maxWidth: 345, className: 'custom-leaflet-popup' });
      marker.locData = loc;
      self.markersList.push(marker);
    });
  },

  reloadMarkers: function (locations) {
    var activeLocations = (locations || (window.getKlePartners ? window.getKlePartners(false) : window.heritageLocations) || []).filter(function (loc) {
      return loc && loc.isActive !== false;
    });
    this.locationsData = activeLocations;
    if (!this.mapInstance) return;
    this.renderMarkersOnMap(activeLocations);
    this.filterMarkers(this.activeCategoryFilter, this.activeSearchQuery, this.activeServiceFilter);
  },

  buildPopupHtml: function (loc) {
    var groupLabel = loc.partnerGroup || (window.inferPartnerGroup ? window.inferPartnerGroup(loc) : loc.category);
    var badgeColor = this.getCategoryColor(loc.category, groupLabel);
    var servicesList = Array.isArray(loc.services) && loc.services.length ? loc.services : [loc.category];
    var servicesBadges = servicesList.slice(0, 4).map(function (srv) {
      return '<span class="badge bg-light text-dark border me-1 mb-1" style="font-size:10px;font-weight:600;">' + srv + '</span>';
    }).join('');

    var linkedCount = 0;
    if (typeof window.getProductsByPartnerId === 'function') {
      linkedCount = window.getProductsByPartnerId(loc.id).length;
    }

    var hasProductsBtn = (linkedCount > 0 || groupLabel === 'Nhà vườn và homestay' || groupLabel === 'Quán ăn và ẩm thực' || groupLabel === 'Đặc sản và sản phẩm địa phương');
    var googleDirUrl = loc.googleMapsDirectionUrl || ('https://www.google.com/maps/dir/?api=1&origin=' + this.ecoStayCoords[0] + ',' + this.ecoStayCoords[1] + '&destination=' + loc.latitude + ',' + loc.longitude + '&travelmode=bicycling');
    var osmUrl = 'https://www.openstreetmap.org/?mlat=' + loc.latitude + '&mlon=' + loc.longitude + '#map=17/' + loc.latitude + '/' + loc.longitude;
    var sampleBadge = loc.isSample ? '<span class="badge bg-warning-subtle text-dark border border-warning-subtle ms-1" style="font-size:9px;">Mô hình liên kết</span>' : '';

    return `
      <div class="map-popup-card">
        <div class="popup-img-wrap">
          <img src="${loc.imageUrl || 'images/hero_ecostay.jpg'}" alt="${loc.name}" class="popup-img" onerror="this.src='images/hero_ecostay.jpg'">
          <span class="popup-badge" style="background: ${badgeColor};">${groupLabel}</span>
        </div>
        <div class="popup-body">
          <h4 class="popup-title mb-1">${loc.name} ${sampleBadge}</h4>
          <p class="popup-desc mb-2">${loc.summary || loc.description || ''}</p>
          <div class="popup-addr"><i class="bi bi-geo-alt-fill text-danger"></i> <strong>Địa chỉ:</strong> ${loc.address}</div>
          <div class="popup-dist"><i class="bi bi-bicycle text-success"></i> <strong>Cự ly:</strong> ${loc.travelEstimate || (loc.distanceFromEcoStayKm + ' km')}</div>
          <div class="popup-time-info"><i class="bi bi-clock-history text-primary"></i> <strong>Giờ mở cửa:</strong> ${loc.openingHours || '07:00 - 20:00'}</div>
          ${loc.phone ? `<div class="popup-time-info"><i class="bi bi-telephone-fill text-success"></i> <strong>Liên hệ:</strong> ${loc.phone}</div>` : ''}
          <div class="mt-2 mb-2">${servicesBadges}</div>

          <div class="d-grid gap-1 mt-2">
            <div class="d-flex gap-1">
              <button class="btn btn-sm btn-outline-success fw-bold flex-grow-1" style="font-size:11.5px;border-radius:8px;" onclick="if(window.openPartnerDetailModal){window.openPartnerDetailModal('${loc.id}');}">
                <i class="bi bi-info-circle-fill me-1"></i>Xem chi tiết
              </button>
              <button class="btn btn-sm btn-success fw-bold flex-grow-1" style="font-size:11.5px;border-radius:8px;" onclick="window.heritageMapController.navigateFromHomestayTo('${loc.id}', 'bike')">
                <i class="bi bi-signpost-2-fill me-1"></i>Chỉ đường
              </button>
            </div>
            ${hasProductsBtn ? `
            <button class="btn btn-sm btn-warning fw-bold text-dark w-100 mt-1" style="font-size:11.5px;border-radius:8px;" onclick="if(window.viewPartnerProducts){window.viewPartnerProducts('${loc.id}');}">
              <i class="bi bi-basket2-fill me-1"></i>Xem sản phẩm & dịch vụ ${linkedCount > 0 ? '(' + linkedCount + ')' : ''}
            </button>
            ` : ''}
          </div>

          <div class="popup-actions mt-2 pt-2 border-top d-flex justify-content-between align-items-center">
            <a href="${googleDirUrl}" target="_blank" rel="noopener" class="btn-popup-maps" title="Chỉ đường bằng Google Maps">
              <i class="bi bi-google"></i> Google Maps
            </a>
            <a href="${osmUrl}" target="_blank" rel="noopener" class="btn-popup-select text-decoration-none" title="Xem trên OpenStreetMap">
              <i class="bi bi-globe2"></i> OpenStreetMap
            </a>
          </div>
        </div>
      </div>
    `;
  },

  getGroupIconClass: function (category, partnerGroup) {
    var g = (partnerGroup || '').toLowerCase();
    var c = (category || '').toLowerCase();
    if (g.indexOf('nhà vườn') >= 0 || c.indexOf('nhà vườn') >= 0 || c.indexOf('homestay') >= 0) return 'bi-house-heart-fill';
    if (g.indexOf('quán ăn') >= 0 || g.indexOf('ẩm thực') >= 0 || c.indexOf('ẩm thực') >= 0) return 'bi-cup-hot-fill';
    if (g.indexOf('đặc sản') >= 0 || g.indexOf('sản phẩm') >= 0 || c.indexOf('làng nghề') >= 0) return 'bi-basket2-fill';
    if (c.indexOf('chùa') >= 0 || c.indexOf('tâm linh') >= 0) return 'bi-yin-yang';
    return 'bi-compass-fill';
  },

  createVisualPhotoIcon: function (loc) {
    var groupLabel = loc.partnerGroup || (window.inferPartnerGroup ? window.inferPartnerGroup(loc) : loc.category);
    var borderColor = this.getCategoryColor(loc.category, groupLabel);
    var iconClass = loc.iconClass || this.getGroupIconClass(loc.category, groupLabel);

    if (loc.isEcoStayCenter) {
      var homestayHtml = `
        <div class="visual-photo-pin pin-center-homestay">
          <div class="pin-pulse-ring"></div>
          <div class="pin-avatar-frame" style="border-color: #FEF08A;">
            <img src="${loc.imageUrl || 'images/nha_ruong_1.jpg'}" alt="${loc.name}" class="pin-avatar-img" onerror="this.src='images/logo.jpg'" />
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
          <img src="${loc.imageUrl || 'images/hero_ecostay.jpg'}" alt="${loc.name}" class="pin-avatar-img" onerror="this.src='images/hero_ecostay.jpg'" />
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

  getCategoryColor: function (category, partnerGroup) {
    var g = (partnerGroup || '').toLowerCase();
    if (g === 'nhà vườn và homestay') return '#15803D'; // Xanh lá sinh thái
    if (g === 'quán ăn và ẩm thực') return '#DC2626'; // Đỏ ẩm thực
    if (g === 'đặc sản và sản phẩm địa phương') return '#D97706'; // Cam vàng đặc sản
    if (g === 'điểm trải nghiệm') return '#2563EB'; // Xanh dương trải nghiệm

    switch (category) {
      case "Ẩm Thực Xứ Huế":
      case "Quán Ăn & Ẩm Thực": return "#DC2626";
      case "Làng Nghề Truyền Thống":
      case "Đặc Sản & Sản Phẩm Địa Phương": return "#D97706";
      case "Quần Thể Di Tích Cố Đô":
      case "Điểm Trải Nghiệm": return "#2563EB";
      case "Chùa Cổ & Tâm Linh": return "#7C3AED";
      case "Nhà Vườn Di Sản":
      case "Nhà Vườn & Homestay":
      case "Trụ Sở Homestay": return "#15803D";
      default: return "#0A3D2C";
    }
  },

  handleStreetFailover: function() {
    if (this.currentMode !== 'street' || !this.mapInstance) return;
    if (this.failedProviders['googleStreet']) return;
    this.failedProviders['googleStreet'] = true;

    console.warn("[HeritageMap] Tự động chuyển lớp bản đồ nền sang OpenStreetMap Standard.");
    if (this.streetLayer && this.mapInstance.hasLayer(this.streetLayer)) {
      this.mapInstance.removeLayer(this.streetLayer);
    }
    if (this.osmStreetLayer) {
      this.osmStreetLayer.addTo(this.mapInstance);
      this.osmStreetLayer.bringToBack();
    }
  },

  handleOsmFailover: function() {
    if (!this.mapInstance) return;
    if (this.failedProviders['osmStandard']) return;
    this.failedProviders['osmStandard'] = true;

    console.warn("[HeritageMap] OpenStreetMap gặp sự cố, chuyển sang Esri World Street Map.");
    if (this.osmStreetLayer && this.mapInstance.hasLayer(this.osmStreetLayer)) {
      this.mapInstance.removeLayer(this.osmStreetLayer);
    }
    if (this.esriStreetLayer) {
      this.esriStreetLayer.addTo(this.mapInstance);
      this.esriStreetLayer.bringToBack();
    }
  },

  handleEsriFailover: function() {
    if (!this.mapInstance) return;
    if (this.failedProviders['esriStreet']) return;
    this.failedProviders['esriStreet'] = true;

    this.showFallbackNotice("Dịch vụ bản đồ nền đang chuyển chế độ dự phòng ngoại tuyến. Danh sách địa điểm và tọa độ đối tác Kim Long vẫn hoạt động đầy đủ.");
  },

  toggleMapMode: function (mode) {
    if (!this.mapInstance) return;
    this.currentMode = mode;
    this.clearFallbackNotice();

    var allBaseLayers = [this.streetLayer, this.esriStreetLayer, this.satelliteLayer, this.labelsLayer, this.osmStreetLayer];
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
    } else if (mode === 'osm') {
      if (this.osmStreetLayer) {
        this.osmStreetLayer.addTo(this.mapInstance);
        this.osmStreetLayer.bringToBack();
      }
    } else if (mode === 'esri_street') {
      var esriTarget = (!this.failedProviders['esriStreet'] && this.esriStreetLayer) ? this.esriStreetLayer : (this.osmStreetLayer || this.streetLayer);
      if (esriTarget) {
        esriTarget.addTo(this.mapInstance);
        esriTarget.bringToBack();
      }
    } else {
      var targetLayer = (!this.failedProviders['googleStreet'] && this.streetLayer) ? this.streetLayer :
                        ((!this.failedProviders['osmStandard'] && this.osmStreetLayer) ? this.osmStreetLayer : this.esriStreetLayer);
      if (targetLayer) {
        targetLayer.addTo(this.mapInstance);
        targetLayer.bringToBack();
      } else {
        this.showFallbackNotice("Bản đồ nền tạm thời gián đoạn kết nối. Vị trí các đối tác & nhà vườn Kim Long EcoStay vẫn hiển thị chính xác.");
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
    this.mapInstance.flyTo(this.ecoStayCoords, 14, { duration: 1.0 });
    setTimeout(function () {
      if (self.mapInstance) {
        self.mapInstance.invalidateSize();
      }
    }, 1100);
  },

  // =========================================================================
  // ĐỊNH VỊ NGƯỜI DÙNG (GEOLOCATION API)
  // =========================================================================
  locateUser: function () {
    var self = this;
    if (!this.mapInstance) return;

    if (!navigator.geolocation) {
      this.showFallbackNotice("Trình duyệt của bạn không hỗ trợ định vị GPS. Bản đồ đang hiển thị tâm điểm tại Kim Long EcoStay.");
      this.focusLocation(this.ecoStayCoords[0], this.ecoStayCoords[1], 15);
      return;
    }

    this.showFallbackNotice("Đang xác định vị trí GPS của bạn...", true);

    navigator.geolocation.getCurrentPosition(
      function (pos) {
        self.clearFallbackNotice();
        var uLat = pos.coords.latitude;
        var uLng = pos.coords.longitude;

        if (self.userLocationMarker && self.mapInstance.hasLayer(self.userLocationMarker)) {
          self.mapInstance.removeLayer(self.userLocationMarker);
        }
        if (self.userLocationCircle && self.mapInstance.hasLayer(self.userLocationCircle)) {
          self.mapInstance.removeLayer(self.userLocationCircle);
        }

        var userIcon = L.divIcon({
          html: '<div style="width:22px;height:22px;border-radius:50%;background:#2563EB;border:3px solid #fff;box-shadow:0 0 0 6px rgba(37,99,235,0.28);"></div>',
          className: 'user-gps-marker',
          iconSize: [22, 22],
          iconAnchor: [11, 11]
        });

        var distToEcoStay = window.calculateDistanceFromCenterKm
          ? window.calculateDistanceFromCenterKm(uLat, uLng)
          : 0;

        self.userLocationMarker = L.marker([uLat, uLng], { icon: userIcon, zIndexOffset: 2500 }).addTo(self.mapInstance);
        self.userLocationMarker.bindPopup(
          '<div class="p-2 text-center">' +
          '<strong class="text-primary"><i class="bi bi-crosshair me-1"></i>Vị trí hiện tại của bạn</strong>' +
          '<div class="small text-muted mt-1">Cách Kim Long EcoStay khoảng <strong>' + distToEcoStay + ' km</strong></div>' +
          '<a href="https://www.google.com/maps/dir/?api=1&origin=' + uLat + ',' + uLng + '&destination=' + self.ecoStayCoords[0] + ',' + self.ecoStayCoords[1] + '" target="_blank" class="btn btn-sm btn-primary mt-2 w-100 text-white">Chỉ đường tới Kim Long EcoStay</a>' +
          '</div>'
        ).openPopup();

        self.mapInstance.flyTo([uLat, uLng], 15, { duration: 1.2 });
      },
      function (err) {
        var reason = "Không thể truy cập vị trí của bạn (Chưa cấp quyền GPS). Đã đưa bản đồ về trung tâm Kim Long EcoStay.";
        if (err && err.code === 1) {
          reason = "Bạn đã từ chối quyền truy cập vị trí GPS. Đã định vị về trung tâm Kim Long EcoStay.";
        }
        self.showFallbackNotice(reason);
        self.focusLocation(self.ecoStayCoords[0], self.ecoStayCoords[1], 15);
        setTimeout(function () { self.clearFallbackNotice(); }, 5000);
      },
      { enableHighAccuracy: true, timeout: 8000, maximumAge: 60000 }
    );
  },

  focusLocation: function (lat, lng, zoomLevel) {
    if (!this.mapInstance) return;
    zoomLevel = zoomLevel || 16;
    this.mapInstance.flyTo([lat, lng], zoomLevel, {
      duration: 1.1,
      easeLinearity: 0.25
    });

    var target = this.markersList.find(function (m) {
      return Math.abs(m.getLatLng().lat - lat) < 0.0001 &&
             Math.abs(m.getLatLng().lng - lng) < 0.0001;
    });

    if (target) {
      setTimeout(function () {
        target.openPopup();
      }, 650);
    }
  },

  focusPartnerById: function (partnerId) {
    var loc = (this.locationsData || []).find(function (l) { return l.id === partnerId; });
    if (!loc && window.getKlePartners) {
      loc = window.getKlePartners(false).find(function (l) { return l.id === partnerId; });
    }
    if (!loc) return false;
    this.focusLocation(loc.latitude, loc.longitude, 17);
    return true;
  },

  filterMarkers: function (category, query, service) {
    if (!this.mapInstance) return;
    this.activeCategoryFilter = category || "Tất Cả Địa Điểm";
    this.activeSearchQuery = (query || "").toLowerCase().trim();
    this.activeServiceFilter = service || "ALL";

    var cat = this.activeCategoryFilter;
    var q = this.activeSearchQuery;
    var srv = this.activeServiceFilter.toLowerCase();

    var self = this;
    var visibleBounds = [];

    this.markersList.forEach(function (marker) {
      var d = marker.locData;
      if (!d || d.isActive === false) {
        if (self.mapInstance.hasLayer(marker)) self.mapInstance.removeLayer(marker);
        return;
      }

      var groupLabel = d.partnerGroup || (window.inferPartnerGroup ? window.inferPartnerGroup(d) : d.category);
      var matchCat = (cat === "Tất Cả Địa Điểm" || cat === "ALL") ||
                     (cat === "Gần Homestay (< 3km)" && d.distanceFromEcoStayKm <= 3.0) ||
                     (cat === "Đối Tác Liên Kết" && d.isPartner) ||
                     (d.category === cat) ||
                     (groupLabel === cat);

      var matchQuery = true;
      if (q.length > 0) {
        var servicesText = Array.isArray(d.services) ? d.services.join(' ').toLowerCase() : '';
        matchQuery = (d.name || '').toLowerCase().includes(q) ||
                     (d.address || '').toLowerCase().includes(q) ||
                     (d.category || '').toLowerCase().includes(q) ||
                     (groupLabel || '').toLowerCase().includes(q) ||
                     (d.summary || '').toLowerCase().includes(q) ||
                     servicesText.includes(q);
      }

      var matchService = true;
      if (srv && srv !== 'all' && srv !== 'tất cả dịch vụ') {
        var sList = Array.isArray(d.services) ? d.services.map(function (x) { return x.toLowerCase(); }) : [];
        matchService = sList.some(function (item) { return item.includes(srv); }) ||
                       (d.summary || '').toLowerCase().includes(srv) ||
                       (d.category || '').toLowerCase().includes(srv);
      }

      if (matchCat && matchQuery && matchService) {
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

    if (visibleBounds.length > 0 && cat !== "Tất Cả Địa Điểm" && cat !== "ALL") {
      var bounds = L.latLngBounds(visibleBounds);
      this.mapInstance.fitBounds(bounds, { padding: [40, 40], maxZoom: 16 });
    }
  },

  // =========================================================================
  // 3. TÍNH NĂNG CHỈ ĐƯỜNG DẪN KHÁCH THỰC TẾ (REAL-TIME GUEST ROUTING)
  // =========================================================================
  navigateFromHomestayTo: function (locationId, travelMode) {
    if (!this.mapInstance) return;

    var loc = this.locationsData.find(function (l) { return l.id === locationId; });
    if (!loc && window.getKlePartners) {
      loc = window.getKlePartners(false).find(function (l) { return l.id === locationId; });
    }
    if (!loc) return;

    this.activeNavLocation = loc;
    this.activeTravelMode = travelMode || 'bike';

    this.clearNavigationLayers();

    var waypoints = this.calculateHueRoadWaypoints(this.ecoStayCoords, [loc.latitude, loc.longitude], loc);

    this.routeGlowPolyline = L.polyline(waypoints, {
      color: '#22c55e',
      weight: 12,
      opacity: 0.35,
      lineCap: 'round',
      lineJoin: 'round'
    }).addTo(this.mapInstance);

    var routeColor = (loc.partnerGroup === "Quán ăn và ẩm thực" || loc.category === "Ẩm Thực Xứ Huế") ? '#DC2626' : '#15803D';
    this.routePolyline = L.polyline(waypoints, {
      color: routeColor,
      weight: 5,
      opacity: 0.95,
      dashArray: '10, 10',
      lineCap: 'round',
      lineJoin: 'round'
    }).addTo(this.mapInstance);

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

    var bounds = L.latLngBounds(waypoints);
    this.mapInstance.fitBounds(bounds, {
      padding: [70, 70],
      maxZoom: 16
    });

    this.updateNavigationHUD(loc, travelMode);
    this.animateBikeOnRoute(waypoints);
  },

  calculateHueRoadWaypoints: function (start, end, loc) {
    var sLat = start[0], sLng = start[1];
    var eLat = end[0], eLng = end[1];

    var pts = [start];
    var ptKimLongStreet = [16.4552, 107.5510];
    pts.push(ptKimLongStreet);

    if (eLng < sLng - 0.003) {
      pts.push([16.4542, 107.5460]);
      if (loc.id === "HERITAGE-THIEN-MU") {
        pts.push([16.4533, 107.5451]);
      } else if (loc.id === "HERITAGE-VAN-MIEU") {
        pts.push([16.4530, 107.5420]);
        pts.push([16.4518, 107.5388]);
      }
    } else if (eLat < 16.4600 && eLng > 107.5600) {
      pts.push([16.4550, 107.5610]);
      pts.push([16.4535, 107.5665]);
      pts.push([16.4515, 107.5700]);
      if (eLat < 16.4450) {
        pts.push([16.4440, 107.5710]);
        pts.push([16.4380, 107.5690]);
      } else {
        pts.push([16.4550, 107.5780]);
        pts.push([16.4600, 107.5850]);
      }
    } else if (eLat > 16.4630 && eLng > 107.5650 && eLng < 107.5900) {
      pts.push([16.4570, 107.5620]);
      pts.push([16.4640, 107.5710]);
      pts.push([16.4680, 107.5760]);
    } else if (loc.id === "FOOD-COM-HEN") {
      pts.push([16.4570, 107.5620]);
      pts.push([16.4650, 107.5750]);
      pts.push([16.4720, 107.5900]);
      pts.push([16.4780, 107.6000]);
      pts.push([16.4820, 107.6045]);
    } else if (loc.distanceFromEcoStayKm <= 0.6) {
      pts.push([16.4555, 107.5505]);
    } else {
      var midLat = (sLat + eLat) / 2 + 0.001;
      var midLng = (sLng + eLng) / 2;
      pts.push([midLat, midLng]);
    }

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

    this.bikeAnimInterval = setInterval(function () {
      curStep++;
      if (curStep >= totalSteps) {
        curStep = 0;
      }
      marker.setLatLng(waypoints[curStep]);
    }, 1800);
  },

  updateNavigationHUD: function (loc, travelMode) {
    var hud = document.getElementById("guest-navigation-hud");
    if (!hud) return;

    travelMode = travelMode || 'bike';
    var dist = loc.distanceFromEcoStayKm || 0.3;

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
    var osmDirUrl = `https://www.openstreetmap.org/directions?engine=fossgis_osrm_bike&route=${this.ecoStayCoords[0]}%2C${this.ecoStayCoords[1]}%3B${loc.latitude}%2C${loc.longitude}`;

    hud.innerHTML = `
      <div class="nav-hud-card shadow-lg">
        <div class="nav-hud-header">
          <div class="d-flex align-items-center gap-2">
            <span class="nav-hud-live-dot"></span>
            <span class="fw-bold text-success text-uppercase" style="font-size: 11px; letter-spacing: 0.5px;">Đang Chỉ Đường Từ Kim Long EcoStay</span>
          </div>
          <button type="button" class="btn-close btn-sm" onclick="window.heritageMapController.clearNavigationLayers()" title="Đóng chỉ đường"></button>
        </div>

        <div class="nav-hud-body">
          <div class="d-flex align-items-start gap-3">
            <img src="${loc.imageUrl || 'images/hero_ecostay.jpg'}" alt="${loc.name}" class="nav-hud-thumb" onerror="this.src='images/hero_ecostay.jpg'">
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

          <div class="d-flex gap-2 mt-3 flex-wrap">
            <a href="${googleMapsDir}" target="_blank" rel="noopener" class="btn btn-success flex-grow-1 btn-sm fw-bold py-2 rounded-pill shadow-sm d-flex align-items-center justify-content-center gap-2">
              <i class="bi bi-google"></i>
              <span>Chỉ đường Google Maps</span>
            </a>
            <a href="${osmDirUrl}" target="_blank" rel="noopener" class="btn btn-outline-primary btn-sm fw-bold py-2 px-3 rounded-pill d-flex align-items-center gap-1">
              <i class="bi bi-globe2"></i>
              <span>OpenStreetMap</span>
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
