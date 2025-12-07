import 'package:flutter/material.dart';
import '../utils/app_theme.dart';

/// Section info model
class SectionInfo {
  final String title;
  final IconData icon;

  const SectionInfo({
    required this.title,
    required this.icon,
  });
}

/// Content sections widget (News, Recent Sessions, etc.)
class ContentSectionsWidget extends StatefulWidget {
  const ContentSectionsWidget({super.key});

  @override
  State<ContentSectionsWidget> createState() => _ContentSectionsWidgetState();
}

class _ContentSectionsWidgetState extends State<ContentSectionsWidget>
    with SingleTickerProviderStateMixin {
  int _activeSectionIndex = 0;
  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;

  List<SectionInfo> get _sections => [
    const SectionInfo(title: 'Kısayollar', icon: Icons.keyboard),
    const SectionInfo(title: 'Favoriler', icon: Icons.star_outline),
    const SectionInfo(title: 'Son Oturumlar', icon: Icons.history),
    const SectionInfo(title: 'Keşfedildi', icon: Icons.explore_outlined),
    const SectionInfo(title: 'Davetler', icon: Icons.mail_outline),
  ];

  @override
  void initState() {
    super.initState();
    _animationController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 300),
    );
    _fadeAnimation = CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeInOut,
    );
    _animationController.forward();
  }

  @override
  void dispose() {
    _animationController.dispose();
    super.dispose();
  }

  void _changeSection(int index) {
    if (index != _activeSectionIndex) {
      setState(() {
        _activeSectionIndex = index;
        _animationController.reset();
        _animationController.forward();
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Modern Section Tabs
        Container(
          padding: const EdgeInsets.all(4),
          decoration: BoxDecoration(
            color: AppTheme.surfaceDark,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(
              color: AppTheme.surfaceLight.withOpacity(0.3),
              width: 1.5,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: _sections.asMap().entries.map((entry) {
              final index = entry.key;
              final section = entry.value;
              final isActive = index == _activeSectionIndex;

              return _ModernTabButton(
                section: section,
                isActive: isActive,
                onTap: () => _changeSection(index),
              );
            }).toList(),
          ),
        ),

        const SizedBox(height: 20),

        // Section Content with Animation
        FadeTransition(
          opacity: _fadeAnimation,
          child: Container(
            width: double.infinity,
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: AppTheme.surfaceMedium,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: AppTheme.surfaceLight.withOpacity(0.3),
                width: 1.5,
              ),
            ),
            child: _buildSectionContent(),
          ),
        ),
      ],
    );
  }

  Widget _buildSectionContent() {
    switch (_activeSectionIndex) {
      case 0: // Kısayollar
        return _buildShortcutsSection();
      case 1: // Favoriler
        return _buildFavoritesSection();
      case 2: // Son Oturumlar
        return _buildRecentSessionsSection();
      default:
        return _buildEmptyState(_sections[_activeSectionIndex].icon);
    }
  }

  Widget _buildEmptyState(IconData icon) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            icon,
            size: 64,
            color: AppTheme.textTertiary.withOpacity(0.5),
          ),
          const SizedBox(height: 16),
          Text(
            'İçerik yakında eklenecek',
            style: TextStyle(
              color: AppTheme.textSecondary,
              fontSize: 16,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildShortcutsSection() {
    return LayoutBuilder(
      builder: (context, constraints) {
        // Tüm kartlar tek satırda, responsive olarak boyutlandırılacak
        return SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              _ModernShortcutCard(
                icon: Icons.videocam_rounded,
                iconColor: AppTheme.errorRed,
                title: 'Ekranı Kaydet',
                description: 'Uzak oturumlarınızı kaydedin',
                shortcutKey: 'Ctrl+R',
                gradient: LinearGradient(
                  colors: [
                    AppTheme.errorRed.withOpacity(0.2),
                    AppTheme.errorRed.withOpacity(0.12),
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
              const SizedBox(width: 16),
              _ModernShortcutCard(
                icon: Icons.fullscreen,
                iconColor: AppTheme.primaryBlue,
                title: 'Tam Ekran',
                description: 'Tam ekran moduna geç',
                shortcutKey: 'F11',
                gradient: LinearGradient(
                  colors: [
                    AppTheme.primaryBlue.withOpacity(0.2),
                    AppTheme.primaryBlueDark.withOpacity(0.12),
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
              const SizedBox(width: 16),
              _ModernShortcutCard(
                icon: Icons.screenshot,
                iconColor: AppTheme.successGreen,
                title: 'Ekran Görüntüsü',
                description: 'Anlık ekran görüntüsü al',
                shortcutKey: 'Ctrl+S',
                gradient: LinearGradient(
                  colors: [
                    AppTheme.successGreen.withOpacity(0.2),
                    AppTheme.successGreen.withOpacity(0.12),
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
              const SizedBox(width: 16),
              _ModernShortcutCard(
                icon: Icons.settings,
                iconColor: AppTheme.textSecondary,
                title: 'Ayarlar',
                description: 'Uygulama ayarlarını aç',
                shortcutKey: 'Ctrl+,',
                gradient: LinearGradient(
                  colors: [
                    AppTheme.surfaceMedium,
                    AppTheme.surfaceDark,
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
              const SizedBox(width: 16),
              _ModernShortcutCard(
                icon: Icons.link_off,
                iconColor: AppTheme.errorRed,
                title: 'Bağlantıyı Kes',
                description: 'Aktif bağlantıyı sonlandır',
                shortcutKey: 'Esc',
                gradient: LinearGradient(
                  colors: [
                    AppTheme.errorRed.withOpacity(0.2),
                    AppTheme.errorRed.withOpacity(0.12),
                  ],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildFavoritesSection() {
    return Wrap(
      spacing: 16,
      runSpacing: 16,
      children: [
        _ModernSessionCard(
          deviceId: '123456789',
          deviceName: 'Ana Bilgisayar',
          isFavorite: true,
          isOnline: true,
          lastConnected: '2 saat önce',
        ),
        _ModernSessionCard(
          deviceId: '987654321',
          deviceName: 'Laptop',
          isFavorite: true,
          isOnline: false,
          lastConnected: '1 gün önce',
        ),
        _ModernSessionCard(
          deviceId: '456789123',
          deviceName: 'Ofis PC',
          isFavorite: true,
          isOnline: true,
          lastConnected: '5 dakika önce',
        ),
      ],
    );
  }

  Widget _buildRecentSessionsSection() {
    return Wrap(
      spacing: 16,
      runSpacing: 16,
      children: [
        _ModernSessionCard(
          deviceId: '123456789',
          deviceName: 'Ana Bilgisayar',
          isFavorite: true,
          isOnline: true,
          lastConnected: '2 saat önce',
        ),
        _ModernSessionCard(
          deviceId: '987654321',
          deviceName: 'Laptop',
          isFavorite: false,
          isOnline: false,
          lastConnected: '1 gün önce',
        ),
        _ModernSessionCard(
          deviceId: '280969031',
          deviceName: 'Sunucu',
          isFavorite: false,
          isOnline: true,
          lastConnected: '30 dakika önce',
        ),
        _ModernSessionCard(
          deviceId: '456789123',
          deviceName: 'Tablet',
          isFavorite: false,
          isOnline: false,
          lastConnected: '3 gün önce',
        ),
      ],
    );
  }
}

class _ModernTabButton extends StatefulWidget {
  final SectionInfo section;
  final bool isActive;
  final VoidCallback onTap;

  const _ModernTabButton({
    required this.section,
    required this.isActive,
    required this.onTap,
  });

  @override
  State<_ModernTabButton> createState() => _ModernTabButtonState();
}

class _ModernTabButtonState extends State<_ModernTabButton> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: widget.onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          curve: Curves.easeInOut,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          decoration: BoxDecoration(
            gradient: widget.isActive
                ? const LinearGradient(
                    colors: [
                      AppTheme.primaryBlue,
                      AppTheme.primaryBlueDark,
                    ],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  )
                : null,
            color: widget.isActive
                ? null
                : (_isHovered
                    ? AppTheme.surfaceMedium.withOpacity(0.5)
                    : Colors.transparent),
            borderRadius: BorderRadius.circular(10),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                widget.section.icon,
                size: 18,
                color: widget.isActive
                    ? Colors.white
                    : AppTheme.textSecondary,
              ),
              const SizedBox(width: 8),
              Text(
                widget.section.title,
                style: TextStyle(
                  color: widget.isActive
                      ? Colors.white
                      : AppTheme.textSecondary,
                  fontSize: 14,
                  fontWeight: widget.isActive
                      ? FontWeight.w600
                      : FontWeight.w500,
                  letterSpacing: 0.3,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ModernNewsCard extends StatefulWidget {
  final IconData icon;
  final Color iconColor;
  final String title;
  final String? description;
  final String? buttonText;
  final Color? buttonColor;
  final bool showClose;
  final Gradient gradient;

  const _ModernNewsCard({
    required this.icon,
    required this.iconColor,
    required this.title,
    this.description,
    this.buttonText,
    this.buttonColor,
    this.showClose = false,
    required this.gradient,
  });

  @override
  State<_ModernNewsCard> createState() => _ModernNewsCardState();
}

class _ModernNewsCardState extends State<_ModernNewsCard> {
  bool _isHovered = false;
  bool _isButtonHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      cursor: SystemMouseCursors.click,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 250),
        curve: Curves.easeOutCubic,
        decoration: BoxDecoration(
          gradient: widget.gradient,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(
            color: _isHovered
                ? widget.iconColor.withOpacity(0.5)
                : AppTheme.surfaceLight.withOpacity(0.25),
            width: _isHovered ? 2 : 1.5,
          ),
          boxShadow: _isHovered
              ? [
                  BoxShadow(
                    color: widget.iconColor.withOpacity(0.3),
                    blurRadius: 20,
                    spreadRadius: 0,
                    offset: const Offset(0, 8),
                  ),
                  BoxShadow(
                    color: Colors.black.withOpacity(0.2),
                    blurRadius: 15,
                    spreadRadius: -5,
                    offset: const Offset(0, 4),
                  ),
                ]
              : [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.15),
                    blurRadius: 10,
                    spreadRadius: -2,
                    offset: const Offset(0, 4),
                  ),
                ],
        ),
        transform: Matrix4.identity()
          ..translate(0.0, _isHovered ? -4.0 : 0.0)
          ..scale(_isHovered ? 1.02 : 1.0),
        child: Stack(
          children: [
            Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  // Icon with enhanced styling
                  AnimatedContainer(
                    duration: const Duration(milliseconds: 250),
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: _isHovered
                          ? widget.iconColor.withOpacity(0.15)
                          : AppTheme.surfaceLight.withOpacity(0.15),
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(
                        color: _isHovered
                            ? widget.iconColor.withOpacity(0.4)
                            : AppTheme.surfaceLight.withOpacity(0.25),
                        width: _isHovered ? 1.5 : 1,
                      ),
                      boxShadow: _isHovered
                          ? [
                              BoxShadow(
                                color: widget.iconColor.withOpacity(0.2),
                                blurRadius: 12,
                                spreadRadius: 0,
                              ),
                            ]
                          : null,
                    ),
                    child: Icon(
                      widget.icon,
                      size: 32,
                      color: widget.iconColor,
                    ),
                  ),
                  const SizedBox(height: 20),

                  // Title
                  Text(
                    widget.title,
                    style: TextStyle(
                      color: AppTheme.textPrimary,
                      fontSize: 17,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.3,
                      height: 1.3,
                    ),
                  ),

                  if (widget.description != null) ...[
                    const SizedBox(height: 10),
                    Expanded(
                      child: Text(
                        widget.description!,
                        style: TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: 13.5,
                          height: 1.5,
                          fontWeight: FontWeight.w400,
                        ),
                        maxLines: 3,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],

                  if (widget.buttonText != null) ...[
                    const SizedBox(height: 18),
                    MouseRegion(
                      onEnter: (_) => setState(() => _isButtonHovered = true),
                      onExit: (_) => setState(() => _isButtonHovered = false),
                      cursor: SystemMouseCursors.click,
                      child: AnimatedContainer(
                        duration: const Duration(milliseconds: 200),
                        padding: const EdgeInsets.symmetric(
                          horizontal: 18,
                          vertical: 12,
                        ),
                        decoration: BoxDecoration(
                          color: _isButtonHovered
                              ? widget.buttonColor?.withOpacity(0.3)
                              : widget.buttonColor?.withOpacity(0.2) ?? AppTheme.surfaceLight.withOpacity(0.2),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                            color: _isButtonHovered
                                ? widget.buttonColor?.withOpacity(0.6) ?? AppTheme.surfaceLight.withOpacity(0.4)
                                : widget.buttonColor?.withOpacity(0.4) ?? AppTheme.surfaceLight.withOpacity(0.3),
                            width: _isButtonHovered ? 1.5 : 1,
                          ),
                          boxShadow: _isButtonHovered
                              ? [
                                  BoxShadow(
                                    color: widget.buttonColor?.withOpacity(0.2) ?? Colors.transparent,
                                    blurRadius: 8,
                                    spreadRadius: 0,
                                  ),
                                ]
                              : null,
                        ),
                        transform: Matrix4.identity()
                          ..scale(_isButtonHovered ? 1.05 : 1.0),
                        child: Text(
                          widget.buttonText!,
                          style: TextStyle(
                            color: widget.buttonColor ?? AppTheme.textPrimary,
                            fontSize: 13.5,
                            fontWeight: FontWeight.w600,
                            letterSpacing: 0.4,
                          ),
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),

            // Close button with enhanced styling
            if (widget.showClose)
              Positioned(
                top: 14,
                right: 14,
                child: MouseRegion(
                  cursor: SystemMouseCursors.click,
                  child: GestureDetector(
                    onTap: () {},
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 200),
                      padding: const EdgeInsets.all(7),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceLight.withOpacity(0.4),
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: AppTheme.surfaceLight.withOpacity(0.5),
                          width: 1,
                        ),
                      ),
                      child: Icon(
                        Icons.close_rounded,
                        size: 16,
                        color: AppTheme.textSecondary,
                      ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _ModernShortcutCard extends StatefulWidget {
  final IconData icon;
  final Color iconColor;
  final String title;
  final String description;
  final String shortcutKey;
  final Gradient gradient;

  const _ModernShortcutCard({
    required this.icon,
    required this.iconColor,
    required this.title,
    required this.description,
    required this.shortcutKey,
    required this.gradient,
  });

  @override
  State<_ModernShortcutCard> createState() => _ModernShortcutCardState();
}

class _ModernShortcutCardState extends State<_ModernShortcutCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      cursor: SystemMouseCursors.click,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: 180,
        height: 150,
        decoration: BoxDecoration(
          gradient: widget.gradient,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(
            color: _isHovered
                ? widget.iconColor.withOpacity(0.5)
                : AppTheme.surfaceLight.withOpacity(0.25),
            width: _isHovered ? 2 : 1.5,
          ),
          boxShadow: _isHovered
              ? [
                  BoxShadow(
                    color: widget.iconColor.withOpacity(0.25),
                    blurRadius: 15,
                    spreadRadius: 0,
                    offset: const Offset(0, 6),
                  ),
                ]
              : [
                  BoxShadow(
                    color: Colors.black.withOpacity(0.1),
                    blurRadius: 8,
                    spreadRadius: -2,
                    offset: const Offset(0, 3),
                  ),
                ],
        ),
        transform: Matrix4.identity()
          ..translate(0.0, _isHovered ? -3.0 : 0.0)
          ..scale(_isHovered ? 1.02 : 1.0),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              // Icon
              AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: _isHovered
                      ? widget.iconColor.withOpacity(0.15)
                      : AppTheme.surfaceLight.withOpacity(0.12),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: _isHovered
                        ? widget.iconColor.withOpacity(0.35)
                        : AppTheme.surfaceLight.withOpacity(0.2),
                    width: _isHovered ? 1.5 : 1,
                  ),
                ),
                child: Icon(
                  widget.icon,
                  size: 22,
                  color: widget.iconColor,
                ),
              ),
              
              const SizedBox(height: 10),
              
              // Title
              Text(
                widget.title,
                style: const TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  letterSpacing: 0.2,
                  height: 1.2,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              
              const SizedBox(height: 3),
              
              // Description
              Text(
                widget.description,
                style: TextStyle(
                  color: AppTheme.textSecondary,
                  fontSize: 10.5,
                  height: 1.3,
                  fontWeight: FontWeight.w400,
                ),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
              
              const Spacer(),

              // Shortcut Key Badge
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceDark.withOpacity(0.6),
                  borderRadius: BorderRadius.circular(6),
                  border: Border.all(
                    color: widget.iconColor.withOpacity(0.3),
                    width: 1,
                  ),
                ),
                child: Text(
                  widget.shortcutKey,
                  style: TextStyle(
                    color: widget.iconColor,
                    fontSize: 9.5,
                    fontWeight: FontWeight.w600,
                    fontFamily: 'monospace',
                    letterSpacing: 0.5,
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

class _ModernSessionCard extends StatefulWidget {
  final String deviceId;
  final String deviceName;
  final bool isFavorite;
  final bool isOnline;
  final String lastConnected;

  const _ModernSessionCard({
    required this.deviceId,
    required this.deviceName,
    required this.isFavorite,
    required this.isOnline,
    required this.lastConnected,
  });

  @override
  State<_ModernSessionCard> createState() => _ModernSessionCardState();
}

class _ModernSessionCardState extends State<_ModernSessionCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: () {
          // TODO: Connect to session
        },
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          width: 240,
          height: 180,
          decoration: BoxDecoration(
            color: AppTheme.surfaceDark,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(
              color: _isHovered
                  ? AppTheme.primaryBlue.withOpacity(0.6)
                  : AppTheme.surfaceLight.withOpacity(0.3),
              width: _isHovered ? 2 : 1.5,
            ),
          ),
          transform: Matrix4.identity()
            ..scale(_isHovered ? 1.01 : 1.0),
          child: Column(
            children: [
              // Thumbnail/Preview Area
              Expanded(
                flex: 3,
                child: Container(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: widget.isOnline
                          ? [
                              AppTheme.primaryBlue.withOpacity(0.3),
                              AppTheme.primaryBlueDark.withOpacity(0.2),
                            ]
                          : [
                              AppTheme.surfaceLight.withOpacity(0.2),
                              AppTheme.surfaceLight.withOpacity(0.1),
                            ],
                    ),
                    borderRadius: const BorderRadius.only(
                      topLeft: Radius.circular(15),
                      topRight: Radius.circular(15),
                    ),
                  ),
                  child: Stack(
                    children: [
                      // Device icon placeholder
                      Center(
                        child: Icon(
                          Icons.desktop_windows,
                          size: 48,
                          color: AppTheme.textTertiary.withOpacity(0.5),
                        ),
                      ),

                      // Status indicator
                      Positioned(
                        top: 12,
                        left: 12,
                        child: Container(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 8,
                            vertical: 4,
                          ),
                          decoration: BoxDecoration(
                            color: widget.isOnline
                                ? AppTheme.successGreen
                                : AppTheme.textTertiary,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: Colors.white.withOpacity(0.2),
                              width: 1,
                            ),
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Container(
                                width: 6,
                                height: 6,
                                decoration: BoxDecoration(
                                  color: Colors.white,
                                  shape: BoxShape.circle,
                                ),
                              ),
                              const SizedBox(width: 6),
                              Text(
                                widget.isOnline ? 'Çevrimiçi' : 'Çevrimdışı',
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 11,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),

                      // Favorite indicator
                      if (widget.isFavorite)
                        Positioned(
                          top: 12,
                          right: 12,
                          child: Container(
                            padding: const EdgeInsets.all(6),
                            decoration: BoxDecoration(
                              color: AppTheme.primaryBlue,
                              shape: BoxShape.circle,
                              border: Border.all(
                                color: Colors.white.withOpacity(0.3),
                                width: 1.5,
                              ),
                            ),
                            child: const Icon(
                              Icons.star,
                              size: 16,
                              color: Colors.white,
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
              ),

              // Device Info
              Expanded(
                flex: 2,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                  decoration: const BoxDecoration(
                    color: AppTheme.surfaceMedium,
                    borderRadius: BorderRadius.only(
                      bottomLeft: Radius.circular(15),
                      bottomRight: Radius.circular(15),
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.center,
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Flexible(
                        child: Text(
                          widget.deviceName,
                          style: const TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                            letterSpacing: 0.2,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(height: 3),
                      Flexible(
                        child: Text(
                          widget.deviceId,
                          style: TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 11,
                            fontFamily: 'monospace',
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            Icons.access_time,
                            size: 11,
                            color: AppTheme.textTertiary,
                          ),
                          const SizedBox(width: 4),
                          Flexible(
                            child: Text(
                              widget.lastConnected,
                              style: TextStyle(
                                color: AppTheme.textTertiary,
                                fontSize: 10,
                              ),
                              maxLines: 1,
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                    ],
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
