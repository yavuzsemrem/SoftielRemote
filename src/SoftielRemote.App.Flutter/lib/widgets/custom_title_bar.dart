import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:window_manager/window_manager.dart';
import '../utils/app_theme.dart';
import 'tab_bar_widget.dart';

/// Custom title bar with logo, tabs, and window controls
class CustomTitleBar extends StatelessWidget {
  const CustomTitleBar({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 48,
      decoration: const BoxDecoration(
        color: AppTheme.backgroundDark,
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // Logo and App Name
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 0),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Logo image
                Image.asset(
                  'lib/images/transparent.png',
                  width: 24,
                  height: 24,
                  fit: BoxFit.contain,
                ),
                const SizedBox(width: 12),
                const Text(
                  'Softiel Remote',
                  style: TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 15,
                    fontWeight: FontWeight.w600,
                    letterSpacing: -0.2,
                  ),
                ),
              ],
            ),
          ),
          
          // Tabs Section (Browser-style tabs) - Logo ile hizalı
          const Expanded(
            child: BrowserTabsWidget(),
          ),
          
          // Window Controls (Desktop only)
          if (!kIsWeb)
            Flexible(
              fit: FlexFit.loose,
              child: const WindowCaption(
                brightness: Brightness.dark,
                backgroundColor: Colors.transparent,
              ),
            ),
        ],
      ),
    );
  }
}

