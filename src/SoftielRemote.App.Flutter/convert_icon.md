# Windows İkon Ayarlama Rehberi

Windows'ta uygulama ikonunu değiştirmek için PNG dosyasını ICO formatına dönüştürmeniz gerekiyor.

## Yöntem 1: Online Converter (En Kolay)

1. Şu adrese gidin: https://convertio.co/png-ico/ veya https://www.icoconverter.com/
2. `lib/images/transparent.png` dosyasını yükleyin
3. ICO formatında indirin
4. İndirdiğiniz dosyayı `windows/runner/resources/app_icon.ico` konumuna kopyalayın (eski dosyanın üzerine yazın)

## Yöntem 2: ImageMagick (Eğer Yüklüyse)

PowerShell'de şu komutu çalıştırın:

```powershell
magick convert lib\images\transparent.png -define icon:auto-resize=256,128,64,48,32,16 windows\runner\resources\app_icon.ico
```

## Yöntem 3: IcoFX veya Benzeri Program

1. IcoFX veya benzeri bir icon editor programı indirin
2. `lib/images/transparent.png` dosyasını açın
3. ICO formatında kaydedin
4. `windows/runner/resources/app_icon.ico` konumuna kaydedin

## Son Adımlar

İkon dosyasını değiştirdikten sonra:

1. Uygulamayı yeniden derleyin:
   ```bash
   flutter clean
   flutter build windows
   ```

2. Uygulamayı çalıştırdığınızda taskbar'da yeni ikon görünecektir.

## Not

- ICO dosyası birden fazla boyutta (16x16, 32x32, 48x48, 64x64, 128x128, 256x256) içermelidir
- Windows bu farklı boyutları farklı yerlerde kullanır (taskbar, window title bar, file explorer, vb.)
- Online converter'lar genellikle tüm boyutları otomatik oluşturur

