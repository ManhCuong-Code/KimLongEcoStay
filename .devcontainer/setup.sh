#!/usr/bin/env bash
set -e

echo "=========================================================================="
echo "  🌿 KIM LONG ECOSTAY - THIET LAP MOI TRUONG GITHUB CODESPACES"
echo "=========================================================================="

# 1. Kiem tra va cai dat .NET 10 SDK neu can
CURRENT_DOTNET=$(dotnet --version 2>/dev/null || echo "none")
echo "[1/4] Kiem tra .NET SDK hien tai: $CURRENT_DOTNET"
if [[ "$CURRENT_DOTNET" != 10.* ]]; then
    echo "Dang cai dat bo sung .NET 10 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --install-dir /home/vscode/.dotnet
    export DOTNET_ROOT=/home/vscode/.dotnet
    export PATH=$DOTNET_ROOT:$PATH
    echo 'export DOTNET_ROOT=/home/vscode/.dotnet' >> ~/.bashrc
    echo 'export PATH=$DOTNET_ROOT:$PATH' >> ~/.bashrc
fi

# 2. Khoi dong SQL Server 2022 qua Docker
echo "[2/4] Khoi dong Microsoft SQL Server Container..."
if ! docker ps -a --format '{{.Names}}' | grep -q "^mssql$"; then
    docker run -d \
      --name mssql \
      -e "ACCEPT_EULA=Y" \
      -e "MSSQL_SA_PASSWORD=KimLongEcoStay@2026!" \
      -e "MSSQL_PID=Express" \
      -p 1433:1433 \
      --restart unless-stopped \
      mcr.microsoft.com/mssql/server:2022-latest
else
    docker start mssql 2>/dev/null || true
fi

# 3. Cho SQL Server san sang va nap CSDL KimLongEcoStayDB
echo "[3/4] Dang doi SQL Server san sang de khoi tao CSDL..."
SQLCMD_CMD="/opt/mssql-tools18/bin/sqlcmd"
for i in {1..40}; do
    if docker exec mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -Q "SELECT 1" >/dev/null 2>&1; then
        echo "SQL Server da san sang!"
        break
    fi
    if docker exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "KimLongEcoStay@2026!" -Q "SELECT 1" >/dev/null 2>&1; then
        SQLCMD_CMD="/opt/mssql-tools/bin/sqlcmd"
        echo "SQL Server da san sang!"
        break
    fi
    sleep 2
done

echo "Dang nap schema database/schema.sql..."
if [ -f "database/schema.sql" ]; then
    docker exec -i mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -i /dev/stdin < database/schema.sql || true
fi

echo "Dang nap migration HR & Tai chinh database/migration_hr_payroll_finance.sql..."
if [ -f "database/migration_hr_payroll_finance.sql" ]; then
    docker exec -i mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -i /dev/stdin < database/migration_hr_payroll_finance.sql || true
fi

echo "Dang nap du lieu UTF-8 chuan database/fix_encoding.sql..."
if [ -f "database/fix_encoding.sql" ]; then
    docker exec -i mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -i /dev/stdin < database/fix_encoding.sql || true
fi

# 4. Bien dich solution
echo "[4/4] Bien dich du an Blazor Kim Long EcoStay..."
dotnet build project/KimLongEcoStay/KimLongEcoStay.Web/KimLongEcoStay.Web.csproj

chmod +x ./start.sh 2>/dev/null || true

echo ""
echo "=========================================================================="
echo "  🎉 THIET LAP HOAN TAT! DE CHAY WEB ONLINE TREN CODESPACES:"
echo "     Chay lenh: ./start.sh"
echo "     Hoac nhan F5 de chay Run & Debug"
echo "=========================================================================="
