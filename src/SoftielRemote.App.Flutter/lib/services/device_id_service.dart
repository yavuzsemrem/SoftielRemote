import 'dart:io';
import 'dart:convert';
import 'package:path_provider/path_provider.dart';
import 'package:flutter/foundation.dart';
import 'package:crypto/crypto.dart';

/// Device ID yükleme ve kaydetme servisi
/// Agent ile aynı deviceid.json dosyasını kullanır
class DeviceIdService {
  static const String _folderName = 'SoftielRemote';
  static const String _fileName = 'deviceid.json';

  /// Device ID'yi yükler (deviceid.json'dan veya makine bazlı ID üretir)
  static Future<String> loadDeviceId() async {
    try {
      // 1. Önce ortak deviceid.json'dan oku (AppData/Library - Agent ve App aynı dosyayı kullanır)
      final deviceIdPath = await _getDeviceIdPath();
      if (deviceIdPath != null) {
        final file = File(deviceIdPath);
        if (await file.exists()) {
          final jsonString = await file.readAsString();
          final json = jsonDecode(jsonString) as Map<String, dynamic>;
          if (json.containsKey('DeviceId')) {
            final deviceId = json['DeviceId'] as String?;
            if (deviceId != null && deviceId.isNotEmpty) {
              debugPrint('🔵 Device ID ortak deviceid.json\'dan okundu: $deviceId');
              return deviceId;
            }
          }
        }
      }

      // 2. Device ID bulunamadıysa, makine bazlı ID üret
      final deviceId = _generateMachineBasedId();
      debugPrint('🔵 Makine bazlı Device ID üretildi: $deviceId');

      // 3. Üretilen ID'yi kaydet
      await saveDeviceId(deviceId);

      return deviceId;
    } catch (e) {
      debugPrint('⚠️ Device ID yüklenirken hata: $e');
      // Hata durumunda makine bazlı ID üret
      final deviceId = _generateMachineBasedId();
      await saveDeviceId(deviceId);
      return deviceId;
    }
  }

  /// Device ID'yi kaydeder (deviceid.json'a)
  static Future<void> saveDeviceId(String deviceId) async {
    try {
      final deviceIdPath = await _getDeviceIdPath();
      if (deviceIdPath == null) {
        debugPrint('⚠️ Device ID kaydedilemedi: Path bulunamadı');
        return;
      }

      final file = File(deviceIdPath);
      final directory = file.parent;
      if (!await directory.exists()) {
        await directory.create(recursive: true);
      }

      final json = {
        'DeviceId': deviceId,
        'MachineName': Platform.localHostname,
        'GeneratedAt': DateTime.now().toUtc().toIso8601String(),
      };

      await file.writeAsString(
        const JsonEncoder.withIndent('  ').convert(json),
      );

      debugPrint('🔵 Device ID ortak deviceid.json\'a kaydedildi: $deviceId, Path=$deviceIdPath');
    } catch (e) {
      debugPrint('⚠️ Device ID kaydedilemedi: $e');
    }
  }

  /// Device ID dosyasının tam yolunu döner
  static Future<String?> _getDeviceIdPath() async {
    try {
      if (Platform.isWindows) {
        // Windows: %LOCALAPPDATA%\SoftielRemote\deviceid.json
        final appDataPath = Platform.environment['LOCALAPPDATA'];
        if (appDataPath != null) {
          return '$appDataPath\\$_folderName\\$_fileName';
        }
      } else if (Platform.isMacOS) {
        // macOS: ~/Library/Application Support/SoftielRemote/deviceid.json
        final directory = await getApplicationSupportDirectory();
        return '${directory.path}/$_folderName/$_fileName';
      } else if (Platform.isLinux) {
        // Linux: ~/.local/share/SoftielRemote/deviceid.json
        final directory = await getApplicationSupportDirectory();
        return '${directory.path}/$_folderName/$_fileName';
      }
    } catch (e) {
      debugPrint('⚠️ Device ID path alınırken hata: $e');
    }
    return null;
  }

  /// Makine bazlı sabit Device ID üretir (Agent ile aynı algoritma)
  /// MAC adresi ve makine adına göre deterministik bir ID üretir
  static String _generateMachineBasedId() {
    try {
      // Makine adı
      final machineName = Platform.localHostname;

      // MAC adresi (ilk network interface'ten)
      String macAddress = '';
      try {
        if (Platform.isWindows) {
          // Windows için MAC adresi almak için platform channel gerekebilir
          // Şimdilik sadece makine adını kullan
          macAddress = machineName;
        } else {
          // macOS/Linux için network interface'lerden MAC alınabilir
          // Şimdilik sadece makine adını kullan
          macAddress = machineName;
        }
      } catch (e) {
        macAddress = machineName;
      }

      // Makine adı + MAC adresi kombinasyonu
      final combined = '${machineName}_$macAddress';

      // SHA256 hash al
      final bytes = utf8.encode(combined);
      final hash = _sha256Hash(bytes);

      // Hash'in ilk 4 byte'ını al ve 9 haneli sayıya çevir
      // Hash unsigned olarak işle (C# ile uyumlu olması için)
      final hashValue = (hash[0] << 24) | 
                       (hash[1] << 16) | 
                       (hash[2] << 8) | 
                       hash[3];
      // Unsigned 32-bit integer olarak işle
      final unsignedHash = hashValue.toUnsigned(32);
      final deviceId = (unsignedHash % 900000000) + 100000000; // 100000000 - 999999999 arası

      return deviceId.toString();
    } catch (e) {
      debugPrint('⚠️ Makine bazlı ID üretilirken hata: $e');
      // Hata durumunda rastgele ID üret
      return _generateRandomId();
    }
  }

  /// SHA256 hash fonksiyonu (crypto paketi kullanarak)
  static List<int> _sha256Hash(List<int> bytes) {
    final digest = sha256.convert(bytes);
    return digest.bytes;
  }

  /// Rastgele 9 haneli Device ID üretir
  static String _generateRandomId() {
    final random = DateTime.now().millisecondsSinceEpoch;
    final deviceId = (random % 900000000) + 100000000;
    return deviceId.toString();
  }
}

