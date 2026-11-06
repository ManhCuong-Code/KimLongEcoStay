/**
 * =========================================================================
 * KIM LONG ECOSTAY — CẤU HÌNH MÔI TRƯỜNG & KẾT NỐI BACKEND (KLE_CONFIG)
 * =========================================================================
 * File này quản lý cấu hình điểm cuối (API Endpoints) cho toàn bộ hệ thống.
 * Được nạp đầu tiên tại <head> trước mọi script điều phối và SPA router.
 *
 * NGUYÊN TẮC BẢO MẬT BẮT BUỘC:
 * 1. Tuyệt đối KHÔNG lưu trữ chuỗi kết nối SQL (Connection String),
 *    mật khẩu, API Key hay Token bảo mật tại file cấu hình Frontend này.
 * 2. Tuyệt đối KHÔNG sử dụng LocalStorage làm nguồn cấu hình URL Backend
 *    đáng tin cậy trong môi trường Production để ngăn chặn tấn công giả mạo (XSS/Spoofing).
 * 3. Tuyệt đối KHÔNG tự động chuyển sang localhost khi chạy trên GitHub Pages.
 * 4. Khi chưa có Backend HTTPS công khai, hệ thống sẽ trả về null và kích hoạt
 *    chế độ Xem Trước Tĩnh (Static Preview), hiển thị thông báo rõ ràng cho quản trị viên
 *    thay vì âm thầm gửi dữ liệu nhạy cảm đến các domain không xác định.
 */

(function(window) {
  'use strict';

  // -----------------------------------------------------------------------
  // CẤU HÌNH ĐỊA CHỈ BACKEND CHÍNH THỨC (HTTPS PRODUCTION)
  // -----------------------------------------------------------------------
  // Quản trị viên sau khi triển khai máy chủ Backend HTTPS (Azure, Railway, VPS...)
  // hãy điền URL đầy đủ vào đây (ví dụ: 'https://api.kimlongecostay.vn').
  // Để trống ('') nếu website đang chạy ở chế độ Xem Trước Tĩnh trên GitHub Pages.
  var PRODUCTION_API_URL = '';

  // -----------------------------------------------------------------------
  // CẤU HÌNH ĐỊA CHỈ BACKEND CỤC BỘ (DEVELOPMENT)
  // -----------------------------------------------------------------------
  var DEVELOPMENT_API_URL = 'http://127.0.0.1:5204';

  var loc = window.location;
  var isLocal = (loc.hostname === 'localhost' || loc.hostname === '127.0.0.1');
  var isGitHubPages = loc.hostname.endsWith('github.io');
  var isAzure = loc.hostname.endsWith('.azurewebsites.net');

  /**
   * Xác định API Base URL chuẩn xác theo môi trường hiện tại
   * @returns {string|null} URL gốc của Backend hoặc null nếu chưa cấu hình
   */
  function getApiBase() {
    // 0. Manual Tunnel / Custom Backend URL (nếu người dùng cấu hình nhanh qua giao diện để test)
    var manual = null;
    try {
      manual = (typeof window !== 'undefined' && window.__activeManualBackend) ||
               (typeof localStorage !== 'undefined' && localStorage.getItem('kle_manual_backend_url'));
    } catch(e) {}
    if (manual && (manual.startsWith('https://') || manual.startsWith('http://localhost') || manual.startsWith('http://127.0.0.1'))) {
      return manual.replace(/\/+$/, '');
    }

    // 1. Môi trường phát triển cục bộ (Local Development)
    if (isLocal) {
      if (loc.port === '5204' || loc.port === '5000' || loc.port === '7120') {
        return loc.origin;
      }
      return DEVELOPMENT_API_URL;
    }

    // 2. Môi trường Azure App Service (*.azurewebsites.net)
    if (isAzure) {
      return loc.origin;
    }

    // 3. Môi trường GitHub Pages (manhcuong-code.github.io)
    if (isGitHubPages) {
      if (PRODUCTION_API_URL && PRODUCTION_API_URL.startsWith('https://')) {
        return PRODUCTION_API_URL.replace(/\/+$/, '');
      }
      // Chưa cấu hình Backend HTTPS: Trả về null (KHÔNG trả về chuỗi rỗng để tránh gọi nhầm origin github.io)
      return null;
    }

    // 4. Môi trường domain riêng có Backend đồng hành
    return loc.origin;
  }

  window.KLE_CONFIG = {
    productionApiUrl: PRODUCTION_API_URL,
    developmentApiUrl: DEVELOPMENT_API_URL,
    isLocal: isLocal,
    isGitHubPages: isGitHubPages,
    isAzure: isAzure,

    /**
     * Kiểm tra Backend đã được cấu hình hợp lệ hay chưa
     */
    isBackendConfigured: function() {
      return getApiBase() !== null;
    },

    /**
     * Lấy API Base URL hiện thời
     */
    getApiBase: getApiBase,

    /**
     * Cài đặt URL máy chủ thủ công (Tunnel HTTPS / VPS)
     */
    setManualBackendUrl: function(url) {
      if (!url) {
        window.__activeManualBackend = null;
        try { localStorage.removeItem('kle_manual_backend_url'); } catch(e) {}
        return;
      }
      var clean = url.trim().replace(/\/+$/, '');
      window.__activeManualBackend = clean;
      try { localStorage.setItem('kle_manual_backend_url', clean); } catch(e) {}
    },

    /**
     * Xóa cấu hình máy chủ thủ công
     */
    clearManualBackendUrl: function() {
      window.__activeManualBackend = null;
      try { localStorage.removeItem('kle_manual_backend_url'); } catch(e) {}
    },

    /**
     * Ghép đường dẫn endpoint với API Base an toàn
     * @param {string} endpoint - Ví dụ '/api/auth/login'
     * @returns {string|null} - URL đầy đủ hoặc null nếu chưa cấu hình
     */
    resolveApiUrl: function(endpoint) {
      var base = getApiBase();
      if (base === null) return null;
      var clean = endpoint.startsWith('/') ? endpoint : '/' + endpoint;
      return base + clean;
    }
  };
})(window);

