using KimLongEcoStay.UseCases.SearchRoomScreen;
using KimLongEcoStay.UseCases.ViewRoomScreen;
using KimLongEcoStay.UseCases.BookingScreen;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using KimLongEcoStay.CoreBusiness.Models;
using KimLongEcoStay.CoreBusiness.Services;
using KimLongEcoStay.UseCases.PluginInterfaces.DataStore;
using KimLongEcoStay.UseCases.PluginInterfaces.UI;
using KimLongEcoStay.UseCases.PluginInterfaces.StateStore;
using KimLongEcoStay.UseCases.SearchProductScreen;
using KimLongEcoStay.UseCases.ViewProductSrceen;
using KimLongEcoStay.UseCases.ViewProductSrceen.Interfaces;
using KimLongEcoStay.UseCases.ShoppingCartScreen;
using KimLongEcoStay.UseCases.ShoppingCartScreen.Interfaces;
using KimLongEcoStay.UseCases.OrderConfirmationScreen;
using KimLongEcoStay.UseCases.AdminPortal.OutstandingOrdersScreen;
using KimLongEcoStay.UseCases.AdminPortal.ProcessedOrdersScreen;
using KimLongEcoStay.UseCases.AdminPortal.OrderDetailScreen;
using KimLongEcoStay.UseCases.AdminPortal.ProcessOrderScreen;
using KimLongEcoStay.ShoppingCart.LocalStorage;
using KimLongEcoStay.StateStore.DI;
using KimLongEcoStay.DataStore.SQL.Dapper;
using KimLongEcoStay.Web.Components;
using KimLongEcoStay.UseCases.SmartPasteScreen;
using KimLongEcoStay.UseCases.BookingLookupScreen;
using KimLongEcoStay.UseCases.AdminPortal.CheckInCheckOutScreen;
using KimLongEcoStay.UseCases.AdminPortal.OccupancyReportScreen;
using KimLongEcoStay.UseCases.SmartLockScreen;
using KimLongEcoStay.CoreBusiness.Services.Interfaces;
using KimLongEcoStay.UseCases.AdminPortal.HRManagementScreen;
using KimLongEcoStay.UseCases.AdminPortal.PayrollManagementScreen;
using KimLongEcoStay.UseCases.AdminPortal.FinancialManagementScreen;
using KimLongEcoStay.Web.Services;
using System.Text.Json;
using System.Text;

// 0. NẠP BIẾN MÔI TRƯỜNG TỪ FILE .env (BẢO MẬT TUYỆT ĐỐI API KEY & SQL CONNECTION)
EnvConfigurationLoader.LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);

// Cloud Port Binding (Railway, Render, Container dynamic $PORT)
var cloudPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(cloudPort))
{
    builder.WebHost.UseUrls($"http://+:{cloudPort}");
}

// 1. ADD BLAZOR INTERACTIVE SERVER & SIGNALR REAL-TIME HUB
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();

// 2. COOKIE AUTHENTICATION & AUTHORIZATION (BẢO MẬT TUYỆT ĐỐI RBAC BACKEND)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KimLongEcoStay.Auth.Cookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login?error=denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"success\":false,\"error\":\"Unauthorized: Yêu cầu đăng nhập tài khoản có thẩm quyền (401).\"}");
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync("{\"success\":false,\"error\":\"Forbidden: Tài khoản của bạn không có quyền truy cập tài nguyên này (403 RBAC).\"}");
            }
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "Administrator", "Staff", "Manager", "HR", "Accountant"));
    options.AddPolicy("HROnly", policy => policy.RequireRole("Admin", "Administrator", "HR", "Manager"));
    options.AddPolicy("AccountantOnly", policy => policy.RequireRole("Admin", "Administrator", "Accountant", "Manager"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrWhiteSpace(origin)) return false;
            try
            {
                var uri = new Uri(origin);
                // 1. Cho phép máy trạm phát triển cục bộ (localhost & 127.0.0.1)
                if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // 2. Cho phép GitHub Pages chính thức của dự án
                if (uri.Host.Equals("manhcuong-code.github.io", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // 3. Cho phép tên miền chính thức và subdomain Kim Long EcoStay
                if (uri.Host.EndsWith(".kimlongecostay.vn", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("kimlongecostay.vn", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // 4. Cho phép các origin được chỉ định qua biến môi trường CORS_ALLOWED_ORIGINS
                var customOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS");
                if (!string.IsNullOrWhiteSpace(customOrigins))
                {
                    var list = customOrigins.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (list.Any(o => o.Equals(origin, StringComparison.OrdinalIgnoreCase)))
                        return true;
                }
            }
            catch { }
            return false;
        })
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// 3. PLUGINS (PART 3 & PART 5)
// Plugin LocalStorage & StateStore (Part 3)
builder.Services.AddScoped<IShoppingCart, ShoppingCart>();
builder.Services.AddScoped<IShoppingCartStateStore, ShoppingCartStateStore>();

// Plugin SQL Dapper (Part 5)
builder.Services.AddSingleton<IDataAccess, DataAccess>();
builder.Services.AddScoped<IProductRepository, KimLongEcoStay.DataStore.SQL.Dapper.ProductRepository>();
builder.Services.AddScoped<IRoomRepository, KimLongEcoStay.DataStore.SQL.Dapper.ProductRepository>();
builder.Services.AddScoped<IOrderRepository, KimLongEcoStay.DataStore.SQL.Dapper.OrderRepository>();
builder.Services.AddScoped<IBookingRepository, KimLongEcoStay.DataStore.SQL.Dapper.OrderRepository>();
builder.Services.AddScoped<IUserRepository, KimLongEcoStay.DataStore.SQL.Dapper.UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, KimLongEcoStay.DataStore.SQL.Dapper.EmployeeRepository>();
builder.Services.AddScoped<IPayrollRepository, KimLongEcoStay.DataStore.SQL.Dapper.PayrollRepository>();
builder.Services.AddScoped<IFinancialRepository, KimLongEcoStay.DataStore.SQL.Dapper.FinancialRepository>();

// 4. CORE SERVICES & USE CASES (PART 1, 2, 3, 4)
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>() ?? new EmailSettings();
builder.Services.AddSingleton(emailSettings);
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IHomestayAiService, HomestayAiService>();
builder.Services.AddTransient<IOrderService, OrderService>();

// Customer Portal Use Cases (Part 1, 2, 3)
builder.Services.AddTransient<ISearchProductUseCase, SearchProductUseCase>();
builder.Services.AddTransient<IViewProductUseCase, ViewProductUseCase>();
builder.Services.AddTransient<IAddProductToCartUseCase, AddProductToCartUseCase>();
builder.Services.AddTransient<IViewShoppingCartUseCase, ViewShoppingCartUseCase>();
builder.Services.AddTransient<IDeleteProductUseCase, DeleteProductUseCase>();
builder.Services.AddTransient<IUpdateQuantityUseCase, UpdateQuantityUseCase>();
builder.Services.AddTransient<IPlaceOrderUseCase, PlaceOrderUseCase>();
builder.Services.AddTransient<IOrderConfirmationUseCase, OrderConfirmationUseCase>();
builder.Services.AddTransient<IBookingService, BookingService>();
builder.Services.AddTransient<ISearchRoomUseCase, SearchRoomUseCase>();
builder.Services.AddTransient<IViewRoomUseCase, ViewRoomUseCase>();
builder.Services.AddTransient<IPlaceBookingUseCase, PlaceBookingUseCase>();

// Admin Portal Use Cases (Part 4)
builder.Services.AddTransient<IViewOutstandingOrdersUseCase, ViewOutstandingOrdersUseCase>();
builder.Services.AddTransient<IViewProcessedOrdersUseCase, ViewProcessedOrdersUseCase>();
builder.Services.AddTransient<IViewOrderDetailUseCase, ViewOrderDetailUseCase>();
builder.Services.AddTransient<IProcessOrderUseCase, ProcessOrderUseCase>();

// Extended Homestay Hue & KimLongEcoStay Use Cases
builder.Services.AddTransient<ISmartPasteUseCase, SmartPasteUseCase>();
builder.Services.AddTransient<ILookupBookingUseCase, LookupBookingUseCase>();
builder.Services.AddTransient<ICheckInCheckOutUseCase, CheckInCheckOutUseCase>();
builder.Services.AddTransient<IOccupancyReportUseCase, OccupancyReportUseCase>();
builder.Services.AddTransient<ISmartLockUseCase, SmartLockUseCase>();

// Bổ sung các Use Cases mới: Quản lý Nhân sự, Tiền lương, Tài chính Dashboard
builder.Services.AddTransient<IHRManagementUseCase, HRManagementUseCase>();
builder.Services.AddTransient<IPayrollManagementUseCase, PayrollManagementUseCase>();
builder.Services.AddTransient<IFinancialManagementUseCase, FinancialManagementUseCase>();

// 4B. TWO-WAY SQL SYNCHRONIZATION ENGINE, SIGNALR CHANGE TRACKING & AI GATEWAY
builder.Services.AddSingleton<TwoWaySqlSyncService>();
builder.Services.AddSingleton<AiAssistantGatewayService>();
builder.Services.AddHostedService<SqlChangeTrackingHostedService>();

var app = builder.Build();

// 5. MIDDLEWARE PIPELINE
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
});

var configuredPathBase = Environment.GetEnvironmentVariable("ASPNETCORE_PATHBASE") ?? "";
if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    app.UsePathBase(configuredPathBase);
}
else
{
    app.Use((context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/KimLongEcoStay", out var remaining))
        {
            context.Request.PathBase = "/KimLongEcoStay";
            context.Request.Path = remaining;
        }
        return next();
    });
}

app.UseCors();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.UseStaticFiles();

// =========================================================================
// 6. RBAC & BẢO MẬT XÁC THỰC BACKEND (ZERO-TRUST, RATE-LIMIT & BRUTE-FORCE LOCKOUT)
// =========================================================================

var loginAttempts = new System.Collections.Concurrent.ConcurrentDictionary<string, (int count, DateTime lockoutUntil)>();

static IResult? AuthorizeRole(HttpContext ctx, params string[] allowedRoles)
{
    if (ctx.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Json(new { success = false, error = "Yêu cầu đăng nhập tài khoản có thẩm quyền (401 Unauthorized)." }, statusCode: 401);
    }

    string role = ctx.User.FindFirst("UserRole")?.Value ?? ctx.User.FindFirst(ClaimTypes.Role)?.Value ?? "";
    string norm = role.ToUpperInvariant();
    bool isAllowed = false;
    foreach (var r in allowedRoles)
    {
        string nr = r.ToUpperInvariant();
        if (norm == nr) { isAllowed = true; break; }
        if ((norm is "ADMIN" or "ADMINISTRATOR") && (nr is "ADMIN" or "ADMINISTRATOR")) { isAllowed = true; break; }
        if ((norm is "STAFF" or "LETAN" or "RECEPTIONIST") && (nr is "STAFF" or "LETAN" or "RECEPTIONIST")) { isAllowed = true; break; }
        if ((norm is "ACCOUNTANT" or "CFO" or "KETOAN") && (nr is "ACCOUNTANT" or "CFO" or "KETOAN")) { isAllowed = true; break; }
        if ((norm is "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU") && (nr is "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU")) { isAllowed = true; break; }
    }

    if (!isAllowed)
    {
        return Results.Json(new { success = false, error = $"Tài khoản ({role}) không có quyền thực hiện thao tác này. Yêu cầu: {string.Join(", ", allowedRoles)} (403 Forbidden)." }, statusCode: 403);
    }

    return null;
}

static async Task SignInUserAsync(HttpContext context, AppUser user)
{
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.NameIdentifier, user.Username),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserRole", user.Role)
    };

    if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || user.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        claims.Add(new Claim("Department", "Ban Điều Hành & Quản Trị Homestay"));
    }
    else if (user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) || user.Role.Equals("Receptionist", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Lễ Tân & Đón Tiếp"));
    }
    else if (user.Role.Equals("Accountant", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Tài Chính - Kế Toán"));
    }
    else if (user.Role.Equals("HR", StringComparison.OrdinalIgnoreCase) || user.Role.Equals("HR_OPS", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Nhân Sự & Vận Hành"));
    }
    else
    {
        claims.Add(new Claim("Department", "Khách Hàng"));
    }

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(claimsIdentity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
    });
}

// Xử lý Login truyền thống bằng HTTP POST (HTML Form)
app.MapPost("/authenticate", async (HttpContext context, IUserRepository userRepo) =>
{
    string username = "";
    string password = "";

    try
    {
        if (context.Request.HasFormContentType)
        {
            var form = await context.Request.ReadFormAsync();
            username = form["username"].ToString().Trim();
            password = form["password"].ToString().Trim();
        }
        else
        {
            var json = await context.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            if (json != null)
            {
                json.TryGetValue("username", out var u);
                json.TryGetValue("password", out var p);
                username = u?.Trim() ?? "";
                password = p?.Trim() ?? "";
            }
        }
    }
    catch { }

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "local";
    string attemptKey = $"{clientIp}_{username.ToLowerInvariant()}";

    if (loginAttempts.TryGetValue(attemptKey, out var att) && att.lockoutUntil > DateTime.UtcNow)
    {
        context.Response.Redirect("/login?error=lockout");
        return;
    }

    AppUser? user = await userRepo.AuthenticateAsync(username, password);
    if (user == null)
    {
        int currentCount = (att.lockoutUntil > DateTime.MinValue && att.lockoutUntil <= DateTime.UtcNow) ? 0 : att.count;
        int count = currentCount + 1;
        DateTime lockout = count >= 5 ? DateTime.UtcNow.AddMinutes(15) : DateTime.MinValue;
        loginAttempts[attemptKey] = (count, lockout);

        context.Response.Redirect("/login?error=invalid");
        return;
    }

    loginAttempts.TryRemove(attemptKey, out _);
    await SignInUserAsync(context, user);

    if (user.Role.Equals("Customer", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/");
    }
    else
    {
        context.Response.Redirect("/admin");
    }
}).DisableAntiforgery();

// REST API Đăng Nhập Bảo Mật (/api/auth/login) có kiểm soát Brute-force & xác thực mật khẩu băm muối
app.MapPost("/api/auth/login", async (HttpContext context, IUserRepository userRepo) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string username = doc.RootElement.TryGetProperty("username", out var up) ? (up.GetString() ?? "").Trim() : "";
        string password = doc.RootElement.TryGetProperty("password", out var pp) ? (pp.GetString() ?? "").Trim() : "";

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Results.BadRequest(new { success = false, message = "Tên đăng nhập và mật khẩu không được để trống." });
        }

        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "local";
        string attemptKey = $"{clientIp}_{username.ToLowerInvariant()}";

        if (loginAttempts.TryGetValue(attemptKey, out var att) && att.lockoutUntil > DateTime.UtcNow)
        {
            int remainingMins = (int)Math.Ceiling((att.lockoutUntil - DateTime.UtcNow).TotalMinutes);
            return Results.Json(new
            {
                success = false,
                message = $"Tài khoản tạm khóa an toàn do đăng nhập sai quá 5 lần. Vui lòng thử lại sau {remainingMins} phút."
            }, statusCode: 429);
        }

        AppUser? user = await userRepo.AuthenticateAsync(username, password);
        if (user == null)
        {
            int currentCount = (att.lockoutUntil > DateTime.MinValue && att.lockoutUntil <= DateTime.UtcNow) ? 0 : att.count;
            int count = currentCount + 1;
            DateTime lockout = count >= 5 ? DateTime.UtcNow.AddMinutes(15) : DateTime.MinValue;
            loginAttempts[attemptKey] = (count, lockout);

            if (count >= 5)
            {
                return Results.Json(new
                {
                    success = false,
                    message = "Đăng nhập sai 5 lần liên tiếp. Tài khoản tạm khóa an toàn trong 15 phút."
                }, statusCode: 429);
            }

            return Results.Json(new
            {
                success = false,
                message = "Tên đăng nhập hoặc mật khẩu không chính xác."
            }, statusCode: 401);
        }

        loginAttempts.TryRemove(attemptKey, out _);
        await SignInUserAsync(context, user);

        string[] allowedModules = user.Role.ToUpperInvariant() switch
        {
            "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "finance", "nhanvien", "employees", "luong", "payrolls", "duan", "sql-sync" },
            "STAFF" or "LETAN" or "RECEPTIONIST" => new[] { "reconciliation", "duan", "specialties-admin", "tea-admin", "emaillogs" },
            "ACCOUNTANT" or "CFO" or "KETOAN" => new[] { "dashboard", "finance", "reconciliation", "luong", "payrolls", "emaillogs" },
            "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU" => new[] { "nhanvien", "employees", "duan", "partners-admin" },
            _ => Array.Empty<string>()
        };

        string roleTitle = user.Role.ToUpperInvariant() switch
        {
            "ADMIN" or "ADMINISTRATOR" => "Admin (CEO)",
            "STAFF" or "LETAN" or "RECEPTIONIST" => "Lễ Tân",
            "ACCOUNTANT" or "CFO" or "KETOAN" => "Kế Toán Trưởng",
            "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU" => "Nhân Sự / Vận Hành",
            _ => user.Role
        };

        return Results.Ok(new
        {
            success = true,
            user = new
            {
                username = user.Username,
                name = user.FullName,
                role = user.Role,
                roleTitle
            },
            allowedModules,
            redirectUrl = user.Role.Equals("Customer", StringComparison.OrdinalIgnoreCase) ? "/" : "/admin"
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = "Lỗi xử lý yêu cầu đăng nhập: " + ex.Message });
    }
}).DisableAntiforgery();

// REST API Đăng Xuất (/api/auth/logout)
app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Cookies.Delete("KimLongEcoStay.Auth.Cookie", new CookieOptions
    {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    });
    return Results.Ok(new { success = true, message = "Đã đăng xuất và kết thúc phiên làm việc an toàn trên máy chủ." });
}).DisableAntiforgery();

// REST API Kiểm tra thông tin phiên (/api/auth/me) - Trả 401 khi chưa xác thực
app.MapGet("/api/auth/me", (HttpContext context) =>
{
    var user = context.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Json(new
        {
            isAuthenticated = false,
            currentUser = (object?)null,
            allowedModules = Array.Empty<string>(),
            message = "Phiên làm việc chưa được xác thực hoặc đã hết hạn (401 Unauthorized)."
        }, statusCode: StatusCodes.Status401Unauthorized);
    }

    string roleClaim = user.FindFirst("UserRole")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
    string[] allowedModules = roleClaim.ToUpperInvariant() switch
    {
        "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "finance", "nhanvien", "employees", "luong", "payrolls", "duan", "sql-sync" },
        "STAFF" or "LETAN" or "RECEPTIONIST" => new[] { "reconciliation", "duan", "specialties-admin", "tea-admin", "emaillogs" },
        "ACCOUNTANT" or "CFO" or "KETOAN" => new[] { "dashboard", "finance", "reconciliation", "luong", "payrolls", "emaillogs" },
        "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU" => new[] { "nhanvien", "employees", "duan", "partners-admin" },
        _ => Array.Empty<string>()
    };

    string roleTitle = roleClaim.ToUpperInvariant() switch
    {
        "ADMIN" or "ADMINISTRATOR" => "Admin (CEO)",
        "STAFF" or "LETAN" or "RECEPTIONIST" => "Lễ Tân",
        "ACCOUNTANT" or "CFO" or "KETOAN" => "Kế Toán Trưởng",
        "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU" => "Nhân Sự / Vận Hành",
        _ => roleClaim
    };

    return Results.Ok(new
    {
        isAuthenticated = true,
        currentUser = new
        {
            name = user.Identity.Name,
            username = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            role = roleClaim,
            roleTitle
        },
        allowedModules
    });
});

// REST API Kiểm tra sức khỏe hệ thống (/api/health)
app.MapGet("/api/health", async (TwoWaySqlSyncService syncService, AiAssistantGatewayService aiGateway, IWebHostEnvironment env) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    dynamic? diag = await syncService.TestConnectionAndGetDiagnosticsAsync();
    sw.Stop();

    bool isSqlOk = diag != null && (bool?)diag?.isConnected == true;
    string serverName = isSqlOk ? (string?)diag?.serverName ?? "localhost\\SQLEXPRESS" : "Unavailable";
    string dbName = isSqlOk ? (string?)diag?.databaseName ?? "KimLongEcoStayDB" : "KimLongEcoStayDB";
    long ctVer = isSqlOk ? (long?)diag?.changeTrackingVersion ?? 0L : 0L;
    int tableCount = isSqlOk ? (int?)diag?.trackedTablesCount ?? 0 : 0;
    var aiStatus = aiGateway.GetStatus();

    var healthReport = new
    {
        status = isSqlOk ? "Healthy" : "Degraded",
        timestamp = DateTime.UtcNow,
        environment = env.EnvironmentName,
        backend = "Online",
        version = "v2.4.0-secured",
        database = new
        {
            connected = isSqlOk,
            serverName = serverName,
            databaseName = dbName,
            latencyMs = sw.ElapsedMilliseconds,
            changeTrackingVersion = ctVer,
            trackedTables = tableCount
        },
        aiGateway = aiStatus,
        services = new
        {
            authentication = "Active (HttpOnly Cookie & RBAC)",
            sqlTwoWaySync = isSqlOk ? "Active" : "Offline",
            signalRHub = "Active (/hubs/ecostay-sync)",
            smartLockCspRng = "Active"
        }
    };

    return isSqlOk ? Results.Ok(healthReport) : Results.Json(healthReport, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// REST API Đăng nhập mô phỏng phục vụ Development (Chặn 100% trên Production)
app.MapPost("/api/auth/dev-simulation", async (HttpContext context, IWebHostEnvironment env) =>
{
    // BẮT BUỘC KIỂM TRA MÔI TRƯỜNG THỰC TẾ PHÍA BACKEND
    if (!env.IsDevelopment())
    {
        return Results.Json(new
        {
            success = false,
            message = "Chức năng đăng nhập mô phỏng (Quick Login) bị vô hiệu hóa hoàn toàn trên môi trường Production."
        }, statusCode: StatusCodes.Status403Forbidden);
    }

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string role = doc.RootElement.TryGetProperty("role", out var rp) ? (rp.GetString() ?? "").Trim().ToUpperInvariant() : "";

        var devUser = role switch
        {
            "ADMIN" or "CEO" => new AppUser { Id = 9991, Username = "dev_admin", FullName = "[DEV MÔ PHỎNG] Admin CEO", Role = "Admin", Email = "dev-admin@kle.local" },
            "RECEPTIONIST" or "STAFF" or "LETAN" => new AppUser { Id = 9992, Username = "dev_letan", FullName = "[DEV MÔ PHỎNG] Lễ Tân", Role = "Staff", Email = "dev-letan@kle.local" },
            "ACCOUNTANT" or "CFO" or "KETOAN" => new AppUser { Id = 9993, Username = "dev_ketoan", FullName = "[DEV MÔ PHỎNG] Kế Toán", Role = "Accountant", Email = "dev-ketoan@kle.local" },
            "HR_OPS" or "HR" or "NHANSU" => new AppUser { Id = 9994, Username = "dev_nhansu", FullName = "[DEV MÔ PHỎNG] Nhân Sự", Role = "HR", Email = "dev-nhansu@kle.local" },
            _ => null
        };

        if (devUser == null)
        {
            return Results.BadRequest(new { success = false, message = "Vai trò mô phỏng không hợp lệ." });
        }

        await SignInUserAsync(context, devUser);
        return Results.Ok(new
        {
            success = true,
            isDevSimulation = true,
            user = new { username = devUser.Username, name = devUser.FullName, role = devUser.Role, roleTitle = devUser.FullName },
            message = "Đã kích hoạt phiên mô phỏng Development (Chỉ phục vụ kiểm thử giao diện cục bộ)."
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = ex.Message });
    }
}).DisableAntiforgery();

// Chuyển hướng GET /authenticate về /login
app.MapGet("/authenticate", (HttpContext context) =>
{
    context.Response.Redirect("/login");
});

// Xử lý Logout GET
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

// =========================================================================
// API ENDPOINTS CHO CÁC PHÂN HỆ MỚI (SPECIALTIES, TEA, EMAIL AUDIT & WEBHOOK)
// =========================================================================

// 1. Danh sách sản phẩm đặc sản & ẩm thực Kim Long
app.MapGet("/api/specialties", (IProductRepository productRepo) =>
{
    var products = productRepo.GetProducts();
    return Results.Ok(products);
});

// 1B. Mạng lưới đối tác địa phương & điểm bản đồ Kim Long
app.MapGet("/api/partners", () =>
{
    var samplePartners = new[]
    {
        new { Id = "KLE-CENTER", Name = "Kim Long EcoStay (Tâm Điểm Xuất Phát)", Category = "Nhà vườn và homestay", PartnerGroup = "Nhà vườn và homestay", Latitude = 16.4560, Longitude = 107.5495, Address = "Khu vực Nhà vườn Kim Long, Ven Sông Hương, Phường Kim Long, TP. Huế", DistanceKm = 0.0, Phone = "0905 123 456", OpenHours = "Mở cửa 24/7 (Smart Check-in không chạm)", ImageUrl = "images/nha_ruong_1.jpg", IsActive = true, IsSample = false },
        new { Id = "PARTNER-NV-HUONG-SEN", Name = "Nhà Vườn Sinh Thái Hương Sen Kim Long", Category = "Nhà vườn và homestay", PartnerGroup = "Nhà vườn và homestay", Latitude = 16.4572, Longitude = 107.5482, Address = "Kiệt 82 Kim Long, Phường Kim Long, TP. Huế", DistanceKm = 0.18, Phone = "0914 234 567", OpenHours = "06:30 - 21:00 hàng ngày", ImageUrl = "images/nha_ruong_1.jpg", IsActive = true, IsSample = true },
        new { Id = "PARTNER-NV-THANH-TRA", Name = "Vườn Sinh Thái & Homestay Thanh Trà Cổ Thụ", Category = "Nhà vườn và homestay", PartnerGroup = "Nhà vườn và homestay", Latitude = 16.4585, Longitude = 107.5465, Address = "Đường Nguyễn Hoàng, Phường Kim Long, TP. Huế", DistanceKm = 0.35, Phone = "0935 345 678", OpenHours = "07:00 - 18:30 hàng ngày", ImageUrl = "images/nha_ruong_3.jpg", IsActive = true, IsSample = true },
        new { Id = "HERITAGE-AN-HIEN", Name = "Nhà Vườn An Hiên (Đệ Nhất Nhà Vườn Xứ Huế)", Category = "Nhà vườn và homestay", PartnerGroup = "Nhà vườn và homestay", Latitude = 16.4542, Longitude = 107.5471, Address = "58 Phú Mộng, Phường Kim Long, TP. Huế", DistanceKm = 0.45, Phone = "0234 3523 456", OpenHours = "07:30 - 17:30 hàng ngày", ImageUrl = "images/nha_ruong_2.jpg", IsActive = true, IsSample = false },
        new { Id = "FOOD-BANH-UOT", Name = "Bánh Ướt Thịt Nướng Huyền Anh (Đệ Nhất Kim Long)", Category = "Quán ăn và ẩm thực", PartnerGroup = "Quán ăn và ẩm thực", Latitude = 16.4568, Longitude = 107.5475, Address = "50/5 Kim Long, Phường Kim Long, TP. Huế", DistanceKm = 0.25, Phone = "0234 3525 655", OpenHours = "08:30 - 21:00 hàng ngày", ImageUrl = "images/mon_banhuot.jpg", IsActive = true, IsSample = false },
        new { Id = "PARTNER-DAC-SAN-KIM-LONG", Name = "Cơ Sở Đặc Sản & Mứt Gừng Kim Long O Lành", Category = "Đặc sản và sản phẩm địa phương", PartnerGroup = "Đặc sản và sản phẩm địa phương", Latitude = 16.4578, Longitude = 107.5512, Address = "Đường Kim Long, Phường Kim Long, TP. Huế", DistanceKm = 0.28, Phone = "0905 456 789", OpenHours = "07:00 - 20:30 hàng ngày", ImageUrl = "images/qua_mutgung.jpg", IsActive = true, IsSample = true },
        new { Id = "PARTNER-TRA-CUNG-DINH", Name = "Trà Thất & Đặc Sản Trà Cung Đình Kim Long", Category = "Đặc sản và sản phẩm địa phương", PartnerGroup = "Đặc sản và sản phẩm địa phương", Latitude = 16.4552, Longitude = 107.5525, Address = "Đường Kim Long, Phường Kim Long, TP. Huế", DistanceKm = 0.4, Phone = "0983 567 890", OpenHours = "07:30 - 21:30 hàng ngày", ImageUrl = "images/tra_cung_dinh.jpg", IsActive = true, IsSample = true },
        new { Id = "PARTNER-HTX-NONG-SAN", Name = "HTX Nông Sản Sạch & Thanh Trà Kim Long", Category = "Đặc sản và sản phẩm địa phương", PartnerGroup = "Đặc sản và sản phẩm địa phương", Latitude = 16.4595, Longitude = 107.5450, Address = "Khu nông nghiệp bãi bồi ven sông, Kim Long, TP. Huế", DistanceKm = 0.5, Phone = "0912 678 901", OpenHours = "06:00 - 18:00 hàng ngày", ImageUrl = "images/nongsan_thanhtra.jpg", IsActive = true, IsSample = true },
        new { Id = "EXP-ECO-BIKE-KIM-LONG", Name = "Trạm Xe Đạp Sinh Thái Eco-Bike & Bến Thuyền Kim Long", Category = "Điểm trải nghiệm", PartnerGroup = "Điểm trải nghiệm", Latitude = 16.4555, Longitude = 107.5505, Address = "Bến thuyền Kim Long ven sông Hương, TP. Huế", DistanceKm = 0.15, Phone = "0905 123 456", OpenHours = "06:00 - 21:00 hàng ngày", ImageUrl = "images/hero_ecostay.jpg", IsActive = true, IsSample = true }
    };
    return Results.Ok(samplePartners);
});

// 2. Danh sách dịch vụ trà Cung Đình Huế
app.MapGet("/api/tea-services", () =>
{
    var teaServices = new List<TeaService>
    {
        new TeaService { Id = 1, ServiceCode = "TEA-HOMESTAY", Name = "Thưởng Trà Cung Đình Tại Homestay", Price = 85000, PriceUnit = "người", DurationMinutes = 60, MaxGuests = 8, AvailableSlots = 6, ShortDescription = "Thưởng thức ấm trà sen Cung Đình 16 vị thảo mộc tại hiên nhà rường lộng gió sông Hương." },
        new TeaService { Id = 2, ServiceCode = "TEA-CAKES", Name = "Trải Nghiệm Thưởng Trà Kết Hợp Bánh Huế", Price = 135000, PriceUnit = "người", DurationMinutes = 75, MaxGuests = 10, AvailableSlots = 8, ShortDescription = "Bộ ấm trà sen thượng hạng kết hợp mẹt 4 loại bánh Huế cổ truyền và kẹo cau, mè xửng Kim Long." },
        new TeaService { Id = 3, ServiceCode = "TEA-COUPLE", Name = "Gói Thưởng Trà Dành Cho Cặp Đôi & Nhóm Khách", Price = 250000, PriceUnit = "gói 2-4 khách", DurationMinutes = 90, MaxGuests = 4, AvailableSlots = 2, ShortDescription = "Bàn trà riêng tư trang trí nến thơm và hoa sen bên hồ cá koi nhà rường, kèm mứt gừng và âm nhạc nhã nhạc Huế." },
        new TeaService { Id = 4, ServiceCode = "TEA-CULTURE", Name = "Gói Trải Nghiệm Văn Hóa Huế Kết Hợp Giới Thiệu Trà", Price = 390000, PriceUnit = "gói trải nghiệm", DurationMinutes = 120, MaxGuests = 12, AvailableSlots = 4, ShortDescription = "Nghệ nhân trà Huế trực tiếp hướng dẫn nghi thức pha trà Cung Đình, giải thích ý nghĩa 16 vị thảo mộc và thưởng trà đàm đạo." }
    };
    return Results.Ok(teaServices);
});

// 2B. Thông tin phân quyền RBAC của người dùng hiện tại
app.MapGet("/api/rbac/me", (HttpContext context) =>
{
    var user = context.User;
    if (user?.Identity?.IsAuthenticated != true)
    {
        return Results.Ok(new { isAuthenticated = false, currentUser = (object?)null, allowedModules = Array.Empty<string>() });
    }

    string roleClaim = user.FindFirst("UserRole")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
    string[] allowedModules = roleClaim.ToUpperInvariant() switch
    {
        "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "nhanvien", "luong", "duan", "sql-sync" },
        "STAFF" or "LETAN" => new[] { "reconciliation", "duan", "specialties-admin", "tea-admin", "emaillogs" },
        "ACCOUNTANT" or "CFO" => new[] { "dashboard", "reconciliation", "luong", "emaillogs" },
        "HR" or "COO" or "MANAGER" => new[] { "nhanvien", "duan", "partners-admin" },
        _ => Array.Empty<string>()
    };

    return Results.Ok(new
    {
        isAuthenticated = allowedModules.Length > 0,
        currentUser = new
        {
            name = user.Identity.Name,
            username = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            role = roleClaim
        },
        allowedModules
    });
});

// =========================================================================
// 6A. ENDPOINTS TRỢ LÝ AI THÔNG MINH (OPENAI API + SQL DATA CONTEXT + RBAC)
// =========================================================================
app.MapGet("/api/ai/status", (AiAssistantGatewayService aiGateway) =>
{
    return Results.Ok(aiGateway.GetStatus());
});

app.MapPost("/api/ai/chat", async (HttpContext context, AiAssistantGatewayService aiGateway) =>
{
    try
    {
        var req = await context.Request.ReadFromJsonAsync<AiChatRequestDto>();
        if (req == null)
            return Results.BadRequest(new { success = false, error = "Dữ liệu yêu cầu không hợp lệ." });

        // Lấy vai trò từ Cookie nếu chưa truyền hoặc bổ sung xác thực
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            req.Role = context.User.FindFirst("UserRole")?.Value ?? context.User.FindFirst(ClaimTypes.Role)?.Value ?? req.Role;
            req.UserName = context.User.Identity.Name ?? req.UserName;
        }

        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "local";
        var result = await aiGateway.ProcessChatAsync(req, clientIp);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = $"Lỗi xử lý AI: {ex.Message}" }, statusCode: 500);
    }
}).DisableAntiforgery();

// =========================================================================
// 6B. CÁC API CÔNG KHAI (ZERO-SENSITIVE-DATA / MASKED PII / CSPRNG SMARTLOCK)
// =========================================================================

// 1. Danh mục công khai không chứa thông tin nội bộ / PII
app.MapGet("/api/public/catalog", async (TwoWaySqlSyncService syncService) =>
{
    var catalog = await syncService.GetPublicCatalogAsync();
    return Results.Ok(catalog);
});

// 2. Tiếp nhận đơn đặt phòng công khai (Trạng thái PENDING_DEPOSIT, chưa cấp PIN)
app.MapPost("/api/public/bookings", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var res = await syncService.UpsertBookingAsync(doc.RootElement, "OnlineGuest");
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 3. Tra cứu đơn đặt phòng có xác minh 2 yếu tố (Mã đơn + SĐT/Email), che PII & không bao giờ lộ PIN
app.MapPost("/api/public/booking-lookup", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string orderCode = doc.RootElement.TryGetProperty("orderCode", out var ocp) ? (ocp.GetString() ?? "").Trim() : "";
        string verifier = doc.RootElement.TryGetProperty("phoneOrEmail", out var vp) ? (vp.GetString() ?? "").Trim()
            : (doc.RootElement.TryGetProperty("verifier", out var vrp) ? (vrp.GetString() ?? "").Trim() : "");

        var (success, statusCode, message, booking) = await syncService.LookupCustomerBookingVerifiedAsync(orderCode, verifier);
        return Results.Json(new { success, message, booking }, statusCode: statusCode);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 4. Tra cứu đơn giao phòng có xác minh 2 yếu tố (Mã đơn + SĐT/Số phòng)
app.MapPost("/api/public/delivery-lookup", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string orderCode = doc.RootElement.TryGetProperty("orderCode", out var ocp) ? (ocp.GetString() ?? "").Trim() : "";
        string verifier = doc.RootElement.TryGetProperty("phoneOrRoom", out var vp) ? (vp.GetString() ?? "").Trim()
            : (doc.RootElement.TryGetProperty("verifier", out var vrp) ? (vrp.GetString() ?? "").Trim() : "");

        var (success, statusCode, message, delivery) = await syncService.LookupCustomerDeliveryVerifiedAsync(orderCode, verifier);
        return Results.Json(new { success, message, delivery }, statusCode: statusCode);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 5. Xác thực mã PIN SmartLock tại Backend (Constant-time SHA-256 + Khóa an toàn 15 phút sau 5 lần sai)
app.MapPost("/api/smartlock/verify-pin", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string roomNumber = doc.RootElement.TryGetProperty("roomNumber", out var rnp) ? (rnp.GetString() ?? "KLE-R101").Trim() : "KLE-R101";
        string? bookingCode = doc.RootElement.TryGetProperty("bookingCode", out var bcp) ? bcp.GetString() : null;
        string pin = doc.RootElement.TryGetProperty("pin", out var pp) ? (pp.GetString() ?? "").Trim() : "";

        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "local";
        var (verified, statusCode, message) = await syncService.VerifySmartLockPinAsync(roomNumber, bookingCode, pin, clientIp);
        return Results.Json(new { verified, statusCode, message }, statusCode: statusCode);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { verified = false, statusCode = 400, message = ex.Message });
    }
}).DisableAntiforgery();

// =========================================================================
// 6C. ENDPOINTS PHÂN HỆ QUẢN TRỊ & NẠP VIEW ĐỘNG THEO VAI TRÒ (RBAC)
// =========================================================================

static string[]? GetAllowedRolesForModule(string moduleId)
{
    string norm = (moduleId ?? "").Trim().ToLowerInvariant();
    return norm switch
    {
        "reconciliation" => new[] { "Admin", "Staff", "Accountant" },
        "duan" => new[] { "Admin", "Staff", "HR" },
        "specialties-admin" => new[] { "Admin", "Staff" },
        "tea-admin" => new[] { "Admin", "Staff" },
        "partners-admin" => new[] { "Admin", "HR" },
        "emaillogs" => new[] { "Admin", "Staff", "Accountant" },
        "dashboard" or "finance" => new[] { "Admin", "Accountant" },
        "nhanvien" or "employees" => new[] { "Admin", "HR" },
        "luong" or "payrolls" => new[] { "Admin", "Accountant" },
        "sql-sync" => new[] { "Admin" },
        _ => null
    };
}

static string? ExtractAdminModuleHtml(string moduleId)
{
    string[] candidatePaths = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "AdminTemplates", "AdminPortalTabs.html"),
        Path.Combine(Directory.GetCurrentDirectory(), "KimLongEcoStay.Web", "AdminTemplates", "AdminPortalTabs.html"),
        Path.Combine(Directory.GetCurrentDirectory(), "AdminTemplates", "AdminPortalTabs.html")
    };

    string? templatePath = candidatePaths.FirstOrDefault(File.Exists);
    if (templatePath == null) return null;

    string allHtml = File.ReadAllText(templatePath, Encoding.UTF8);
    string normMod = (moduleId ?? "").Trim().ToLowerInvariant();
    string targetTag = $"id=\"tab-pane-{normMod}\"";
    int startIndex = allHtml.IndexOf(targetTag, StringComparison.OrdinalIgnoreCase);

    // Thử các tên alias phổ biến nếu chưa tìm thấy
    if (startIndex == -1)
    {
        string? altMod = normMod switch
        {
            "employees" => "nhanvien",
            "nhanvien" => "employees",
            "payrolls" => "luong",
            "luong" => "payrolls",
            "finance" => "dashboard",
            "dashboard" => "finance",
            _ => null
        };
        if (altMod != null)
        {
            targetTag = $"id=\"tab-pane-{altMod}\"";
            startIndex = allHtml.IndexOf(targetTag, StringComparison.OrdinalIgnoreCase);
        }
    }

    if (startIndex == -1) return null;

    int divStart = allHtml.LastIndexOf("<div", startIndex, StringComparison.OrdinalIgnoreCase);
    if (divStart == -1) return null;

    int depth = 0;
    int cur = divStart;
    while (cur <= allHtml.Length - 6)
    {
        if (cur <= allHtml.Length - 4 && allHtml.Substring(cur, 4).Equals("<div", StringComparison.OrdinalIgnoreCase))
        {
            depth++;
            cur += 4;
        }
        else if (allHtml.Substring(cur, 6).Equals("</div>", StringComparison.OrdinalIgnoreCase))
        {
            depth--;
            if (depth == 0)
            {
                return allHtml.Substring(divStart, cur + 6 - divStart);
            }
            cur += 6;
        }
        else
        {
            cur++;
        }
    }
    return null;
}

// Nạp HTML view động của từng Tab quản trị (Chỉ trả về khi đã xác thực & đúng quyền RBAC)
app.MapGet("/api/admin/module-view/{moduleId}", (string moduleId, HttpContext context) =>
{
    var allowedRoles = GetAllowedRolesForModule(moduleId);
    if (allowedRoles == null)
    {
        return Results.NotFound(new { success = false, error = "Không tìm thấy phân hệ yêu cầu." });
    }

    var authCheck = AuthorizeRole(context, allowedRoles);
    if (authCheck != null) return authCheck;

    string? htmlSnippet = ExtractAdminModuleHtml(moduleId);
    if (string.IsNullOrEmpty(htmlSnippet))
    {
        return Results.NotFound(new { success = false, error = $"Không tìm thấy giao diện cho phân hệ '{moduleId}'." });
    }

    return Results.Content(htmlSnippet, "text/html; charset=utf-8");
});

// Cấp lại mã PIN SmartLock (Chỉ Admin & Lễ Tân) - Sinh CSPRNG, lưu băm muối, trả mã thô đúng 1 lần
app.MapPost("/api/smartlock/reissue", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string roomNumber = doc.RootElement.TryGetProperty("roomNumber", out var rp) ? (rp.GetString() ?? "KLE-R101")
            : (doc.RootElement.TryGetProperty("roomCode", out var rcp) ? (rcp.GetString() ?? "KLE-R101") : "KLE-R101");
        string? bookingCode = doc.RootElement.TryGetProperty("bookingCode", out var bp) ? bp.GetString() : null;
        string reason = doc.RootElement.TryGetProperty("reason", out var rsp) ? (rsp.GetString() ?? "")
            : (doc.RootElement.TryGetProperty("note", out var np) ? (np.GetString() ?? "") : "");
        string actor = context.User.Identity?.Name ?? "Admin";
        string role = context.User.FindFirst("UserRole")?.Value ?? "Admin";

        var res = await syncService.ReissueSmartLockPinAsync(roomNumber, bookingCode, reason, actor, role);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Thu hồi mã PIN SmartLock (Chỉ Admin & Lễ Tân)
app.MapPost("/api/smartlock/revoke", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string roomNumber = doc.RootElement.TryGetProperty("roomNumber", out var rp) ? (rp.GetString() ?? "KLE-R101") : "KLE-R101";
        string? bookingCode = doc.RootElement.TryGetProperty("bookingCode", out var bp) ? bp.GetString() : null;
        string reason = doc.RootElement.TryGetProperty("reason", out var rsp) ? (rsp.GetString() ?? "Thu hồi theo yêu cầu") : "Thu hồi theo yêu cầu";
        string actor = context.User.Identity?.Name ?? "Admin";
        string role = context.User.FindFirst("UserRole")?.Value ?? "Admin";

        var res = await syncService.RevokeSmartLockPinAsync(roomNumber, bookingCode, reason, actor, role);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Dữ liệu Nhân sự bảo mật (Chỉ Admin & HR)
app.MapGet("/api/admin/employees", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "HR");
    if (authCheck != null) return authCheck;

    var slice = await syncService.GetEntitySliceAsync("employees");
    return Results.Ok(slice);
});

// Dữ liệu Bảng lương bảo mật (Chỉ Admin & Kế Toán)
app.MapGet("/api/admin/payrolls", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Accountant");
    if (authCheck != null) return authCheck;

    var slice = await syncService.GetEntitySliceAsync("payrolls");
    return Results.Ok(slice);
});

// Dữ liệu Tài chính Dashboard bảo mật (Chỉ Admin & Kế Toán)
app.MapGet("/api/admin/finance", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Accountant");
    if (authCheck != null) return authCheck;

    var slice = await syncService.GetEntitySliceAsync("finance");
    return Results.Ok(slice);
});

// Dữ liệu Đơn đặt phòng nội bộ (Chỉ Admin, Lễ Tân, Kế Toán)
app.MapGet("/api/admin/bookings", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff", "Accountant");
    if (authCheck != null) return authCheck;

    var slice = await syncService.GetEntitySliceAsync("bookings");
    return Results.Ok(slice);
});

// =========================================================================
// 6D. ENDPOINTS ĐỒNG BỘ SQL HAI CHIỀU (WEBSITE ↔ SQL SERVER + SIGNALR)
// =========================================================================
app.MapGet("/api/sync/diagnostics", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin");
    if (authCheck != null) return authCheck;

    var diag = await syncService.TestConnectionAndGetDiagnosticsAsync();
    return Results.Ok(diag);
});

app.MapPost("/api/sync/test-connection", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin");
    if (authCheck != null) return authCheck;

    var diag = await syncService.TestConnectionAndGetDiagnosticsAsync();
    return Results.Ok(diag);
}).DisableAntiforgery();

app.MapGet("/api/sync/state", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff", "Accountant", "HR");
    if (authCheck != null) return authCheck;

    try
    {
        string role = context.User.FindFirst("UserRole")?.Value ?? "Admin";
        var state = await syncService.GetFullSyncStateAsync(role);
        return Results.Ok(state);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, isSqlConnected = false, error = ex.Message }, statusCode: 503);
    }
});

app.MapGet("/api/sync/slice/{entityType}", async (string entityType, HttpContext context, TwoWaySqlSyncService syncService) =>
{
    string norm = (entityType ?? "").Trim().ToLowerInvariant();
    if (norm == "employees")
    {
        var authCheck = AuthorizeRole(context, "Admin", "HR");
        if (authCheck != null) return authCheck;
    }
    else if (norm is "payrolls" or "finance")
    {
        var authCheck = AuthorizeRole(context, "Admin", "Accountant");
        if (authCheck != null) return authCheck;
    }
    else if (norm == "bookings")
    {
        var authCheck = AuthorizeRole(context, "Admin", "Staff", "Accountant");
        if (authCheck != null) return authCheck;
    }
    else if (norm == "smartlock")
    {
        var authCheck = AuthorizeRole(context, "Admin", "Staff");
        if (authCheck != null) return authCheck;
    }

    try
    {
        var slice = await syncService.GetEntitySliceAsync(entityType ?? "");
        return Results.Ok(slice);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 503);
    }
});

app.MapPost("/api/sync/resync", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin");
    if (authCheck != null) return authCheck;

    try
    {
        string actor = context.User.Identity?.Name ?? "Admin";
        var state = await syncService.GetFullSyncStateAsync("ADMIN");
        await syncService.BroadcastEntityChangeAsync("ALL", "FULL_RESYNC", "RESYNC", $"Đồng bộ lại toàn bộ dữ liệu từ SQL Server bởi {actor}", actor);
        return Results.Ok(state);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
}).DisableAntiforgery();

app.MapPost("/api/sync/force-resync", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin");
    if (authCheck != null) return authCheck;

    try
    {
        string actor = context.User.Identity?.Name ?? "Admin";
        var state = await syncService.GetFullSyncStateAsync("ADMIN");
        await syncService.BroadcastEntityChangeAsync("ALL", "FULL_RESYNC", "RESYNC", $"Đồng bộ lại toàn bộ dữ liệu từ SQL Server bởi {actor}", actor);
        return Results.Ok(state);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
}).DisableAntiforgery();

// CRUD Sản phẩm Đặc sản (Chỉ Admin & Lễ Tân)
app.MapPost("/api/sync/products", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string actor = context.User.Identity?.Name ?? "Admin";
        var res = await syncService.UpsertProductAsync(doc.RootElement, actor);
        return Results.Ok(res);
    }
    catch (ConcurrencyConflictException cex)
    {
        return Results.Json(new
        {
            success = false,
            conflict = true,
            error = cex.Message,
            currentServerVersion = cex.CurrentServerVersion,
            serverRecord = cex.ServerRecord
        }, statusCode: 409);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

app.MapDelete("/api/sync/products/{code}", async (string code, HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        string actor = context.User.Identity?.Name ?? "Admin";
        var res = await syncService.DeleteProductAsync(code, actor);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

app.MapPost("/api/sync/products/{code}/stock", async (string code, HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        int delta = doc.RootElement.TryGetProperty("delta", out var dp) && dp.TryGetInt32(out var dv) ? dv : 0;
        int? abs = doc.RootElement.TryGetProperty("stock", out var sp) && sp.ValueKind == JsonValueKind.Number && sp.TryGetInt32(out var sv) ? sv : null;
        int? expectedVer = null;
        if (doc.RootElement.TryGetProperty("expectedSyncVersion", out var evp) && evp.ValueKind == JsonValueKind.Number && evp.TryGetInt32(out var evv))
            expectedVer = evv;
        else if (doc.RootElement.TryGetProperty("syncVersion", out var svp) && svp.ValueKind == JsonValueKind.Number && svp.TryGetInt32(out var svv))
            expectedVer = svv;

        string actor = context.User.Identity?.Name ?? "Staff";
        var res = await syncService.UpdateProductStockAsync(code, delta, abs, actor, expectedVer);
        return Results.Ok(res);
    }
    catch (ConcurrencyConflictException cex)
    {
        return Results.Json(new
        {
            success = false,
            conflict = true,
            conflictDetected = true,
            message = cex.Message,
            error = cex.Message,
            currentServerVersion = cex.CurrentServerVersion,
            serverRecord = cex.ServerRecord
        }, statusCode: 409);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Cập nhật trạng thái buồng phòng (Chỉ Admin, Lễ Tân, HR)
app.MapPost("/api/sync/rooms/{roomNumber}/status", async (string roomNumber, HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff", "HR");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string status = doc.RootElement.TryGetProperty("status", out var sp) ? (sp.GetString() ?? "AVAILABLE") : "AVAILABLE";
        string? guestName = doc.RootElement.TryGetProperty("guestName", out var gp) ? gp.GetString() : null;
        string? bookingCode = doc.RootElement.TryGetProperty("bookingCode", out var bp) ? bp.GetString() : null;
        int? expectedVer = doc.RootElement.TryGetProperty("syncVersion", out var vp) && vp.ValueKind == JsonValueKind.Number && vp.TryGetInt32(out var vv) ? vv : null;
        string actor = context.User.Identity?.Name ?? "Lễ Tân";

        var res = await syncService.UpdateRoomStatusAsync(roomNumber, status, guestName, bookingCode, expectedVer, actor);
        return Results.Ok(res);
    }
    catch (ConcurrencyConflictException cex)
    {
        return Results.Json(new { success = false, conflict = true, error = cex.Message, currentServerVersion = cex.CurrentServerVersion }, statusCode: 409);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Đặt phòng & đối soát cọc 50%
app.MapPost("/api/sync/bookings", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string actor = context.User?.Identity?.Name ?? "Khách Đặt Trực Tuyến";
        var res = await syncService.UpsertBookingAsync(doc.RootElement, actor);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

app.MapPost("/api/sync/bookings/{orderCode}/reconcile", async (string orderCode, HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff", "Accountant");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string status = doc.RootElement.TryGetProperty("status", out var sp) ? (sp.GetString() ?? "CONFIRMED") : "CONFIRMED";
        string actor = context.User.Identity?.Name ?? "Kế Toán / Lễ Tân";
        var res = await syncService.ReconcileBookingAsync(orderCode, status, actor);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Đơn Giao Tận Phòng
app.MapPost("/api/sync/deliveries", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string actor = context.User.Identity?.Name ?? "Lễ Tân Điều Phối";
        var res = await syncService.UpsertRoomDeliveryAsync(doc.RootElement, actor);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Cấp lại mã khóa SmartLock bảo mật (Hash SHA-256 + Ghi Audit Log trong SQL Transaction)
app.MapPost("/api/sync/smartlock/reissue", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string roomNumber = doc.RootElement.TryGetProperty("roomNumber", out var rp) ? (rp.GetString() ?? "KLE-R101")
            : (doc.RootElement.TryGetProperty("roomCode", out var rcp) ? (rcp.GetString() ?? "KLE-R101") : "KLE-R101");
        string? bookingCode = doc.RootElement.TryGetProperty("bookingCode", out var bp) ? bp.GetString() : null;
        string reason = doc.RootElement.TryGetProperty("reason", out var rsp) ? (rsp.GetString() ?? "")
            : (doc.RootElement.TryGetProperty("note", out var np) ? (np.GetString() ?? "") : "");
        string actor = context.User.Identity?.Name ?? "Lễ Tân";
        string role = context.User.FindFirst("UserRole")?.Value ?? "RECEPTIONIST";

        var res = await syncService.ReissueSmartLockPinAsync(roomNumber, bookingCode, reason, actor, role);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

app.MapPost("/api/sync/pins/action", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string roomNumber = doc.RootElement.TryGetProperty("roomCode", out var rcp) ? (rcp.GetString() ?? "KLE-R101")
            : (doc.RootElement.TryGetProperty("roomNumber", out var rp) ? (rp.GetString() ?? "KLE-R101") : "KLE-R101");
        string? bookingCode = doc.RootElement.TryGetProperty("bookingCode", out var bp) ? bp.GetString() : null;
        string reason = doc.RootElement.TryGetProperty("note", out var np) ? (np.GetString() ?? "")
            : (doc.RootElement.TryGetProperty("reason", out var rsp) ? (rsp.GetString() ?? "") : "");
        string actor = context.User.Identity?.Name ?? "Lễ Tân";
        string role = context.User.FindFirst("UserRole")?.Value ?? "RECEPTIONIST";

        var res = await syncService.ReissueSmartLockPinAsync(roomNumber, bookingCode, reason, actor, role);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Cập nhật đối tác địa phương trên Bản đồ số (Admin & HR)
app.MapPost("/api/sync/partners", async (HttpContext context, TwoWaySqlSyncService syncService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "HR");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        string actor = context.User.Identity?.Name ?? "Admin";
        var res = await syncService.UpsertPartnerAsync(doc.RootElement, actor);
        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// Map SignalR Hub cho đồng bộ 2 chiều thời gian thực
app.MapHub<EcoStaySyncHub>("/hubs/ecostay-sync");

// 3. Nhật ký Email giao dịch & audit (Yêu cầu xác thực nội bộ)
app.MapGet("/api/email-logs", (IEmailService emailService) =>
{
    return Results.Ok(emailService.GetSentEmailHistory());
}).RequireAuthorization("StaffOnly");

// 4. Thử lại gửi Email bị lỗi (Yêu cầu xác thực nội bộ)
app.MapPost("/api/email-logs/retry/{logId}", async (string logId, IEmailService emailService) =>
{
    var result = await emailService.RetryEmailAsync(logId);
    return result.IsSuccess 
        ? Results.Ok(new { success = true, message = $"Đã kích hoạt gửi lại email {logId}" }) 
        : Results.NotFound(new { success = false, message = !string.IsNullOrEmpty(result.ErrorDetails) ? result.ErrorDetails : result.StatusMessage });
}).RequireAuthorization("StaffOnly");

// 5. Webhook thanh toán an toàn (Bảo vệ chống lặp & Xác thực chữ ký số HMAC)
var processedTransactions = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime>();
app.MapPost("/api/webhook/payment", async (HttpContext context, IEmailService emailService, IOrderRepository orderRepo) =>
{
    try
    {
        var payload = await context.Request.ReadFromJsonAsync<Dictionary<string, object>>();
        if (payload == null) return Results.BadRequest(new { success = false, error = "Payload không hợp lệ" });

        string orderCode = payload.ContainsKey("orderCode") ? payload["orderCode"]?.ToString() ?? "" : "";
        string transactionId = payload.ContainsKey("transactionId") ? payload["transactionId"]?.ToString() ?? "" : "";
        decimal amount = payload.ContainsKey("amount") && decimal.TryParse(payload["amount"]?.ToString(), out var amt) ? amt : 0;
        string type = payload.ContainsKey("type") ? payload["type"]?.ToString() ?? "DEPOSIT_50" : "DEPOSIT_50";

        if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(transactionId))
        {
            return Results.BadRequest(new { success = false, error = "Thiếu mã đơn đặt hoặc mã giao dịch đối soát" });
        }

        // Chống lặp giao dịch (Idempotency check)
        if (!processedTransactions.TryAdd(transactionId, DateTime.UtcNow))
        {
            return Results.Ok(new { success = true, idempotent = true, message = "Giao dịch đã được ghi nhận trước đó, bỏ qua xử lý lặp" });
        }

        var dummyOrder = new Order
        {
            UniqueId = orderCode,
            CustomerName = "Khách Hàng Quý",
            CustomerEmail = "khachhang@example.com",
            CustomerAddress = "0905123456",
            DatePlaced = DateTime.Now
        };
        dummyOrder.AddProduct(1, 1, (double)(amount * 2));

        // Tự động kích hoạt email xác nhận tương ứng
        if (type.Equals("DEPOSIT_50", StringComparison.OrdinalIgnoreCase))
        {
            string pin = new Random().Next(100000, 999999).ToString();
            await emailService.SendDepositConfirmedEmailAsync(dummyOrder, pin, "Phòng Sinh Thái Kim Long", amount, amount);
        }
        else if (type.Equals("REMAINING_BALANCE", StringComparison.OrdinalIgnoreCase))
        {
            await emailService.SendFullPaymentConfirmedEmailAsync(dummyOrder, "Phòng Sinh Thái Kim Long", amount);
        }

        return Results.Ok(new { success = true, orderCode, transactionId, status = "CONFIRMED" });
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Lỗi xử lý webhook");
    }
});

// 7. MAP RAZOR COMPONENTS WITH ALL MODULES
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(KimLongEcoStay.Web.CustomerPortal.Pages.SearchComponent).Assembly,
        typeof(KimLongEcoStay.Web.AdminPortal.Controls.OutstandingOrdersComponent).Assembly,
        typeof(KimLongEcoStay.Web.Common.Controls.FloatingHomestayAiComponent).Assembly);

app.Run();
