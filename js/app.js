/**
 * JAVASCRIPT LOGIC - KIM LONG ECOSTAY PROTOTYPE MVP
 * Đề án Khởi nghiệp & Đổi mới sáng tạo - Đại học Huế
 */

document.addEventListener("DOMContentLoaded", () => {
  // Trạng thái ứng dụng (Application State)
  const state = {
    currentTab: "booking",
    currentLang: "vi",
    selectedRoom: null,
    activeSpotIndex: 0,
    isPlayingAudio: false,
    audioProgress: 0,
    audioInterval: null,
    speechUtterance: null,
    cart: [],
    deliveryCountdownInterval: null,
    deliverySecondsLeft: 900, // 15 phút = 900 giây
    currentPinInput: "",
    lastGeneratedPin: "686899",
    isBikingSimulation: false,
    bikingInterval: null
  };

  // Khởi tạo Web Audio Synthesizer cho âm thanh bàn phím Smart Lock
  const AudioContext = window.AudioContext || window.webkitAudioContext;
  let audioCtx = null;

  function initAudioCtx() {
    if (!audioCtx) {
      audioCtx = new AudioContext();
    }
    if (audioCtx.state === "suspended") {
      audioCtx.resume();
    }
  }

  function playKeyBeep(freq = 600, duration = 0.08) {
    try {
      initAudioCtx();
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();
      osc.type = "sine";
      osc.frequency.value = freq;
      gain.gain.setValueAtTime(0.15, audioCtx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + duration);
      osc.connect(gain);
      gain.connect(audioCtx.destination);
      osc.start();
      osc.stop(audioCtx.currentTime + duration);
    } catch (e) {
      console.log("Audio not supported or blocked", e);
    }
  }

  function playSuccessTone() {
    try {
      initAudioCtx();
      [523.25, 659.25, 783.99, 1046.50].forEach((freq, i) => {
        setTimeout(() => playKeyBeep(freq, 0.12), i * 110);
      });
    } catch (e) {}
  }

  function playErrorTone() {
    try {
      initAudioCtx();
      [300, 220].forEach((freq, i) => {
        setTimeout(() => playKeyBeep(freq, 0.18), i * 140);
      });
    } catch (e) {}
  }

  // =========================================================================
  // 1. NAVIGATION TAB SWITCHING
  // =========================================================================
  const navTabs = document.querySelectorAll(".nav-tab-btn");
  const sections = document.querySelectorAll(".section-wrapper");

  function switchTab(tabId) {
    state.currentTab = tabId;
    navTabs.forEach(btn => {
      btn.classList.toggle("active", btn.dataset.tab === tabId);
    });
    sections.forEach(sec => {
      sec.classList.toggle("active", sec.id === `section-${tabId}`);
    });
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  navTabs.forEach(btn => {
    btn.addEventListener("click", () => {
      switchTab(btn.dataset.tab);
    });
  });

  // Chuyển ngôn ngữ toàn hệ thống
  const langBtns = document.querySelectorAll(".lang-btn");
  langBtns.forEach(btn => {
    btn.addEventListener("click", () => {
      langBtns.forEach(b => b.classList.remove("active"));
      btn.classList.add("active");
      state.currentLang = btn.dataset.lang;
      updateSpotNarrationView();
    });
  });

  // =========================================================================
  // 2. PHÂN HỆ 1: ĐẶT PHÒNG TRỰC TIẾP (DIRECT BOOKING ENGINE)
  // =========================================================================
  const roomsContainer = document.getElementById("rooms-grid-container");

  function renderRooms(customRooms) {
    if (!roomsContainer || !window.ECOSTAY_DATA) return;
    const rooms = customRooms || window.ECOSTAY_DATA.rooms;

    roomsContainer.innerHTML = rooms.map(room => `
      <div class="room-card" data-room-id="${room.id}">
        <div class="room-image-wrap">
          <img src="${room.imageUrl}" alt="${room.name}" class="room-card-img" loading="lazy">
          <span class="room-badge">${room.badge}</span>
          <span class="room-smartlock-tag">
            <span>🔑</span> ${room.smartLockId} (Nhận phòng 10s)
          </span>
          <span style="position: absolute; bottom: 10px; left: 10px; background: rgba(22, 101, 52, 0.9); color: #fff; font-size: 11px; font-weight: 700; padding: 3px 10px; border-radius: 20px; backdrop-filter: blur(4px); box-shadow: 0 2px 6px rgba(0,0,0,0.2);">
            ● CÒN TRỐNG
          </span>
        </div>
        <div class="room-body">
          <span class="room-house-tag">${room.gardenHouse}</span>
          <h3 class="room-title">${room.name}</h3>
          <div class="room-specs">
            <span class="spec-item">📐 ${room.area}</span>
            <span class="spec-item">👥 ${room.capacity}</span>
            <span class="spec-item">🛏️ ${room.bedType.split('(')[0]}</span>
          </div>
          <p style="font-size: 13px; color: var(--c-text-muted); margin-bottom: 12px;">${room.description}</p>
          <ul class="room-features-list">
            ${room.features.slice(0, 3).map(f => `<li>${f}</li>`).join('')}
          </ul>
          <div class="room-footer">
            <div class="room-price-wrap">
              <span class="room-original-price">${room.originalPrice.toLocaleString('vi-VN')} đ</span>
              <span class="room-price">${room.price.toLocaleString('vi-VN')} đ <small>/đêm</small></span>
            </div>
            <button class="btn-book-room" data-room-id="${room.id}">
              <span>Đặt Phòng</span> ➔
            </button>
          </div>
        </div>
      </div>
    `).join('');

    // Gắn sự kiện nút Đặt phòng
    document.querySelectorAll(".btn-book-room").forEach(btn => {
      btn.addEventListener("click", (e) => {
        const roomId = btn.dataset.roomId;
        openBookingModal(roomId);
      });
    });
  }

  // SỰ KIỆN NÚT "TÌM PHÒNG TRỐNG": Chuyển ngay qua tab Danh Sách Phòng & cuộn mượt xuống
  const btnFilterSearch = document.getElementById("btn-filter-search");
  if (btnFilterSearch) {
    btnFilterSearch.addEventListener("click", () => {
      // 1. Chuyển ngay qua tab Danh Sách Phòng Trống (tab booking)
      switchTab("booking");

      // 2. Lấy tiêu chí tìm kiếm
      const checkin = document.getElementById("filter-checkin")?.value || "2026-10-01";
      const checkout = document.getElementById("filter-checkout")?.value || "2026-10-03";
      const guests = parseInt(document.getElementById("filter-guests")?.value || "2", 10);

      // 3. Lọc danh sách phòng trống
      const allRooms = window.ECOSTAY_DATA.rooms;
      const matchedRooms = allRooms.filter(r => {
        if (guests <= 2) return true;
        if (guests === 3) return r.capacity.includes("3") || r.capacity.includes("4");
        if (guests >= 4) return r.capacity.includes("4");
        return true;
      });

      renderRooms(matchedRooms);

      // 4. Hiển thị thông báo tìm phòng trống
      const searchBanner = document.getElementById("rooms-search-banner");
      if (searchBanner) {
        searchBanner.style.display = "block";
        searchBanner.innerHTML = `
          <div style="background: linear-gradient(135deg, rgba(22, 101, 52, 0.08) 0%, rgba(184, 134, 11, 0.08) 100%); border: 1.5px solid #166534; border-radius: 12px; padding: 14px 18px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 10px;">
            <div style="display: flex; align-items: center; gap: 12px;">
              <span style="font-size: 26px;">🌿</span>
              <div>
                <strong style="color: #166534; font-size: 15px;">Đã tìm thấy ${matchedRooms.length} phòng sinh thái đang còn trống!</strong>
                <div style="font-size: 13px; color: #475569; margin-top: 2px;">
                  📅 Nhận phòng: <strong>${checkin}</strong> ➔ Trả phòng: <strong>${checkout}</strong> &bull; 👥 Số lượng: <strong>${guests} khách</strong> &bull; Trạng thái: <span style="color: #166534; font-weight: 700;">100% Sẵn sàng đón tiếp</span>
                </div>
              </div>
            </div>
            <button id="btn-reset-room-filter" style="background: #166534; color: #fff; border: none; padding: 7px 16px; border-radius: 20px; font-size: 12.5px; font-weight: 700; cursor: pointer; transition: 0.2s;">
              Xem tất cả 5 phòng
            </button>
          </div>
        `;
        document.getElementById("btn-reset-room-filter")?.addEventListener("click", () => {
          renderRooms(window.ECOSTAY_DATA.rooms);
          searchBanner.style.display = "none";
        });
      }

      // 5. Cuộn mượt mà ngay lập tức đến danh sách phòng trống
      setTimeout(() => {
        const target = searchBanner || roomsContainer;
        if (target) {
          target.scrollIntoView({ behavior: "smooth", block: "start" });
        }
      }, 60);
    });
  }

  const btnHeroCta = document.getElementById("btn-hero-cta");
  if (btnHeroCta) {
    btnHeroCta.addEventListener("click", () => {
      document.getElementById("btn-filter-search")?.click();
    });
  }

  // MODAL ĐẶT PHÒNG
  const bookingModal = document.getElementById("booking-modal");
  const modalCloseBtn = document.getElementById("btn-close-modal");
  const bookingSummaryBox = document.getElementById("modal-booking-summary");
  const bookingForm = document.getElementById("form-booking");

  function openBookingModal(roomId) {
    const room = window.ECOSTAY_DATA.rooms.find(r => r.id === roomId);
    if (!room) return;
    state.selectedRoom = room;

    bookingSummaryBox.innerHTML = `
      <div style="display: flex; gap: 14px; align-items: center;">
        <img src="${room.imageUrl}" style="width: 80px; height: 60px; object-fit: cover; border-radius: 8px;">
        <div>
          <h4 style="color: var(--c-primary); font-size: 15px; font-weight: 800;">${room.name}</h4>
          <p style="font-size: 12px; color: var(--c-text-muted);">${room.gardenHouse}</p>
          <span style="font-size: 15px; font-weight: 800; color: var(--c-primary);">${room.price.toLocaleString('vi-VN')} đ / đêm</span>
        </div>
      </div>
    `;

    bookingModal.classList.add("active");
  }

  if (modalCloseBtn) {
    modalCloseBtn.addEventListener("click", () => {
      bookingModal.classList.remove("active");
    });
  }

  if (bookingModal) {
    bookingModal.addEventListener("click", (e) => {
      if (e.target === bookingModal) {
        bookingModal.classList.remove("active");
      }
    });
  }

  // Chọn phương thức thanh toán
  const payOptions = document.querySelectorAll(".payment-opt-card");
  payOptions.forEach(opt => {
    opt.addEventListener("click", () => {
      payOptions.forEach(o => o.classList.remove("active"));
      opt.classList.add("active");
    });
  });

  // =========================================================================
  // KIỂM TRA BÁO LỖI FORM ĐẶT PHÒNG (PHONE, EMAIL, NAME, DATES)
  // =========================================================================
  const nameInput = document.getElementById("guest-name");
  const phoneInput = document.getElementById("guest-phone");
  const emailInput = document.getElementById("guest-email");
  const checkinInput = document.getElementById("modal-checkin-date");
  const checkoutInput = document.getElementById("modal-checkout-date");
  const bookingAlertBox = document.getElementById("booking-alert-box");
  const bookingAlertText = document.getElementById("booking-alert-text");

  const errName = document.getElementById("err-guest-name");
  const errPhone = document.getElementById("err-guest-phone");
  const errEmail = document.getElementById("err-guest-email");
  const errDates = document.getElementById("err-booking-dates");

  // Regex kiểm tra số điện thoại di động Việt Nam (10 số, đầu 03, 05, 07, 08, 09 hoặc +84)
  const VN_PHONE_REGEX = /^(?:(?:\+84)|0)(?:3[2-9]|5[25689]|7[06-9]|8[1-9]|9\d)\d{7}$/;

  // Regex kiểm tra địa chỉ Email chuẩn RFC
  const EMAIL_REGEX = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

  function setFieldError(inputEl, errorEl, message) {
    if (!inputEl) return;
    if (message) {
      inputEl.classList.add("is-invalid");
      inputEl.classList.remove("is-valid");
      if (errorEl) {
        errorEl.textContent = message;
        errorEl.classList.add("active");
      }
    } else {
      inputEl.classList.remove("is-invalid");
      inputEl.classList.add("is-valid");
      if (errorEl) {
        errorEl.textContent = "";
        errorEl.classList.remove("active");
      }
    }
  }

  function validateGuestName() {
    if (!nameInput) return true;
    const val = nameInput.value.trim();
    if (!val) {
      setFieldError(nameInput, errName, "⚠️ Vui lòng nhập họ và tên khách lưu trú.");
      return false;
    }
    if (val.length < 2) {
      setFieldError(nameInput, errName, "⚠️ Họ và tên quá ngắn. Vui lòng nhập họ tên đầy đủ.");
      return false;
    }
    setFieldError(nameInput, errName, "");
    return true;
  }

  function validateGuestPhone() {
    if (!phoneInput) return true;
    const raw = phoneInput.value.trim();
    const clean = raw.replace(/[\s.-]/g, "");
    if (!clean) {
      setFieldError(phoneInput, errPhone, "⚠️ Vui lòng nhập số điện thoại để nhận mã PIN Smart Lock qua Zalo/SMS.");
      return false;
    }
    if (!VN_PHONE_REGEX.test(clean)) {
      setFieldError(phoneInput, errPhone, "❌ Số điện thoại không đúng định dạng! Vui lòng nhập SĐT Việt Nam gồm 10 chữ số (Ví dụ: 0905 123 456, 0912 345 678).");
      return false;
    }
    setFieldError(phoneInput, errPhone, "");
    return true;
  }

  function validateGuestEmail() {
    if (!emailInput) return true;
    const val = emailInput.value.trim();
    if (!val) {
      setFieldError(emailInput, errEmail, "⚠️ Vui lòng nhập địa chỉ Email để nhận xác nhận và hướng dẫn mở cửa phòng.");
      return false;
    }
    if (!EMAIL_REGEX.test(val)) {
      setFieldError(emailInput, errEmail, "❌ Định dạng Email không chính xác! Ví dụ đúng: khachhang@gmail.com.");
      return false;
    }
    setFieldError(emailInput, errEmail, "");
    return true;
  }

  function validateBookingDates() {
    if (!checkinInput || !checkoutInput) return true;
    const cin = new Date(checkinInput.value);
    const cout = new Date(checkoutInput.value);
    if (isNaN(cin.getTime()) || isNaN(cout.getTime())) {
      if (errDates) {
        errDates.textContent = "⚠️ Vui lòng chọn ngày đến và ngày đi hợp lệ.";
        errDates.classList.add("active");
      }
      return false;
    }
    if (cout <= cin) {
      if (errDates) {
        errDates.textContent = "❌ Ngày trả phòng phải sau ngày nhận phòng ít nhất 1 đêm!";
        errDates.classList.add("active");
      }
      checkoutInput.classList.add("is-invalid");
      return false;
    }
    if (errDates) {
      errDates.textContent = "";
      errDates.classList.remove("active");
    }
    checkoutInput.classList.remove("is-invalid");
    return true;
  }

  // Gắn sự kiện kiểm tra trực tiếp (Live Validation) khi người dùng nhập liệu
  if (nameInput) {
    nameInput.addEventListener("input", validateGuestName);
    nameInput.addEventListener("blur", validateGuestName);
  }
  if (phoneInput) {
    phoneInput.addEventListener("input", validateGuestPhone);
    phoneInput.addEventListener("blur", validateGuestPhone);
  }
  if (emailInput) {
    emailInput.addEventListener("input", validateGuestEmail);
    emailInput.addEventListener("blur", validateGuestEmail);
  }
  if (checkinInput) {
    checkinInput.addEventListener("change", validateBookingDates);
  }
  if (checkoutInput) {
    checkoutInput.addEventListener("change", validateBookingDates);
  }

  // Submit đặt phòng với kiểm tra bắt lỗi toàn diện
  if (bookingForm) {
    bookingForm.addEventListener("submit", (e) => {
      e.preventDefault();

      const validName = validateGuestName();
      const validPhone = validateGuestPhone();
      const validEmail = validateGuestEmail();
      const validDates = validateBookingDates();

      if (!validName || !validPhone || !validEmail || !validDates) {
        if (bookingAlertBox && bookingAlertText) {
          bookingAlertText.textContent = "⚠️ Vui lòng sửa lại các thông tin chưa hợp lệ (được đánh dấu đỏ bên dưới).";
          bookingAlertBox.classList.add("active");
        }
        // Tự động focus vào ô bị lỗi đầu tiên
        if (!validName && nameInput) nameInput.focus();
        else if (!validPhone && phoneInput) phoneInput.focus();
        else if (!validEmail && emailInput) emailInput.focus();
        else if (!validDates && checkoutInput) checkoutInput.focus();
        return;
      }

      // Xóa thông báo lỗi tổng
      if (bookingAlertBox) {
        bookingAlertBox.classList.remove("active");
      }

      const guestName = nameInput.value.trim();
      const guestPhone = phoneInput.value.trim().replace(/[\s.-]/g, "");
      const guestEmail = emailInput ? emailInput.value.trim() : "";
      const checkinDate = checkinInput.value;
      const checkoutDate = checkoutInput.value;

      // Tạo mã PIN ngẫu nhiên 6 chữ số
      const generatedPin = Math.floor(100000 + Math.random() * 900000).toString();
      state.lastGeneratedPin = generatedPin;
      const bookingCode = "KLE-" + Math.floor(1000 + Math.random() * 9000);

      bookingModal.classList.remove("active");

      // Cập nhật preset PIN trong phân hệ Smart Lock
      updatePresetPins(generatedPin, state.selectedRoom.name);

      // Gửi email xác nhận kèm mã PIN tới hòm thư thật của khách hàng
      if (guestEmail) {
        try {
          fetch("https://formsubmit.co/ajax/" + encodeURIComponent(guestEmail), {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              "Accept": "application/json"
            },
            body: JSON.stringify({
              _subject: `[Kim Long EcoStay] Xác Nhận Đặt Phòng & Mã PIN SmartLock #${bookingCode}`,
              "Mã Đặt Phòng": bookingCode,
              "Khách Hàng": guestName,
              "Số Điện Thoại": guestPhone,
              "Hòm Thư Nhận": guestEmail,
              "Căn Phòng": state.selectedRoom.name,
              "Thời Gian": `${checkinDate} đến ${checkoutDate}`,
              "MÃ PIN SMARTLOCK 24/7": generatedPin,
              "Hướng Dẫn Nhận Phòng": `Đến trước cửa phòng ${state.selectedRoom.name}, bấm mã PIN 6 số: ${generatedPin} rồi bấm phím # để mở khóa tự động trong 10 giây!`
            })
          }).catch(err => console.log("Email dispatch:", err));
        } catch (e) {}
      }

      // Hiển thị hộp thoại xác nhận chuyên nghiệp, mã PIN thật và nút mở email
      showBookingSuccessModal({
        bookingCode,
        guestName,
        guestPhone,
        guestEmail,
        roomName: state.selectedRoom.name,
        checkinDate,
        checkoutDate,
        generatedPin
      });
    });
  }

  // =========================================================================
  // 3. PHÂN HỆ 2: MÔ PHỎNG SMART LOCK CHECK-IN 10 GIÂY
  // =========================================================================
  const lockPinDisplay = document.getElementById("lock-pin-display");
  const lockFeedback = document.getElementById("lock-feedback-status");
  const lockStatusBadge = document.getElementById("lock-status-badge");
  const keypadBtns = document.querySelectorAll(".keypad-btn");
  const presetPinContainer = document.getElementById("preset-pins-list");

  function updatePresetPins(newPin = "686899", roomName = "Phòng Eco-Nest Ban Mai") {
    if (!presetPinContainer) return;
    presetPinContainer.innerHTML = `
      <button class="btn-preset-pin active-pin" data-pin="${newPin}">
        ★ PIN mới nhận phòng: ${newPin} (${roomName})
      </button>
      <button class="btn-preset-pin" data-pin="686899">
        Mã PIN mẫu 1: 686899 (Phòng R101)
      </button>
      <button class="btn-preset-pin" data-pin="779922">
        Mã PIN mẫu 2: 779922 (Garden Suite R201)
      </button>
      <button class="btn-preset-pin" data-pin="123456">
        Mã PIN sai thử nghiệm: 123456
      </button>
    `;

    document.querySelectorAll(".btn-preset-pin").forEach(btn => {
      btn.addEventListener("click", () => {
        state.currentPinInput = btn.dataset.pin;
        updateLockDisplay();
        playKeyBeep(700, 0.08);
      });
    });
  }

  function updateLockDisplay() {
    if (!lockPinDisplay) return;
    if (state.currentPinInput.length === 0) {
      lockPinDisplay.textContent = "______";
    } else {
      const masked = state.currentPinInput.split('').map(() => "●").join('');
      const pad = "_".repeat(Math.max(0, 6 - state.currentPinInput.length));
      lockPinDisplay.textContent = masked + pad;
    }
  }

  function checkSmartLockPin() {
    const validPins = [
      state.lastGeneratedPin,
      "686899", "123888", "779922", "998833", "369852"
    ];

    if (validPins.includes(state.currentPinInput)) {
      playSuccessTone();
      lockStatusBadge.textContent = "● ĐÃ MỞ CHỐT (UNLOCKED)";
      lockStatusBadge.style.color = "#34D399";
      lockFeedback.className = "lock-feedback-status success";
      lockFeedback.innerHTML = `
        <div style="font-size: 15px; margin-bottom: 4px;">✅ MỞ KHÓA THÀNH CÔNG TRONG 2 GIÂY!</div>
        <div style="font-size: 12.5px; line-height: 1.5; color: #E2E8F0;">
          • Cửa phòng đã mở chốt an toàn.<br>
          • Cảm biến IoT tự động kích hoạt: <strong>Đèn ngủ ánh vàng & Điều hòa 24°C đã bật</strong> mát mẻ chào đón bạn.<br>
          • Mật khẩu Wi-Fi Mesh: <strong>KimLongEcoStay@2026</strong>.<br>
          • Quý khách có thể quét mã QR trên bàn để bắt đầu Tour Audio Guide và gọi món nóng sốt!
        </div>
      `;
    } else {
      playErrorTone();
      lockStatusBadge.textContent = "● TỪ CHỐI TRUY CẬP (ACCESS DENIED)";
      lockStatusBadge.style.color = "#F87171";
      lockFeedback.className = "lock-feedback-status error";
      lockFeedback.innerHTML = `
        ❌ MÃ PIN KHÔNG CHÍNH XÁC!<br>
        <small style="color: #CBD5E1;">Vui lòng kiểm tra lại tin nhắn Zalo/SMS hoặc bấm vào nút PIN mẫu bên cạnh để thử nghiệm.</small>
      `;
    }
  }

  keypadBtns.forEach(btn => {
    btn.addEventListener("click", () => {
      const key = btn.dataset.key;
      playKeyBeep(key === "#" ? 880 : (key === "C" ? 400 : 550), 0.06);

      if (key === "C") {
        state.currentPinInput = "";
        lockFeedback.className = "lock-feedback-status";
        lockFeedback.textContent = "Trạng thái: Đang chờ nhập mã số PIN 6 chữ số...";
        lockStatusBadge.textContent = "● SẴN SÀNG NHẬN PHÒNG (READY)";
        lockStatusBadge.style.color = "#38BDF8";
        updateLockDisplay();
      } else if (key === "#") {
        if (state.currentPinInput.length === 6) {
          checkSmartLockPin();
        } else {
          playErrorTone();
          alert("Vui lòng nhập đủ 6 chữ số mã PIN trước khi bấm phím # mở cửa.");
        }
      } else {
        if (state.currentPinInput.length < 6) {
          state.currentPinInput += key;
          updateLockDisplay();
          if (state.currentPinInput.length === 6) {
            setTimeout(checkSmartLockPin, 200);
          }
        }
      }
    });
  });

  // =========================================================================
  // 4. PHÂN HỆ 3: GPS AUDIO GUIDE (SMART HERITAGE TOUR)
  // =========================================================================
  const spotsContainer = document.getElementById("spots-list-container");
  const audioThumb = document.getElementById("audio-spot-thumb");
  const audioSpotName = document.getElementById("audio-spot-name");
  const audioSpotDist = document.getElementById("audio-spot-distance");
  const audioScriptBox = document.getElementById("audio-script-box");
  const btnPlayAudio = document.getElementById("btn-play-audio");
  const audioProgressFill = document.getElementById("audio-progress-fill");
  const audioCurrentTime = document.getElementById("audio-current-time");
  const audioTotalDuration = document.getElementById("audio-total-duration");
  const btnSimulateBike = document.getElementById("btn-simulate-bike");
  const bikeGpsMarker = document.getElementById("bike-gps-marker");

  function renderHeritageSpots() {
    if (!spotsContainer || !window.ECOSTAY_DATA) return;
    const spots = window.ECOSTAY_DATA.heritageSpots;

    spotsContainer.innerHTML = spots.map((spot, idx) => `
      <div class="spot-item-card ${idx === state.activeSpotIndex ? 'active' : ''}" data-spot-idx="${idx}">
        <img src="${spot.image}" alt="${spot.name}" class="spot-item-img" loading="lazy">
        <div class="spot-item-details">
          <div class="spot-item-name">${spot.name}</div>
          <div class="spot-item-distance">📍 ${spot.distance} • ⏱️ ${spot.audioDuration}</div>
        </div>
        <span style="font-size: 18px; color: var(--c-primary);">🔊</span>
      </div>
    `).join('');

    document.querySelectorAll(".spot-item-card").forEach(item => {
      item.addEventListener("click", () => {
        const idx = parseInt(item.dataset.spotIdx, 10);
        selectHeritageSpot(idx);
      });
    });
  }

  function selectHeritageSpot(index, autoPlay = false) {
    state.activeSpotIndex = index;
    const spots = window.ECOSTAY_DATA.heritageSpots;
    const spot = spots[index];

    document.querySelectorAll(".spot-item-card").forEach((c, idx) => {
      c.classList.toggle("active", idx === index);
    });

    audioThumb.src = spot.image;
    audioSpotName.textContent = spot.name;
    audioSpotDist.textContent = `📍 ${spot.distance} • Phân loại: ${spot.category}`;
    audioTotalDuration.textContent = spot.audioDuration;

    updateSpotNarrationView();
    stopAudio();

    // Di chuyển marker xe đạp GPS trên bản đồ
    moveBikeMarkerToSpot(index);

    if (autoPlay) {
      startAudio();
    }
  }

  function updateSpotNarrationView() {
    if (!window.ECOSTAY_DATA) return;
    const spot = window.ECOSTAY_DATA.heritageSpots[state.activeSpotIndex];
    const text = spot.narrations[state.currentLang] || spot.narrations.vi;
    if (audioScriptBox) {
      audioScriptBox.textContent = `"${text}"`;
    }
  }

  // Tùy chọn đổi ngôn ngữ Audio Guide
  document.querySelectorAll(".btn-audio-lang").forEach(btn => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".btn-audio-lang").forEach(b => b.classList.remove("active"));
      btn.classList.add("active");
      state.currentLang = btn.dataset.lang;
      updateSpotNarrationView();
      if (state.isPlayingAudio) {
        stopAudio();
        startAudio();
      }
    });
  });

  // PHÁT THUYẾT MINH BẰNG WEB SPEECH API (TEXT-TO-SPEECH THỰC TẾ)
  function startAudio() {
    state.isPlayingAudio = true;
    btnPlayAudio.innerHTML = "❚❚";

    const spot = window.ECOSTAY_DATA.heritageSpots[state.activeSpotIndex];
    const text = spot.narrations[state.currentLang] || spot.narrations.vi;

    // Giả lập thanh thời gian tiến độ
    let elapsed = 0;
    const totalEst = 35; // giả lập 35 giây
    clearInterval(state.audioInterval);
    state.audioInterval = setInterval(() => {
      elapsed += 0.5;
      const pct = Math.min(100, (elapsed / totalEst) * 100);
      audioProgressFill.style.width = pct + "%";
      const mins = Math.floor(elapsed / 60);
      const secs = Math.floor(elapsed % 60);
      audioCurrentTime.textContent = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
      if (elapsed >= totalEst) {
        stopAudio();
      }
    }, 500);

    // Kích hoạt Text-to-Speech nếu trình duyệt hỗ trợ
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      if (state.currentLang === "en") {
        utterance.lang = "en-US";
      } else if (state.currentLang === "fr") {
        utterance.lang = "fr-FR";
      } else {
        utterance.lang = "vi-VN";
      }
      utterance.rate = 0.95; // Tốc độ ấm áp, truyền cảm
      utterance.onend = () => {
        stopAudio();
      };
      state.speechUtterance = utterance;
      window.speechSynthesis.speak(utterance);
    }
  }

  function stopAudio() {
    state.isPlayingAudio = false;
    btnPlayAudio.innerHTML = "▶";
    clearInterval(state.audioInterval);
    if ('speechSynthesis' in window) {
      window.speechSynthesis.cancel();
    }
  }

  if (btnPlayAudio) {
    btnPlayAudio.addEventListener("click", () => {
      if (state.isPlayingAudio) {
        stopAudio();
      } else {
        startAudio();
      }
    });
  }

  // Tọa độ các điểm trên bản đồ SVG (để di chuyển xe đạp)
  const mapCoordinates = [
    { x: 380, y: 150 }, // 0: Chùa Thiên Mụ
    { x: 480, y: 190 }, // 1: Nhà vườn An Hiên
    { x: 260, y: 130 }, // 2: Văn Miếu
    { x: 560, y: 220 }, // 3: Bến thuyền Kim Long
    { x: 520, y: 260 }, // 4: Lò bánh Huyền Anh
    { x: 420, y: 290 }, // 5: Đồng lúa sinh thái
    { x: 360, y: 340 }, // 6: Vườn bưởi thanh trà
    { x: 120, y: 90 },  // 7: Chùa Huyền Không
    { x: 440, y: 380 }, // 8: Làng Hương Thủy Xuân
    { x: 620, y: 170 }, // 9: Đại Nội Huế
    { x: 710, y: 130 }, // 10: Làng Tranh Làng Sình
    { x: 740, y: 160 }, // 11: Làng Hoa Giấy Thanh Tiên
    { x: 540, y: 240 }, // 12: Làng Đúc Đồng Phường Đúc
    { x: 670, y: 190 }, // 13: Bún Bò Huế Mụ Rơi
    { x: 650, y: 230 }, // 14: Bánh Bèo Bà Đỏ
    { x: 720, y: 250 }, // 15: Cơm Hến Hoa Đông (Cồn Hến)
    { x: 610, y: 280 }, // 16: Chè Hẻm Cố Đô
    { x: 590, y: 310 }, // 17: Cà Phê Muối Xứ Huế
    { x: 390, y: 420 }, // 18: Đồi Vọng Cảnh
    { x: 640, y: 220 }, // 19: Cầu Trường Tiền
    { x: 650, y: 200 }, // 20: Chợ Đông Ba
    { x: 450, y: 390 }, // 21: Bánh Ép Chị Huệ
    { x: 625, y: 185 }  // 22: Bánh Khoái Lạc Thiện
  ];

  function moveBikeMarkerToSpot(spotIndex) {
    if (!bikeGpsMarker) return;
    const coord = mapCoordinates[spotIndex] || { x: 420, y: 290 };
    bikeGpsMarker.setAttribute("transform", `translate(${coord.x}, ${coord.y})`);
  }

  // Nút "Mô phỏng Đạp xe dạo chơi": Tự động di chuyển qua các điểm di tích
  if (btnSimulateBike) {
    btnSimulateBike.addEventListener("click", () => {
      if (state.isBikingSimulation) {
        // Dừng mô phỏng
        state.isBikingSimulation = false;
        clearInterval(state.bikingInterval);
        btnSimulateBike.textContent = "🚲 Mô phỏng Dạo Xe Đạp";
        btnSimulateBike.style.background = "var(--c-gold)";
        stopAudio();
      } else {
        // Bắt đầu mô phỏng
        state.isBikingSimulation = true;
        btnSimulateBike.textContent = "⏹ Đang dạo mát (Dừng lại)";
        btnSimulateBike.style.background = "#34D399";
        let cur = state.activeSpotIndex;

        // Tự động nhảy điểm và phát audio
        selectHeritageSpot(cur, true);

        state.bikingInterval = setInterval(() => {
          cur = (cur + 1) % window.ECOSTAY_DATA.heritageSpots.length;
          selectHeritageSpot(cur, true);
        }, 12000); // 12 giây mỗi điểm
      }
    });
  }

  // Tương tác click trực tiếp lên các điểm pin trên bản đồ SVG
  document.querySelectorAll(".map-spot-pin").forEach(pin => {
    pin.addEventListener("click", () => {
      const idx = parseInt(pin.dataset.spotIdx, 10);
      selectHeritageSpot(idx, true);
    });
  });

  // =========================================================================
  // 5. PHÂN HỆ 4: ẨM THỰC 15 PHÚT & NÔNG SẢN BAO TIÊU
  // =========================================================================
  const foodMenuContainer = document.getElementById("food-menu-grid");
  const cartItemsList = document.getElementById("cart-items-list");
  const cartTotalAmount = document.getElementById("cart-total-amount");
  const btnPlaceOrder = document.getElementById("btn-place-order");
  const deliveryTracker = document.getElementById("delivery-tracker");
  const timerNum = document.getElementById("countdown-timer-num");

  function renderFoodMenu() {
    if (!foodMenuContainer || !window.ECOSTAY_DATA) return;
    const menu = window.ECOSTAY_DATA.foodAndFarmMenu;

    foodMenuContainer.innerHTML = menu.map(item => `
      <div class="menu-item-card" data-food-id="${item.id}">
        <div class="menu-img-wrap">
          <img src="${item.image}" alt="${item.name}" class="menu-item-img" loading="lazy">
          <span class="menu-time-tag">⏱️ ${item.time}</span>
        </div>
        <div class="menu-body">
          <span class="menu-type-badge">${item.type}</span>
          <h4 class="menu-name">${item.name}</h4>
          <span class="menu-partner">${item.partner}</span>
          <p class="menu-desc">${item.desc}</p>
          <div class="menu-bottom-row">
            <span class="menu-price">${item.price.toLocaleString('vi-VN')} đ</span>
            <button class="btn-add-food" data-food-id="${item.id}">
              + Thêm món
            </button>
          </div>
        </div>
      </div>
    `).join('');

    document.querySelectorAll(".btn-add-food").forEach(btn => {
      btn.addEventListener("click", () => {
        addToCart(btn.dataset.foodId);
      });
    });
  }

  function addToCart(foodId) {
    const item = window.ECOSTAY_DATA.foodAndFarmMenu.find(f => f.id === foodId);
    if (!item) return;

    const existing = state.cart.find(c => c.id === foodId);
    if (existing) {
      existing.qty += 1;
    } else {
      state.cart.push({ ...item, qty: 1 });
    }
    updateCartView();
  }

  function updateCartView() {
    if (!cartItemsList || !cartTotalAmount) return;

    if (state.cart.length === 0) {
      cartItemsList.innerHTML = `<p style="font-size: 13px; color: var(--c-text-muted); text-align: center; padding: 16px 0;">Chưa có món nào được chọn.</p>`;
      cartTotalAmount.textContent = "0 đ";
      if (btnPlaceOrder) btnPlaceOrder.disabled = true;
      return;
    }

    if (btnPlaceOrder) btnPlaceOrder.disabled = false;

    let total = 0;
    cartItemsList.innerHTML = state.cart.map(item => {
      const subtotal = item.price * item.qty;
      total += subtotal;
      return `
        <div class="cart-item-row">
          <div>
            <span class="cart-item-qty">${item.qty}x</span>
            <span>${item.name}</span>
          </div>
          <span style="font-weight: 700;">${subtotal.toLocaleString('vi-VN')} đ</span>
        </div>
      `;
    }).join('');

    cartTotalAmount.textContent = total.toLocaleString('vi-VN') + " đ";
  }

  if (btnPlaceOrder) {
    btnPlaceOrder.addEventListener("click", () => {
      if (state.cart.length === 0) return;

      deliveryTracker.classList.add("active");
      state.deliverySecondsLeft = 900; // 15:00
      clearInterval(state.deliveryCountdownInterval);

      state.deliveryCountdownInterval = setInterval(() => {
        state.deliverySecondsLeft -= 1;
        const mins = Math.floor(state.deliverySecondsLeft / 60);
        const secs = state.deliverySecondsLeft % 60;
        timerNum.textContent = `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;

        // Cập nhật trạng thái từng bước
        if (state.deliverySecondsLeft > 600) {
          updateDelStep(1);
        } else if (state.deliverySecondsLeft > 180) {
          updateDelStep(2);
        } else if (state.deliverySecondsLeft > 0) {
          updateDelStep(3);
        } else {
          updateDelStep(4);
          clearInterval(state.deliveryCountdownInterval);
          alert("🛎️ MÓN ĐẶC SẢN ĐÃ ĐƯỢC GIAO TỚI BÀN CỦA BẠN!\nChúc quý khách ngon miệng trong không gian đồng lúa Kim Long!");
        }
      }, 1000);

      alert(
        "🛎️ ĐÃ GỬI ĐƠN HÀNG THÀNH CÔNG!\n" +
        "Đơn đã được tự động chuyển đến Lò Bánh Ướt Kim Long cách homestay 250m.\n" +
        "Bánh ướt thịt nướng than hoa sẽ được đóng hộp kraft và giao tận bàn trong 15 phút!"
      );
    });
  }

  function updateDelStep(activeStep) {
    const steps = document.querySelectorAll(".del-step");
    steps.forEach((step, idx) => {
      const stepIdx = idx + 1;
      if (stepIdx < activeStep) {
        step.className = "del-step done";
      } else if (stepIdx === activeStep) {
        step.className = "del-step active";
      } else {
        step.className = "del-step";
      }
    });
  }

  // =========================================================================
  // 6. PHÂN HỆ 5: CỔNG CHỦ VƯỜN & ADMIN DASHBOARD
  // =========================================================================
  const farmerTableBody = document.getElementById("farmer-lease-table-body");
  const sopListContainer = document.getElementById("sop-checklist-container");

  function renderAdminData() {
    if (!window.ECOSTAY_DATA) return;
    const { farmerPartners, sopHousekeeping8Steps } = window.ECOSTAY_DATA.operationalMetrics;

    if (farmerTableBody) {
      farmerTableBody.innerHTML = farmerPartners.map(p => `
        <tr>
          <td>
            <strong>${p.farmerName}</strong><br>
            <small style="color: var(--c-text-muted);">${p.house}</small>
          </td>
          <td>${p.landArea}</td>
          <td><span style="background: #DCFCE7; color: #15803D; padding: 4px 8px; border-radius: 4px; font-weight: 700; font-size: 11.5px;">10.000.000 đ/tháng</span></td>
          <td><strong>${p.doubleIncome}</strong></td>
          <td><small style="color: #0F172A;">${p.agriSupport}</small></td>
        </tr>
      `).join('');
    }

    if (sopListContainer) {
      sopListContainer.innerHTML = sopHousekeeping8Steps.map(step => `
        <label class="sop-check-item">
          <input type="checkbox" checked>
          <span><strong>Bước ${step.step}:</strong> ${step.title}</span>
        </label>
      `).join('');
    }
  }

  // =========================================================================
  // HỘP THOẠI XÁC NHẬN ĐẶT PHÒNG & MÃ PIN GỬI VỀ EMAIL THẬT
  // =========================================================================
  function showBookingSuccessModal(data) {
    let modal = document.getElementById("booking-success-popup-modal");
    if (!modal) {
      modal = document.createElement("div");
      modal.id = "booking-success-popup-modal";
      modal.style.cssText = `
        position: fixed; inset: 0; background: rgba(3, 31, 23, 0.8);
        backdrop-filter: blur(8px); z-index: 999999; display: flex;
        align-items: center; justify-content: center; padding: 20px;
      `;
      document.body.appendChild(modal);
    }

    const mailtoUrl = `mailto:${encodeURIComponent(data.guestEmail)}?subject=${encodeURIComponent(`[Kim Long EcoStay] Xác Nhận Đặt Phòng & Mã PIN SmartLock #${data.bookingCode}`)}&body=${encodeURIComponent(
`Kính gửi quý khách ${data.guestName},

Kim Long EcoStay xin chân thành cảm ơn quý khách đã tin tưởng đặt phòng nghỉ dưỡng tại Nhà Vườn Sinh Thái Kim Long - Cố Đô Huế!

THÔNG TIN ĐƠN ĐẶT PHÒNG:
• Mã đặt phòng: #${data.bookingCode}
• Căn phòng: ${data.roomName}
• Thời gian lưu trú: Từ ${data.checkinDate} đến ${data.checkoutDate}
• Khách hàng: ${data.guestName}
• Số điện thoại: ${data.guestPhone}

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔑 MÃ PIN SMARTLOCK 24/7 NHẬN PHÒNG TỰ ĐỘNG: 【 ${data.generatedPin} 】
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
HƯỚNG DẪN MỞ PHÒNG TRONG 10 GIÂY:
1. Khi đến trước cửa phòng ${data.roomName}, quý khách chạm tay vào bàn phím số để sáng đèn.
2. Nhập chính xác 6 chữ số: ${data.generatedPin}
3. Bấm phím [#] để mở chốt cửa an toàn.
Hệ thống cảm biến IoT sẽ tự động bật đèn ngủ ấm áp và điều hòa nhiệt độ phòng đón chào quý khách!

Mật khẩu Wi-Fi Mesh toàn homestay: KimLongEcoStay@2026
Hotline hỗ trợ 24/7: 0905.888.666

Kính chúc quý khách có một kỳ nghỉ an yên và trọn vẹn tại Cố Đô Huế!
Kim Long EcoStay Team`
    )}`;

    modal.innerHTML = `
      <div style="background: #ffffff; border-radius: 20px; max-width: 580px; width: 100%; overflow: hidden; box-shadow: 0 25px 60px rgba(0,0,0,0.35); font-family: 'Plus Jakarta Sans', sans-serif;">
        <div style="background: linear-gradient(135deg, #073B2B, #0E5E45); color: #fff; padding: 24px 28px; text-align: center;">
          <div style="display: inline-block; background: #D4AF37; color: #031F17; font-weight: 800; font-size: 11px; padding: 4px 12px; border-radius: 20px; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;">
            ✓ Xác Nhận Đặt Phòng Thành Công
          </div>
          <h2 style="font-family: 'Playfair Display', Georgia, serif; margin: 0 0 6px 0; font-size: 24px; color: #fff;">
            Chào Mừng Đến Kim Long EcoStay
          </h2>
          <p style="margin: 0; color: rgba(255,255,255,0.85); font-size: 13.5px;">
            Đơn phòng <strong>#${data.bookingCode}</strong> đã được xác nhận giữ chỗ thành công
          </p>
        </div>

        <div style="padding: 24px 28px;">
          <!-- Hộp mã PIN to nổi bật -->
          <div style="background: linear-gradient(135deg, #031F17, #073B2B); border-radius: 14px; padding: 18px 20px; text-align: center; color: #fff; margin-bottom: 20px; border: 1px solid rgba(212,175,55,0.4);">
            <div style="font-size: 12px; text-transform: uppercase; letter-spacing: 1px; color: #D4AF37; font-weight: 700; margin-bottom: 6px;">
              🔑 MÃ PIN SMARTLOCK MỞ PHÒNG 24/7 (CHECK-IN 10 GIÂY)
            </div>
            <div style="font-size: 38px; font-weight: 900; letter-spacing: 8px; color: #34D399; font-family: monospace;">
              ${data.generatedPin}
            </div>
            <div style="font-size: 12px; color: rgba(255,255,255,0.8); margin-top: 6px;">
              Đến trước cửa phòng, bấm <strong>${data.generatedPin}</strong> rồi bấm phím <strong>#</strong> để mở khóa
            </div>
          </div>

          <!-- Trạng thái email thật -->
          <div style="background: #F0FDF4; border: 1px solid #BBF7D0; border-radius: 12px; padding: 14px 16px; margin-bottom: 20px; display: flex; align-items: flex-start; gap: 12px;">
            <div style="font-size: 24px;">📧</div>
            <div style="font-size: 13px; color: #166534; line-height: 1.5;">
              <strong style="color: #15803D;">Đã gửi thông tin & mã PIN tới email thật:</strong><br>
              <span style="font-family: monospace; font-size: 14px; font-weight: 700; color: #0F172A;">${data.guestEmail || 'Email khách hàng'}</span><br>
              <span style="font-size: 12px; color: #15803D;">Vui lòng kiểm tra Hộp thư đến (Inbox) hoặc Thư rác (Spam) trong hòm thư của bạn.</span>
            </div>
          </div>

          <!-- Chi tiết phòng -->
          <div style="background: #F8FAF8; border-radius: 10px; padding: 12px 16px; margin-bottom: 22px; font-size: 13px; color: #334155; line-height: 1.6;">
            <div>• <strong>Căn phòng:</strong> ${data.roomName}</div>
            <div>• <strong>Khách lưu trú:</strong> ${data.guestName} (${data.guestPhone})</div>
            <div>• <strong>Lịch trình:</strong> ${data.checkinDate} → ${data.checkoutDate}</div>
          </div>

          <!-- Các nút hành động -->
          <div style="display: flex; flex-direction: column; gap: 10px;">
            <a href="${mailtoUrl}" target="_blank" style="background: linear-gradient(135deg, #D97706, #B45309); color: #fff; text-decoration: none; text-align: center; padding: 12px; border-radius: 10px; font-weight: 800; font-size: 14px; display: flex; align-items: center; justify-content: center; gap: 8px; box-shadow: 0 4px 12px rgba(217,119,6,0.25);">
              ✉️ Mở Hòm Thư Email Nhận Mã PIN Ngay
            </a>
            <div style="display: flex; gap: 10px;">
              <button id="btn-popup-try-smartlock" style="flex: 1; background: #073B2B; color: #fff; border: none; padding: 11px; border-radius: 10px; font-weight: 700; font-size: 13px; cursor: pointer;">
                🔑 Thử Mở Khóa SmartLock 10s
              </button>
              <button id="btn-popup-close-modal" style="flex: 1; background: #E2E8F0; color: #334155; border: none; padding: 11px; border-radius: 10px; font-weight: 700; font-size: 13px; cursor: pointer;">
                Đóng Cửa Sổ
              </button>
            </div>
          </div>
        </div>
      </div>
    `;

    document.getElementById("btn-popup-try-smartlock").onclick = () => {
      modal.remove();
      switchTab("smartlock");
      state.currentPinInput = data.generatedPin;
      updateLockDisplay();
      setTimeout(checkSmartLockPin, 400);
    };

    document.getElementById("btn-popup-close-modal").onclick = () => {
      modal.remove();
      switchTab("smartlock");
      state.currentPinInput = "";
      updateLockDisplay();
    };
  }

  // =========================================================================
  // KHỞI ĐỘNG CÁC PHÂN HỆ
  // =========================================================================
  renderRooms();
  updatePresetPins();
  renderHeritageSpots();
  renderFoodMenu();
  renderAdminData();
  selectHeritageSpot(0);
});
