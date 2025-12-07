# Windows Icon Update Script
# This script helps convert PNG to ICO and update the Windows icon

param(
    [string]$PngPath = "lib\images\transparent.png",
    [string]$OutputPath = "windows\runner\resources\app_icon.ico"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Windows Icon Update Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if PNG file exists
if (-not (Test-Path $PngPath)) {
    Write-Host "HATA: PNG dosyası bulunamadı: $PngPath" -ForegroundColor Red
    Write-Host "Lütfen dosya yolunu kontrol edin." -ForegroundColor Yellow
    exit 1
}

Write-Host "PNG dosyası bulundu: $PngPath" -ForegroundColor Green
Write-Host ""

# Check if .NET System.Drawing is available
$hasSystemDrawing = $false
try {
    Add-Type -AssemblyName System.Drawing
    $hasSystemDrawing = $true
    Write-Host ".NET System.Drawing bulundu." -ForegroundColor Green
} catch {
    Write-Host ".NET System.Drawing bulunamadı." -ForegroundColor Yellow
}

if ($hasSystemDrawing) {
    Write-Host ""
    Write-Host "PNG'yi ICO'ya dönüştürmeye çalışılıyor..." -ForegroundColor Yellow
    
    try {
        # Load PNG
        $bitmap = New-Object System.Drawing.Bitmap($PngPath)
        Write-Host "PNG yüklendi: $($bitmap.Width)x$($bitmap.Height)" -ForegroundColor Green
        
        # Create output directory if it doesn't exist
        $outputDir = Split-Path -Parent $OutputPath
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
            Write-Host "Çıktı dizini oluşturuldu: $outputDir" -ForegroundColor Green
        }
        
        # Create multiple sizes for ICO (Windows requires multiple sizes)
        $sizes = @(16, 32, 48, 64, 128, 256)
        $iconImages = New-Object System.Collections.ArrayList
        
        Write-Host "Farklı boyutlarda ikonlar oluşturuluyor..." -ForegroundColor Yellow
        foreach ($size in $sizes) {
            $resized = New-Object System.Drawing.Bitmap($bitmap, $size, $size)
            $iconImages.Add($resized) | Out-Null
            Write-Host "  - $size x $size oluşturuldu" -ForegroundColor Gray
        }
        
        # Note: System.Drawing doesn't have native ICO save support
        # We need to use a workaround or external tool
        Write-Host ""
        Write-Host "UYARI: .NET System.Drawing ICO formatını doğrudan kaydedemez." -ForegroundColor Yellow
        Write-Host "Lütfen aşağıdaki yöntemlerden birini kullanın:" -ForegroundColor Yellow
        Write-Host ""
        
        # Cleanup
        $bitmap.Dispose()
        foreach ($img in $iconImages) {
            $img.Dispose()
        }
        
    } catch {
        Write-Host "HATA: PNG işlenirken hata oluştu: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ICO Dönüştürme Yöntemleri" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "YÖNTEM 1: Online Converter (ÖNERİLEN)" -ForegroundColor Green
Write-Host "1. https://convertio.co/png-ico/ adresine gidin" -ForegroundColor White
Write-Host "2. '$PngPath' dosyasını yükleyin" -ForegroundColor White
Write-Host "3. ICO formatında indirin" -ForegroundColor White
Write-Host "4. İndirdiğiniz dosyayı '$OutputPath' konumuna kopyalayın" -ForegroundColor White
Write-Host ""
Write-Host "YÖNTEM 2: ImageMagick (Eğer yüklüyse)" -ForegroundColor Green
Write-Host "magick convert `"$PngPath`" -define icon:auto-resize=256,128,64,48,32,16 `"$OutputPath`"" -ForegroundColor White
Write-Host ""
Write-Host "YÖNTEM 3: IcoFX veya benzeri program" -ForegroundColor Green
Write-Host "1. IcoFX programını indirin ve yükleyin" -ForegroundColor White
Write-Host "2. '$PngPath' dosyasını açın" -ForegroundColor White
Write-Host "3. ICO formatında kaydedin" -ForegroundColor White
Write-Host "4. '$OutputPath' konumuna kaydedin" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "İkon güncellendikten sonra:" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "flutter clean" -ForegroundColor Yellow
Write-Host "flutter build windows" -ForegroundColor Yellow
Write-Host ""




