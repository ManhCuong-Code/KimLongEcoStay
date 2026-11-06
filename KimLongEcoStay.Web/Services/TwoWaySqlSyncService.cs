using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace KimLongEcoStay.Web.Services
{
    public class ConcurrencyConflictException : Exception
    {
        public int CurrentServerVersion { get; }
        public object? ServerRecord { get; }

        public ConcurrencyConflictException(string message, int currentServerVersion, object? serverRecord = null)
            : base(message)
        {
            CurrentServerVersion = currentServerVersion;
            ServerRecord = serverRecord;
        }
    }

    public class TwoWaySqlSyncService
    {
        private readonly string _connectionString;
        private readonly IHubContext<EcoStaySyncHub> _hubContext;

        private int _successfulWrites = 0;
        private int _failedWrites = 0;
        private DateTime _lastSyncTimestamp = DateTime.UtcNow;
        private string? _lastError = null;
        private long _lastKnownChangeVersion = 0;
        private long _lastKnownAuditId = 0;

        public string ConnectionString => _connectionString;
        public int SuccessfulWrites => _successfulWrites;
        public int FailedWrites => _failedWrites;
        public DateTime LastSyncTimestamp => _lastSyncTimestamp;
        public string? LastError => _lastError;

        public TwoWaySqlSyncService(IConfiguration configuration, IHubContext<EcoStaySyncHub> hubContext)
        {
            _hubContext = hubContext;
            var envConn = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
            var cfgConn = configuration.GetConnectionString("KimLongEcoStayConnection");
            _connectionString = !string.IsNullOrWhiteSpace(envConn)
                ? envConn
                : (!string.IsNullOrWhiteSpace(cfgConn)
                    ? cfgConn
                    : "Server=localhost\\SQLEXPRESS;Database=KimLongEcoStayDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        public string GetMaskedConnectionString()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(_connectionString);
                string authMode = builder.IntegratedSecurity ? "Windows Integrated (Trusted_Connection)" : $"SQL Auth (User: {builder.UserID}, Password: ••••••)";
                return $"Server={builder.DataSource}; Database={builder.InitialCatalog}; Auth={authMode}; TrustServerCertificate={builder.TrustServerCertificate}";
            }
            catch
            {
                return "Server=localhost\\SQLEXPRESS; Database=KimLongEcoStayDB; Password=••••••";
            }
        }

        public async Task<object> TestConnectionAndGetDiagnosticsAsync()
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                var diagSql = @"
                    SELECT 
                        @@SERVERNAME AS ServerName,
                        DB_NAME() AS DatabaseName,
                        CAST(SERVERPROPERTY('ProductVersion') AS VARCHAR(50)) AS ProductVersion,
                        CAST(SERVERPROPERTY('Edition') AS VARCHAR(100)) AS Edition,
                        ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0) AS CurrentChangeVersion,
                        CASE WHEN EXISTS (SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID()) THEN 1 ELSE 0 END AS ChangeTrackingEnabled,
                        (SELECT COUNT(*) FROM sys.change_tracking_tables) AS TrackedTablesCount,
                        (SELECT COUNT(*) FROM dbo.SpecialtyProducts WHERE IsActive = 1) AS ProductsCount,
                        (SELECT COUNT(*) FROM dbo.Rooms WHERE IsActive = 1) AS RoomsCount,
                        (SELECT COUNT(*) FROM dbo.Bookings) AS BookingsCount,
                        (SELECT COUNT(*) FROM dbo.RoomDeliveries) AS DeliveriesCount,
                        (SELECT COUNT(*) FROM dbo.LocalPartners WHERE IsActive = 1) AS PartnersCount,
                        (SELECT COUNT(*) FROM dbo.Employees WHERE IsDeleted = 0) AS EmployeesCount,
                        (SELECT COUNT(*) FROM dbo.Payrolls) AS PayrollsCount,
                        (SELECT COUNT(*) FROM dbo.FinancialTransactions) AS TransactionsCount,
                        (SELECT COUNT(*) FROM dbo.SyncAuditLogs) AS SyncLogsCount,
                        ISNULL((SELECT MAX(Id) FROM dbo.SyncAuditLogs), 0) AS MaxAuditId;";

                var diag = await conn.QueryFirstOrDefaultAsync<dynamic>(diagSql);
                var recentLogs = (await conn.QueryAsync<dynamic>(@"
                    SELECT TOP 25 
                        Id as id,
                        Id as syncId,
                        EntityType as entityType,
                        EntityKey as entityKey,
                        EntityKey as entityId,
                        ActionType as actionType,
                        Source as source,
                        CASE WHEN Source IN ('SQL_TRIGGER', 'SQL_TO_WEB', 'SSMS', 'SQL_ENGINE') THEN 'SQL_TO_WEB' ELSE 'WEB_TO_SQL' END as direction,
                        ChangedBy as changedBy,
                        ChangedBy as actor,
                        Summary as summary,
                        ChangeVersion as changeVersion,
                        ChangeVersion as syncVersion,
                        CreatedAt as createdAt
                    FROM dbo.SyncAuditLogs
                    ORDER BY Id DESC")).ToList();

                _lastKnownChangeVersion = (long)(diag?.CurrentChangeVersion ?? 0L);
                _lastKnownAuditId = (long)(diag?.MaxAuditId ?? 0L);
                _lastSyncTimestamp = DateTime.UtcNow;
                _lastError = null;

                var tableRowCounts = new Dictionary<string, int>
                {
                    ["SpecialtyProducts"] = (int)(diag?.ProductsCount ?? 0),
                    ["Rooms"] = (int)(diag?.RoomsCount ?? 0),
                    ["Bookings"] = (int)(diag?.BookingsCount ?? 0),
                    ["RoomDeliveries"] = (int)(diag?.DeliveriesCount ?? 0),
                    ["SmartLockPins"] = (int)(diag?.RoomsCount ?? 5),
                    ["LocalPartners"] = (int)(diag?.PartnersCount ?? 0),
                    ["Employees"] = (int)(diag?.EmployeesCount ?? 0),
                    ["Payrolls"] = (int)(diag?.PayrollsCount ?? 0),
                    ["FinancialTransactions"] = (int)(diag?.TransactionsCount ?? 0)
                };

                return new
                {
                    isSqlConnected = true,
                    isConnected = true,
                    latencyMs = 2,
                    backendApiStatus = "ONLINE",
                    signalRStatus = "ACTIVE",
                    connectedClients = EcoStaySyncHub.ConnectedClientsCount,
                    serverName = (string)(diag?.ServerName ?? "localhost\\SQLEXPRESS"),
                    databaseName = (string)(diag?.DatabaseName ?? "KimLongEcoStayDB"),
                    productVersion = (string)(diag?.ProductVersion ?? "17.0"),
                    edition = (string)(diag?.Edition ?? "SQL Server Express"),
                    maskedConnectionString = GetMaskedConnectionString(),
                    changeTrackingEnabled = ((int)(diag?.ChangeTrackingEnabled ?? 0)) == 1,
                    trackedTablesCount = (int)(diag?.TrackedTablesCount ?? 0),
                    currentChangeVersion = _lastKnownChangeVersion,
                    changeTrackingVersion = _lastKnownChangeVersion,
                    maxAuditId = _lastKnownAuditId,
                    lastSyncTimestamp = _lastSyncTimestamp,
                    checkedAt = _lastSyncTimestamp,
                    successfulWrites = _successfulWrites,
                    failedWrites = _failedWrites,
                    lastError = _lastError,
                    tableCounts = new
                    {
                        products = (int)(diag?.ProductsCount ?? 0),
                        rooms = (int)(diag?.RoomsCount ?? 0),
                        bookings = (int)(diag?.BookingsCount ?? 0),
                        deliveries = (int)(diag?.DeliveriesCount ?? 0),
                        partners = (int)(diag?.PartnersCount ?? 0),
                        employees = (int)(diag?.EmployeesCount ?? 0),
                        payrolls = (int)(diag?.PayrollsCount ?? 0),
                        transactions = (int)(diag?.TransactionsCount ?? 0),
                        syncLogs = (int)(diag?.SyncLogsCount ?? 0)
                    },
                    tableRowCounts,
                    recentLogs,
                    recentSyncLogs = recentLogs
                };
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return new
                {
                    isSqlConnected = false,
                    backendApiStatus = "ONLINE_DEGRADED",
                    signalRStatus = "ACTIVE",
                    connectedClients = EcoStaySyncHub.ConnectedClientsCount,
                    serverName = "localhost\\SQLEXPRESS",
                    databaseName = "KimLongEcoStayDB",
                    maskedConnectionString = GetMaskedConnectionString(),
                    changeTrackingEnabled = false,
                    currentChangeVersion = _lastKnownChangeVersion,
                    lastSyncTimestamp = _lastSyncTimestamp,
                    successfulWrites = _successfulWrites,
                    failedWrites = _failedWrites,
                    lastError = ex.Message,
                    tableCounts = new { products = 0, rooms = 0, bookings = 0, deliveries = 0, partners = 0, employees = 0, payrolls = 0, transactions = 0, syncLogs = 0 },
                    recentLogs = Array.Empty<object>()
                };
            }
        }

        public static string MaskPhone(string? rawPhone)
        {
            if (string.IsNullOrWhiteSpace(rawPhone)) return "•••";
            var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
            if (digits.Length < 6) return "••••••";
            return $"{digits.Substring(0, 3)}•••{digits.Substring(digits.Length - 3)}";
        }

        public static string MaskEmail(string? rawEmail)
        {
            if (string.IsNullOrWhiteSpace(rawEmail) || !rawEmail.Contains('@')) return "•••@•••";
            var parts = rawEmail.Split('@');
            var local = parts[0];
            var domain = parts[1];
            string maskedLocal = local.Length <= 2 ? $"{local[0]}•••" : $"{local.Substring(0, 2)}•••";
            return $"{maskedLocal}@{domain}";
        }

        public async Task<object> GetPublicCatalogAsync()
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var products = await QueryProductsInternalAsync(conn);
            var rawRooms = await conn.QueryAsync<dynamic>(@"
                SELECT r.Id, r.RoomNumber, r.RoomTypeId, r.Status,
                       r.UpdatedAt, r.SyncVersion,
                       rt.TypeName, rt.Description, rt.MaxGuests, rt.BasePrice, rt.ImageUrl
                FROM dbo.Rooms r
                JOIN dbo.RoomTypes rt ON r.RoomTypeId = rt.Id
                WHERE r.IsActive = 1
                ORDER BY r.Id ASC;");

            var rooms = rawRooms.Select(r => (object)new
            {
                id = (int)r.Id,
                roomNumber = (string)r.RoomNumber,
                name = $"{r.TypeName} ({r.RoomNumber})",
                typeName = (string)r.TypeName,
                brand = ((int)r.Id <= 2) ? "Nhà Vườn Hương Sen (Kim Long)" : "Nhà Vườn Nguyệt Biều (Kim Long)",
                price = (decimal)r.BasePrice,
                maxGuests = (int)r.MaxGuests,
                status = (string)r.Status,
                imageUrl = ((string?)r.ImageUrl ?? "images/room_econest.jpg").TrimStart('/'),
                updatedAt = (DateTime)r.UpdatedAt,
                syncVersion = (int)r.SyncVersion
            }).ToList();

            var partners = await QueryPartnersInternalAsync(conn);
            var teaServices = await QueryTeaServicesInternalAsync(conn);

            return new
            {
                success = true,
                isSqlConnected = true,
                syncedAt = DateTime.UtcNow,
                products,
                rooms,
                partners,
                teaServices
            };
        }

        public async Task<object> GetFullSyncStateAsync(string callerRole = "ADMIN")
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string normRole = (callerRole ?? "ADMIN").Trim().ToUpperInvariant();
            bool isAdmin = normRole is "ADMIN" or "ADMINISTRATOR";
            bool isReceptionist = normRole is "STAFF" or "LETAN" or "RECEPTIONIST";
            bool isAccountant = normRole is "ACCOUNTANT" or "CFO" or "KETOAN";
            bool isHr = normRole is "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU";

            var products = await QueryProductsInternalAsync(conn);
            var rooms = await QueryRoomsInternalAsync(conn);
            var bookings = (isAdmin || isReceptionist || isAccountant) ? await QueryBookingsInternalAsync(conn) : new List<object>();
            var deliveries = (isAdmin || isReceptionist || isAccountant || isHr) ? await QueryDeliveriesInternalAsync(conn) : new List<object>();
            var smartlocks = (isAdmin || isReceptionist || isHr) ? await QuerySmartLocksInternalAsync(conn) : new List<object>();
            var smartlockLogs = (isAdmin || isReceptionist || isHr) ? await QuerySmartLockAuditLogsInternalAsync(conn) : new List<object>();
            var partners = await QueryPartnersInternalAsync(conn);
            var teaServices = await QueryTeaServicesInternalAsync(conn);
            var employees = (isAdmin || isHr) ? await QueryEmployeesInternalAsync(conn) : new List<object>();
            var payrolls = (isAdmin || isAccountant) ? await QueryPayrollsInternalAsync(conn) : new List<object>();
            var transactions = (isAdmin || isAccountant) ? await QueryTransactionsInternalAsync(conn) : new List<object>();
            var expenses = (isAdmin || isAccountant) ? await QueryExpensesInternalAsync(conn) : new List<object>();
            var emailLogs = (isAdmin || isReceptionist || isAccountant) ? await QueryEmailLogsInternalAsync(conn) : new List<object>();
            var syncLogs = isAdmin ? await QuerySyncLogsInternalAsync(conn) : new List<object>();

            var ctVer = await conn.ExecuteScalarAsync<long?>("SELECT ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0);") ?? 0;
            var maxAuditId = await conn.ExecuteScalarAsync<long?>("SELECT ISNULL(MAX(Id), 0) FROM dbo.SyncAuditLogs;") ?? 0;

            _lastKnownChangeVersion = ctVer;
            _lastKnownAuditId = maxAuditId;
            _lastSyncTimestamp = DateTime.UtcNow;

            return new
            {
                success = true,
                isSqlConnected = true,
                roleScope = normRole,
                changeVersion = ctVer,
                maxAuditId = maxAuditId,
                syncedAt = _lastSyncTimestamp,
                products,
                rooms,
                bookings,
                deliveries,
                smartlocks,
                smartlockLogs,
                partners,
                teaServices,
                employees,
                payrolls,
                transactions,
                expenses,
                emailLogs,
                syncLogs
            };
        }

        public async Task<object> GetEntitySliceAsync(string entityType)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            string norm = (entityType ?? "").Trim().ToLowerInvariant();
            object? data = norm switch
            {
                "products" or "specialtyproducts" => await QueryProductsInternalAsync(conn),
                "rooms" => await QueryRoomsInternalAsync(conn),
                "bookings" => await QueryBookingsInternalAsync(conn),
                "deliveries" or "roomdeliveries" => await QueryDeliveriesInternalAsync(conn),
                "smartlock" or "smartlockpins" => new
                {
                    pins = await QuerySmartLocksInternalAsync(conn),
                    logs = await QuerySmartLockAuditLogsInternalAsync(conn)
                },
                "partners" or "localpartners" => await QueryPartnersInternalAsync(conn),
                "tea" or "teaservices" => await QueryTeaServicesInternalAsync(conn),
                "employees" => await QueryEmployeesInternalAsync(conn),
                "payrolls" => await QueryPayrollsInternalAsync(conn),
                "finance" or "financialtransactions" => new
                {
                    transactions = await QueryTransactionsInternalAsync(conn),
                    expenses = await QueryExpensesInternalAsync(conn)
                },
                _ => await GetFullSyncStateAsync()
            };

            var ctVer = await conn.ExecuteScalarAsync<long?>("SELECT ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0);") ?? 0;
            var syncLogs = await QuerySyncLogsInternalAsync(conn, 15);
            _lastSyncTimestamp = DateTime.UtcNow;

            return new
            {
                success = true,
                entityType = norm,
                changeVersion = ctVer,
                syncedAt = _lastSyncTimestamp,
                data,
                syncLogs
            };
        }

        // =========================================================================
        // INTERNAL READ QUERIES
        // =========================================================================
        private async Task<List<object>> QueryProductsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT Id, ProductCode, Name, Category, Price, Unit, StockQuantity,
                       ImageLink, Origin, PartnerId, PartnerName, Description,
                       IsActive, IsFeatured, UpdatedAt, SyncVersion
                FROM dbo.SpecialtyProducts
                WHERE IsActive = 1
                ORDER BY ProductCode ASC;");

            return rows.Select(r => (object)new
            {
                dbId = (int)r.Id,
                id = (string)r.ProductCode,
                name = (string)r.Name,
                category = (string)r.Category,
                price = (decimal)r.Price,
                unit = (string)r.Unit,
                stock = (int)r.StockQuantity,
                img = ((string?)r.ImageLink ?? "images/qua_mutgung.jpg").TrimStart('/'),
                origin = (string?)r.Origin ?? "Phường Kim Long, TP. Huế",
                partnerId = (string?)r.PartnerId ?? "PARTNER-DAC-SAN-KIM-LONG",
                partnerName = (string?)r.PartnerName ?? "Mạng Lưới Đối Tác Kim Long",
                desc = (string?)r.Description ?? "",
                isActive = (bool)r.IsActive,
                isFeatured = (bool)r.IsFeatured,
                updatedAt = (DateTime)r.UpdatedAt,
                syncVersion = (int)r.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QueryRoomsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT r.Id, r.RoomNumber, r.RoomTypeId, r.Status, r.GuestName, r.BookingCode,
                       r.UpdatedAt, r.SyncVersion,
                       rt.TypeName, rt.Description, rt.MaxGuests, rt.BasePrice, rt.ImageUrl
                FROM dbo.Rooms r
                JOIN dbo.RoomTypes rt ON r.RoomTypeId = rt.Id
                WHERE r.IsActive = 1
                ORDER BY r.Id ASC;");

            return rows.Select(r => (object)new
            {
                id = (int)r.Id,
                roomNumber = (string)r.RoomNumber,
                name = $"{r.TypeName} ({r.RoomNumber})",
                typeName = (string)r.TypeName,
                brand = ((int)r.Id <= 2) ? "Nhà Vườn Hương Sen (Kim Long)" : "Nhà Vườn Nguyệt Biều (Kim Long)",
                price = (decimal)r.BasePrice,
                maxGuests = (int)r.MaxGuests,
                status = (string)r.Status,
                guestName = (string?)r.GuestName,
                bookingCode = (string?)r.BookingCode,
                imageUrl = ((string?)r.ImageUrl ?? "images/room_econest.jpg").TrimStart('/'),
                updatedAt = (DateTime)r.UpdatedAt,
                syncVersion = (int)r.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QueryBookingsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT b.Id, b.BookingCode, b.GuestId,
                       ISNULL(b.GuestName, g.FullName) AS GuestName,
                       ISNULL(b.GuestPhone, g.PhoneNumber) AS GuestPhone,
                       ISNULL(b.GuestEmail, g.Email) AS GuestEmail,
                       ISNULL(b.RoomName, N'Eco-Nest Ban Mai (KLE-R101)') AS RoomName,
                       b.CheckInDate, b.CheckOutDate, b.TotalGuests,
                       b.TotalAmount, b.DepositAmount, b.Status,
                       b.PaymentStatus, b.PaymentMethod, b.TransferContent,
                       b.Notes, b.CreatedAt, b.UpdatedAt, b.SyncVersion
                FROM dbo.Bookings b
                LEFT JOIN dbo.Guests g ON b.GuestId = g.Id
                ORDER BY b.Id DESC;");

            return rows.Select(b =>
            {
                decimal total = (decimal)b.TotalAmount;
                decimal dep = (decimal)b.DepositAmount;
                string paySt = (string?)b.PaymentStatus ?? "WAITING_RECONCILIATION";
                decimal rem = paySt == "COMPLETED" ? 0 : (total - dep);
                return (object)new
                {
                    id = (int)b.Id,
                    orderCode = (string)b.BookingCode,
                    pin = "••••••", // Luôn ẩn mã PIN theo yêu cầu bảo mật
                    name = (string?)b.GuestName ?? "Khách Lưu Trú",
                    phone = (string?)b.GuestPhone ?? "0905123456",
                    email = (string?)b.GuestEmail ?? "khachhang@example.com",
                    roomName = (string?)b.RoomName ?? "Eco-Nest Ban Mai (KLE-R101)",
                    checkIn = ((DateTime)b.CheckInDate).ToString("yyyy-MM-dd"),
                    checkOut = ((DateTime)b.CheckOutDate).ToString("yyyy-MM-dd"),
                    totalGuests = (int)b.TotalGuests,
                    totalPrice = total,
                    depositRequired = dep,
                    depositPaid = dep,
                    remainingBalance = rem,
                    status = paySt,
                    bookingStatus = (string)b.Status,
                    paymentMethod = (string?)b.PaymentMethod ?? "VietQR",
                    transferContent = (string?)b.TransferContent ?? $"KLE {b.BookingCode} COC50",
                    notes = (string?)b.Notes ?? "",
                    createdAt = (DateTime)b.CreatedAt,
                    updatedAt = (DateTime)b.UpdatedAt,
                    syncVersion = (int)b.SyncVersion
                };
            }).ToList();
        }

        private async Task<List<object>> QueryDeliveriesInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT Id, OrderCode, DeliveryMethod, RoomNumber, RoomName, BookingCode,
                       CustomerName, Phone, Email, DeliveryTime, ServiceNotes,
                       PaymentMethod, PaymentStatus, ItemsJson, Subtotal, ShippingFee, Total,
                       Status, StatusHistoryJson, CreatedAt, UpdatedAt, SyncVersion
                FROM dbo.RoomDeliveries
                ORDER BY Id DESC;");

            var list = new List<object>();
            foreach (var r in rows)
            {
                object? items = null;
                object? history = null;
                try { items = JsonSerializer.Deserialize<JsonElement>((string)r.ItemsJson); } catch { items = Array.Empty<object>(); }
                try { history = !string.IsNullOrWhiteSpace((string?)r.StatusHistoryJson) ? JsonSerializer.Deserialize<JsonElement>((string)r.StatusHistoryJson) : Array.Empty<object>(); } catch { history = Array.Empty<object>(); }

                list.Add(new
                {
                    id = (int)r.Id,
                    orderCode = (string)r.OrderCode,
                    deliveryMethod = (string)r.DeliveryMethod,
                    roomNumber = (string)r.RoomNumber,
                    roomName = (string)r.RoomName,
                    bookingCode = (string?)r.BookingCode ?? "",
                    customerName = (string)r.CustomerName,
                    phone = (string)r.Phone,
                    email = (string?)r.Email ?? "",
                    deliveryTime = (string)r.DeliveryTime,
                    serviceNotes = (string?)r.ServiceNotes ?? "",
                    paymentMethod = (string)r.PaymentMethod,
                    paymentStatus = (string)r.PaymentStatus,
                    items,
                    subtotal = (decimal)r.Subtotal,
                    shippingFee = (decimal)r.ShippingFee,
                    total = (decimal)r.Total,
                    status = (string)r.Status,
                    statusHistory = history,
                    createdAt = (DateTime)r.CreatedAt,
                    updatedAt = (DateTime)r.UpdatedAt,
                    syncVersion = (int)r.SyncVersion
                });
            }
            return list;
        }

        private async Task<List<object>> QuerySmartLocksInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT Id, RoomNumber, RoomName, BookingCode, GuestName, GuestPhone,
                       MaskedDisplay, PinStatus, BatteryPercent, ValidFrom, ValidUntil,
                       FailedAttempts, LockedUntil, IsDemoSimulation,
                       LastReissuedBy, LastReissuedReason, UpdatedAt, SyncVersion
                FROM dbo.SmartLockPins
                ORDER BY RoomNumber ASC;");

            return rows.Select(s => (object)new
            {
                id = (int)s.Id,
                roomNumber = (string)s.RoomNumber,
                roomName = (string)s.RoomName,
                bookingCode = (string?)s.BookingCode,
                guestName = (string?)s.GuestName,
                guestPhone = MaskPhone((string?)s.GuestPhone),
                maskedPin = "••••••", // Không bao giờ trả về mã PIN thật
                pinStatus = (string)s.PinStatus,
                batteryPercent = (int)s.BatteryPercent,
                validFrom = (DateTime?)s.ValidFrom,
                validUntil = (DateTime?)s.ValidUntil,
                failedAttempts = (int)(s.FailedAttempts ?? 0),
                lockedUntil = (DateTime?)s.LockedUntil,
                isDemoSimulation = (bool)(s.IsDemoSimulation ?? false),
                lastReissuedBy = (string?)s.LastReissuedBy,
                lastReissuedReason = (string?)s.LastReissuedReason,
                updatedAt = (DateTime)s.UpdatedAt,
                syncVersion = (int)s.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QuerySmartLockAuditLogsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT TOP 30 Id, LogCode, RoomNumber, BookingCode, ActionType,
                              PerformedBy, RoleName, Reason, CreatedAt
                FROM dbo.SmartLockAuditLogs
                ORDER BY Id DESC;");

            return rows.Select(l => (object)new
            {
                id = (int)l.Id,
                logCode = (string)l.LogCode,
                roomNumber = (string)l.RoomNumber,
                bookingCode = (string?)l.BookingCode ?? "",
                actionType = (string)l.ActionType,
                performedBy = (string)l.PerformedBy,
                roleName = (string)l.RoleName,
                reason = (string)l.Reason,
                createdAt = (DateTime)l.CreatedAt
            }).ToList();
        }

        private async Task<List<object>> QueryPartnersInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT PartnerId, Name, Category, PartnerGroup, Latitude, Longitude,
                       Address, DistanceKm, Phone, OpenHours, ImageUrl, Description,
                       ServicesJson, IsActive, UpdatedAt, SyncVersion
                FROM dbo.LocalPartners
                ORDER BY DistanceKm ASC;");

            var list = new List<object>();
            foreach (var p in rows)
            {
                string[] services = new[] { "Tham quan & Liên kết" };
                try
                {
                    if (!string.IsNullOrWhiteSpace((string?)p.ServicesJson))
                        services = JsonSerializer.Deserialize<string[]>((string)p.ServicesJson) ?? services;
                }
                catch { }

                list.Add(new
                {
                    id = (string)p.PartnerId,
                    name = (string)p.Name,
                    category = (string)p.Category,
                    partnerGroup = (string)p.PartnerGroup,
                    latitude = (double)p.Latitude,
                    longitude = (double)p.Longitude,
                    address = (string)p.Address,
                    distanceKm = (double)p.DistanceKm,
                    phone = (string?)p.Phone ?? "0905 123 456",
                    openHours = (string?)p.OpenHours ?? "07:00 - 21:00",
                    imageUrl = ((string?)p.ImageUrl ?? "images/nha_ruong_1.jpg").TrimStart('/'),
                    description = (string?)p.Description ?? "",
                    services,
                    isActive = (bool)p.IsActive,
                    updatedAt = (DateTime)p.UpdatedAt,
                    syncVersion = (int)p.SyncVersion
                });
            }
            return list;
        }

        private async Task<List<object>> QueryTeaServicesInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT Id, ServiceCode, Name, PackageType, Price, PriceUnit,
                       DurationMinutes, MaxGuests, AvailableSlots, ServingSchedule,
                       ImageUrl, Description, IsActive, UpdatedAt, SyncVersion
                FROM dbo.TeaServices
                WHERE IsActive = 1
                ORDER BY Id ASC;");

            return rows.Select(t => (object)new
            {
                id = (int)t.Id,
                code = (string)t.ServiceCode,
                name = (string)t.Name,
                packageType = (string)t.PackageType,
                price = (decimal)t.Price,
                priceType = (string)t.PriceUnit,
                duration = $"{t.DurationMinutes} phút",
                maxCapacity = (int)t.MaxGuests,
                remainingSlots = (int)t.AvailableSlots,
                schedule = (string)t.ServingSchedule,
                img = ((string?)t.ImageUrl ?? "images/tra_cung_dinh.jpg").TrimStart('/'),
                desc = (string?)t.Description ?? "",
                status = ((int)t.AvailableSlots > 0) ? "OPEN" : "FULL",
                updatedAt = (DateTime)t.UpdatedAt,
                syncVersion = (int)t.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QueryEmployeesInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT e.Id, e.EmployeeCode, e.FullName, e.AvatarUrl, e.DateOfBirth, e.Gender,
                       e.PhoneNumber, e.Email, e.Address, e.DepartmentId, d.Name AS DepartmentName,
                       e.PositionId, p.Title AS PositionTitle, e.HireDate, e.ContractType,
                       e.ContractStartDate, e.ContractEndDate, e.WorkStatus, e.BaseSalary,
                       e.Allowance, e.Notes, e.UpdatedAt, e.SyncVersion
                FROM dbo.Employees e
                JOIN dbo.Departments d ON e.DepartmentId = d.Id
                JOIN dbo.Positions p ON e.PositionId = p.Id
                WHERE e.IsDeleted = 0
                ORDER BY e.Id ASC;");

            return rows.Select(e => (object)new
            {
                id = (int)e.Id,
                code = (string)e.EmployeeCode,
                fullName = (string)e.FullName,
                avatarUrl = (string?)e.AvatarUrl ?? "",
                gender = (string)e.Gender,
                phone = (string)e.PhoneNumber,
                email = (string)e.Email,
                address = (string?)e.Address ?? "",
                departmentId = (int)e.DepartmentId,
                departmentName = (string)e.DepartmentName,
                positionId = (int)e.PositionId,
                positionTitle = (string)e.PositionTitle,
                contractType = (string)e.ContractType,
                workStatus = (string)e.WorkStatus,
                baseSalary = (decimal)e.BaseSalary,
                allowance = (decimal)e.Allowance,
                notes = (string?)e.Notes ?? "",
                updatedAt = (DateTime)e.UpdatedAt,
                syncVersion = (int)e.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QueryPayrollsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT p.Id, p.PayrollCode, p.EmployeeId, e.EmployeeCode, e.FullName AS EmployeeName,
                       d.Name AS DepartmentName, pos.Title AS PositionTitle,
                       p.Month, p.Year, p.BaseSalary, p.Allowance, p.Bonus, p.OvertimePay,
                       p.Deductions, p.AdvancePayment, p.GrossIncome, p.NetIncome,
                       p.PaymentDate, p.PaymentMethod, p.Status, p.ApprovedBy, p.Notes, p.SyncVersion
                FROM dbo.Payrolls p
                JOIN dbo.Employees e ON p.EmployeeId = e.Id
                JOIN dbo.Departments d ON e.DepartmentId = d.Id
                JOIN dbo.Positions pos ON e.PositionId = pos.Id
                ORDER BY p.Year DESC, p.Month DESC, e.Id ASC;");

            return rows.Select(p => (object)new
            {
                id = (int)p.Id,
                payrollCode = (string)p.PayrollCode,
                employeeId = (int)p.EmployeeId,
                employeeCode = (string)p.EmployeeCode,
                employeeName = (string)p.EmployeeName,
                departmentName = (string)p.DepartmentName,
                positionTitle = (string)p.PositionTitle,
                month = (int)p.Month,
                year = (int)p.Year,
                baseSalary = (decimal)p.BaseSalary,
                allowance = (decimal)p.Allowance,
                bonus = (decimal)p.Bonus,
                overtimePay = (decimal)p.OvertimePay,
                deductions = (decimal)p.Deductions,
                grossIncome = (decimal)p.GrossIncome,
                netIncome = (decimal)p.NetIncome,
                paymentMethod = (string)p.PaymentMethod,
                status = (string)p.Status,
                approvedBy = (string?)p.ApprovedBy ?? "",
                notes = (string?)p.Notes ?? "",
                syncVersion = (int)p.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QueryTransactionsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT TOP 50 Id, TransactionCode, TransactionDate, Type, Category,
                              SourceType, SourceRefId, Amount, PaymentMethod, Status,
                              CreatedBy, Description, SyncVersion
                FROM dbo.FinancialTransactions
                ORDER BY TransactionDate DESC, Id DESC;");

            return rows.Select(t => (object)new
            {
                id = (int)t.Id,
                transactionCode = (string)t.TransactionCode,
                transactionDate = (DateTime)t.TransactionDate,
                type = (string)t.Type,
                category = (string)t.Category,
                sourceType = (string)t.SourceType,
                sourceRefId = (string?)t.SourceRefId ?? "",
                amount = (decimal)t.Amount,
                paymentMethod = (string)t.PaymentMethod,
                status = (string)t.Status,
                createdBy = (string)t.CreatedBy,
                description = (string?)t.Description ?? "",
                syncVersion = (int)t.SyncVersion
            }).ToList();
        }

        private async Task<List<object>> QueryExpensesInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT TOP 50 e.Id, e.VoucherCode, e.ExpenseDate, e.CategoryId, c.Name AS CategoryName,
                              e.Title, e.Amount, e.PayeeName, e.PaymentMethod, e.Status, e.CreatedBy, e.Notes
                FROM dbo.Expenses e
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.Id
                ORDER BY e.ExpenseDate DESC, e.Id DESC;");

            return rows.Select(e => (object)new
            {
                id = (int)e.Id,
                voucherCode = (string)e.VoucherCode,
                expenseDate = (DateTime)e.ExpenseDate,
                categoryId = (int)e.CategoryId,
                categoryName = (string)e.CategoryName,
                title = (string)e.Title,
                amount = (decimal)e.Amount,
                payeeName = (string)e.PayeeName,
                paymentMethod = (string)e.PaymentMethod,
                status = (string)e.Status,
                createdBy = (string)e.CreatedBy,
                notes = (string?)e.Notes ?? ""
            }).ToList();
        }

        private async Task<List<object>> QueryEmailLogsInternalAsync(SqlConnection conn)
        {
            var rows = await conn.QueryAsync<dynamic>(@"
                SELECT TOP 30 Id, EmailId, TemplateType, RecipientEmail, Subject,
                              Status, StatusMessage, AssociatedBookingCode, SentAt, RetryCount
                FROM dbo.EmailLogs
                ORDER BY Id DESC;");

            return rows.Select(e => (object)new
            {
                id = (string)e.EmailId,
                templateType = (string)e.TemplateType,
                recipient = (string)e.RecipientEmail,
                subject = (string)e.Subject,
                status = (string)e.Status,
                message = (string?)e.StatusMessage ?? "",
                bookingCode = (string?)e.AssociatedBookingCode ?? "",
                sentAt = (DateTime)e.SentAt,
                retryCount = (int)e.RetryCount
            }).ToList();
        }

        private async Task<List<object>> QuerySyncLogsInternalAsync(SqlConnection conn, int top = 30)
        {
            var rows = await conn.QueryAsync<dynamic>($@"
                SELECT TOP ({top})
                    Id as id, EntityType as entityType, EntityKey as entityKey,
                    ActionType as actionType, Source as source, ChangedBy as changedBy,
                    Summary as summary, ChangeVersion as changeVersion, CreatedAt as createdAt
                FROM dbo.SyncAuditLogs
                ORDER BY Id DESC;");
            return rows.Cast<object>().ToList();
        }

        // =========================================================================
        // DIRECTION 1: WEBSITE → SQL SERVER (WITH SQL TRANSACTION & CONCURRENCY)
        // =========================================================================

        public async Task<object> UpsertProductAsync(JsonElement payload, string actor)
        {
            string code = payload.TryGetProperty("id", out var idProp) ? (idProp.GetString() ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(code))
                code = $"SP{DateTime.UtcNow:HHmmss}";

            string name = payload.TryGetProperty("name", out var nProp) ? (nProp.GetString() ?? "").Trim() : "";
            string category = payload.TryGetProperty("category", out var cProp) ? (cProp.GetString() ?? "Đặc sản Huế").Trim() : "Đặc sản Huế";
            decimal price = payload.TryGetProperty("price", out var pProp) && pProp.TryGetDecimal(out var pr) ? pr : 0;
            int stock = payload.TryGetProperty("stock", out var sProp) && sProp.TryGetInt32(out var st) ? st : 0;
            string unit = payload.TryGetProperty("unit", out var uProp) ? (uProp.GetString() ?? "phần").Trim() : "phần";
            string img = payload.TryGetProperty("img", out var imgProp) ? (imgProp.GetString() ?? "images/qua_mutgung.jpg").Trim() : "images/qua_mutgung.jpg";
            string origin = payload.TryGetProperty("origin", out var oProp) ? (oProp.GetString() ?? "Phường Kim Long, TP. Huế").Trim() : "Phường Kim Long, TP. Huế";
            string partnerId = payload.TryGetProperty("partnerId", out var pidProp) ? (pidProp.GetString() ?? "PARTNER-DAC-SAN-KIM-LONG").Trim() : "PARTNER-DAC-SAN-KIM-LONG";
            string partnerName = payload.TryGetProperty("partnerName", out var pnProp) ? (pnProp.GetString() ?? "Đối Tác Kim Long").Trim() : "Đối Tác Kim Long";
            string desc = payload.TryGetProperty("desc", out var dProp) ? (dProp.GetString() ?? "").Trim() : "";
            int? expectedVersion = payload.TryGetProperty("syncVersion", out var vProp) && vProp.ValueKind == JsonValueKind.Number && vProp.TryGetInt32(out var sv) ? sv : null;
            bool forceOverwrite = payload.TryGetProperty("forceOverwrite", out var fProp) && fProp.ValueKind == JsonValueKind.True;

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên sản phẩm không được để trống.");
            if (price < 0)
                throw new ArgumentException("Giá bán sản phẩm không được nhỏ hơn 0 VNĐ.");
            if (stock < 0)
                throw new ArgumentException("Tồn kho sản phẩm không được nhỏ hơn 0.");

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);

                try
                {
                    var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT Id, ProductCode, Name, Price, StockQuantity, SyncVersion, UpdatedAt FROM dbo.SpecialtyProducts WITH (UPDLOCK) WHERE ProductCode = @Code",
                        new { Code = code }, tx);

                    int newSyncVersion = 1;
                    string actionType = "INSERT";

                    if (existing != null)
                    {
                        int currentVer = (int)existing.SyncVersion;
                        if (!forceOverwrite && expectedVersion.HasValue && expectedVersion.Value > 0 && expectedVersion.Value != currentVer)
                        {
                            await tx.RollbackAsync();
                            Interlocked.Increment(ref _failedWrites);
                            throw new ConcurrencyConflictException(
                                $"Xung đột dữ liệu: Sản phẩm '{code}' vừa được cập nhật trong SQL Server (Phiên bản hiện tại: v{currentVer}, phiên bản form của bạn: v{expectedVersion.Value}).",
                                currentVer,
                                new { id = code, name = (string)existing.Name, price = (decimal)existing.Price, stock = (int)existing.StockQuantity, syncVersion = currentVer });
                        }

                        newSyncVersion = currentVer + 1;
                        actionType = "UPDATE";

                        await conn.ExecuteAsync(@"
                            UPDATE dbo.SpecialtyProducts
                            SET Name = @Name, Category = @Category, Price = @Price,
                                StockQuantity = @Stock, Unit = @Unit, ImageLink = @Img,
                                Origin = @Origin, PartnerId = @PartnerId, PartnerName = @PartnerName,
                                Description = @Desc, IsActive = 1,
                                UpdatedAt = SYSUTCDATETIME(), SyncVersion = @NewVer
                            WHERE ProductCode = @Code;",
                            new { Code = code, Name = name, Category = category, Price = price, Stock = stock, Unit = unit, Img = img, Origin = origin, PartnerId = partnerId, PartnerName = partnerName, Desc = desc, NewVer = newSyncVersion },
                            tx);
                    }
                    else
                    {
                        await conn.ExecuteAsync(@"
                            INSERT INTO dbo.SpecialtyProducts (
                                ProductCode, Name, Category, Price, StockQuantity, Unit, ImageLink,
                                Origin, PartnerId, PartnerName, Description, IsActive, IsFeatured, IsSample,
                                CreatedAt, UpdatedAt, SyncVersion
                            ) VALUES (
                                @Code, @Name, @Category, @Price, @Stock, @Unit, @Img,
                                @Origin, @PartnerId, @PartnerName, @Desc, 1, 0, 0,
                                SYSUTCDATETIME(), SYSUTCDATETIME(), 1
                            );",
                            new { Code = code, Name = name, Category = category, Price = price, Stock = stock, Unit = unit, Img = img, Origin = origin, PartnerId = partnerId, PartnerName = partnerName, Desc = desc },
                            tx);
                    }

                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                        VALUES ('products', @Code, @ActionType, 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                        new
                        {
                            Code = code,
                            ActionType = actionType,
                            Actor = actor,
                            Summary = $"[{actionType}] Sản phẩm {code} ({name}) - Giá: {price:N0}đ, Tồn: {stock} {unit}"
                        }, tx);

                    await tx.CommitAsync();
                    Interlocked.Increment(ref _successfulWrites);
                    _lastSyncTimestamp = DateTime.UtcNow;

                    var updatedSlice = await GetEntitySliceAsync("products");
                    await BroadcastEntityChangeAsync("products", code, actionType, $"Đã lưu sản phẩm {name} ({code}) vào SQL Server", actor);
                    return new { success = true, id = code, syncVersion = newSyncVersion, slice = updatedSlice };
                }
                catch
                {
                    if (tx.Connection != null) await tx.RollbackAsync();
                    throw;
                }
            }
            catch (ConcurrencyConflictException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> DeleteProductAsync(string productCode, string actor)
        {
            if (string.IsNullOrWhiteSpace(productCode))
                throw new ArgumentException("Mã sản phẩm không hợp lệ.");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Name FROM dbo.SpecialtyProducts WHERE ProductCode = @Code",
                    new { Code = productCode }, tx);

                await conn.ExecuteAsync(
                    "DELETE FROM dbo.SpecialtyProducts WHERE ProductCode = @Code;",
                    new { Code = productCode }, tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('products', @Code, 'DELETE', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new
                    {
                        Code = productCode,
                        Actor = actor,
                        Summary = $"[DELETE] Xóa sản phẩm {productCode} ({(string?)existing?.Name ?? productCode}) khỏi SQL Server"
                    }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                var updatedSlice = await GetEntitySliceAsync("products");
                await BroadcastEntityChangeAsync("products", productCode, "DELETE", $"Đã xóa sản phẩm {productCode} khỏi SQL Server", actor);
                return new { success = true, id = productCode, slice = updatedSlice };
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> UpdateProductStockAsync(string productCode, int delta, int? absoluteStock, string actor, int? expectedVersion = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Name, StockQuantity, SyncVersion FROM dbo.SpecialtyProducts WITH (UPDLOCK) WHERE ProductCode = @Code",
                    new { Code = productCode }, tx);

                if (existing == null)
                    throw new KeyNotFoundException($"Không tìm thấy sản phẩm {productCode} trong SQL Server.");

                int currentVer = (int)existing.SyncVersion;
                if (expectedVersion.HasValue && expectedVersion.Value != currentVer)
                {
                    await tx.RollbackAsync();
                    Interlocked.Increment(ref _failedWrites);
                    throw new ConcurrencyConflictException(
                        $"Xung đột dữ liệu (Optimistic Concurrency 409): Sản phẩm '{productCode}' đang ở phiên bản SQL v{currentVer} (phiên bản yêu cầu: v{expectedVersion.Value}). Đã chặn ghi đè và nạp lại dữ liệu mới nhất.",
                        currentVer,
                        new { id = productCode, name = (string)existing.Name, stock = (int)existing.StockQuantity, syncVersion = currentVer });
                }

                int currentStock = (int)existing.StockQuantity;
                int newStock = absoluteStock.HasValue ? Math.Max(0, absoluteStock.Value) : Math.Max(0, currentStock + delta);
                int newVer = currentVer + 1;

                await conn.ExecuteAsync(@"
                    UPDATE dbo.SpecialtyProducts
                    SET StockQuantity = @NewStock, UpdatedAt = SYSUTCDATETIME(), SyncVersion = @NewVer
                    WHERE ProductCode = @Code;",
                    new { Code = productCode, NewStock = newStock, NewVer = newVer }, tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('SpecialtyProducts', @Code, 'UPDATE_STOCK', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new
                    {
                        Code = productCode,
                        Actor = actor,
                        Summary = $"[STOCK] Cập nhật tồn kho {productCode} ({(string)existing.Name}): {currentStock} → {newStock}"
                    }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                await BroadcastEntityChangeAsync("SpecialtyProducts", productCode, "UPDATE_STOCK", $"Tồn kho {productCode}: {currentStock} → {newStock}", actor);
                return new
                {
                    success = true,
                    id = productCode,
                    stock = newStock,
                    syncVersion = newVer,
                    newSyncVersion = newVer,
                    message = $"Đã cập nhật tồn kho {productCode} ({(string)existing.Name}): {currentStock} → {newStock}"
                };
            }
            catch (ConcurrencyConflictException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> UpdateRoomStatusAsync(string roomNumber, string newStatus, string? guestName, string? bookingCode, int? expectedVersion, string actor)
        {
            var validStates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "AVAILABLE", "RESERVED", "OCCUPIED", "DELIVERING_SERVICE",
                "CHECKOUT_PENDING", "CLEANING", "CLEANED_READY", "MAINTENANCE"
            };
            string normStatus = (newStatus ?? "").Trim().ToUpperInvariant() switch
            {
                "READY" => "CLEANED_READY",
                "EMPTY" => "AVAILABLE",
                "BOOKED" or "WAITING_CHECKIN" => "RESERVED",
                "WAITING_CHECKOUT" => "CHECKOUT_PENDING",
                var s => s
            };
            if (!validStates.Contains(normStatus))
                throw new ArgumentException($"Trạng thái phòng '{newStatus}' không thuộc 8 trạng thái chuẩn của hệ thống.");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var room = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id, RoomNumber, Status, GuestName, BookingCode, SyncVersion FROM dbo.Rooms WITH (UPDLOCK) WHERE RoomNumber = @RoomNumber",
                    new { RoomNumber = roomNumber }, tx);

                if (room == null)
                    throw new KeyNotFoundException($"Không tìm thấy buồng phòng {roomNumber} trong SQL Server.");

                int currentVer = (int)room.SyncVersion;
                if (expectedVersion.HasValue && expectedVersion.Value > 0 && expectedVersion.Value != currentVer)
                {
                    await tx.RollbackAsync();
                    Interlocked.Increment(ref _failedWrites);
                    throw new ConcurrencyConflictException(
                        $"Phòng {roomNumber} vừa được nhân viên khác cập nhật sang trạng thái '{(string)room.Status}' (v{currentVer}).",
                        currentVer, room);
                }

                int newVer = currentVer + 1;
                string? finalGuest = (normStatus == "AVAILABLE" || normStatus == "CLEANING" || normStatus == "CLEANED_READY" || normStatus == "MAINTENANCE")
                    ? null
                    : (guestName ?? (string?)room.GuestName);
                string? finalBooking = (normStatus == "AVAILABLE" || normStatus == "CLEANING" || normStatus == "CLEANED_READY" || normStatus == "MAINTENANCE")
                    ? null
                    : (bookingCode ?? (string?)room.BookingCode);

                await conn.ExecuteAsync(@"
                    UPDATE dbo.Rooms
                    SET Status = @Status, GuestName = @GuestName, BookingCode = @BookingCode,
                        UpdatedAt = SYSUTCDATETIME(), SyncVersion = @NewVer
                    WHERE RoomNumber = @RoomNumber;",
                    new { RoomNumber = roomNumber, Status = normStatus, GuestName = finalGuest, BookingCode = finalBooking, NewVer = newVer }, tx);

                // Khi trả phòng / dọn phòng, tự động thu hồi hiệu lực mã PIN SmartLock trong cùng Transaction
                if (normStatus == "CLEANING" || normStatus == "AVAILABLE")
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.SmartLockPins
                        SET PinStatus = 'EXPIRED', UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                        WHERE RoomNumber = @RoomNumber AND PinStatus = 'ACTIVE';",
                        new { RoomNumber = roomNumber }, tx);
                }
                else if (normStatus == "OCCUPIED" || normStatus == "RESERVED")
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.SmartLockPins
                        SET PinStatus = 'ACTIVE', GuestName = COALESCE(@GuestName, GuestName),
                            BookingCode = COALESCE(@BookingCode, BookingCode),
                            UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                        WHERE RoomNumber = @RoomNumber;",
                        new { RoomNumber = roomNumber, GuestName = finalGuest, BookingCode = finalBooking }, tx);
                }

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('rooms', @RoomNumber, 'UPDATE_STATUS', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new
                    {
                        RoomNumber = roomNumber,
                        Actor = actor,
                        Summary = $"[ROOM] Chuyển trạng thái phòng {roomNumber}: {(string)room.Status} → {normStatus}"
                    }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                await BroadcastEntityChangeAsync("rooms", roomNumber, "UPDATE_STATUS", $"Phòng {roomNumber} → {normStatus}", actor);
                return new { success = true, roomNumber, status = normStatus, syncVersion = newVer };
            }
            catch (Exception ex) when (ex is not ConcurrencyConflictException)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> UpsertBookingAsync(JsonElement payload, string actor)
        {
            string orderCode = payload.TryGetProperty("orderCode", out var oc) ? (oc.GetString() ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(orderCode))
                orderCode = $"KLE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10, 99)}";

            string name = payload.TryGetProperty("name", out var np) ? (np.GetString() ?? "Khách Lưu Trú").Trim() : "Khách Lưu Trú";
            string phone = payload.TryGetProperty("phone", out var pp) ? (pp.GetString() ?? "0905123456").Trim() : "0905123456";
            string email = payload.TryGetProperty("email", out var ep) ? (ep.GetString() ?? "khachhang@example.com").Trim() : "khachhang@example.com";
            string roomName = payload.TryGetProperty("roomName", out var rp) ? (rp.GetString() ?? "Eco-Nest Ban Mai (KLE-R101)").Trim() : "Eco-Nest Ban Mai (KLE-R101)";
            DateTime checkIn = payload.TryGetProperty("checkIn", out var ci) && DateTime.TryParse(ci.GetString(), out var cid) ? cid.Date : DateTime.Today;
            DateTime checkOut = payload.TryGetProperty("checkOut", out var co) && DateTime.TryParse(co.GetString(), out var cod) ? cod.Date : checkIn.AddDays(1);
            if (checkOut <= checkIn) checkOut = checkIn.AddDays(1);

            decimal totalPrice = payload.TryGetProperty("totalPrice", out var tp) && tp.TryGetDecimal(out var tpr) ? tpr : 390000;
            decimal deposit = payload.TryGetProperty("depositRequired", out var dp) && dp.TryGetDecimal(out var dpr) ? dpr : Math.Round(totalPrice * 0.5m, 0);
            string paymentStatus = payload.TryGetProperty("status", out var sp) ? (sp.GetString() ?? "WAITING_RECONCILIATION").Trim() : "WAITING_RECONCILIATION";
            string bookingStatus = payload.TryGetProperty("bookingStatus", out var bsp) ? (bsp.GetString() ?? "PENDING_DEPOSIT").Trim() : "PENDING_DEPOSIT";
            string transferContent = payload.TryGetProperty("transferContent", out var tcp) ? (tcp.GetString() ?? $"KLE {orderCode} COC50").Trim() : $"KLE {orderCode} COC50";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                // 1. Đảm bảo Guest tồn tại
                int guestId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT TOP 1 Id FROM dbo.Guests WHERE PhoneNumber = @Phone;",
                    new { Phone = phone }, tx) ?? 0;

                if (guestId == 0)
                {
                    guestId = await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO dbo.Guests (FullName, PhoneNumber, Email, CreatedAt)
                        VALUES (@Name, @Phone, @Email, SYSUTCDATETIME());
                        SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new { Name = name, Phone = phone, Email = email }, tx);
                }

                // 2. Upsert Booking
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id, SyncVersion FROM dbo.Bookings WITH (UPDLOCK) WHERE BookingCode = @Code;",
                    new { Code = orderCode }, tx);

                int newVer = 1;
                if (existing != null)
                {
                    newVer = ((int)existing.SyncVersion) + 1;
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.Bookings
                        SET GuestName = @Name, GuestPhone = @Phone, GuestEmail = @Email,
                            RoomName = @RoomName, CheckInDate = @CheckIn, CheckOutDate = @CheckOut,
                            TotalAmount = @Total, DepositAmount = @Deposit,
                            Status = @BookingStatus, PaymentStatus = @PaymentStatus,
                            TransferContent = @TransferContent,
                            UpdatedAt = SYSUTCDATETIME(), SyncVersion = @NewVer
                        WHERE BookingCode = @Code;",
                        new { Code = orderCode, Name = name, Phone = phone, Email = email, RoomName = roomName, CheckIn = checkIn, CheckOut = checkOut, Total = totalPrice, Deposit = deposit, BookingStatus = bookingStatus, PaymentStatus = paymentStatus, TransferContent = transferContent, NewVer = newVer }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.Bookings (
                            BookingCode, GuestId, GuestName, GuestPhone, GuestEmail, RoomName,
                            CheckInDate, CheckOutDate, TotalGuests, TotalAmount, DepositAmount,
                            Status, PaymentStatus, PaymentMethod, TransferContent, CreatedAt, UpdatedAt, SyncVersion
                        ) VALUES (
                            @Code, @GuestId, @Name, @Phone, @Email, @RoomName,
                            @CheckIn, @CheckOut, 2, @Total, @Deposit,
                            @BookingStatus, @PaymentStatus, 'VietQR', @TransferContent, SYSUTCDATETIME(), SYSUTCDATETIME(), 1
                        );",
                        new { Code = orderCode, GuestId = guestId, Name = name, Phone = phone, Email = email, RoomName = roomName, CheckIn = checkIn, CheckOut = checkOut, Total = totalPrice, Deposit = deposit, BookingStatus = bookingStatus, PaymentStatus = paymentStatus, TransferContent = transferContent }, tx);

                    // Ghi Email Log tiếp nhận đơn
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.EmailLogs (EmailId, TemplateType, RecipientEmail, Subject, Status, StatusMessage, AssociatedBookingCode)
                        VALUES (@EmailId, 'BOOKING_REQUEST_RECEIVED', @Email, @Subject, 'SUCCESS', N'Đã gửi hướng dẫn thanh toán cọc 50% VietQR', @Code);",
                        new { EmailId = $"EM-{DateTime.UtcNow:HHmmss}-{Random.Shared.Next(10, 99)}", Email = email, Subject = $"[Kim Long EcoStay] Tiếp nhận đặt phòng #{orderCode}", Code = orderCode }, tx);
                }

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('bookings', @Code, 'UPSERT', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new { Code = orderCode, Actor = actor, Summary = $"[BOOKING] Lưu đơn đặt phòng {orderCode} ({name} - {roomName}) - Tổng: {totalPrice:N0}đ" }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                await BroadcastEntityChangeAsync("bookings", orderCode, "UPSERT", $"Đơn đặt phòng {orderCode} ({name}) đã đồng bộ SQL", actor);
                return new { success = true, orderCode, syncVersion = newVer };
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> ReconcileBookingAsync(string orderCode, string newStatus, string actor)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var booking = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT * FROM dbo.Bookings WITH (UPDLOCK) WHERE BookingCode = @Code;",
                    new { Code = orderCode }, tx);

                if (booking == null)
                    throw new KeyNotFoundException($"Không tìm thấy đơn đặt phòng {orderCode} trong SQL Server.");

                string norm = (newStatus ?? "CONFIRMED").Trim().ToUpperInvariant();
                string dbStatus = norm switch
                {
                    "CONFIRMED" => "CONFIRMED",
                    "CHECKED_IN" => "CHECKED_IN",
                    "COMPLETED" or "CHECKED_OUT" => "CHECKED_OUT",
                    "CANCELLED" => "CANCELLED",
                    _ => "PENDING_DEPOSIT"
                };

                int newVer = ((int)booking.SyncVersion) + 1;
                await conn.ExecuteAsync(@"
                    UPDATE dbo.Bookings
                    SET Status = @DbStatus, PaymentStatus = @PayStatus,
                        UpdatedAt = SYSUTCDATETIME(), SyncVersion = @NewVer
                    WHERE BookingCode = @Code;",
                    new { Code = orderCode, DbStatus = dbStatus, PayStatus = norm, NewVer = newVer }, tx);

                // Trích xuất mã phòng từ RoomName (VD: KLE-R101)
                string roomName = (string?)booking.RoomName ?? "";
                string? roomNum = null;
                foreach (var candidate in new[] { "KLE-R101", "KLE-R102", "KLE-R201", "KLE-R202", "KLE-R203" })
                {
                    if (roomName.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        roomNum = candidate;
                        break;
                    }
                }

                string? newlyGeneratedPin = null;

                // Đồng bộ trạng thái phòng và khóa SmartLock trong cùng Transaction
                if (!string.IsNullOrEmpty(roomNum))
                {
                    string targetRoomState = norm switch
                    {
                        "CONFIRMED" => "RESERVED",
                        "CHECKED_IN" => "OCCUPIED",
                        "COMPLETED" or "CHECKED_OUT" => "CLEANING",
                        _ => "AVAILABLE"
                    };

                    await conn.ExecuteAsync(@"
                        UPDATE dbo.Rooms
                        SET Status = @RoomState,
                            GuestName = CASE WHEN @RoomState IN ('RESERVED','OCCUPIED') THEN @GuestName ELSE NULL END,
                            BookingCode = CASE WHEN @RoomState IN ('RESERVED','OCCUPIED') THEN @Code ELSE NULL END,
                            UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                        WHERE RoomNumber = @RoomNum;",
                        new { RoomState = targetRoomState, GuestName = (string?)booking.GuestName, Code = orderCode, RoomNum = roomNum }, tx);

                    if (norm == "CONFIRMED")
                    {
                        // Sinh mã PIN bảo mật ngẫu nhiên CSPRNG kèm muối mới
                        newlyGeneratedPin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
                        string pinSalt = Guid.NewGuid().ToString("N");
                        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{pinSalt}:{newlyGeneratedPin}"));
                        string pinHash = Convert.ToHexString(hashBytes);

                        DateTime validFrom = ((DateTime)booking.CheckInDate).Date.AddHours(14); // 14:00 ngày check-in
                        DateTime validUntil = ((DateTime)booking.CheckOutDate).Date.AddHours(12); // 12:00 ngày check-out

                        await conn.ExecuteAsync(@"
                            UPDATE dbo.SmartLockPins
                            SET PinHash = @PinHash,
                                PinSalt = @PinSalt,
                                MaskedDisplay = '••••••',
                                PinStatus = 'ACTIVE',
                                FailedAttempts = 0,
                                LockedUntil = NULL,
                                BookingCode = @Code,
                                GuestName = @GuestName,
                                GuestPhone = @GuestPhone,
                                ValidFrom = @ValidFrom,
                                ValidUntil = @ValidUntil,
                                LastReissuedBy = @Actor,
                                LastReissuedReason = N'Tự động cấp PIN bảo mật khi đối soát xác nhận cọc 50%',
                                UpdatedAt = SYSUTCDATETIME(),
                                SyncVersion = SyncVersion + 1
                            WHERE RoomNumber = @RoomNum;",
                            new
                            {
                                PinHash = pinHash,
                                PinSalt = pinSalt,
                                Code = orderCode,
                                GuestName = (string?)booking.GuestName,
                                GuestPhone = (string?)booking.GuestPhone,
                                ValidFrom = validFrom,
                                ValidUntil = validUntil,
                                Actor = actor,
                                RoomNum = roomNum
                            }, tx);

                        await conn.ExecuteAsync(@"
                            INSERT INTO dbo.SmartLockAuditLogs (
                                LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason
                            ) VALUES (
                                @LogCode, @RoomNum, @Code, 'AUTO_ISSUE_ON_DEPOSIT', @Actor, 'SYSTEM', N'Hệ thống tự động cấp PIN bảo mật CSPRNG sau đối soát cọc thành công'
                            );",
                            new
                            {
                                LogCode = $"LOG-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                                RoomNum = roomNum,
                                Code = orderCode,
                                Actor = actor
                            }, tx);
                    }
                    else if (norm == "CHECKED_IN")
                    {
                        await conn.ExecuteAsync(@"
                            UPDATE dbo.SmartLockPins
                            SET PinStatus = 'ACTIVE', BookingCode = @Code, GuestName = @GuestName,
                                UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                            WHERE RoomNumber = @RoomNum;",
                            new { Code = orderCode, GuestName = (string?)booking.GuestName, RoomNum = roomNum }, tx);
                    }
                    else if (norm == "COMPLETED" || norm == "CHECKED_OUT")
                    {
                        await conn.ExecuteAsync(@"
                            UPDATE dbo.SmartLockPins
                            SET PinStatus = 'EXPIRED',
                                PinHash = @RevokedHash,
                                FailedAttempts = 0,
                                LockedUntil = NULL,
                                UpdatedAt = SYSUTCDATETIME(),
                                SyncVersion = SyncVersion + 1
                            WHERE RoomNumber = @RoomNum;",
                            new { RoomNum = roomNum, RevokedHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) }, tx);

                        await conn.ExecuteAsync(@"
                            INSERT INTO dbo.SmartLockAuditLogs (
                                LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason
                            ) VALUES (
                                @LogCode, @RoomNum, @Code, 'AUTO_REVOKE_ON_CHECKOUT', @Actor, 'SYSTEM', N'Thu hồi mã PIN phòng khi khách trả phòng hoàn tất'
                            );",
                            new
                            {
                                LogCode = $"LOG-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                                RoomNum = roomNum,
                                Code = orderCode,
                                Actor = actor
                            }, tx);
                    }
                }

                // Ghi nhận giao dịch tài chính tự động khi xác nhận cọc hoặc tất toán
                decimal amountToLog = norm == "CONFIRMED"
                    ? (decimal)booking.DepositAmount
                    : (norm == "COMPLETED" ? ((decimal)booking.TotalAmount - (decimal)booking.DepositAmount) : 0);

                if (amountToLog > 0)
                {
                    string txCode = $"TX-{orderCode}-{norm.Substring(0, Math.Min(4, norm.Length))}-{DateTime.UtcNow:mmss}";
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.FinancialTransactions (
                            TransactionCode, TransactionDate, Type, Category, SourceType, SourceRefId,
                            Amount, PaymentMethod, Status, CreatedBy, Description, UpdatedAt, SyncVersion
                        ) VALUES (
                            @TxCode, SYSUTCDATETIME(), 'INCOME',
                            @Category, 'BOOKING', @Code, @Amount, 'VietQR', 'COMPLETED', @Actor, @Desc, SYSUTCDATETIME(), 1
                        );",
                        new
                        {
                            TxCode = txCode,
                            Category = norm == "CONFIRMED" ? "Tiền đặt cọc phòng 50%" : "Tất toán 50% tiền phòng còn lại",
                            Code = orderCode,
                            Amount = amountToLog,
                            Actor = actor,
                            Desc = $"Đối soát đơn đặt phòng #{orderCode} ({norm}) bởi {actor}"
                        }, tx);
                }

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('bookings', @Code, 'RECONCILE', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new { Code = orderCode, Actor = actor, Summary = $"[RECONCILE] Đối soát đơn {orderCode} → {norm} (Bởi: {actor})" }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                await BroadcastEntityChangeAsync("bookings", orderCode, "RECONCILE", $"Đơn {orderCode} chuyển trạng thái {norm}", actor);
                return new
                {
                    success = true,
                    orderCode,
                    status = norm,
                    syncVersion = newVer,
                    oneTimePin = newlyGeneratedPin,
                    pinSecurityNotice = newlyGeneratedPin != null
                        ? "Mã PIN phòng được sinh ngẫu nhiên an toàn (CSPRNG) và chỉ xuất hiện DUY NHẤT một lần trong phản hồi này. Hệ thống không lưu trữ PIN ở dạng thô."
                        : null
                };
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> UpsertRoomDeliveryAsync(JsonElement payload, string actor)
        {
            string orderCode = payload.TryGetProperty("orderCode", out var oc) ? (oc.GetString() ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(orderCode))
                orderCode = $"KLE-DELIV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10, 99)}";

            string deliveryMethod = payload.TryGetProperty("deliveryMethod", out var dm) ? (dm.GetString() ?? "ROOM_DELIVERY") : "ROOM_DELIVERY";
            string roomNumber = payload.TryGetProperty("roomNumber", out var rn) ? (rn.GetString() ?? "KLE-R101") : "KLE-R101";
            string roomName = payload.TryGetProperty("roomName", out var rnm) ? (rnm.GetString() ?? roomNumber) : roomNumber;
            string bookingCode = payload.TryGetProperty("bookingCode", out var bc) ? (bc.GetString() ?? "") : "";
            string customerName = payload.TryGetProperty("customerName", out var cn) ? (cn.GetString() ?? "Khách Lưu Trú") : "Khách Lưu Trú";
            string phone = payload.TryGetProperty("phone", out var ph) ? (ph.GetString() ?? "0905123456") : "0905123456";
            string email = payload.TryGetProperty("email", out var em) ? (em.GetString() ?? "") : "";
            string deliveryTime = payload.TryGetProperty("deliveryTime", out var dt) ? (dt.GetString() ?? "Giao ngay (15 - 30 phút)") : "Giao ngay (15 - 30 phút)";
            string serviceNotes = payload.TryGetProperty("serviceNotes", out var sn) ? (sn.GetString() ?? "") : "";
            string paymentMethod = payload.TryGetProperty("paymentMethod", out var pm) ? (pm.GetString() ?? "VIETQR") : "VIETQR";
            string paymentStatus = payload.TryGetProperty("paymentStatus", out var ps) ? (ps.GetString() ?? "PAID") : "PAID";
            decimal subtotal = payload.TryGetProperty("subtotal", out var st) && st.TryGetDecimal(out var stv) ? stv : 0;
            decimal shippingFee = payload.TryGetProperty("shippingFee", out var sf) && sf.TryGetDecimal(out var sfv) ? sfv : 0;
            decimal total = payload.TryGetProperty("total", out var tt) && tt.TryGetDecimal(out var ttv) ? ttv : subtotal + shippingFee;
            string status = payload.TryGetProperty("status", out var stProp) ? (stProp.GetString() ?? "PENDING") : "PENDING";

            string itemsJson = payload.TryGetProperty("items", out var itProp) ? itProp.GetRawText() : "[]";
            string historyJson = payload.TryGetProperty("statusHistory", out var hsProp) ? hsProp.GetRawText() : "[]";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT Id, SyncVersion FROM dbo.RoomDeliveries WITH (UPDLOCK) WHERE OrderCode = @Code;",
                    new { Code = orderCode }, tx);

                int newVer = 1;
                if (existing != null)
                {
                    newVer = ((int)existing.SyncVersion) + 1;
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.RoomDeliveries
                        SET Status = @Status, PaymentStatus = @PaymentStatus,
                            StatusHistoryJson = @HistoryJson,
                            UpdatedAt = SYSUTCDATETIME(), SyncVersion = @NewVer
                        WHERE OrderCode = @Code;",
                        new { Code = orderCode, Status = status, PaymentStatus = paymentStatus, HistoryJson = historyJson, NewVer = newVer }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.RoomDeliveries (
                            OrderCode, DeliveryMethod, RoomNumber, RoomName, BookingCode,
                            CustomerName, Phone, Email, DeliveryTime, ServiceNotes,
                            PaymentMethod, PaymentStatus, ItemsJson, Subtotal, ShippingFee, Total,
                            Status, StatusHistoryJson, CreatedAt, UpdatedAt, SyncVersion
                        ) VALUES (
                            @Code, @DeliveryMethod, @RoomNumber, @RoomName, @BookingCode,
                            @CustomerName, @Phone, @Email, @DeliveryTime, @ServiceNotes,
                            @PaymentMethod, @PaymentStatus, @ItemsJson, @Subtotal, @ShippingFee, @Total,
                            @Status, @HistoryJson, SYSUTCDATETIME(), SYSUTCDATETIME(), 1
                        );",
                        new
                        {
                            Code = orderCode, DeliveryMethod = deliveryMethod, RoomNumber = roomNumber, RoomName = roomName,
                            BookingCode = bookingCode, CustomerName = customerName, Phone = phone, Email = email,
                            DeliveryTime = deliveryTime, ServiceNotes = serviceNotes, PaymentMethod = paymentMethod,
                            PaymentStatus = paymentStatus, ItemsJson = itemsJson, Subtotal = subtotal,
                            ShippingFee = shippingFee, Total = total, Status = status, HistoryJson = historyJson
                        }, tx);

                    // Trừ tồn kho thực tế trong cùng SQL Transaction
                    if (payload.TryGetProperty("items", out var itemsArr) && itemsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsArr.EnumerateArray())
                        {
                            string pCode = item.TryGetProperty("id", out var pid) ? (pid.GetString() ?? "") : "";
                            int qty = item.TryGetProperty("qty", out var qProp) && qProp.TryGetInt32(out var qv) ? qv : 1;
                            if (!string.IsNullOrWhiteSpace(pCode) && qty > 0)
                            {
                                await conn.ExecuteAsync(@"
                                    UPDATE dbo.SpecialtyProducts
                                    SET StockQuantity = CASE WHEN StockQuantity >= @Qty THEN StockQuantity - @Qty ELSE 0 END,
                                        UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                                    WHERE ProductCode = @PCode;",
                                    new { PCode = pCode, Qty = qty }, tx);
                            }
                        }
                    }
                }

                // Đồng bộ trạng thái phòng khi đang giao dịch vụ tận phòng
                if (status == "DELIVERING" && !string.IsNullOrWhiteSpace(roomNumber))
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.Rooms
                        SET Status = 'DELIVERING_SERVICE', UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                        WHERE RoomNumber = @RoomNumber;",
                        new { RoomNumber = roomNumber }, tx);
                }
                else if (status == "DELIVERED" && !string.IsNullOrWhiteSpace(roomNumber))
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.Rooms
                        SET Status = 'OCCUPIED', UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                        WHERE RoomNumber = @RoomNumber AND Status = 'DELIVERING_SERVICE';",
                        new { RoomNumber = roomNumber }, tx);
                }

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('deliveries', @Code, 'UPSERT', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new { Code = orderCode, Actor = actor, Summary = $"[DELIVERY] Đơn giao phòng {orderCode} ({roomNumber}) → {status} ({total:N0}đ)" }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                await BroadcastEntityChangeAsync("deliveries", orderCode, "UPSERT", $"Đơn giao phòng {orderCode} ({roomNumber}) → {status}", actor);
                return new { success = true, orderCode, status, syncVersion = newVer };
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> ReissueSmartLockPinAsync(string roomNumber, string? bookingCode, string reason, string actor, string role)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
                throw new ArgumentException("Bắt buộc nhập lý do cấp lại mã khóa SmartLock (tối thiểu 3 ký tự) để lưu nhật ký kiểm toán.");

            string newRawPin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            string pinSalt = Guid.NewGuid().ToString("N");
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{pinSalt}:{newRawPin}"));
            string pinHash = Convert.ToHexString(hashBytes);

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                var p = new DynamicParameters();
                p.Add("@RoomNumber", roomNumber);
                p.Add("@BookingCode", bookingCode);
                p.Add("@NewPinHash", pinHash);
                p.Add("@PinSalt", pinSalt);
                p.Add("@PerformedBy", actor);
                p.Add("@RoleName", role);
                p.Add("@Reason", reason.Trim());

                await conn.ExecuteAsync("dbo.sp_ReissueSmartLockPinWithAudit", p, commandType: CommandType.StoredProcedure);

                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                var updatedSlice = await GetEntitySliceAsync("smartlock");
                await BroadcastEntityChangeAsync("smartlock", roomNumber, "REISSUE_PIN", $"Đã cấp lại mã PIN bảo mật cho phòng {roomNumber}", actor);

                return new
                {
                    success = true,
                    roomNumber,
                    maskedPin = "••••••", // Không bao giờ trả về mã PIN thật trên giao diện quản trị
                    oneTimePin = newRawPin,
                    pinSecurityNotice = "Mã PIN phòng được sinh ngẫu nhiên an toàn (CSPRNG) và chỉ xuất hiện DUY NHẤT một lần trong phản hồi này. Vui lòng chuyển ngay cho khách hàng.",
                    reissuedBy = actor,
                    reason = reason.Trim(),
                    reissuedAt = DateTime.UtcNow,
                    slice = updatedSlice
                };
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> RevokeSmartLockPinAsync(string roomNumber, string? bookingCode, string reason, string actor, string role)
        {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3)
                throw new ArgumentException("Bắt buộc nhập lý do thu hồi mã PIN (tối thiểu 3 ký tự).");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                string randomizedRevocationHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                await conn.ExecuteAsync(@"
                    UPDATE dbo.SmartLockPins
                    SET PinStatus = 'REVOKED',
                        PinHash = @PinHash,
                        FailedAttempts = 0,
                        LockedUntil = NULL,
                        UpdatedAt = SYSUTCDATETIME(),
                        SyncVersion = SyncVersion + 1
                    WHERE RoomNumber = @RoomNumber;",
                    new { RoomNumber = roomNumber, PinHash = randomizedRevocationHash }, tx);

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SmartLockAuditLogs (
                        LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason
                    ) VALUES (
                        @LogCode, @RoomNumber, @BookingCode, 'REVOKE_PIN', @Actor, @Role, @Reason
                    );",
                    new
                    {
                        LogCode = $"REV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                        RoomNumber = roomNumber,
                        BookingCode = bookingCode ?? "",
                        Actor = actor,
                        Role = role,
                        Reason = reason.Trim()
                    }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                var updatedSlice = await GetEntitySliceAsync("smartlock");
                await BroadcastEntityChangeAsync("smartlock", roomNumber, "REVOKE_PIN", $"Đã thu hồi mã PIN phòng {roomNumber}", actor);

                return new
                {
                    success = true,
                    roomNumber,
                    pinStatus = "REVOKED",
                    revokedBy = actor,
                    reason = reason.Trim(),
                    revokedAt = DateTime.UtcNow,
                    slice = updatedSlice
                };
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<(bool verified, int statusCode, string message)> VerifySmartLockPinAsync(string roomNumber, string? bookingCode, string enteredPin, string clientIp)
        {
            if (string.IsNullOrWhiteSpace(enteredPin) || enteredPin.Trim().Length != 6 || !enteredPin.Trim().All(char.IsDigit))
            {
                return (false, 400, "Mã PIN phải bao gồm đúng 6 chữ số.");
            }

            string cleanPin = enteredPin.Trim();
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var lockRow = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT sl.Id, sl.RoomNumber, sl.BookingCode, sl.PinHash, sl.PinSalt,
                       sl.PinStatus, sl.FailedAttempts, sl.LockedUntil, sl.ValidFrom, sl.ValidUntil,
                       r.Status AS RoomStatus, b.Status AS BookingStatus
                FROM dbo.SmartLockPins sl
                LEFT JOIN dbo.Rooms r ON sl.RoomNumber = r.RoomNumber
                LEFT JOIN dbo.Bookings b ON sl.BookingCode = b.BookingCode
                WHERE sl.RoomNumber = @RoomNumber;",
                new { RoomNumber = roomNumber });

            if (lockRow == null)
            {
                return (false, 404, "Không tìm thấy thiết bị SmartLock cho phòng này.");
            }

            DateTime now = DateTime.UtcNow;

            // 1. Kiểm tra tạm khóa chống Brute-Force (Lockout 15 phút)
            DateTime? lockedUntil = (DateTime?)lockRow.LockedUntil;
            if (lockedUntil.HasValue && lockedUntil.Value > now)
            {
                int remainingMinutes = (int)Math.Ceiling((lockedUntil.Value - now).TotalMinutes);
                return (false, 429, $"Khóa phòng đang tạm khóa an toàn do nhập sai quá 5 lần. Vui lòng thử lại sau {remainingMinutes} phút.");
            }

            // 2. Kiểm tra trạng thái PIN & Đơn phòng
            string pinStatus = (string)lockRow.PinStatus;
            if (!pinStatus.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return (false, 403, "Khóa SmartLock của phòng hiện chưa được kích hoạt hoặc đã hết hạn.");
            }

            string? bookingStatus = (string?)lockRow.BookingStatus;
            if (!string.IsNullOrEmpty(bookingStatus) &&
                !bookingStatus.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase) &&
                !bookingStatus.Equals("CHECKED_IN", StringComparison.OrdinalIgnoreCase))
            {
                return (false, 403, "Đơn đặt phòng liên kết chưa được xác nhận hoặc đã kết thúc lưu trú.");
            }

            // 3. Kiểm tra khung giờ hiệu lực
            DateTime? validFrom = (DateTime?)lockRow.ValidFrom;
            DateTime? validUntil = (DateTime?)lockRow.ValidUntil;
            if (validFrom.HasValue && now < validFrom.Value)
            {
                return (false, 403, "Chưa đến thời gian nhận phòng hợp lệ theo hợp đồng đặt phòng.");
            }
            if (validUntil.HasValue && now > validUntil.Value)
            {
                return (false, 403, "Mã khóa SmartLock đã quá hạn thời gian lưu trú.");
            }

            // 4. Xác minh băm SHA-256 kèm muối bằng Constant-Time Equals
            string storedHash = (string)lockRow.PinHash;
            string pinSalt = (string?)lockRow.PinSalt ?? "KLE_SL_SALT_2026";
            byte[] computedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{pinSalt}:{cleanPin}"));
            string computedHash = Convert.ToHexString(computedHashBytes);

            byte[] storedBytes = Encoding.UTF8.GetBytes(storedHash.ToUpperInvariant());
            byte[] compBytes = Encoding.UTF8.GetBytes(computedHash.ToUpperInvariant());

            bool isMatch = CryptographicOperations.FixedTimeEquals(storedBytes, compBytes);

            if (isMatch)
            {
                // Mở khóa thành công: Reset số lần nhập sai
                await conn.ExecuteAsync(@"
                    UPDATE dbo.SmartLockPins
                    SET FailedAttempts = 0, LockedUntil = NULL, UpdatedAt = SYSUTCDATETIME()
                    WHERE RoomNumber = @RoomNumber;",
                    new { RoomNumber = roomNumber });

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SmartLockAuditLogs (
                        LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason
                    ) VALUES (
                        @LogCode, @RoomNumber, @BookingCode, 'PIN_UNLOCK_SUCCESS', @ClientIp, 'GUEST', N'Mở khóa phòng thành công qua mã PIN SmartLock 6 số'
                    );",
                    new
                    {
                        LogCode = $"UNL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                        RoomNumber = roomNumber,
                        BookingCode = (string?)lockRow.BookingCode ?? "",
                        ClientIp = clientIp
                    });

                return (true, 200, "Xác thực mã PIN thành công. Đã mở chốt cửa an toàn!");
            }
            else
            {
                // Nhập sai PIN: Tăng FailedAttempts
                int currentFailed = (int)(lockRow.FailedAttempts ?? 0) + 1;
                DateTime? newLockedUntil = currentFailed >= 5 ? now.AddMinutes(15) : null;

                await conn.ExecuteAsync(@"
                    UPDATE dbo.SmartLockPins
                    SET FailedAttempts = @Failed,
                        LockedUntil = @LockedUntil,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE RoomNumber = @RoomNumber;",
                    new { Failed = currentFailed, LockedUntil = newLockedUntil, RoomNumber = roomNumber });

                if (currentFailed >= 5)
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.SmartLockAuditLogs (
                            LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason
                        ) VALUES (
                            @LogCode, @RoomNumber, @BookingCode, 'BRUTE_FORCE_LOCKOUT', @ClientIp, 'GUEST', N'Tự động khóa 15 phút do nhập sai mã PIN quá 5 lần liên tiếp'
                        );",
                        new
                        {
                            LogCode = $"LCK-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                            RoomNumber = roomNumber,
                            BookingCode = (string?)lockRow.BookingCode ?? "",
                            ClientIp = clientIp
                        });

                    return (false, 429, "Quý khách đã nhập sai mã PIN 5 lần. Khóa phòng tạm khóa an toàn trong 15 phút.");
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.SmartLockAuditLogs (
                            LogCode, RoomNumber, BookingCode, ActionType, PerformedBy, RoleName, Reason
                        ) VALUES (
                            @LogCode, @RoomNumber, @BookingCode, 'PIN_ATTEMPT_FAILED', @ClientIp, 'GUEST', N'Nhập sai mã PIN SmartLock'
                        );",
                        new
                        {
                            LogCode = $"ERR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
                            RoomNumber = roomNumber,
                            BookingCode = (string?)lockRow.BookingCode ?? "",
                            ClientIp = clientIp
                        });

                    return (false, 401, $"Mã PIN không chính xác. Quý khách còn {5 - currentFailed} lần thử.");
                }
            }
        }

        public async Task<(bool success, int statusCode, string message, object? booking)> LookupCustomerBookingVerifiedAsync(string orderCode, string phoneOrEmail)
        {
            if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(phoneOrEmail))
            {
                return (false, 400, "Vui lòng nhập đầy đủ Mã Đặt Phòng và Số Điện Thoại / Email để xác minh danh tính.", null);
            }

            string cleanCode = orderCode.Trim().ToUpperInvariant();
            string cleanVerifier = phoneOrEmail.Trim().ToLowerInvariant();
            string digitsVerifier = new string(cleanVerifier.Where(char.IsDigit).ToArray());

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT b.Id, b.BookingCode, b.GuestName, b.GuestPhone, b.GuestEmail,
                       b.RoomName, b.CheckInDate, b.CheckOutDate, b.TotalGuests,
                       b.TotalAmount, b.DepositAmount, b.Status, b.PaymentStatus,
                       b.PaymentMethod, b.TransferContent, b.Notes, b.CreatedAt, b.UpdatedAt
                FROM dbo.Bookings b
                WHERE b.BookingCode = @Code;",
                new { Code = cleanCode });

            if (row == null)
            {
                return (false, 404, "Không tìm thấy đơn đặt phòng với mã cung cấp.", null);
            }

            string guestPhone = (string?)row.GuestPhone ?? "";
            string guestEmail = (string?)row.GuestEmail ?? "";
            string guestDigits = new string(guestPhone.Where(char.IsDigit).ToArray());

            bool isMatch = false;
            if (!string.IsNullOrEmpty(digitsVerifier) && digitsVerifier.Length >= 4 && guestDigits.EndsWith(digitsVerifier))
            {
                isMatch = true;
            }
            else if (!string.IsNullOrEmpty(guestEmail) && guestEmail.Trim().Equals(cleanVerifier, StringComparison.OrdinalIgnoreCase))
            {
                isMatch = true;
            }

            if (!isMatch)
            {
                return (false, 403, "Số điện thoại hoặc email không khớp với đơn đặt phòng này. Để bảo mật thông tin khách hàng, yêu cầu bị từ chối.", null);
            }

            decimal total = (decimal)row.TotalAmount;
            decimal deposit = (decimal)row.DepositAmount;
            string payStatus = (string?)row.PaymentStatus ?? "WAITING_RECONCILIATION";
            decimal remaining = payStatus == "COMPLETED" ? 0 : (total - deposit);

            var sanitizedBooking = new
            {
                orderCode = (string)row.BookingCode,
                roomName = (string?)row.RoomName ?? "Eco-Nest Ban Mai",
                checkIn = ((DateTime)row.CheckInDate).ToString("yyyy-MM-dd"),
                checkOut = ((DateTime)row.CheckOutDate).ToString("yyyy-MM-dd"),
                totalGuests = (int)row.TotalGuests,
                totalPrice = total,
                depositPaid = deposit,
                remainingBalance = remaining,
                status = payStatus,
                bookingStatus = (string)row.Status,
                transferContent = (string?)row.TransferContent ?? $"KLE {cleanCode} COC50",
                guestName = (string?)row.GuestName ?? "Quý Khách",
                maskedPhone = MaskPhone(guestPhone),
                maskedEmail = MaskEmail(guestEmail),
                pin = "••••••" // Luôn ẩn PIN trong API tra cứu công khai để chống lộ lọt
            };

            return (true, 200, "Xác minh đơn đặt phòng thành công.", sanitizedBooking);
        }

        public async Task<(bool success, int statusCode, string message, object? delivery)> LookupCustomerDeliveryVerifiedAsync(string orderCode, string phoneOrRoom)
        {
            if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(phoneOrRoom))
            {
                return (false, 400, "Vui lòng nhập đầy đủ Mã Đơn Hàng và Số Điện Thoại / Số Phòng để xác minh.", null);
            }

            string cleanCode = orderCode.Trim().ToUpperInvariant();
            string cleanVerifier = phoneOrRoom.Trim().ToLowerInvariant();
            string digitsVerifier = new string(cleanVerifier.Where(char.IsDigit).ToArray());

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var row = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT Id, OrderCode, DeliveryMethod, RoomNumber, RoomName, BookingCode,
                       CustomerName, Phone, Email, DeliveryTime, ServiceNotes,
                       PaymentMethod, PaymentStatus, ItemsJson, Subtotal, ShippingFee, Total,
                       Status, StatusHistoryJson, CreatedAt, UpdatedAt
                FROM dbo.RoomDeliveries
                WHERE OrderCode = @Code;",
                new { Code = cleanCode });

            if (row == null)
            {
                return (false, 404, "Không tìm thấy đơn giao dịch vụ với mã cung cấp.", null);
            }

            string phone = (string?)row.Phone ?? "";
            string roomNum = (string?)row.RoomNumber ?? "";
            string phoneDigits = new string(phone.Where(char.IsDigit).ToArray());

            bool isMatch = false;
            if (!string.IsNullOrEmpty(digitsVerifier) && digitsVerifier.Length >= 4 && phoneDigits.EndsWith(digitsVerifier))
            {
                isMatch = true;
            }
            else if (!string.IsNullOrEmpty(roomNum) && roomNum.Trim().Equals(cleanVerifier, StringComparison.OrdinalIgnoreCase))
            {
                isMatch = true;
            }

            if (!isMatch)
            {
                return (false, 403, "Thông tin xác minh không khớp với đơn hàng này.", null);
            }

            object? items = null;
            try { items = JsonSerializer.Deserialize<JsonElement>((string)row.ItemsJson); } catch { items = Array.Empty<object>(); }

            var sanitizedDelivery = new
            {
                orderCode = (string)row.OrderCode,
                roomNumber = (string)row.RoomNumber,
                roomName = (string)row.RoomName,
                customerName = (string)row.CustomerName,
                maskedPhone = MaskPhone(phone),
                deliveryTime = (string)row.DeliveryTime,
                serviceNotes = (string?)row.ServiceNotes ?? "",
                paymentStatus = (string)row.PaymentStatus,
                status = (string)row.Status,
                items,
                subtotal = (decimal)row.Subtotal,
                shippingFee = (decimal)row.ShippingFee,
                total = (decimal)row.Total,
                createdAt = (DateTime)row.CreatedAt
            };

            return (true, 200, "Xác minh đơn giao phòng thành công.", sanitizedDelivery);
        }

        public async Task<object> UpsertPartnerAsync(JsonElement payload, string actor)
        {
            string id = payload.TryGetProperty("id", out var idProp) ? (idProp.GetString() ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(id))
                id = $"PARTNER-{DateTime.UtcNow:HHmmss}";

            string name = payload.TryGetProperty("name", out var np) ? (np.GetString() ?? "").Trim() : "";
            string group = payload.TryGetProperty("partnerGroup", out var gp) ? (gp.GetString() ?? "Nhà vườn và homestay").Trim() : "Nhà vườn và homestay";
            string category = payload.TryGetProperty("category", out var cp) ? (cp.GetString() ?? group).Trim() : group;
            double lat = payload.TryGetProperty("latitude", out var latP) && latP.TryGetDouble(out var lv) ? lv : 16.4565;
            double lng = payload.TryGetProperty("longitude", out var lngP) && lngP.TryGetDouble(out var lnv) ? lnv : 107.5490;
            string address = payload.TryGetProperty("address", out var ap) ? (ap.GetString() ?? "Phường Kim Long, TP. Huế").Trim() : "Phường Kim Long, TP. Huế";
            double dist = payload.TryGetProperty("distanceKm", out var dp) && dp.TryGetDouble(out var dv) ? dv : 0.3;
            string phone = payload.TryGetProperty("phone", out var php) ? (php.GetString() ?? "0905 123 456").Trim() : "0905 123 456";
            string hours = payload.TryGetProperty("openHours", out var hp) ? (hp.GetString() ?? "07:00 - 21:00").Trim() : "07:00 - 21:00";
            string img = payload.TryGetProperty("imageUrl", out var ip) ? (ip.GetString() ?? "images/nha_ruong_1.jpg").Trim() : "images/nha_ruong_1.jpg";
            string desc = payload.TryGetProperty("description", out var dsp) ? (dsp.GetString() ?? "").Trim() : "";
            string servicesJson = payload.TryGetProperty("services", out var svp) && svp.ValueKind == JsonValueKind.Array ? svp.GetRawText() : "[\"Tham quan & Liên kết\"]";

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Tên đối tác địa phương không được để trống.");

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM dbo.LocalPartners WHERE PartnerId = @Id;",
                    new { Id = id }, tx);

                if (exists > 0)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE dbo.LocalPartners
                        SET Name = @Name, Category = @Category, PartnerGroup = @Group,
                            Latitude = @Lat, Longitude = @Lng, Address = @Address,
                            DistanceKm = @Dist, Phone = @Phone, OpenHours = @Hours,
                            ImageUrl = @Img, Description = @Desc, ServicesJson = @Services,
                            IsActive = 1, UpdatedAt = SYSUTCDATETIME(), SyncVersion = SyncVersion + 1
                        WHERE PartnerId = @Id;",
                        new { Id = id, Name = name, Category = category, Group = group, Lat = lat, Lng = lng, Address = address, Dist = dist, Phone = phone, Hours = hours, Img = img, Desc = desc, Services = servicesJson }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO dbo.LocalPartners (
                            PartnerId, Name, Category, PartnerGroup, Latitude, Longitude,
                            Address, DistanceKm, Phone, OpenHours, ImageUrl, Description,
                            ServicesJson, IsActive, CreatedAt, UpdatedAt, SyncVersion
                        ) VALUES (
                            @Id, @Name, @Category, @Group, @Lat, @Lng,
                            @Address, @Dist, @Phone, @Hours, @Img, @Desc,
                            @Services, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), 1
                        );",
                        new { Id = id, Name = name, Category = category, Group = group, Lat = lat, Lng = lng, Address = address, Dist = dist, Phone = phone, Hours = hours, Img = img, Desc = desc, Services = servicesJson }, tx);
                }

                await conn.ExecuteAsync(@"
                    INSERT INTO dbo.SyncAuditLogs (EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion)
                    VALUES ('partners', @Id, 'UPSERT', 'WEB_API', @Actor, @Summary, ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0));",
                    new { Id = id, Actor = actor, Summary = $"[PARTNER] Cập nhật đối tác {name} ({id}) trên bản đồ số" }, tx);

                await tx.CommitAsync();
                Interlocked.Increment(ref _successfulWrites);
                _lastSyncTimestamp = DateTime.UtcNow;

                await BroadcastEntityChangeAsync("partners", id, "UPSERT", $"Đối tác {name} đã đồng bộ SQL", actor);
                return new { success = true, id };
            }
            catch (Exception ex)
            {
                if (tx.Connection != null) await tx.RollbackAsync();
                Interlocked.Increment(ref _failedWrites);
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task BroadcastEntityChangeAsync(string entityType, string entityKey, string actionType, string summary, string changedBy, string source = "WEB_API")
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("DataChanged", new
                {
                    entityType,
                    entityKey,
                    actionType,
                    summary,
                    changedBy,
                    source,
                    changeVersion = _lastKnownChangeVersion,
                    timestamp = DateTime.UtcNow
                });
            }
            catch
            {
                // Không làm gián đoạn luồng chính nếu SignalR chưa có client
            }
        }
    }
}

