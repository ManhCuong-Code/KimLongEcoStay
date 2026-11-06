@echo off
chcp 65001 >nul
title [KIM LONG ECOSTAY] - DONG GOI MA NGUON SAN SANG DUA LEN CLOUD HOSTING 24/7
color 0B

echo =========================================================================
echo    KIM LONG ECOSTAY - NGHI DUONG NHA VUON CO DO HUE
echo    CONG CU DONG GOI TU DONG DUA LEN CLOUD (CHAY 24/7 KHI TAT MAY)
echo =========================================================================
echo.

set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo [1/3] Dang khoi phuc cac goi thu vien NuGet...
dotnet restore KimLongEcoStay.sln --nologo
if %ERRORLEVEL% NEQ 0 (
    echo [!] Loi khi khoi phuc goi NuGet.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/3] Dang bien dich toan bo he thong (Release)...
dotnet build KimLongEcoStay.sln -c Release --no-restore --nologo
if %ERRORLEVEL% NEQ 0 (
    echo [!] Loi bien dich.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/3] Dang xuat ban (Publish) Website ra thu muc publish_cloud...
if exist "publish_cloud" rd /s /q "publish_cloud"
dotnet publish "KimLongEcoStay.Web/KimLongEcoStay.Web.csproj" -c Release -o "publish_cloud" --no-build --nologo

if %ERRORLEVEL% EQU 0 (
    echo.
    echo =========================================================================
    echo    DONG GOI THANH CONG!
    echo =========================================================================
    echo Toan bo tep website da nam tai thu muc:
    echo %SCRIPT_DIR%publish_cloud
    echo.
    echo HUONG DAN DUA LEN CLOUD DE CHAY 24/7:
    echo 1. Dang ky tai khoan mien phi tai: https://www.monsterasp.net/
    echo 2. Nen toan bo file ben trong thu muc 'publish_cloud' thanh file .zip
    echo 3. Upload file .zip len muc File Manager cua Hosting.
    echo 4. Website se chay online 24/7 ke ca khi ban tat may tinh!
    echo =========================================================================
) else (
    echo [!] Dong goi that bai.
)

echo.
pause
