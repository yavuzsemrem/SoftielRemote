import 'package:flutter/material.dart';
import '../utils/app_theme.dart';

/// Modern Chrome-style tab bar widget (for title bar)
class BrowserTabsWidget extends StatefulWidget {
  const BrowserTabsWidget({super.key});

  @override
  State<BrowserTabsWidget> createState() => _BrowserTabsWidgetState();
}

class _BrowserTabsWidgetState extends State<BrowserTabsWidget> {
  int _activeTabIndex = 0;
  
  final List<TabItem> _tabs = [
    const TabItem(
      title: 'Yeni Bağlantı',
      icon: Icons.desktop_windows,
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 40,
      color: AppTheme.backgroundDark,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Tabs
          Expanded(
            child: ListView.builder(
              scrollDirection: Axis.horizontal,
              itemCount: _tabs.length,
              itemBuilder: (context, index) {
                final tab = _tabs[index];
                final isActive = index == _activeTabIndex;
                final isLast = index == _tabs.length - 1;
                
                return _TabItemWidget(
                  tab: tab,
                  isActive: isActive,
                  isLast: isLast,
                  onTap: () {
                    setState(() {
                      _activeTabIndex = index;
                    });
                  },
                  onClose: _tabs.length > 1
                      ? () {
                          setState(() {
                            _tabs.removeAt(index);
                            if (_activeTabIndex >= _tabs.length) {
                              _activeTabIndex = _tabs.length - 1;
                            }
                            if (_activeTabIndex < 0) {
                              _activeTabIndex = 0;
                            }
                          });
                        }
                      : null,
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class TabItem {
  final String title;
  final IconData icon;

  const TabItem({
    required this.title,
    required this.icon,
  });
}

class _TabItemWidget extends StatefulWidget {
  final TabItem tab;
  final bool isActive;
  final bool isLast;
  final VoidCallback onTap;
  final VoidCallback? onClose;

  const _TabItemWidget({
    required this.tab,
    required this.isActive,
    required this.isLast,
    required this.onTap,
    this.onClose,
  });

  @override
  State<_TabItemWidget> createState() => _TabItemWidgetState();
}

class _TabItemWidgetState extends State<_TabItemWidget> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: widget.onTap,
      child: MouseRegion(
        cursor: SystemMouseCursors.click,
        onEnter: (_) {
          if (mounted) {
            Future.microtask(() {
              if (mounted) {
                setState(() => _isHovered = true);
              }
            });
          }
        },
        onExit: (_) {
          if (mounted) {
            Future.microtask(() {
              if (mounted) {
                setState(() => _isHovered = false);
              }
            });
          }
        },
        child: Container(
          margin: EdgeInsets.only(
            right: widget.isLast ? 0 : 4,
            top: 4,
          ),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          constraints: const BoxConstraints(
            minHeight: 36,
            maxHeight: 36,
            minWidth: 120,
          ),
          decoration: BoxDecoration(
            color: widget.isActive 
                ? AppTheme.surfaceDark 
                : (_isHovered 
                    ? AppTheme.surfaceMedium.withOpacity(0.6)
                    : AppTheme.surfaceMedium.withOpacity(0.4)),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              // Icon
              Icon(
                widget.tab.icon,
                size: 18,
                color: widget.isActive 
                    ? AppTheme.primaryBlue 
                    : AppTheme.textSecondary,
              ),
              const SizedBox(width: 10),
              
              // Title
              Flexible(
                child: Text(
                  widget.tab.title,
                  overflow: TextOverflow.ellipsis,
                  maxLines: 1,
                  style: TextStyle(
                    color: widget.isActive 
                        ? AppTheme.textPrimary 
                        : AppTheme.textSecondary,
                    fontSize: 13,
                    fontWeight: widget.isActive 
                        ? FontWeight.w600 
                        : FontWeight.normal,
                    height: 1.2,
                  ),
                ),
              ),
              
              // Close button (always show if multiple tabs)
              if (widget.onClose != null) ...[
                const SizedBox(width: 10),
                MouseRegion(
                  cursor: SystemMouseCursors.click,
                  child: GestureDetector(
                    onTap: widget.onClose,
                    behavior: HitTestBehavior.opaque,
                    child: Container(
                      width: 18,
                      height: 18,
                      padding: const EdgeInsets.all(2),
                      decoration: BoxDecoration(
                        color: _isHovered 
                            ? AppTheme.surfaceLight.withOpacity(0.3)
                            : Colors.transparent,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Icon(
                        Icons.close,
                        size: 14,
                        color: AppTheme.textSecondary.withOpacity(0.8),
                      ),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}


