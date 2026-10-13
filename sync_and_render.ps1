$srcDir = "c:\Users\TUF\Desktop\KimLongEcoStay\sync_bangchamcong"
$destDir = "c:\Users\TUF\Desktop\bangChamCong\uml"
$imagesDir = "c:\Users\TUF\Desktop\bangChamCong\uml\images"

if (!(Test-Path $imagesDir)) {
    New-Item -ItemType Directory -Path $imagesDir -Force | Out-Null
}

Get-ChildItem -Path $srcDir -Filter "*.puml" | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $destDir -Force
    Write-Host "Copied $($_.Name)"
}

$filesToRender = @(
  "01_usecase_overview",
  "02_usecase_employee",
  "03_usecase_hr_admin",
  "04_class_domain_model",
  "05_class_layered_architecture",
  "06_sequence_login",
  "07_sequence_payroll_calculation",
  "08_sequence_lock_payroll",
  "09_sequence_ai_inquiry",
  "10_activity_timesheet_leave",
  "11_activity_payroll_settlement",
  "12_state_payroll_period",
  "13_deployment_diagram"
)

foreach ($item in $filesToRender) {
    $pumlPath = Join-Path $destDir "$item.puml"
    $pngPath = Join-Path $imagesDir "$item.png"
    try {
        $content = Get-Content -Path $pumlPath -Raw -Encoding UTF8
        Invoke-RestMethod -Uri "https://kroki.io/plantuml/png" -Method Post -Body $content -ContentType "text/plain; charset=utf-8" -OutFile $pngPath -TimeoutSec 30
        $size = (Get-Item $pngPath).Length
        Write-Host "OK: $item ($size bytes)"
    } catch {
        Write-Host "FAILED: $item - $($_.Exception.Message)"
    }
}
Write-Host "Sync and Render Complete!"
