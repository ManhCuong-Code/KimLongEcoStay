#!/usr/bin/env bash

echo "=========================================================================="
echo "  🌿 KIM LONG ECOSTAY - SERVER ONLINE TREN GITHUB CODESPACES"
echo "=========================================================================="

# 1. Kiem tra va khoi dong SQL Server container neu docker kha dung
if command -v docker &> /dev/null; then
    if docker ps 2>/dev/null | grep -q "mssql"; then
        echo "  [✓] Microsoft SQL Server dang chay san sang."
    else
        echo "  [i] Dang bat SQL Server Container..."
        docker start mssql 2>/dev/null || docker run -d --name mssql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=KimLongEcoStay@2026!" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest 2>/dev/null || echo "  [!] Docker dang ban, tiep tuc nap ung dung..."
        sleep 3
    fi
else
    echo "  [i] Docker khong co san trong shell nay, tiep tuc chay dotnet..."
fi

# 2. Thiet lap bien moi truong
export ConnectionStrings__KimLongEcoStayConnection="Server=localhost,1433;Database=KimLongEcoStayDB;User Id=sa;Password=KimLongEcoStay@2026!;TrustServerCertificate=True;"
export ASPNETCORE_URLS="http://0.0.0.0:5204"
export ASPNETCORE_ENVIRONMENT="Development"

echo "  [✓] Cong web: 5204 (Binding 0.0.0.0 - Ho tro Public link ngoai Internet)"
echo "  [✓] Co so du lieu: KimLongEcoStayDB (SQL Server 2022)"
echo "  [✓] Dang khoi chay Blazor Server..."
echo "--------------------------------------------------------------------------"
echo "  👉 Luu y: Mo tab 'PORTS' tren VS Code Codespaces -> Chuot phai cong 5204"
echo "     chuyen 'Port Visibility' thanh 'Public' de bat ky ai cung xem duoc!"
echo "=========================================================================="

dotnet run --project KimLongEcoStay.Web/KimLongEcoStay.Web.csproj --urls "http://0.0.0.0:5204"
