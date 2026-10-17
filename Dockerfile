# =========================================================================
# DOCKERFILE CHO KIM LONG ECOSTAY (.NET 10 BLAZOR SERVER BACKEND)
# Phù hợp triển khai lên Railway, Render, Fly.io, Azure App Service, VPS Docker
# =========================================================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy toàn bộ solution và csproj để tối ưu layer cache
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

RUN dotnet restore "KimLongEcoStay.sln"

# Copy toàn bộ mã nguồn và build Release
COPY . .
WORKDIR "/src/KimLongEcoStay.Web"
RUN dotnet publish "KimLongEcoStay.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
EXPOSE 5204
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:5204
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["sh", "-c", "dotnet KimLongEcoStay.Web.dll --urls http://+:${PORT:-5204}"]
