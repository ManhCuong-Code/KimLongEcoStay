#!/usr/bin/env bash

echo "=========================================================================="
echo "  🌿 KIM LONG ECOSTAY - THIET LAP MOI TRUONG GITHUB CODESPACES"
echo "=========================================================================="

chmod +x start.sh 2>/dev/null || true

# 1. Kiem tra .NET SDK
CURRENT_DOTNET=$(dotnet --version 2>/dev/null || echo "none")
echo "[1/4] Kiem tra .NET SDK: $CURRENT_DOTNET"
if [[ "$CURRENT_DOTNET" != 10.* ]]; then
    echo "Dang cai dat bo sung .NET 10 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0 --install-dir /home/codespace/.dotnet 2>/dev/null || true
    export DOTNET_ROOT=/home/codespace/.dotnet
    export PATH=$DOTNET_ROOT:$PATH
    echo 'export DOTNET_ROOT=/home/codespace/.dotnet' >> ~/.bashrc 2>/dev/null || true
    echo 'export PATH=$DOTNET_ROOT:$PATH' >> ~/.bashrc 2>/dev/null || true
fi

# 2. Khoi dong SQL Server qua Docker neu kha dung
if command -v docker &> /dev/null; then
    echo "[2/4] Khoi dong Microsoft SQL Server Container..."
    if ! docker ps -a --format '{{.Names}}' 2>/dev/null | grep -q "^mssql$"; then
        docker run -d \
          --name mssql \
          -e "ACCEPT_EULA=Y" \
          -e "MSSQL_SA_PASSWORD=KimLongEcoStay@2026!" \
          -e "MSSQL_PID=Express" \
          -p 1433:1433 \
          --restart unless-stopped \
          mcr.microsoft.com/mssql/server:2022-latest 2>/dev/null || true
    else
        docker start mssql 2>/dev/null || true
    fi

    # 3. Cho SQL Server san sang va nap CSDL
    echo "[3/4] Dang doi SQL Server san sang de khoi tao CSDL..."
    SQLCMD_CMD="/opt/mssql-tools18/bin/sqlcmd"
    for i in {1..25}; do
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
        docker exec -i mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -i /dev/stdin < database/schema.sql 2>/dev/null || true
    fi

    echo "Dang nap migration HR & Tai chinh..."
    if [ -f "database/migration_hr_payroll_finance.sql" ]; then
        docker exec -i mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -i /dev/stdin < database/migration_hr_payroll_finance.sql 2>/dev/null || true
    fi

    echo "Dang nap du lieu UTF-8..."
    if [ -f "database/fix_encoding.sql" ]; then
        docker exec -i mssql $SQLCMD_CMD -S localhost -U sa -P "KimLongEcoStay@2026!" -C -i /dev/stdin < database/fix_encoding.sql 2>/dev/null || true
    fi
else
    echo "[2/4] Docker khong kha dung trong container nay, bo qua buoc khoi tao CSDL Docker."
fi

# 4. Bien dich solution
echo "[4/4] Bien dich du an Blazor Kim Long EcoStay..."
dotnet build project/KimLongEcoStay/KimLongEcoStay.Web/KimLongEcoStay.Web.csproj 2>/dev/null || true

echo ""
echo "=========================================================================="
echo "  🎉 THIET LAP HOAN TAT! CHAY WEB: ./start.sh hoac bash start.sh"
echo "=========================================================================="
