$ErrorActionPreference = "Stop"

$docPath = "C:\Users\TUF\Desktop\KimLongEcoStay\BAO_CAO_THIET_KE_UML_HOMESTAY_HUE.doc"
$docxPath = "C:\Users\TUF\Desktop\KimLongEcoStay\BAO_CAO_THIET_KE_UML_HOMESTAY_HUE.docx"

Write-Host "Opening Word Application..."
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0

Write-Host "Loading $docPath..."
$doc = $word.Documents.Open($docPath)
Write-Host "Found $($doc.InlineShapes.Count) inline shapes / images."

# Convert linked pictures to fully embedded pictures
foreach ($shape in $doc.InlineShapes) {
    try {
        if ($shape.LinkFormat) {
            $shape.LinkFormat.SavePictureWithDocument = $true
            $shape.LinkFormat.BreakLink()
            Write-Host "Successfully embedded shape."
        }
    } catch {
        Write-Host "Shape already embedded or cannot break link: $($_.Exception.Message)"
    }
}

if (Test-Path $docxPath) {
    Remove-Item -Force $docxPath
}

Write-Host "Saving native Word DOCX package to $docxPath..."
$doc.SaveAs([ref]$docxPath, [ref]16) # wdFormatXMLDocument = 16 (.docx)

$doc.Close()
$word.Quit()
[System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null

$docxSize = (Get-Item $docxPath).Length
Write-Host "COMPLETE: $docxPath ($docxSize bytes)"
