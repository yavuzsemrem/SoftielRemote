# Windows İkon Güncelleme Talimatları

Flutter ikonunu değiştirmek için aşağıdaki adımları izleyin:

## Adım 1: PNG'yi ICO'ya Dönüştürün

### Yöntem 1: Online Converter (ÖNERİLEN - En Kolay)

1. **https://convertio.co/png-ico/** adresine gidin
2. **"Choose Files"** butonuna tıklayın
3. Şu dosyayı seçin: `lib/images/transparent.png`
4. **"Convert"** butonuna tıklayın
5. Dönüştürme tamamlandığında **"Download"** butonuna tıklayın
6. İndirilen ICO dosyasını kaydedin

### Yöntem 2: ImageMagick (Eğer yüklüyse)

PowerShell'de şu komutu çalıştırın:

```powershell
cd src/SoftielRemote.App.Flutter
magick convert lib\images\transparent.png -define icon:auto-resize=256,128,64,48,32,16 windows\runner\resources\app_icon.ico
```

## Adım 2: ICO Dosyasını Yerleştirin

1. İndirdiğiniz veya oluşturduğunuz ICO dosyasını bulun
2. Dosyayı şu konuma kopyalayın:
   ```
   src/SoftielRemote.App.Flutter/windows/runner/resources/app_icon.ico
   ```
3. **Eski `app_icon.ico` dosyasının üzerine yazın** (değiştirin)

## Adım 3: Uygulamayı Yeniden Derleyin

PowerShell'de şu komutları çalıştırın:

```powershell
cd src/SoftielRemote.App.Flutter
flutter clean
flutter build windows
```

## Adım 4: Test Edin

1. Uygulamayı çalıştırın: `build\windows\x64\runner\Release\softiel_remote_app.exe`
2. Taskbar'da ikonun değiştiğini kontrol edin
3. Artık Flutter ikonu yerine transparent.png ikonu görünmelidir

## Notlar

- ICO dosyası birden fazla boyut içermelidir (16x16, 32x32, 48x48, 64x64, 128x128, 256x256)
- Online converter'lar genellikle tüm boyutları otomatik oluşturur
- Eğer ikon değişmediyse, `flutter clean` komutunu çalıştırıp tekrar derleyin

## Sorun Giderme

- **İkon hala Flutter ikonu görünüyorsa:**
  - `flutter clean` komutunu çalıştırın
  - `windows/runner/resources/app_icon.ico` dosyasının doğru konumda olduğundan emin olun
  - Uygulamayı tamamen kapatıp yeniden açın
  - Windows icon cache'ini temizleyin (opsiyonel)

- **ICO dosyası oluşturulamıyorsa:**
  - Farklı bir online converter deneyin (örn: https://www.icoconverter.com/)
  - IcoFX gibi bir icon editor programı kullanın

