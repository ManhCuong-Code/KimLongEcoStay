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

var builder = WebApplication.CreateBuilder(args);

// Cloud Port Binding (Railway, Render, Container dynamic $PORT)
var cloudPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(cloudPort))
{
    builder.WebHost.UseUrls($"http://+:{cloudPort}");
}

// 1. ADD BLAZOR INTERACTIVE SERVER
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 2. COOKIE AUTHENTICATION & AUTHORIZATION (PART 4)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "KimLongEcoStay.Auth.Cookie";
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login?error=denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
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
        policy.SetIsOriginAllowed(_ => true)
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

// 6. MINIMAL API ENDPOINTS FOR AUTHENTICATION (PART 4)
// Xử lý Login truyền thống bằng HTTP POST để phát hành Cookie trình duyệt
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
        else if (context.Request.Query.ContainsKey("username"))
        {
            username = context.Request.Query["username"].ToString().Trim();
            password = context.Request.Query["password"].ToString().Trim();
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
    catch
    {
        // Fallback an toàn
    }

    AppUser? user = null;
    try
    {
        user = await userRepo.AuthenticateAsync(username, password);
    }
    catch
    {
        // Khi không kết nối được CSDL, fallback sang tài khoản mẫu
    }

    if (user == null)
    {
        if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && (password == "123456" || string.IsNullOrEmpty(password)))
        {
            user = new AppUser { Username = "admin", FullName = "Quản Trị Viên Kim Long EcoStay", Role = "Admin" };
        }
        else if ((username.Equals("letan", StringComparison.OrdinalIgnoreCase) || username.Equals("staff", StringComparison.OrdinalIgnoreCase)) && (password == "123456" || string.IsNullOrEmpty(password)))
        {
            user = new AppUser { Username = "letan", FullName = "Lễ Tân Kim Long EcoStay (Nhân Viên)", Role = "Staff" };
        }
        else if (username.Equals("khachhang", StringComparison.OrdinalIgnoreCase) && password == "123456")
        {
            user = new AppUser { Username = "khachhang", FullName = "Nguyễn Văn An (Khách Hàng)", Role = "Customer" };
        }
    }

    if (user != null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.NameIdentifier, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("UserRole", user.Role)
        };

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, "Staff"));
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
            claims.Add(new Claim("Department", "Ban Điều Hành & Quản Trị Homestay"));
        }
        else if (user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("Department", "Bộ Phận Tiếp Tân & Kiểm Tra"));
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

        // Nhân viên và Admin bắt buộc vào Admin Portal để kiểm tra
        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) || user.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/admin");
        }
        else
        {
            // Khách hàng trở về trang đặt phòng
            context.Response.Redirect("/");
        }
        return;
    }

    context.Response.Redirect("/login?error=invalid");
}).DisableAntiforgery();

// Endpoint Đăng Nhập Nhanh 1-Click (Quick Login) dành cho Host / Admin / Khách
app.MapGet("/quick-login", async (string? role, HttpContext context) =>
{
    string targetRole = (role ?? "admin").Trim().ToLowerInvariant();
    AppUser user = targetRole switch
    {
        "letan" or "staff" or "nhanvien" => new AppUser { Username = "letan", FullName = "Lễ Tân Kim Long (Nhân Viên)", Role = "Staff" },
        "ketoan" => new AppUser { Username = "ketoan", FullName = "Trần Minh Khoa (Kế Toán Trưởng - CFO)", Role = "Accountant" },
        "nhansu" => new AppUser { Username = "nhansu", FullName = "Trần Viết Mạnh Cường (Trưởng Phòng Nhân Sự - COO)", Role = "HR" },
        "quanly" or "host" => new AppUser { Username = "quanly", FullName = "Trần Thị Thu (Giám Đốc Quản Lý - CEO)", Role = "Manager" },
        "khachhang" or "customer" => new AppUser { Username = "khachhang", FullName = "Nguyễn Văn An (Khách Hàng)", Role = "Customer" },
        _ => new AppUser { Username = "admin", FullName = "Quản Trị Viên Kim Long EcoStay", Role = "Admin" }
    };

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.NameIdentifier, user.Username),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim("UserRole", user.Role)
    };

    if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim(ClaimTypes.Role, "Staff"));
        claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        claims.Add(new Claim(ClaimTypes.Role, "Manager"));
        claims.Add(new Claim(ClaimTypes.Role, "HR"));
        claims.Add(new Claim(ClaimTypes.Role, "Accountant"));
        claims.Add(new Claim("Department", "Ban Điều Hành & Quản Trị Homestay"));
    }
    else if (user.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim(ClaimTypes.Role, "Staff"));
        claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        claims.Add(new Claim("Department", "Ban Giám Đốc Điều Hành"));
    }
    else if (user.Role.Equals("HR", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim(ClaimTypes.Role, "Staff"));
        claims.Add(new Claim("Department", "Bộ Phận Nhân Sự & Tuyển Dụng"));
    }
    else if (user.Role.Equals("Accountant", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim(ClaimTypes.Role, "Staff"));
        claims.Add(new Claim("Department", "Bộ Phận Tài Chính - Kế Toán"));
    }
    else if (user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Tiếp Tân & Kiểm Tra"));
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

    if (user.Role == "Customer")
    {
        context.Response.Redirect("/");
    }
    else
    {
        context.Response.Redirect("/admin");
    }
});

// Chuyển hướng GET /authenticate về /login
app.MapGet("/authenticate", (HttpContext context) =>
{
    context.Response.Redirect("/login");
});

// Xử lý Logout
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

// 7. MAP RAZOR COMPONENTS WITH ALL MODULES
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(
        typeof(KimLongEcoStay.Web.CustomerPortal.Pages.SearchComponent).Assembly,
        typeof(KimLongEcoStay.Web.AdminPortal.Controls.OutstandingOrdersComponent).Assembly,
        typeof(KimLongEcoStay.Web.Common.Controls.FloatingHomestayAiComponent).Assembly);

app.Run();
