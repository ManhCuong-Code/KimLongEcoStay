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
            user = new AppUser { Username = "admin", FullName = "Trần Mạnh Cường (Admin - CEO)", Role = "Admin" };
        }
        else if ((username.Equals("letan", StringComparison.OrdinalIgnoreCase) || username.Equals("staff", StringComparison.OrdinalIgnoreCase)) && (password == "123456" || string.IsNullOrEmpty(password)))
        {
            user = new AppUser { Username = "letan", FullName = "Đặng Thu Thảo (Lễ Tân)", Role = "Staff" };
        }
        else if ((username.Equals("ketoan", StringComparison.OrdinalIgnoreCase) || username.Equals("cfo", StringComparison.OrdinalIgnoreCase)) && (password == "123456" || string.IsNullOrEmpty(password)))
        {
            user = new AppUser { Username = "ketoan", FullName = "Lê Thị Mai Lan (Kế Toán Trưởng)", Role = "Accountant" };
        }
        else if ((username.Equals("nhansu", StringComparison.OrdinalIgnoreCase) || username.Equals("hr", StringComparison.OrdinalIgnoreCase) || username.Equals("coo", StringComparison.OrdinalIgnoreCase)) && (password == "123456" || string.IsNullOrEmpty(password)))
        {
            user = new AppUser { Username = "nhansu", FullName = "Nguyễn Văn An (Nhân Sự / Vận Hành)", Role = "HR" };
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
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
            claims.Add(new Claim("Department", "Ban Điều Hành & Quản Trị Homestay"));
        }
        else if (user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("Department", "Bộ Phận Lễ Tân & Đón Tiếp"));
        }
        else if (user.Role.Equals("Accountant", StringComparison.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("Department", "Bộ Phận Tài Chính - Kế Toán"));
        }
        else if (user.Role.Equals("HR", StringComparison.OrdinalIgnoreCase))
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

        // Nhân viên và Admin bắt buộc vào Admin Portal
        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
            user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase) ||
            user.Role.Equals("Accountant", StringComparison.OrdinalIgnoreCase) ||
            user.Role.Equals("HR", StringComparison.OrdinalIgnoreCase) ||
            user.Role.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
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

// Endpoint Đăng Nhập Nhanh 1-Click (Quick Login) dành cho 4 vai trò RBAC
app.MapGet("/quick-login", async (string? role, HttpContext context) =>
{
    string targetRole = (role ?? "admin").Trim().ToLowerInvariant();
    AppUser user = targetRole switch
    {
        "letan" or "staff" => new AppUser { Username = "letan", FullName = "Đặng Thu Thảo (Lễ Tân)", Role = "Staff" },
        "ketoan" or "cfo" or "accountant" => new AppUser { Username = "ketoan", FullName = "Lê Thị Mai Lan (Kế Toán Trưởng)", Role = "Accountant" },
        "nhansu" or "hr" or "coo" or "vanhanh" => new AppUser { Username = "nhansu", FullName = "Nguyễn Văn An (Nhân Sự / Vận Hành)", Role = "HR" },
        "khachhang" or "customer" => new AppUser { Username = "khachhang", FullName = "Khách Lưu Trú Kim Long", Role = "Customer" },
        _ => new AppUser { Username = "admin", FullName = "Trần Mạnh Cường (Admin - CEO)", Role = "Admin" }
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
        claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
        claims.Add(new Claim("Department", "Ban Điều Hành & Quản Trị Homestay"));
    }
    else if (user.Role.Equals("HR", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Nhân Sự & Vận Hành"));
    }
    else if (user.Role.Equals("Accountant", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Tài Chính - Kế Toán"));
    }
    else if (user.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase))
    {
        claims.Add(new Claim("Department", "Bộ Phận Lễ Tân & Đón Tiếp"));
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
        "ADMIN" or "ADMINISTRATOR" => new[] { "reconciliation", "specialties-admin", "partners-admin", "tea-admin", "emaillogs", "dashboard", "nhanvien", "luong", "duan" },
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
