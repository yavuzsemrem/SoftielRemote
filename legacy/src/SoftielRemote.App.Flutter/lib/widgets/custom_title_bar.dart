import 'package:flutter/material.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:window_manager/window_manager.dart';
import '../utils/app_theme.dart';
import 'tab_bar_widget.dart';
import 'profile_dropdown.dart';

/// Custom title bar with logo, tabs, and window controls
class CustomTitleBar extends StatefulWidget {
  const CustomTitleBar({super.key});

  @override
  State<CustomTitleBar> createState() => _CustomTitleBarState();
}

class _CustomTitleBarState extends State<CustomTitleBar> {
  final GlobalKey _profileButtonKey = GlobalKey();

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
          // Logo and App Name - Draggable area
          if (!kIsWeb)
            DragToMoveArea(
              child: Padding(
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
            )
          else
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
          
          // Tabs Section (Browser-style tabs) - Logo ile hizalı - Draggable area
          if (!kIsWeb)
            Expanded(
              child: DragToMoveArea(
                child: const BrowserTabsWidget(),
              ),
            )
          else
            const Expanded(
              child: BrowserTabsWidget(),
            ),
          
          // Right side: Profile Icon + Window Controls (birlikte en sağda)
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Profile Icon Button (Window controls'ün solunda)
              Builder(
                builder: (context) => MouseRegion(
                  cursor: SystemMouseCursors.click,
                  child: Container(
                    key: _profileButtonKey,
                    margin: const EdgeInsets.only(
                      right: 4,
                      left: 12,
                    ),
                    child: Material(
                      color: Colors.transparent,
                      child: InkWell(
                        onTap: () {
                          ProfileDropdown(buttonKey: _profileButtonKey)
                              .showDropdown(context);
                        },
                        borderRadius: BorderRadius.circular(18),
                        child: Container(
                          width: 36,
                          height: 36,
                          decoration: BoxDecoration(
                            color: AppTheme.surfaceMedium,
                            shape: BoxShape.circle,
                            border: Border.all(
                              color: AppTheme.surfaceLight.withOpacity(0.3),
                              width: 1,
                            ),
                          ),
                          child: const Icon(
                            Icons.person_outline_rounded,
                            color: AppTheme.textSecondary,
                            size: 20,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ),
              
              // Window Controls (Desktop only - En sağda)
              if (!kIsWeb)
                SizedBox(
                  width: 138, // Window controls için sabit genişlik
                  child: const WindowCaption(
                    brightness: Brightness.dark,
                    backgroundColor: Colors.transparent,
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }
}

