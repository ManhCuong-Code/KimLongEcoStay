// Slide Presentation Controller & Interactive Demonstrations
document.addEventListener('DOMContentLoaded', () => {
  const slides = document.querySelectorAll('.slide');
  const dotsContainer = document.getElementById('slideDots');
  const currentSlideNum = document.getElementById('currentSlideNum');
  const totalSlidesNum = document.getElementById('totalSlidesNum');
  const prevBtn = document.getElementById('prevBtn');
  const nextBtn = document.getElementById('nextBtn');
  const fullscreenBtn = document.getElementById('fullscreenBtn');
  const notesBtn = document.getElementById('notesBtn');
  const printBtn = document.getElementById('printBtn');
  const notesDrawer = document.getElementById('notesDrawer');
  const notesContent = document.getElementById('notesContent');
  const closeNotesBtn = document.getElementById('closeNotesBtn');
  const themeToggleBtn = document.getElementById('themeToggleBtn');
  const themeIcon = document.getElementById('themeIcon');
  const themeText = document.getElementById('themeText');

  let currentSlide = 0;
  const totalSlides = slides.length;
  totalSlidesNum.textContent = totalSlides;

  // Theme Management (Default: Light Theme)
  let currentTheme = localStorage.getItem('kimlong_ecostay_theme') || localStorage.getItem('kimlong_ecostay_theme') || 'light';
  applyTheme(currentTheme);

  if (themeToggleBtn) {
    themeToggleBtn.addEventListener('click', () => {
      currentTheme = currentTheme === 'light' ? 'dark' : 'light';
      localStorage.setItem('kimlong_ecostay_theme', currentTheme);
      applyTheme(currentTheme);
    });
  }

  function applyTheme(theme) {
    if (theme === 'dark') {
      document.documentElement.setAttribute('data-theme', 'dark');
      if (themeIcon) themeIcon.textContent = '🌙';
      if (themeText) themeText.textContent = 'Tông Tối';
    } else {
      document.documentElement.removeAttribute('data-theme');
      if (themeIcon) themeIcon.textContent = '☀️';
      if (themeText) themeText.textContent = 'Tông Sáng';
    }
  }

  // Speaker notes content for each slide - Clear, natural, and persuasive
  const speakerNotes = [
    // Slide 1
    `<strong>[Trang 1 - Mở đầu & Giới thiệu dự án]:</strong><br>
    "Kính thưa Thầy/Cô và các bạn, hôm nay nhóm em xin trình bày về dự án: <em>'Hệ thống Đặt phòng Nghỉ dưỡng Sinh thái Kim Long EcoStay'</em>.<br><br>
    Lý do nhóm triển khai dự án này rất thực tế: Kim Long EcoStay tọa lạc tại vùng đất nhà vườn sinh thái Kim Long - Cố Đô Huế. Tuy nhiên, hiện nay hầu hết chủ nhà vườn đều phải phụ thuộc hoàn toàn vào các sàn như Booking.com hay Agoda. Cứ mỗi 100 triệu tiền phòng thu được, chủ nhà phải nộp cho sàn từ 15 đến 25 triệu tiền hoa hồng, chưa kể tiền phòng bị giam cả tháng.<br><br>
    Vì vậy, mục tiêu của dự án là xây dựng nền tảng Kim Long EcoStay đặt phòng trực tiếp, giúp chủ homestay tiết kiệm 100% tiền phí môi giới, tiền cọc về thẳng tài khoản và quản lý buồng phòng một cách tự động, thông minh."`,

    // Slide 2
    `<strong>[Trang 2 - Bối cảnh & Hai đối tượng sử dụng]:</strong><br>
    "Ở slide thứ hai, nhóm xin so sánh lợi ích thực tế và 2 nhóm người dùng của hệ thống:<br><br>
    • <em>Về lợi ích:</em> Khác với việc bán qua sàn OTA bị cắt phế cao và không biết thông tin khách, website riêng Kim Long EcoStay giúp chủ nhà giữ trọn 100% doanh thu, tiền cọc vào tài khoản tức thì và nắm được số điện thoại, Zalo của khách để tiếp đón chu đáo.<br><br>
    • <em>Về người dùng:</em> Hệ thống phục vụ 2 nhóm đối tượng:<br>
    1. <strong>Khách du lịch:</strong> Lên web tìm phòng theo ngày, chọn đặt nhiều phòng cùng lúc (ví dụ đặt 1 phòng vườn và 1 phòng nhà rường), thấy rõ giá từng đêm và nhận mã đặt phòng ngay.<br>
    2. <strong>Chủ homestay/Lễ tân:</strong> Dễ dàng đăng phòng, đổi giá ngày thường/cuối tuần, bấm nút Check-in khi khách tới và Check-out khi khách về, đồng thời xem biểu đồ phòng tháng này kín bao nhiêu %."`,

    // Slide 3
    `<strong>[Trang 3 - 6 Bảng dữ liệu & 3 Luật nghiệp vụ cốt lõi]:</strong><br>
    "Để hệ thống vận hành chính xác, nhóm thiết kế 6 bảng dữ liệu: Loại phòng, Phòng cụ thể, Bảng giá, Khách hàng, Đơn đặt và Từng đêm lưu trú.<br><br>
    Điểm cốt lõi của hệ thống nằm ở <strong>3 quy tắc nghiệp vụ</strong> được giải thích bằng ví dụ số rất trực quan:<br>
    1. <strong>Chống trùng phòng thông minh:</strong> Khách A ở từ 15 đến 18/10 (trả phòng lúc 12h ngày 18). Khách B muốn đặt từ 18 đến 20/10 (nhận phòng lúc 14h ngày 18). Hệ thống vẫn cho phép đặt bình thường vì khoảng giữa 12h-14h là thời gian dọn phòng, không bị đè lịch.<br>
    2. <strong>Tự tính tiền theo từng đêm:</strong> Ví dụ khách ở đêm Thứ Năm giá ngày thường là 680.000đ, đêm Thứ Sáu giá cuối tuần là 850.000đ. Hệ thống tự động cộng ra đúng 1.530.000đ (cọc 50% 765.000đ) mà không cần lễ tân bấm máy tính.<br>
    3. <strong>Luật hủy cọc 48 tiếng:</strong> Hủy trước 2 ngày hoàn tiền 100%. Hủy gấp trong 48 tiếng thì thu phí 1 đêm đầu tiên để bảo vệ quyền lợi chủ nhà."`,

    // Slide 4
    `<strong>[Trang 4 - Kỹ thuật SQL & Tính năng AI SmartPaste]:</strong><br>
    "Ở slide này, nhóm muốn nhấn mạnh 2 điểm sáng kỹ thuật:<br><br>
    • <strong>Thứ nhất là câu lệnh SQL tìm phòng trống:</strong> Chúng em tách riêng từng đêm khách ở thành bảng BookingNight. Nhờ đó, máy tính chỉ cần kiểm tra xem phòng đó đêm đó có ai ở chưa. Thuật toán chạy cực kỳ nhanh dưới 20ms, an toàn và đảm bảo 100% không bao giờ xảy ra lỗi 2 khách cùng được cấp chung 1 phòng.<br><br>
    • <strong>Thứ hai là tính năng AI SmartPaste:</strong> Thực tế người Việt mình đi du lịch rất hay vào Zalo hoặc Fanpage nhắn tin: <em>'Dạ chị ơi bên mình còn phòng vườn 2 người từ 20 đến 22/10 ko, mình tên Đăng 0912345678'</em>. Lễ tân trước đây phải mở phần mềm rồi gõ tay lại từng chữ rất lâu và dễ nhầm. Với AI SmartPaste, lễ tân chỉ cần bấm 1 nút dán đoạn tin nhắn vào là AI tự điền toàn bộ tên, SĐT, loại phòng, số ngày vào form trong đúng 1 giây!"`,

    // Slide 5
    `<strong>[Trang 5 - Kế hoạch 4 tuần & Cam kết kết quả]:</strong><br>
    "Cuối cùng là kế hoạch triển khai cụ thể trong 4 tuần (4 Sprints):<br><br>
    • <strong>Tuần 1:</strong> Khảo sát thực tế mô hình nhà vườn Kim Long EcoStay, thiết kế giao diện Figma và khởi tạo CSDL 6 bảng SQL (KimLongEcoStayDB).<br>
    • <strong>Tuần 2:</strong> Hoàn thiện trang Quản trị (Admin/Host): đăng buồng phòng, phân quyền lễ tân và cài đặt bảng giá linh hoạt ngày thường/cuối tuần.<br>
    • <strong>Tuần 3:</strong> Lập trình cổng đặt phòng trực tiếp cho khách, thuật toán chống trùng phòng 0% overbooking, tự tính tiền từng đêm và xử lý Check-in/Check-out.<br>
    • <strong>Tuần 4:</strong> Tích hợp trợ lý AI SmartPaste tin nhắn Zalo, vẽ biểu đồ công suất, kiểm thử 50 tình huống và đóng gói toàn bộ đồ án, slide bảo vệ.<br><br>
    Nhóm cam kết sản phẩm đạt 0% lỗi trùng phòng, tốc độ tìm phòng dưới 200ms và hoàn thành 100% đúng hạn. Em xin cảm ơn Thầy/Cô và các bạn đã lắng nghe!"`
  ];

  // Initialize Indicator Dots
  function initDots() {
    dotsContainer.innerHTML = '';
    slides.forEach((_, idx) => {
      const dot = document.createElement('div');
      dot.className = `slide-dot ${idx === currentSlide ? 'active' : ''}`;
      dot.title = `Chuyển tới Trang ${idx + 1}`;
      dot.addEventListener('click', () => goToSlide(idx));
      dotsContainer.appendChild(dot);
    });
  }

  // Update Slide UI
  function updateSlideUI() {
    slides.forEach((slide, idx) => {
      slide.classList.toggle('active', idx === currentSlide);
    });

    currentSlideNum.textContent = currentSlide + 1;

    // Update dots
    const dots = dotsContainer.querySelectorAll('.slide-dot');
    dots.forEach((dot, idx) => {
      dot.classList.toggle('active', idx === currentSlide);
    });

    // Update Speaker notes
    notesContent.innerHTML = speakerNotes[currentSlide] || 'Không có ghi chú cho trang này.';

    // Disable / Enable buttons
    prevBtn.disabled = currentSlide === 0;
    prevBtn.style.opacity = currentSlide === 0 ? '0.5' : '1';
    prevBtn.style.cursor = currentSlide === 0 ? 'not-allowed' : 'pointer';

    if (currentSlide === totalSlides - 1) {
      nextBtn.innerHTML = 'Hoàn tất 🎉';
    } else {
      nextBtn.innerHTML = 'Trang kế tiếp <span>→</span>';
    }
  }

  function goToSlide(index) {
    if (index >= 0 && index < totalSlides) {
      currentSlide = index;
      updateSlideUI();
    }
  }

  function nextSlide() {
    if (currentSlide < totalSlides - 1) {
      currentSlide++;
      updateSlideUI();
    }
  }

  function prevSlide() {
    if (currentSlide > 0) {
      currentSlide--;
      updateSlideUI();
    }
  }

  // Button Listeners
  prevBtn.addEventListener('click', prevSlide);
  nextBtn.addEventListener('click', () => {
    if (currentSlide === totalSlides - 1) {
      goToSlide(0);
    } else {
      nextSlide();
    }
  });

  // Keyboard navigation
  document.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowRight' || e.key === 'PageDown' || (e.key === ' ' && !e.target.matches('input, textarea'))) {
      e.preventDefault();
      nextSlide();
    } else if (e.key === 'ArrowLeft' || e.key === 'PageUp') {
      e.preventDefault();
      prevSlide();
    } else if (e.key === 'Home') {
      e.preventDefault();
      goToSlide(0);
    } else if (e.key === 'End') {
      e.preventDefault();
      goToSlide(totalSlides - 1);
    } else if (e.key === 'n' || e.key === 'N') {
      toggleNotes();
    }
  });

  // Fullscreen toggle
  fullscreenBtn.addEventListener('click', () => {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().catch(err => {
        console.warn(`Error attempting fullscreen: ${err.message}`);
      });
    } else {
      if (document.exitFullscreen) {
        document.exitFullscreen();
      }
    }
  });

  // Speaker notes drawer toggle
  function toggleNotes() {
    notesDrawer.classList.toggle('open');
  }

  notesBtn.addEventListener('click', toggleNotes);
  closeNotesBtn.addEventListener('click', () => {
    notesDrawer.classList.remove('open');
  });

  // Print / Export PDF
  printBtn.addEventListener('click', () => {
    window.print();
  });

  // ==========================================
  // INTERACTIVE AI SMARTPASTE SIMULATION (SLIDE 4)
  // ==========================================
  const sampleMessages = [
    {
      text: '"Chào Kim Long EcoStay, mình tên Nguyễn Hải Đăng (0912345678), muốn book 1 phòng Eco-Nest Ban Mai cho 2 người lớn từ ngày 20/10 đến 22/10 nhé!"',
      parsed: {
        name: 'Nguyễn Hải Đăng',
        phone: '0912345678',
        room: 'Eco-Nest Ban Mai (KLE-R101)',
        dates: '20/10 ➔ 22/10 (2 đêm)'
      }
    },
    {
      text: '"Dạ cho em hỏi bên mình còn phòng Garden Suite Cố Đô Thanh Trà không ạ? Em là Trần Thị Mai Phương, SĐT 0988776655. Em muốn đặt 2 đêm từ 15/10 đến 17/10."',
      parsed: {
        name: 'Trần Thị Mai Phương',
        phone: '0988776655',
        room: 'Garden Suite Cố Đô Thanh Trà (KLE-R201)',
        dates: '15/10 ➔ 17/10 (2 đêm)'
      }
    },
    {
      text: '"Alo bên mình ơi, book giúp anh Lê Hoàng Nam 0905112233 căn Family Eco-Villa Gắn Kết check-in 15/11 check-out 18/11 cho 4 khách nhé. Có xe đạp gia đình không?"',
      parsed: {
        name: 'Lê Hoàng Nam',
        phone: '0905112233',
        room: 'Family Eco-Villa Gắn Kết (KLE-R202)',
        dates: '15/11 ➔ 18/11 (3 đêm)'
      }
    }
  ];

  let currentSampleIdx = 0;
  const sampleBtn = document.getElementById('sampleBtn');
  const chatInputDemo = document.getElementById('chatInputDemo');
  const runAiDemoBtn = document.getElementById('runAiDemoBtn');
  const resName = document.getElementById('resName');
  const resPhone = document.getElementById('resPhone');
  const resRoom = document.getElementById('resRoom');
  const resDates = document.getElementById('resDates');
  const aiOutputForm = document.getElementById('aiOutputForm');

  if (sampleBtn && chatInputDemo && runAiDemoBtn) {
    sampleBtn.addEventListener('click', () => {
      currentSampleIdx = (currentSampleIdx + 1) % sampleMessages.length;
      chatInputDemo.textContent = sampleMessages[currentSampleIdx].text;
      
      // Reset output preview to blank
      resName.textContent = '---';
      resPhone.textContent = '---';
      resRoom.textContent = '---';
      resDates.textContent = '---';
      aiOutputForm.style.borderColor = 'rgba(255, 255, 255, 0.1)';
    });

    runAiDemoBtn.addEventListener('click', () => {
      runAiDemoBtn.disabled = true;
      runAiDemoBtn.innerHTML = '<span>⏳ Đang phân tích tin nhắn (AI NLP)...</span>';
      
      setTimeout(() => {
        const data = sampleMessages[currentSampleIdx].parsed;
        resName.textContent = data.name;
        resPhone.textContent = data.phone;
        resRoom.textContent = data.room;
        resDates.textContent = data.dates;

        aiOutputForm.style.borderColor = 'var(--garden-emerald)';
        aiOutputForm.style.boxShadow = '0 0 15px rgba(16, 185, 129, 0.25)';
        
        runAiDemoBtn.disabled = false;
        runAiDemoBtn.innerHTML = '<span>✔ Đã Bóc Tách Thành Công! Thử Tin Khác</span>';
      }, 500);
    });
  }

  // Initialize presentation
  initDots();
  updateSlideUI();
});
