@echo off
chcp 65001 >nul
title [KIM LONG ECOSTAY] - MO CONG SERVER RA INTERNET (PUBLIC HTTPS)
color 0A

echo =========================================================================
echo    🌿 KIM LONG ECOSTAY — NGHỈ DƯỠNG NHÀ VƯỜN CỐ ĐÔ & KHÓA SMARTLOCK IOT
echo    PHÁT MÁY CHỦ BLAZOR .NET 10 + DATABASE RA INTERNET CÔNG KHAI (HTTPS)
echo =========================================================================
echo.

cd /d "%~dp0"

echo [1/2] Đang kiểm tra máy chủ Localhost:5204...
curl -s http://localhost:5204/ >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Máy chủ 5204 chưa chạy. Đang tự động khởi động máy chủ .NET...
    start "KimLongEcoStay Server" cmd /k "dotnet run --project KimLongEcoStay.Web/KimLongEcoStay.Web.csproj --urls http://localhost:5204"
    timeout /t 5 /nobreak >nul
) else (
    echo Máy chủ Localhost:5204 đang hoạt động tốt!
)

echo.
echo [2/2] Đang kết nối đường hầm bảo mật HTTPS miễn phí ra toàn cầu...
echo.
echo -------------------------------------------------------------------------
echo HƯỚNG DẪN:
echo - Màn hình bên dưới sẽ hiển thị một đường dẫn HTTPS công khai.
echo - Bạn có thể copy đường dẫn đó để mở trên điện thoại, máy tính bảng hoặc
echo   gửi cho ban giám khảo để kiểm thử hệ thống trực tiếp từ xa!
echo -------------------------------------------------------------------------
echo.

ssh -o StrictHostKeyChecking=no -R 80:localhost:5204 nokey@localhost.run

pause
