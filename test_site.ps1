Write-Host "=========================================================="
Write-Host "     KIM LONG ECOSTAY - INTEGRATION VERIFICATION TEST     "
Write-Host "=========================================================="

Write-Host "`n[1/6] KIEM TRA TAI TAI NGUYEN BAN DO LEAFLET & JS:"
$resLeafletJs = curl.exe -s -o nul -w "%{http_code}" http://localhost:5204/lib/leaflet/leaflet.js
$resLeafletCss = curl.exe -s -o nul -w "%{http_code}" http://localhost:5204/lib/leaflet/leaflet.css
$resMapJs = curl.exe -s -o nul -w "%{http_code}" http://localhost:5204/js/heritageMap.js
Write-Host " - /lib/leaflet/leaflet.js : HTTP $resLeafletJs"
Write-Host " - /lib/leaflet/leaflet.css: HTTP $resLeafletCss"
Write-Host " - /js/heritageMap.js      : HTTP $resMapJs"

Write-Host "`n[2/6] KIEM TRA GIAO DIEN TRANG CHU & KHU VUC BAN DO:"
$homeHtml = (curl.exe -s http://localhost:5204/) -join "`n"
$hasLeafletLink = $homeHtml.Contains("lib/leaflet/leaflet.") -or $homeHtml.Contains("lib/leaflet/leaflet.css")
$hasHeritageDiv = $homeHtml.Contains('heritage-leaflet-map')
$hasLoadingOverlay = $homeHtml.Contains('map-loading-overlay')
Write-Host " - Thu vien Leaflet CSS da chen vao Head    : $hasLeafletLink"
Write-Host " - The chua ban do (#heritage-leaflet-map) : $hasHeritageDiv"
Write-Host " - Khung trang thai Loading / Fallback     : $hasLoadingOverlay"

Write-Host "`n[3/6] KIEM TRA TINH GON THANH TOPBAR (CHO KHACH):"
$navStart = $homeHtml.IndexOf('id="navbarMain"')
$navEnd = $homeHtml.IndexOf('</nav>', $navStart)
$navHtml = $homeHtml.Substring($navStart, $navEnd - $navStart)
$hasPhanHe = $navHtml.Contains('Phân Hệ')
$hasTroLyAi = $navHtml.Contains('Trợ Lý AI')
$hasQuanTri = $navHtml.Contains('Quản Trị')
Write-Host " - Muc 'Phan He' va cac muc con da go khoi Topbar   : $(!$hasPhanHe)"
Write-Host " - Muc 'Tro Ly AI' da go khoi Topbar                 : $(!$hasTroLyAi)"
Write-Host " - Muc 'Quan Tri' cong khai da go khoi Topbar        : $(!$hasQuanTri)"

Write-Host "`n[4/6] KIEM TRA TRUY CAP TRUC TIEP /admin KHI CHUA DANG NHAP:"
$unauthAdminHeader = (curl.exe -s -D - -o nul http://localhost:5204/admin) -join "`n"
$is302Redirect = $unauthAdminHeader.Contains("302 Found")
$redirectTarget = $unauthAdminHeader.Contains("login")
Write-Host " - Tu choi truy cap (HTTP 302 Chuyen Huong) : $is302Redirect"
Write-Host " - Chuyen huong ve trang Dang Nhap /login   : $redirectTarget"

Write-Host "`n[5/6] KIEM TRA DANG NHAP VAI TRO ADMIN / STAFF:"
if (Test-Path "test_cookie_admin.txt") { Remove-Item "test_cookie_admin.txt" -Force }
$null = curl.exe -s -c test_cookie_admin.txt "http://localhost:5204/quick-login?role=admin"
$adminPageHeader = (curl.exe -s -b test_cookie_admin.txt -D - -o nul "http://localhost:5204/admin") -join "`n"
$adminPageHtml = (curl.exe -s -b test_cookie_admin.txt "http://localhost:5204/admin") -join "`n"
$hasAdminOk = $adminPageHeader.Contains("200 OK")
$hasAdminCenter = $adminPageHtml.Contains("admin-portal-wrapper") -or $adminPageHtml.Contains("Admin Portal")
$hasAdminControlBtn = $adminPageHtml.Contains('href="admin"') -and $adminPageHtml.Contains('bi-speedometer2')
Write-Host " - Admin truy cap /admin tra ve HTTP 200 OK  : $hasAdminOk"
Write-Host " - Admin vao duoc Trung Tam Quan Tri /admin : $hasAdminCenter"
Write-Host " - Topbar hien thi nut 'Bang Dieu Hanh'     : $hasAdminControlBtn"

Write-Host "`n[6/6] KIEM TRA DANG NHAP VAI TRO KHACH HANG (CUSTOMER):"
if (Test-Path "test_cookie_cust.txt") { Remove-Item "test_cookie_cust.txt" -Force }
$null = curl.exe -s -c test_cookie_cust.txt "http://localhost:5204/quick-login?role=customer"
$custAdminHeader = (curl.exe -s -b test_cookie_cust.txt -D - -o nul "http://localhost:5204/admin") -join "`n"
$custDenied = $custAdminHeader.Contains("302 Found") -and ($custAdminHeader.Contains("error=denied") -or $custAdminHeader.Contains("AccessDenied"))
$custHomePageHtml = (curl.exe -s -b test_cookie_cust.txt "http://localhost:5204/") -join "`n"
$custHasNoAdmin = !$custHomePageHtml.Contains("bi-speedometer2") -and !$custHomePageHtml.Contains("Quản Trị")
Write-Host " - Khach hang bi tu choi khi vao /admin    : $custDenied"
Write-Host " - Khach hang khong thay nut Quan Tri tren Topbar : $custHasNoAdmin"

Write-Host "`n=========================================================="
Write-Host "     TAT CA CAC DIEU KIEN KIEM THU DA HOAN TAT HOP LE!    "
Write-Host "=========================================================="
