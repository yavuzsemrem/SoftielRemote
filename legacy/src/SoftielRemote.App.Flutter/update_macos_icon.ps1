# macOS Icon Update Script
# This script helps resize transparent.png to all required macOS icon sizes

param(
    [string]$SourcePng = "lib\images\transparent.png",
    [string]$OutputDir = "macos\Runner\Assets.xcassets\AppIcon.appiconset"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "macOS Icon Update Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if source PNG exists
if (-not (Test-Path $SourcePng)) {
    Write-Host "ERROR: PNG file not found: $SourcePng" -ForegroundColor Red
    exit 1
}

Write-Host "Source PNG file found: $SourcePng" -ForegroundColor Green
Write-Host ""

# Check if .NET System.Drawing is available
try {
    Add-Type -AssemblyName System.Drawing
    Write-Host ".NET System.Drawing found." -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET System.Drawing not found." -ForegroundColor Red
    Write-Host "Please install .NET Framework or use ImageMagick." -ForegroundColor Yellow
    exit 1
}

# Create output directory if it doesn't exist
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Output directory created: $OutputDir" -ForegroundColor Green
}

# macOS icon sizes (based on Contents.json)
$iconSizes = @(
    @{Size=16; File="app_icon_16.png"},
    @{Size=32; File="app_icon_32.png"},
    @{Size=64; File="app_icon_64.png"},
    @{Size=128; File="app_icon_128.png"},
    @{Size=256; File="app_icon_256.png"},
    @{Size=512; File="app_icon_512.png"},
    @{Size=1024; File="app_icon_1024.png"}
)

Write-Host "Creating icon sizes..." -ForegroundColor Yellow
Write-Host ""

try {
    # Load source image
    $sourceBitmap = New-Object System.Drawing.Bitmap($SourcePng)
    Write-Host "Source image loaded: $($sourceBitmap.Width)x$($sourceBitmap.Height)" -ForegroundColor Green
    Write-Host ""

    foreach ($iconSize in $iconSizes) {
        $size = $iconSize.Size
        $filename = $iconSize.File
        $outputPath = Join-Path $OutputDir $filename

        # Create resized bitmap
        $resized = New-Object System.Drawing.Bitmap($sourceBitmap, $size, $size)
        
        # Save as PNG
        $resized.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        
        Write-Host "  OK $size x $size -> $filename" -ForegroundColor Green
        
        # Dispose
        $resized.Dispose()
    }

    # Dispose source
    $sourceBitmap.Dispose()

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Success!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "All icon files created:" -ForegroundColor Green
    Write-Host "  Location: $OutputDir" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. flutter clean" -ForegroundColor White
    Write-Host "2. flutter build macos" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to create icons: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
