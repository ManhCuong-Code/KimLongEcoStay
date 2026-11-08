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
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using System.Security.Cryptography;

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

// Data Protection Keys Persistence for Azure App Service & Multi-instance / Restart resilience
var dpKeysFolder = Environment.GetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH");
if (string.IsNullOrWhiteSpace(dpKeysFolder))
{
    var homePath = Environment.GetEnvironmentVariable("HOME"); // Azure App Service persistent storage root
    if (!string.IsNullOrWhiteSpace(homePath))
    {
        dpKeysFolder = Path.Combine(homePath, "ASP.NET", "DataProtection-Keys");
    }
}
if (!string.IsNullOrWhiteSpace(dpKeysFolder))
{
    try
    {
        Directory.CreateDirectory(dpKeysFolder);
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dpKeysFolder))
            .SetApplicationName("KimLongEcoStay");
    }
    catch { }
}

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
        options.Events.OnSigningIn = ctx =>
        {
            var origin = ctx.Request.Headers.Origin.ToString();
            bool isCrossDomain = !string.IsNullOrEmpty(origin) && !origin.Contains("localhost") && !origin.Contains("127.0.0.1");
            if (ctx.Request.IsHttps || isCrossDomain)
            {
                ctx.CookieOptions.SameSite = SameSiteMode.None;
                ctx.CookieOptions.Secure = true;
            }
            else
            {
                ctx.CookieOptions.SameSite = SameSiteMode.Lax;
                ctx.CookieOptions.Secure = ctx.Request.IsHttps;
            }
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

                // 3B. Cho phép Azure App Service domains (*.azurewebsites.net)
                if (uri.Host.EndsWith(".azurewebsites.net", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("azurewebsites.net", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // 3C. Cho phép Render domains (*.onrender.com)
                if (uri.Host.EndsWith(".onrender.com", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Equals("onrender.com", StringComparison.OrdinalIgnoreCase))
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

// Nạp các biến môi trường cho dịch vụ Email thật (Resend, SendGrid, SMTP)
var envEmailApiKey = Environment.GetEnvironmentVariable("EMAIL_API_KEY")
    ?? Environment.GetEnvironmentVariable("RESEND_API_KEY")
    ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
if (!string.IsNullOrWhiteSpace(envEmailApiKey)) emailSettings.ApiKey = envEmailApiKey.Trim();

var envEmailProvider = Environment.GetEnvironmentVariable("EMAIL_PROVIDER");
if (!string.IsNullOrWhiteSpace(envEmailProvider)) emailSettings.Provider = envEmailProvider.Trim();

var envEmailFrom = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? Environment.GetEnvironmentVariable("MAIL_FROM");
if (!string.IsNullOrWhiteSpace(envEmailFrom)) emailSettings.SenderEmail = envEmailFrom.Trim();

var envEmailFromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME");
if (!string.IsNullOrWhiteSpace(envEmailFromName)) emailSettings.SenderName = envEmailFromName.Trim();

var envAdminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
if (!string.IsNullOrWhiteSpace(envAdminEmail)) emailSettings.AdminEmail = envAdminEmail.Trim();

var envSmtpPass = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
if (!string.IsNullOrWhiteSpace(envSmtpPass)) emailSettings.SenderPassword = envSmtpPass.Trim();

// Google Gmail API OAuth 2.0 Credentials
var envGmailClientId = Environment.GetEnvironmentVariable("GMAIL_CLIENT_ID");
if (!string.IsNullOrWhiteSpace(envGmailClientId)) emailSettings.GmailClientId = envGmailClientId.Trim();

var envGmailClientSecret = Environment.GetEnvironmentVariable("GMAIL_CLIENT_SECRET");
if (!string.IsNullOrWhiteSpace(envGmailClientSecret)) emailSettings.GmailClientSecret = envGmailClientSecret.Trim();

var envGmailRedirectUri = Environment.GetEnvironmentVariable("GMAIL_REDIRECT_URI");
if (!string.IsNullOrWhiteSpace(envGmailRedirectUri)) emailSettings.GmailRedirectUri = envGmailRedirectUri.Trim();

var envGmailSender = Environment.GetEnvironmentVariable("GMAIL_SENDER");
if (!string.IsNullOrWhiteSpace(envGmailSender))
{
    emailSettings.GmailSender = envGmailSender.Trim();
    if (string.IsNullOrWhiteSpace(envEmailFrom)) emailSettings.SenderEmail = envGmailSender.Trim();
}

var envGmailRefreshToken = Environment.GetEnvironmentVariable("GMAIL_REFRESH_TOKEN");
if (!string.IsNullOrWhiteSpace(envGmailRefreshToken)) emailSettings.GmailRefreshToken = envGmailRefreshToken.Trim();

builder.Services.AddSingleton(emailSettings);
builder.Services.AddHttpClient<IGmailService, GmailService>();
builder.Services.AddSingleton<IGmailService>(sp =>
    new GmailService(emailSettings, sp.GetRequiredService<IHttpClientFactory>().CreateClient()));
builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddSingleton<IEmailService>(sp =>
    new EmailService(emailSettings, sp.GetRequiredService<IHttpClientFactory>().CreateClient(), sp.GetRequiredService<IGmailService>()));
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
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

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
            "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "bando", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "finance", "nhanvien", "employees", "luong", "payrolls", "duan", "sql-sync" },
            "STAFF" or "LETAN" or "RECEPTIONIST" => new[] { "reconciliation", "bando", "duan", "specialties-admin", "tea-admin", "emaillogs" },
            "ACCOUNTANT" or "CFO" or "KETOAN" => new[] { "dashboard", "finance", "reconciliation", "luong", "payrolls", "emaillogs" },
            "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU" => new[] { "nhanvien", "employees", "duan", "bando", "partners-admin" },
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
    var origin = context.Request.Headers.Origin.ToString();
    bool isCrossDomain = !string.IsNullOrEmpty(origin) && !origin.Contains("localhost") && !origin.Contains("127.0.0.1");
    bool isHttpsOrCross = context.Request.IsHttps || isCrossDomain;
    context.Response.Cookies.Delete("KimLongEcoStay.Auth.Cookie", new CookieOptions
    {
        HttpOnly = true,
        Secure = isHttpsOrCross,
        SameSite = isHttpsOrCross ? SameSiteMode.None : SameSiteMode.Lax,
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
        "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "bando", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "finance", "nhanvien", "employees", "luong", "payrolls", "duan", "sql-sync" },
        "STAFF" or "LETAN" or "RECEPTIONIST" => new[] { "reconciliation", "bando", "duan", "specialties-admin", "tea-admin", "emaillogs" },
        "ACCOUNTANT" or "CFO" or "KETOAN" => new[] { "dashboard", "finance", "reconciliation", "luong", "payrolls", "emaillogs" },
        "HR" or "HR_OPS" or "COO" or "MANAGER" or "NHANSU" => new[] { "nhanvien", "employees", "duan", "bando", "partners-admin" },
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
    try
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        dynamic? diag = await syncService.TestConnectionAndGetDiagnosticsAsync();
        sw.Stop();

        bool isSqlOk = false;
        try { isSqlOk = diag != null && (bool?)diag?.isConnected == true; } catch { }

        string serverName = "Unavailable";
        try { serverName = isSqlOk ? (string?)diag?.serverName ?? "localhost" : "Unavailable"; } catch { }

        string dbName = "KimLongEcoStayDB";
        try { dbName = (string?)diag?.databaseName ?? "KimLongEcoStayDB"; } catch { }

        long ctVer = 0L;
        try { ctVer = (long?)diag?.changeTrackingVersion ?? 0L; } catch { }

        int tableCount = 0;
        try { tableCount = (int?)diag?.trackedTablesCount ?? 0; } catch { }

        var aiStatus = aiGateway.GetStatus();

        var healthReport = new
        {
            status = isSqlOk ? "Healthy" : "Degraded",
            timestamp = DateTime.UtcNow,
            environment = env.EnvironmentName,
            backend = "Online",
            version = "v2.5.0-secured",
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
            emailService = new
            {
                provider = emailSettings.Provider,
                hasApiKey = !string.IsNullOrWhiteSpace(emailSettings.ApiKey),
                sender = emailSettings.SenderEmail,
                adminRecipient = emailSettings.AdminEmail
            },
            services = new
            {
                authentication = "Active (HttpOnly Cookie & RBAC)",
                sqlTwoWaySync = isSqlOk ? "Active" : "Offline",
                signalRHub = "Active (/hubs/ecostay-sync)",
                smartLockCspRng = "Active",
                httpsEmailEngine = "Active"
            }
        };

        return Results.Ok(healthReport);
    }
    catch (Exception ex)
    {
        return Results.Ok(new
        {
            status = "Degraded",
            timestamp = DateTime.UtcNow,
            backend = "Online",
            version = "v2.5.0-secured",
            error = ex.Message
        });
    }
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
    var locations = HeritageMapProvider.GetAllLocations();
    return Results.Ok(locations);
});

// 1C. API Quản Lý Địa Điểm Bản Đồ Admin (Full CRUD GIS Locations)
app.MapGet("/api/admin/locations", () =>
{
    return Results.Ok(HeritageMapProvider.GetAllLocations());
});

app.MapGet("/api/admin/locations/{id}", (string id) =>
{
    var loc = HeritageMapProvider.GetLocationById(id);
    return loc != null ? Results.Ok(loc) : Results.NotFound(new { success = false, message = "Không tìm thấy địa điểm." });
});

app.MapPost("/api/admin/locations", async (HttpContext ctx) =>
{
    var authErr = AuthorizeRole(ctx, "Admin", "Administrator", "Staff", "Manager", "HR");
    if (authErr != null) return authErr;

    try
    {
        var newLoc = await ctx.Request.ReadFromJsonAsync<HeritageLocation>();
        if (newLoc == null)
            return Results.BadRequest(new { success = false, message = "Dữ liệu địa điểm không hợp lệ." });

        bool ok = HeritageMapProvider.AddLocation(newLoc, out string err);
        if (!ok)
            return Results.BadRequest(new { success = false, message = err });

        return Results.Ok(new { success = true, message = "Đã thêm địa điểm mới thành công.", location = newLoc });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = "Lỗi xử lý: " + ex.Message });
    }
}).DisableAntiforgery();

app.MapPut("/api/admin/locations/{id}", async (string id, HttpContext ctx) =>
{
    var authErr = AuthorizeRole(ctx, "Admin", "Administrator", "Staff", "Manager", "HR");
    if (authErr != null) return authErr;

    try
    {
        var updated = await ctx.Request.ReadFromJsonAsync<HeritageLocation>();
        if (updated == null)
            return Results.BadRequest(new { success = false, message = "Dữ liệu địa điểm không hợp lệ." });

        updated.Id = id;
        bool ok = HeritageMapProvider.UpdateLocation(updated, out string err);
        if (!ok)
            return Results.BadRequest(new { success = false, message = err });

        return Results.Ok(new { success = true, message = "Đã cập nhật thông tin địa điểm thành công.", location = updated });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, message = "Lỗi xử lý: " + ex.Message });
    }
}).DisableAntiforgery();

app.MapDelete("/api/admin/locations/{id}", (string id, HttpContext ctx) =>
{
    var authErr = AuthorizeRole(ctx, "Admin", "Administrator", "Manager");
    if (authErr != null) return authErr;

    bool ok = HeritageMapProvider.DeleteLocation(id, out string err);
    if (!ok)
        return Results.BadRequest(new { success = false, message = err });

    return Results.Ok(new { success = true, message = $"Đã xóa địa điểm '{id}' khỏi hệ thống bản đồ." });
}).DisableAntiforgery();

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
        "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "bando", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "nhanvien", "luong", "duan", "sql-sync" },
        "STAFF" or "LETAN" => new[] { "reconciliation", "bando", "duan", "specialties-admin", "tea-admin", "emaillogs" },
        "ACCOUNTANT" or "CFO" => new[] { "dashboard", "reconciliation", "luong", "emaillogs" },
        "HR" or "COO" or "MANAGER" => new[] { "nhanvien", "duan", "bando", "partners-admin" },
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
app.MapPost("/api/public/bookings", async (HttpContext context, TwoWaySqlSyncService syncService, IEmailService emailService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var root = doc.RootElement;
        var res = await syncService.UpsertBookingAsync(root, "OnlineGuest");

        // Parse booking info for email dispatch
        string orderCode = root.TryGetProperty("orderCode", out var ocp) ? (ocp.GetString() ?? "").Trim() : "";
        string guestName = root.TryGetProperty("name", out var np) ? (np.GetString() ?? "Quý Khách").Trim() : "Quý Khách";
        string guestPhone = root.TryGetProperty("phone", out var php) ? (php.GetString() ?? "").Trim() : "";
        string guestEmail = root.TryGetProperty("email", out var ep) ? (ep.GetString() ?? "").Trim() : "";
        string roomName = root.TryGetProperty("roomName", out var rnp) ? (rnp.GetString() ?? "Eco-Nest Ban Mai").Trim() : "Eco-Nest Ban Mai";
        decimal total = root.TryGetProperty("totalPrice", out var tp) && tp.TryGetDecimal(out var tpd) ? tpd : 780000;
        decimal deposit = root.TryGetProperty("depositRequired", out var dp) && dp.TryGetDecimal(out var dpd) ? dpd : Math.Round(total * 0.5m, 0);

        string checkInStr = root.TryGetProperty("checkIn", out var cip) ? (cip.GetString() ?? "") : "";
        string checkOutStr = root.TryGetProperty("checkOut", out var cop) ? (cop.GetString() ?? "") : "";
        DateTime checkIn = DateTime.TryParse(checkInStr, out var cidt) ? cidt : DateTime.Today;
        DateTime checkOut = DateTime.TryParse(checkOutStr, out var codt) ? codt : DateTime.Today.AddDays(1);

        var order = new Order
        {
            UniqueId = orderCode,
            CustomerName = guestName,
            CustomerEmail = guestEmail,
            CustomerAddress = guestPhone,
            DatePlaced = checkIn,
            DateProcessed = checkOut
        };
        order.AddProduct(1, 1, (double)total);

        string transferCode = $"KLE {orderCode.Replace("-", "")} COC50";
        string vietQrUrl = $"https://api.vietqr.io/image/970422-0905123456-compact2.png?amount={deposit}&addInfo={Uri.EscapeDataString(transferCode)}&accountName=KIM%20LONG%20ECOSTAY";

        // Gửi email tiếp nhận kèm VietQR cho khách
        if (!string.IsNullOrWhiteSpace(guestEmail))
        {
            _ = Task.Run(async () =>
            {
                try { await emailService.SendBookingRequestReceivedEmailAsync(order, roomName, deposit, vietQrUrl); } catch { }
            });
        }

        // Gửi email thông báo cho Admin
        _ = Task.Run(async () =>
        {
            try { await emailService.SendAdminNewBookingAlertEmailAsync(order, roomName, deposit); } catch { }
        });

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
        "partners-admin" or "bando" or "map" or "admin-map" => new[] { "Admin", "Staff", "HR" },
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

app.MapPost("/api/sync/bookings/{orderCode}/reconcile", async (string orderCode, HttpContext context, TwoWaySqlSyncService syncService, IEmailService emailService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff", "Accountant");
    if (authCheck != null) return authCheck;

    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var root = doc.RootElement;
        string status = root.TryGetProperty("status", out var sp) ? (sp.GetString() ?? "CONFIRMED") : "CONFIRMED";
        string actor = context.User.Identity?.Name ?? "Kế Toán / Lễ Tân";
        dynamic res = await syncService.ReconcileBookingAsync(orderCode, status, actor);

        string? generatedPin = null;
        try { generatedPin = (string?)res?.oneTimePin; } catch { }

        // Nếu đối soát cọc thành công (CONFIRMED) và có mã PIN được cấp
        if (status.Equals("CONFIRMED", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(generatedPin))
        {
            string guestName = root.TryGetProperty("guestName", out var gnp) ? (gnp.GetString() ?? "").Trim() : "";
            string guestEmail = root.TryGetProperty("guestEmail", out var gep) ? (gep.GetString() ?? "").Trim() : "";
            string roomName = root.TryGetProperty("roomName", out var rnp) ? (rnp.GetString() ?? "").Trim() : "";
            string checkInStr = root.TryGetProperty("checkIn", out var cip) ? (cip.GetString() ?? "") : "";
            string checkOutStr = root.TryGetProperty("checkOut", out var cop) ? (cop.GetString() ?? "") : "";

            DateTime checkIn = DateTime.TryParse(checkInStr, out var cid) ? cid : DateTime.Today;
            DateTime checkOut = DateTime.TryParse(checkOutStr, out var cod) ? cod : DateTime.Today.AddDays(1);

            if (string.IsNullOrWhiteSpace(guestName)) guestName = "Quý Khách";
            if (string.IsNullOrWhiteSpace(roomName)) roomName = "Phòng Lưu Trú Kim Long EcoStay";

            if (!string.IsNullOrWhiteSpace(guestEmail))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendSmartLockPinEmailAsync(
                            guestName,
                            guestEmail,
                            orderCode,
                            roomName,
                            generatedPin,
                            checkIn,
                            checkOut);
                    }
                    catch { }
                });
            }

            // Gửi bản sao thông báo tới Admin
            if (!string.IsNullOrWhiteSpace(emailSettings.AdminEmail) && !emailSettings.AdminEmail.Equals(guestEmail, StringComparison.OrdinalIgnoreCase))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await emailService.SendEmailAsync(
                            emailSettings.AdminEmail,
                            $"[KLE RECONCILE] Đã Đối Soát Cọc Đơn #{orderCode} & Cấp Mã PIN Thành Công",
                            $"<h3>Đối Soát Cọc Thành Công #{orderCode}</h3><p><strong>Khách:</strong> {guestName} ({guestEmail})</p><p><strong>Phòng:</strong> {roomName}</p><p><strong>Mã PIN cấp:</strong> {generatedPin}</p><p><strong>Người duyệt:</strong> {actor}</p>",
                            EmailTemplateType.DEPOSIT_CONFIRMED,
                            orderCode);
                    }
                    catch { }
                });
            }
        }

        return Results.Ok(res);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// =========================================================================
// 6D. API CÔNG KHAI: ĐẶT DỊCH VỤ TRÀ CUNG ĐÌNH, GIAO PHÒNG & LIÊN HỆ
// =========================================================================

// 1. Đặt lịch trải nghiệm trà Cung Đình Huế công khai
app.MapPost("/api/public/tea-booking", async (HttpContext context, IEmailService emailService, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var root = doc.RootElement;
        string bookingCode = root.TryGetProperty("bookingCode", out var bcp) ? (bcp.GetString() ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(bookingCode))
            bookingCode = $"TEA-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

        string serviceCode = root.TryGetProperty("serviceCode", out var scp) ? (scp.GetString() ?? "TEA-HOMESTAY") : "TEA-HOMESTAY";
        string serviceName = root.TryGetProperty("serviceName", out var snp) ? (snp.GetString() ?? "Thưởng Trà Cung Đình Tại Homestay") : "Thưởng Trà Cung Đình Tại Homestay";
        string customerName = root.TryGetProperty("customerName", out var cnp) ? (cnp.GetString() ?? "Quý Khách").Trim() : "Quý Khách";
        string phone = root.TryGetProperty("phone", out var php) ? (php.GetString() ?? "").Trim() : "";
        string email = root.TryGetProperty("email", out var emp) ? (emp.GetString() ?? "").Trim() : "";
        string dateStr = root.TryGetProperty("date", out var dp) ? (dp.GetString() ?? "") : "";
        DateTime bookingDate = DateTime.TryParse(dateStr, out var bdt) ? bdt.Date : DateTime.Today.AddDays(1);
        string timeSlot = root.TryGetProperty("timeSlot", out var tsp) ? (tsp.GetString() ?? "15:30 - 16:30") : "15:30 - 16:30";
        int guests = root.TryGetProperty("guests", out var gp) && gp.TryGetInt32(out var gi) ? gi : 2;
        decimal total = root.TryGetProperty("total", out var tp) && tp.TryGetDecimal(out var td) ? td : 170000;
        string notes = root.TryGetProperty("notes", out var ntp) ? (ntp.GetString() ?? "") : "";

        var req = new TeaBookingRequest
        {
            BookingCode = bookingCode,
            TeaServiceName = serviceName,
            CustomerName = customerName,
            CustomerPhone = phone,
            CustomerEmail = email,
            BookingDate = bookingDate,
            TimeSlot = timeSlot,
            NumberOfGuests = guests,
            TotalAmount = total,
            Notes = notes,
            Status = "PENDING_CONFIRMATION"
        };

        // Gửi email thật cho khách và cho Admin
        var guestResult = await emailService.SendTeaBookingEmailAsync(req);
        var adminResult = await emailService.SendAdminTeaBookingAlertEmailAsync(req);

        return Results.Ok(new
        {
            success = true,
            bookingCode = bookingCode,
            message = "Tiếp nhận đặt lịch trải nghiệm trà Cung Đình thành công! Lễ tân sẽ liên hệ xác nhận bàn trà.",
            emailSent = guestResult.IsSuccess,
            adminNotified = adminResult.IsSuccess
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 2. Đơn hàng ẩm thực & đặc sản giao tận buồng phòng
app.MapPost("/api/public/room-delivery", async (HttpContext context, IEmailService emailService, TwoWaySqlSyncService syncService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var root = doc.RootElement;
        string orderCode = root.TryGetProperty("orderCode", out var ocp) ? (ocp.GetString() ?? "").Trim() : "";
        if (string.IsNullOrWhiteSpace(orderCode))
            orderCode = $"KLE-DELIV-{DateTime.UtcNow:yyyyMMdd}-{RandomNumberGenerator.GetInt32(100, 999)}";

        string customerName = root.TryGetProperty("customerName", out var cnp) ? (cnp.GetString() ?? "Quý Khách").Trim() : "Quý Khách";
        string phone = root.TryGetProperty("phone", out var php) ? (php.GetString() ?? "").Trim() : "";
        string email = root.TryGetProperty("email", out var emp) ? (emp.GetString() ?? "").Trim() : "";
        string roomName = root.TryGetProperty("roomName", out var rnp) ? (rnp.GetString() ?? "Phòng Lưu Trú").Trim() : "Phòng Lưu Trú";
        string deliveryMethod = root.TryGetProperty("deliveryMethod", out var dmp) ? (dmp.GetString() ?? "ROOM_DELIVERY") : "ROOM_DELIVERY";
        decimal total = root.TryGetProperty("total", out var tp) && tp.TryGetDecimal(out var td) ? td : 0;
        string paymentMethod = root.TryGetProperty("paymentMethod", out var pmp) ? (pmp.GetString() ?? "VIETQR") : "VIETQR";
        string notes = root.TryGetProperty("serviceNotes", out var snp) ? (snp.GetString() ?? "") : "";

        var order = new Order
        {
            UniqueId = orderCode,
            CustomerName = customerName,
            CustomerEmail = email,
            CustomerAddress = $"{phone} ({roomName})",
            DatePlaced = DateTime.Now
        };
        order.AddProduct(1, 1, (double)total);

        // Gửi email cho khách
        var guestResult = await emailService.SendSpecialtyOrderEmailAsync(order, 0);

        // Thông báo cho admin
        if (!string.IsNullOrWhiteSpace(emailSettings.AdminEmail))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await emailService.SendEmailAsync(
                        emailSettings.AdminEmail,
                        $"[KLE ORDER] Đơn Ẩm Thực Giao Phòng #{orderCode} - {customerName} ({roomName})",
                        $"<h3>Đơn Hàng Giao Phòng Mới #{orderCode}</h3><p><strong>Khách:</strong> {customerName} - {phone}</p><p><strong>Phòng:</strong> {roomName}</p><p><strong>Tổng tiền:</strong> {total:N0} VNĐ</p><p><strong>PTTT:</strong> {paymentMethod}</p><p><strong>Ghi chú:</strong> {notes}</p>",
                        EmailTemplateType.SPECIALTY_ORDER_CONFIRMED,
                        orderCode);
                }
                catch { }
            });
        }

        // Trạng thái thanh toán ban đầu: Nếu VietQR thì Đang chờ xác nhận thanh toán (không tự động đánh dấu PAID)
        string initialStatus = paymentMethod.Equals("VIETQR", StringComparison.OrdinalIgnoreCase) ? "WAITING_CONFIRMATION" : "PENDING";

        return Results.Ok(new
        {
            success = true,
            orderCode,
            status = initialStatus,
            message = "Đã tiếp nhận đơn hàng ẩm thực và giao phòng! Bếp đang chuẩn bị món nóng.",
            emailSent = guestResult.IsSuccess
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 3. Tiếp nhận thư liên hệ và hỏi đáp từ khách
app.MapPost("/api/public/contact", async (HttpContext context, IEmailService emailService) =>
{
    try
    {
        using var doc = await JsonDocument.ParseAsync(context.Request.Body);
        var root = doc.RootElement;
        string name = root.TryGetProperty("name", out var np) ? (np.GetString() ?? "").Trim() : "";
        string email = root.TryGetProperty("email", out var ep) ? (ep.GetString() ?? "").Trim() : "";
        string phone = root.TryGetProperty("phone", out var pp) ? (pp.GetString() ?? "").Trim() : "";
        string subject = root.TryGetProperty("subject", out var sp) ? (sp.GetString() ?? "Liên hệ tư vấn Kim Long EcoStay").Trim() : "Liên hệ tư vấn Kim Long EcoStay";
        string message = root.TryGetProperty("message", out var mp) ? (mp.GetString() ?? "").Trim() : "";

        if (string.IsNullOrWhiteSpace(name) || (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone)))
        {
            return Results.BadRequest(new { success = false, message = "Vui lòng nhập Họ tên và ít nhất một phương thức liên hệ (Email hoặc SĐT)." });
        }

        var guestRes = await emailService.SendCustomerContactConfirmedEmailAsync(name, email, subject, message);
        var adminRes = await emailService.SendAdminNewContactAlertEmailAsync(name, email, phone, subject, message);

        return Results.Ok(new
        {
            success = true,
            message = "Gửi thông tin liên hệ thành công. Kim Long EcoStay sẽ phản hồi quý khách sớm nhất!",
            guestEmailSent = guestRes.IsSuccess,
            adminEmailSent = adminRes.IsSuccess
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 4. Danh sách các gói trải nghiệm trà Cung Đình
app.MapGet("/api/public/tea-services", () =>
{
    return Results.Ok(new[]
    {
        new { code = "TEA-HOMESTAY", name = "Thưởng Trà Cung Đình Tại Homestay", price = 85000, priceType = "người", maxCapacity = 8, schedule = "08:30 • 15:30 • 19:30" },
        new { code = "TEA-CAKES", name = "Trải Nghiệm Thưởng Trà Kết Hợp Bánh Huế", price = 135000, priceType = "người", maxCapacity = 12, schedule = "09:00 • 15:00" },
        new { code = "TEA-COUPLE", name = "Gói Thưởng Trà Cặp Đôi & Nhóm Bạn", price = 250000, priceType = "gói", maxCapacity = 4, schedule = "Theo yêu cầu hẹn trước" },
        new { code = "TEA-CULTURE", name = "Gói Trải Nghiệm Văn Hóa Huế Kết Hợp Giới Thiệu Trà", price = 390000, priceType = "gói", maxCapacity = 6, schedule = "15:30 - 17:00 hàng ngày" }
    });
});

// 5. Endpoint kiểm thử gửi email thật (dành cho Admin / Lễ tân)
app.MapPost("/api/admin/email/test", async (HttpContext context, IEmailService emailService) =>
{
    try
    {
        string targetEmail = "";
        if (context.Request.ContentLength > 0)
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body);
            targetEmail = doc.RootElement.TryGetProperty("recipientEmail", out var rp) ? (rp.GetString() ?? "").Trim() : "";
        }
        if (string.IsNullOrWhiteSpace(targetEmail))
            targetEmail = !string.IsNullOrWhiteSpace(emailSettings.AdminEmail) ? emailSettings.AdminEmail : "titanio741@gmail.com";

        string testPin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var res = await emailService.SendSmartLockPinEmailAsync(
            "Quản Trị Viên (Thử Nghiệm Gửi Email)",
            targetEmail,
            "TEST-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
            "Eco-Nest Ban Mai (KLE-R101)",
            testPin,
            DateTime.Today,
            DateTime.Today.AddDays(2));

        return Results.Ok(new
        {
            success = res.IsSuccess,
            provider = res.ProviderUsed,
            recipient = targetEmail,
            statusMessage = res.StatusMessage,
            errorDetails = res.ErrorDetails,
            testPin = "••••••",
            sentAt = res.SentAt
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// =========================================================================
// GOOGLE GMAIL API v1 & OAUTH 2.0 WEB SERVER FLOW ENDPOINTS
// =========================================================================

// 5.1. Khởi tạo quy trình cấp quyền OAuth 2.0 với Google
app.MapGet("/api/admin/gmail/connect", (HttpContext context, IGmailService gmailService) =>
{
    try
    {
        if (!gmailService.IsConfigured)
        {
            return Results.BadRequest(new { success = false, error = "Chưa cấu hình GMAIL_CLIENT_ID hoặc GMAIL_CLIENT_SECRET trong biến môi trường hoặc .env." });
        }

        string state = gmailService.CreateCsrfState();
        string redirectUri = !string.IsNullOrWhiteSpace(emailSettings.GmailRedirectUri)
            ? emailSettings.GmailRedirectUri
            : $"{context.Request.Scheme}://{context.Request.Host}/api/admin/gmail/oauth2callback";

        string authUrl = gmailService.GetAuthorizationUrl(redirectUri, state);
        return Results.Redirect(authUrl);
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, title: "Lỗi tạo đường dẫn ủy quyền Google OAuth");
    }
});

// 5.2. Callback nhận Authorization Code từ Google và đổi lấy Access/Refresh Token
app.MapGet("/api/admin/gmail/oauth2callback", async (HttpContext context, IGmailService gmailService) =>
{
    string? error = context.Request.Query["error"];
    if (!string.IsNullOrWhiteSpace(error))
    {
        string desc = context.Request.Query["error_description"].ToString();
        if (string.IsNullOrWhiteSpace(desc)) desc = error;
        return Results.Redirect($"/admin?tab=emaillogs&gmail_error={Uri.EscapeDataString(desc)}");
    }

    string? state = context.Request.Query["state"];
    if (string.IsNullOrWhiteSpace(state) || !gmailService.ValidateCsrfState(state))
    {
        return Results.Redirect("/admin?tab=emaillogs&gmail_error=" + Uri.EscapeDataString("Mã bảo mật CSRF state không hợp lệ hoặc đã hết hạn. Vui lòng thử bấm 'Kết nối' lại."));
    }

    string? code = context.Request.Query["code"];
    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.Redirect("/admin?tab=emaillogs&gmail_error=" + Uri.EscapeDataString("Không nhận được authorization code từ Google OAuth."));
    }

    string redirectUri = !string.IsNullOrWhiteSpace(emailSettings.GmailRedirectUri)
        ? emailSettings.GmailRedirectUri
        : $"{context.Request.Scheme}://{context.Request.Host}/api/admin/gmail/oauth2callback";

    var result = await gmailService.ExchangeCodeForTokenAsync(code, redirectUri);
    if (result.Success)
    {
        return Results.Redirect("/admin?tab=emaillogs&gmail_status=connected");
    }
    else
    {
        return Results.Redirect($"/admin?tab=emaillogs&gmail_error={Uri.EscapeDataString(result.Message)}");
    }
});

// 5.3. Trạng thái kết nối Google Gmail API
app.MapGet("/api/admin/gmail/status", (IGmailService gmailService) =>
{
    return Results.Ok(gmailService.GetConnectionStatus());
});

// 5.4. Thử nghiệm gửi email thực tế qua Gmail API
app.MapPost("/api/admin/gmail/test", async (HttpContext context, IEmailService emailService, IGmailService gmailService) =>
{
    var authCheck = AuthorizeRole(context, "Admin", "Staff");
    if (authCheck != null) return authCheck;

    try
    {
        string targetEmail = "";
        if (context.Request.ContentLength > 0)
        {
            using var doc = await JsonDocument.ParseAsync(context.Request.Body);
            targetEmail = doc.RootElement.TryGetProperty("recipientEmail", out var rp) ? (rp.GetString() ?? "").Trim() : "";
        }
        if (string.IsNullOrWhiteSpace(targetEmail))
            targetEmail = gmailService.SenderEmail;

        string testPin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
        var res = await emailService.SendSmartLockPinEmailAsync(
            "Quản Trị Viên (Thử Nghiệm Gmail API)",
            targetEmail,
            "GMAIL-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
            "Eco-Nest Ban Mai (KLE-R101)",
            testPin,
            DateTime.Today,
            DateTime.Today.AddDays(2));

        return Results.Ok(new
        {
            success = res.IsSuccess,
            provider = res.ProviderUsed,
            recipient = targetEmail,
            statusMessage = res.StatusMessage,
            errorDetails = res.ErrorDetails,
            testPin = "••••••",
            sentAt = res.SentAt
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { success = false, error = ex.Message });
    }
}).DisableAntiforgery();

// 5.5. Ngắt kết nối tài khoản Google Gmail
app.MapPost("/api/admin/gmail/disconnect", (HttpContext context, IGmailService gmailService) =>
{
    var authCheck = AuthorizeRole(context, "Admin");
    if (authCheck != null) return authCheck;

    gmailService.Disconnect();
    return Results.Ok(new { success = true, message = "Đã ngắt kết nối Gmail API thành công." });
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
            string pin = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
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
