$diagramsDir = "C:\Users\TUF\Desktop\KimLongEcoStay\diagrams"
$imagesDir = Join-Path $diagramsDir "images"
if (!(Test-Path $imagesDir)) {
    New-Item -ItemType Directory -Path $imagesDir -Force | Out-Null
}

$diagramFiles = @(
    "use_case_diagram",
    "class_diagram",
    "sequence_booking",
    "sequence_ai_smartpaste",
    "activity_diagram",
    "state_machine_diagram"
)

foreach ($name in $diagramFiles) {
    $pumlPath = Join-Path $diagramsDir "$name.puml"
    $pngPath = Join-Path $imagesDir "$name.png"
    Write-Host "Rendering $name..."
    try {
        $pumlContent = Get-Content -Path $pumlPath -Raw -Encoding UTF8
        Invoke-RestMethod -Uri "https://kroki.io/plantuml/png" -Method Post -Body $pumlContent -ContentType "text/plain; charset=utf-8" -OutFile $pngPath -TimeoutSec 30
        $size = (Get-Item $pngPath).Length
        Write-Host "SUCCESS: $name.png ($size bytes)"
    } catch {
        Write-Host "ERROR on $name : $($_.Exception.Message)"
    }
}

Write-Host "All rendering complete!"
