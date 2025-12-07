# Flutter Build'i D:\ Diskine Yönlendirme Script'i

# TEMP ve TMP environment variables'ları D:\'ye ayarla
$env:TEMP = "D:\Temp"
$env:TMP = "D:\Temp"

# Temp klasörünü oluştur
if (-not (Test-Path D:\Temp)) {
    New-Item -ItemType Directory -Path D:\Temp -Force | Out-Null
    Write-Host "D:\Temp klasörü oluşturuldu" -ForegroundColor Green
}

# Eski build klasörünü temizle
Write-Host "Eski build klasörü temizleniyor..." -ForegroundColor Yellow
Remove-Item -Path build -Recurse -Force -ErrorAction SilentlyContinue

# Flutter PATH'i ekle
$env:Path += ';D:\Flutter\bin'

Write-Host "`nTEMP ve TMP D:\Temp olarak ayarlandı" -ForegroundColor Green
Write-Host "Flutter PATH eklendi" -ForegroundColor Green
Write-Host "`nUygulamayı çalıştırmak için:" -ForegroundColor Cyan
Write-Host "flutter run -d windows" -ForegroundColor White

