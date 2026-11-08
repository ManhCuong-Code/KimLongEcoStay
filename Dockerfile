# =========================================================================
# DOCKERFILE CHO KIM LONG ECOSTAY (.NET 10 BLAZOR SERVER + API BACKEND)
# Tương thích 100% Render, Railway, Fly.io, Azure App Service, Docker VPS
# =========================================================================

# Stage 1: Build, Test & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 1. Copy toàn bộ solution và csproj để tối ưu layer cache
COPY ["KimLongEcoStay.sln", "./"]
COPY ["KimLongEcoStay.CoreBusiness/KimLongEcoStay.CoreBusiness.csproj", "KimLongEcoStay.CoreBusiness/"]
COPY ["KimLongEcoStay.UseCases/KimLongEcoStay.UseCases.csproj", "KimLongEcoStay.UseCases/"]
COPY ["KimLongEcoStay.Web/KimLongEcoStay.Web.csproj", "KimLongEcoStay.Web/"]
COPY ["KimLongEcoStay.Web.Modules/KimLongEcoStay.Web.AdminPortal/KimLongEcoStay.Web.AdminPortal.csproj", "KimLongEcoStay.Web.Modules/KimLongEcoStay.Web.AdminPortal/"]
COPY ["KimLongEcoStay.Web.Modules/KimLongEcoStay.Web.Common/KimLongEcoStay.Web.Common.csproj", "KimLongEcoStay.Web.Modules/KimLongEcoStay.Web.Common/"]
COPY ["KimLongEcoStay.Web.Modules/KimLongEcoStay.Web.CustomerPortal/KimLongEcoStay.Web.CustomerPortal.csproj", "KimLongEcoStay.Web.Modules/KimLongEcoStay.Web.CustomerPortal/"]
COPY ["Plugins/KimLongEcoStay.DataStore.SQL.Dapper/KimLongEcoStay.DataStore.SQL.Dapper.csproj", "Plugins/KimLongEcoStay.DataStore.SQL.Dapper/"]
COPY ["Plugins/KimLongEcoStay.DataStore.HandCoded/KimLongEcoStay.DataStore.HandCoded.csproj", "Plugins/KimLongEcoStay.DataStore.HandCoded/"]
COPY ["Plugins/KimLongEcoStay.ShoppingCart.LocalStorage/KimLongEcoStay.ShoppingCart.LocalStorage.csproj", "Plugins/KimLongEcoStay.ShoppingCart.LocalStorage/"]
COPY ["Plugins/KimLongEcoStay.StateStore.DI/KimLongEcoStay.StateStore.DI.csproj", "Plugins/KimLongEcoStay.StateStore.DI/"]
COPY ["KimLongEcoStay.Tests/KimLongEcoStay.Tests.csproj", "KimLongEcoStay.Tests/"]

# 2. Khôi phục các gói thư viện NuGet
RUN dotnet restore "KimLongEcoStay.sln"

# 3. Copy toàn bộ mã nguồn
COPY . .

# 4. Kiểm thử chất lượng tự động (CI Quality Gate) trước khi đóng gói
RUN dotnet test "KimLongEcoStay.Tests/KimLongEcoStay.Tests.csproj" -c Release --no-restore

# 5. Xuất bản ứng dụng ở chế độ Release
WORKDIR "/src/KimLongEcoStay.Web"
RUN dotnet publish "KimLongEcoStay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Mở các cổng mạng: Render sử dụng cổng $PORT hoặc 10000; Azure dùng 8080/80; Local dùng 5204
EXPOSE 10000
EXPOSE 8080
EXPOSE 5204

ENV ASPNETCORE_URLS=http://+:10000;http://+:5204;http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KimLongEcoStay.Web.dll"]
