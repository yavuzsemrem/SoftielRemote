# macOS İkon Güncelleme Talimatları

macOS için uygulama ikonunu güncellemek için aşağıdaki adımları izleyin.

## Otomatik Yöntem (PowerShell Script)

Windows'ta PowerShell kullanarak otomatik olarak tüm boyutları oluşturabilirsiniz:

```powershell
cd src/SoftielRemote.App.Flutter
.\update_macos_icon.ps1
```

Bu script `transparent.png` dosyasını alıp macOS için gerekli tüm boyutlarda (16x16, 32x32, 64x64, 128x128, 256x256, 512x512, 1024x1024) ikon dosyalarını oluşturur.

## Manuel Yöntem

Eğer script çalışmazsa veya macOS'ta çalışıyorsanız:

### Adım 1: Gerekli Boyutları Oluşturun

macOS için şu boyutlarda PNG dosyaları gereklidir:

- `app_icon_16.png` - 16x16 piksel
- `app_icon_32.png` - 32x32 piksel
- `app_icon_64.png` - 64x64 piksel
- `app_icon_128.png` - 128x128 piksel
- `app_icon_256.png` - 256x256 piksel
- `app_icon_512.png` - 512x512 piksel
- `app_icon_1024.png` - 1024x1024 piksel

### Adım 2: Online Resize Tool Kullanın

1. **https://www.iloveimg.com/resize-image** veya benzeri bir tool kullanın
2. `lib/images/transparent.png` dosyasını yükleyin
3. Her boyut için ayrı ayrı resize edin ve indirin
4. Dosyaları şu konuma kopyalayın:
   ```
   macos/Runner/Assets.xcassets/AppIcon.appiconset/
   ```

### Adım 3: ImageMagick Kullanın (Eğer Yüklüyse)

macOS'ta Terminal'de:

```bash
cd src/SoftielRemote.App.Flutter

# Her boyut için ayrı ayrı
magick convert lib/images/transparent.png -resize 16x16 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_16.png
magick convert lib/images/transparent.png -resize 32x32 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_32.png
magick convert lib/images/transparent.png -resize 64x64 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_64.png
magick convert lib/images/transparent.png -resize 128x128 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_128.png
magick convert lib/images/transparent.png -resize 256x256 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_256.png
magick convert lib/images/transparent.png -resize 512x512 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_512.png
magick convert lib/images/transparent.png -resize 1024x1024 macos/Runner/Assets.xcassets/AppIcon.appiconset/app_icon_1024.png
```

### Adım 4: Uygulamayı Yeniden Derleyin

```bash
flutter clean
flutter build macos
```

## İkon Dosyalarının Konumu

macOS ikon dosyaları şu klasörde bulunmalıdır:

```
macos/Runner/Assets.xcassets/AppIcon.appiconset/
```

Bu klasörde şu dosyalar olmalıdır:
- `app_icon_16.png`
- `app_icon_32.png`
- `app_icon_64.png`
- `app_icon_128.png`
- `app_icon_256.png`
- `app_icon_512.png`
- `app_icon_1024.png`
- `Contents.json` (bu dosyayı değiştirmeyin)

## Notlar

- macOS ikonları PNG formatında olmalıdır (ICO değil)
- Tüm boyutların mevcut olması önemlidir
- `Contents.json` dosyası zaten doğru yapılandırılmıştır, değiştirmenize gerek yok
- İkonlar retina display için 2x boyutlarda da kullanılır (örneğin 16x16 için 32x32 de gerekir)

## Sorun Giderme

- **İkon görünmüyorsa:**
  - `flutter clean` komutunu çalıştırın
  - Tüm boyutların doğru konumda olduğundan emin olun
  - `Contents.json` dosyasının değişmediğinden emin olun
  - Uygulamayı tamamen kapatıp yeniden açın

- **Script çalışmıyorsa:**
  - .NET Framework'ün yüklü olduğundan emin olun
  - Manuel yöntemi kullanın
  - ImageMagick kullanın (eğer yüklüyse)

