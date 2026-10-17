@echo off
chcp 65001 >nul
title KIM LONG ECOSTAY - KHOI CHAY WEB BLAZOR

echo ==========================================================================
echo           🌿 KIM LONG ECOSTAY - KHOI CHAY WEB BLAZOR C# .NET
echo ==========================================================================
echo.
echo [1/2] Dang khoi dong may chu Web Blazor tai http://localhost:5204 ...
echo       (De dung he thong, nhan to hop phim: Ctrl + C)
echo.

start "" "http://localhost:5204"

dotnet run --project "%~dp0KimLongEcoStay.Web\KimLongEcoStay.Web.csproj" --urls "http://localhost:5204"

pause
