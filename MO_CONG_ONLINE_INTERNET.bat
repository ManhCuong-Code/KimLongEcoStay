@echo off
chcp 65001 >nul
title [KIM LONG ECOSTAY] - PHAT SONG WEBSITE RA TOAN CAU (PUBLIC HTTPS)
color 0A

echo =========================================================================
echo    🌿 KIM LONG ECOSTAY — NGHỈ DƯỠNG NHÀ VƯỜN CỐ ĐÔ HUẾ
echo    HỆ THỐNG MỞ CỔNG HTTPS CÔNG KHAI RA INTERNET TOÀN CẦU (MIỄN PHÍ 100%%)
echo    Không cần thẻ Visa • Không cần tài khoản Azure • Dùng được ngay lập tức!
echo =========================================================================
echo.

cd /d "%~dp0"

echo [1/2] Đang kiểm tra dịch vụ máy chủ Backend (Port 5204)...
curl -s http://127.0.0.1:5204/api/health >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo [!] Máy chủ 5204 chưa chạy. Đang tự động khởi động dịch vụ ASP.NET Core Kestrel...
    start "KimLongEcoStay Backend Server" cmd /k "dotnet run --project KimLongEcoStay.Web/KimLongEcoStay.Web.csproj --urls http://127.0.0.1:5204"
    echo Đang chờ máy chủ sẵn sàng trong 6 giây...
    timeout /t 6 /nobreak >nul
) else (
    echo [OK] Máy chủ Backend (Port 5204) + SQL Server đang hoạt động tốt!
)

echo.
echo [2/2] Đang thiết lập đường hầm bảo mật SSL/HTTPS công khai ra Internet...
echo.
echo =========================================================================
echo HƯỚNG DẪN SỬ DỤNG:
echo 1. Nhìn dòng chữ có chứa liên kết https://xxxxxx.lhr.life xuất hiện bên dưới.
echo 2. Copy đường link HTTPS đó (hoặc quét mã QR trên điện thoại).
echo 3. Mở trên bất kỳ điện thoại, máy tính bảng hoặc máy tính nào ở xa!
echo 4. Đường link này có đầy đủ:
echo    - Trang chủ: https://xxxxxx.lhr.life/
echo    - Trang đăng nhập: https://xxxxxx.lhr.life/login/ (Tài khoản: admin / 123456)
echo    - Bảng điều hành Quản trị RBAC, Tra cứu khóa SmartLock, Trợ lý AI
echo -------------------------------------------------------------------------
echo Lưu ý: Giữ nguyên cửa sổ này để duy trì kết nối Internet.
echo =========================================================================
echo.

ssh -o StrictHostKeyChecking=no -R 80:127.0.0.1:5204 nokey@localhost.run

pause
