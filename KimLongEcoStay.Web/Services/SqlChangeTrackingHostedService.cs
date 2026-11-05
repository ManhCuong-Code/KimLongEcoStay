using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;

namespace KimLongEcoStay.Web.Services
{
    /// <summary>
    /// Dịch vụ nền (BackgroundService) theo dõi SQL Server Change Tracking & bảng SyncAuditLogs
    /// Giúp phát hiện tức thì các thay đổi trực tiếp từ SQL Server Management Studio (SSMS),
    /// Stored Procedures hoặc tiến trình ngoài và đẩy thông báo SignalR thời gian thực về Website.
    /// </summary>
    public class SqlChangeTrackingHostedService : BackgroundService
    {
        private readonly TwoWaySqlSyncService _syncService;
        private readonly IHubContext<EcoStaySyncHub> _hubContext;
        private long _lastAuditId = -1;
        private long _lastChangeTrackingVersion = -1;

        public SqlChangeTrackingHostedService(TwoWaySqlSyncService syncService, IHubContext<EcoStaySyncHub> hubContext)
        {
            _syncService = syncService;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Chờ máy chủ khởi động ổn định
            await Task.Delay(1500, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollSqlChangesAsync(stoppingToken);
                }
                catch
                {
                    // Bỏ qua lỗi kết nối tạm thời, tự động thử lại ở chu kỳ kế tiếp
                }

                await Task.Delay(1500, stoppingToken);
            }
        }

        private async Task PollSqlChangesAsync(CancellationToken ct)
        {
            await using var conn = new SqlConnection(_syncService.ConnectionString);
            await conn.OpenAsync(ct);

            var watermark = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT 
                    ISNULL(CHANGE_TRACKING_CURRENT_VERSION(), 0) AS CtVer,
                    ISNULL((SELECT MAX(Id) FROM dbo.SyncAuditLogs), 0) AS MaxAuditId;");

            if (watermark == null) return;

            long currentCtVer = (long)watermark.CtVer;
            long currentMaxAuditId = (long)watermark.MaxAuditId;

            // Khởi tạo mốc ban đầu ở lần chạy đầu tiên
            if (_lastAuditId < 0)
            {
                _lastAuditId = currentMaxAuditId;
                _lastChangeTrackingVersion = currentCtVer;
                return;
            }

            if (currentMaxAuditId > _lastAuditId || currentCtVer > _lastChangeTrackingVersion)
            {
                var newEvents = (await conn.QueryAsync<dynamic>(@"
                    SELECT TOP 10 Id, EntityType, EntityKey, ActionType, Source, ChangedBy, Summary, ChangeVersion, CreatedAt
                    FROM dbo.SyncAuditLogs
                    WHERE Id > @LastId
                    ORDER BY Id ASC;",
                    new { LastId = _lastAuditId })).ToList();

                _lastAuditId = currentMaxAuditId;
                _lastChangeTrackingVersion = currentCtVer;

                if (newEvents.Count > 0)
                {
                    foreach (var ev in newEvents)
                    {
                        string src = (string?)ev.Source ?? "SQL_ENGINE";
                        // Nếu đã được broadcast trực tiếp bởi WEB_API thì chỉ phát bổ sung cho các thay đổi từ SQL_ENGINE / SSMS
                        if (!string.Equals(src, "WEB_API", StringComparison.OrdinalIgnoreCase))
                        {
                            await _hubContext.Clients.All.SendAsync("DataChanged", new
                            {
                                id = (long)ev.Id,
                                entityType = (string)ev.EntityType,
                                entityKey = (string)ev.EntityKey,
                                actionType = (string)ev.ActionType,
                                source = src,
                                changedBy = (string)ev.ChangedBy,
                                summary = (string?)ev.Summary ?? $"Cập nhật từ SQL Server ({ev.EntityType}: {ev.EntityKey})",
                                changeVersion = currentCtVer,
                                timestamp = (DateTime)ev.CreatedAt
                            }, ct);
                        }
                    }
                }
                else
                {
                    // Có thay đổi Change Tracking nhưng không qua Trigger
                    await _hubContext.Clients.All.SendAsync("DataChanged", new
                    {
                        entityType = "all",
                        entityKey = "SQL_CHANGE_TRACKING",
                        actionType = "SYNC",
                        source = "SQL_CHANGE_TRACKING",
                        changedBy = "SQL Server",
                        summary = $"Phát hiện thay đổi CSDL SQL Server (Change Tracking v{currentCtVer})",
                        changeVersion = currentCtVer,
                        timestamp = DateTime.UtcNow
                    }, ct);
                }
            }
        }
    }
}

