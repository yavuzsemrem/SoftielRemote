import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:window_manager/window_manager.dart';
import 'screens/home_screen.dart';
import 'utils/app_theme.dart';
import 'widgets/notification_overlay.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Window manager initialization (Desktop only, not for web)
  if (!kIsWeb) {
    try {
      await windowManager.ensureInitialized();
      
      const windowOptions = WindowOptions(
        size: Size(1400, 800),
        minimumSize: Size(600, 400),
        center: true,
        backgroundColor: Colors.transparent,
        skipTaskbar: false,
        titleBarStyle: TitleBarStyle.hidden,
      );
      
      await windowManager.waitUntilReadyToShow(windowOptions, () async {
        await windowManager.show();
        await windowManager.focus();
      });
    } catch (e) {
      // Window manager not available (e.g., web platform)
      debugPrint('Window manager not available: $e');
    }
  }

  runApp(
    const ProviderScope(
      child: SoftielRemoteApp(),
    ),
  );
}

/// Custom painter for rounded border with smooth antialiasing
class _RoundedBorderPainter extends CustomPainter {
  final Color borderColor;
  final double borderWidth;
  final double borderRadius;

  _RoundedBorderPainter({
    required this.borderColor,
    required this.borderWidth,
    required this.borderRadius,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = borderColor
      ..style = PaintingStyle.stroke
      ..strokeWidth = borderWidth
      ..isAntiAlias = true; // Antialiasing aktif

    final rect = Rect.fromLTWH(
      borderWidth / 2,
      borderWidth / 2,
      size.width - borderWidth,
      size.height - borderWidth,
    );

    final rrect = RRect.fromRectAndRadius(rect, Radius.circular(borderRadius));
    canvas.drawRRect(rrect, paint);
  }

  @override
  bool shouldRepaint(_RoundedBorderPainter oldDelegate) {
    return oldDelegate.borderColor != borderColor ||
        oldDelegate.borderWidth != borderWidth ||
        oldDelegate.borderRadius != borderRadius;
  }
}

class SoftielRemoteApp extends StatefulWidget {
  const SoftielRemoteApp({super.key});

  @override
  State<SoftielRemoteApp> createState() => _SoftielRemoteAppState();
}

class _SoftielRemoteAppState extends State<SoftielRemoteApp> with WindowListener {
  bool _isMaximized = false;

  @override
  void initState() {
    super.initState();
    if (!kIsWeb) {
      windowManager.addListener(this);
      _checkWindowState();
    }
  }

  @override
  void dispose() {
    if (!kIsWeb) {
      windowManager.removeListener(this);
    }
    super.dispose();
  }

  Future<void> _checkWindowState() async {
    if (kIsWeb) return;
    try {
      final isMaximized = await windowManager.isMaximized();
      if (mounted) {
        setState(() {
          _isMaximized = isMaximized;
        });
      }
    } catch (e) {
      debugPrint('Window state check error: $e');
    }
  }

  @override
  void onWindowMaximize() {
    setState(() {
      _isMaximized = true;
    });
  }

  @override
  void onWindowUnmaximize() {
    setState(() {
      _isMaximized = false;
    });
  }

  @override
  void onWindowEnterFullScreen() {
    setState(() {
      _isMaximized = true;
    });
  }

  @override
  void onWindowLeaveFullScreen() {
    setState(() {
      _isMaximized = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Softiel Remote',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: ThemeMode.dark, // Default dark mode
      builder: (context, child) {
        return Stack(
          children: [
            child ?? const SizedBox(),
            // Border overlay - full screen'de border radius 0, normal modda 8
            if (!_isMaximized)
              Positioned.fill(
                child: IgnorePointer(
                  child: CustomPaint(
                    painter: _RoundedBorderPainter(
                      borderColor: AppTheme.primaryBlue,
                      borderWidth: 1.5,
                      borderRadius: 8,
                    ),
                  ),
                ),
              ),
            // Notification overlay - sağ üstte
            const NotificationOverlay(),
          ],
        );
      },
      home: const HomeScreen(),
    );
  }
}

