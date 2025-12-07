# PNG to ICO Converter Script for Windows
# This script converts transparent.png to app_icon.ico

param(
    [string]$PngPath = "lib\images\transparent.png",
    [string]$OutputPath = "windows\runner\resources\app_icon.ico"
)

# Check if PNG file exists
if (-not (Test-Path $PngPath)) {
    Write-Host "Error: PNG file not found at: $PngPath" -ForegroundColor Red
    exit 1
}

# Check if .NET is available
try {
    Add-Type -AssemblyName System.Drawing
} catch {
    Write-Host "Error: System.Drawing assembly not available. Please install .NET Framework or use an online converter." -ForegroundColor Red
    Write-Host "Alternative: Use an online converter like https://convertio.co/png-ico/ or https://www.icoconverter.com/" -ForegroundColor Yellow
    exit 1
}

try {
    # Load the PNG image
    $bitmap = New-Object System.Drawing.Bitmap($PngPath)
    
    # Create ICO file with multiple sizes (Windows requires multiple sizes)
    $sizes = @(16, 32, 48, 64, 128, 256)
    $images = New-Object System.Collections.ArrayList
    
    foreach ($size in $sizes) {
        $resized = New-Object System.Drawing.Bitmap($bitmap, $size, $size)
        $images.Add($resized) | Out-Null
    }
    
    # Save as ICO
    # Note: .NET doesn't have native ICO support, so we'll use a workaround
    # For production, consider using ImageMagick or an online converter
    
    Write-Host "Converting PNG to ICO..." -ForegroundColor Green
    
    # Create output directory if it doesn't exist
    $outputDir = Split-Path -Parent $OutputPath
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }
    
    # Use System.Drawing.Icon.Save() workaround
    # Create a temporary icon from the largest bitmap
    $stream = New-Object System.IO.MemoryStream
    $bitmap256 = New-Object System.Drawing.Bitmap($bitmap, 256, 256)
    
    # Save as PNG first, then convert (simplified approach)
    # For a proper ICO, we need to use a library or external tool
    
    Write-Host "Warning: Direct ICO conversion requires additional tools." -ForegroundColor Yellow
    Write-Host "Please use one of these methods:" -ForegroundColor Yellow
    Write-Host "1. Online converter: https://convertio.co/png-ico/" -ForegroundColor Cyan
    Write-Host "2. ImageMagick: magick convert $PngPath -define icon:auto-resize=256,128,64,48,32,16 $OutputPath" -ForegroundColor Cyan
    Write-Host "3. IcoFX or similar icon editor" -ForegroundColor Cyan
    
    # Cleanup
    $bitmap.Dispose()
    foreach ($img in $images) {
        $img.Dispose()
    }
    
    Write-Host "`nManual steps:" -ForegroundColor Yellow
    Write-Host "1. Convert $PngPath to ICO format using an online tool or ImageMagick" -ForegroundColor White
    Write-Host "2. Save the ICO file to: $OutputPath" -ForegroundColor White
    Write-Host "3. Rebuild the app: flutter build windows" -ForegroundColor White
    
} catch {
    Write-Host "Error during conversion: $_" -ForegroundColor Red
    exit 1
}

