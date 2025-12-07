import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/app_state_provider.dart';
import '../utils/app_theme.dart';

/// Device ID display section
class DeviceIdSection extends ConsumerStatefulWidget {
  const DeviceIdSection({super.key});

  @override
  ConsumerState<DeviceIdSection> createState() => _DeviceIdSectionState();
}

class _DeviceIdSectionState extends ConsumerState<DeviceIdSection> {
  bool _isInviteHovered = false;

  Future<void> _copyToClipboard(String text, String label) async {
    await Clipboard.setData(ClipboardData(text: text));
    
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('$label kopyalandı'),
          duration: const Duration(seconds: 1),
          backgroundColor: AppTheme.successGreen,
        ),
      );
    }
  }

  void _onInvitePressed() {
    // TODO: Davet etme işlevselliği eklenecek
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Davet etme özelliği yakında eklenecek'),
          duration: Duration(seconds: 2),
        ),
      );
    }
  }

  void _onChangePasswordPressed() {
    // TODO: Şifre değiştirme işlevselliği eklenecek
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Şifre değiştirme özelliği yakında eklenecek'),
          duration: Duration(seconds: 2),
        ),
      );
    }
  }

  /// Device ID'yi her 3 karakter arasında boşluk ile formatlar
  String _formatDeviceId(String deviceId) {
    // Boşlukları kaldır ve sadece rakamları al
    final cleanId = deviceId.replaceAll(RegExp(r'\s'), '');
    // Her 3 karakterden sonra boşluk ekle
    final buffer = StringBuffer();
    for (int i = 0; i < cleanId.length; i++) {
      if (i > 0 && i % 3 == 0) {
        buffer.write(' ');
      }
      buffer.write(cleanId[i]);
    }
    return buffer.toString();
  }

  @override
  Widget build(BuildContext context) {
    final deviceInfo = ref.watch(appStateProvider).deviceInfo;
    final formattedDeviceId = _formatDeviceId(deviceInfo.deviceId);

    return Center(
      child: Container(
        margin: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
        decoration: BoxDecoration(
          color: AppTheme.surfaceMedium,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: AppTheme.surfaceLight.withOpacity(0.4),
            width: 1.5,
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 36, vertical: 32),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              // Kilit ikonu - güvenlik göstergesi
              Icon(
                Icons.lock_outline_rounded,
                size: 20,
                color: AppTheme.textSecondary,
              ),
              const SizedBox(width: 12),
              // Bağlantı Kodu label - solda, beyaz ve büyük
              Text(
                'Bağlantı Kodu',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 16,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 0.5,
                ),
              ),
              const SizedBox(width: 24),
              // Device ID - renk değişmedi (gradient, font size aynı)
              MouseRegion(
                cursor: SystemMouseCursors.click,
                child: GestureDetector(
                  onTap: () => _copyToClipboard(deviceInfo.deviceId, 'Bağlantı Kodu'),
                  child: ShaderMask(
                    shaderCallback: (bounds) => const LinearGradient(
                      colors: [
                        AppTheme.primaryBlue,
                        AppTheme.primaryBlueDark,
                        AppTheme.primaryBlueDarker,
                      ],
                    ).createShader(bounds),
                    child: Text(
                      formattedDeviceId,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 56,
                        fontWeight: FontWeight.w900,
                        letterSpacing: 2,
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 16),
              // Copy button - temaya uygun, daha modern
              MouseRegion(
                cursor: SystemMouseCursors.click,
                child: Material(
                  color: Colors.transparent,
                  child: InkWell(
                    onTap: () => _copyToClipboard(deviceInfo.deviceId, 'Bağlantı Kodu'),
                    borderRadius: BorderRadius.circular(10),
                    child: Container(
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceLight.withOpacity(0.4),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: AppTheme.surfaceLight.withOpacity(0.6),
                          width: 1,
                        ),
                      ),
                      child: Icon(
                        Icons.copy_rounded,
                        size: 22,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 16),
              // Şifre değiştirme ikonu
              MouseRegion(
                cursor: SystemMouseCursors.click,
                child: Material(
                  color: Colors.transparent,
                  child: InkWell(
                    onTap: _onChangePasswordPressed,
                    borderRadius: BorderRadius.circular(10),
                    child: Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceLight.withOpacity(0.4),
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: AppTheme.surfaceLight.withOpacity(0.6),
                          width: 1,
                        ),
                      ),
                      child: Icon(
                        Icons.vpn_key_rounded,
                        size: 20,
                        color: AppTheme.textPrimary,
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(width: 16),
              // Davet Et butonu - hover efekti ile
              MouseRegion(
                cursor: SystemMouseCursors.click,
                onEnter: (_) => setState(() => _isInviteHovered = true),
                onExit: (_) => setState(() => _isInviteHovered = false),
                child: Material(
                  color: Colors.transparent,
                  child: InkWell(
                    onTap: _onInvitePressed,
                    borderRadius: BorderRadius.circular(10),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
                      decoration: BoxDecoration(
                        color: _isInviteHovered 
                            ? Color.lerp(AppTheme.primaryBlue, AppTheme.primaryBlueDark, 0.3)!
                            : Colors.transparent,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: Color.lerp(AppTheme.primaryBlue, AppTheme.primaryBlueDark, 0.3)!,
                          width: 1.5,
                        ),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.person_add_rounded,
                            size: 18,
                            color: _isInviteHovered 
                                ? AppTheme.textPrimary 
                                : Color.lerp(AppTheme.primaryBlue, AppTheme.primaryBlueDark, 0.3)!,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Davet Et',
                            style: TextStyle(
                              color: _isInviteHovered 
                                  ? AppTheme.textPrimary 
                                  : Color.lerp(AppTheme.primaryBlue, AppTheme.primaryBlueDark, 0.3)!,
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              letterSpacing: 0.3,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

