import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../utils/app_theme.dart';
import '../providers/favorites_provider.dart';
import '../screens/home_screen.dart';

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
class ContentSectionsWidget extends ConsumerStatefulWidget {
  const ContentSectionsWidget({super.key});

  @override
  ConsumerState<ContentSectionsWidget> createState() => _ContentSectionsWidgetState();
}

class _ContentSectionsWidgetState extends ConsumerState<ContentSectionsWidget>
    with SingleTickerProviderStateMixin {
  int _activeSectionIndex = 0;
  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;

  List<SectionInfo> get _sections => [
    const SectionInfo(title: 'Kısayollar', icon: Icons.keyboard),
    const SectionInfo(title: 'Favoriler', icon: Icons.star_outline),
    const SectionInfo(title: 'Son Oturumlar', icon: Icons.history),
    const SectionInfo(title: 'Dosya Transfer', icon: Icons.folder_open),
    const SectionInfo(title: 'Ekran Kayıtları', icon: Icons.video_library),
    const SectionInfo(title: 'Ayarlar', icon: Icons.settings_rounded),
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
        // Modern Section Tabs with View Mode Selector
        Row(
          children: [
            Expanded(
              child: Container(
                padding: const EdgeInsets.all(4),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceDark,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(
                    color: AppTheme.surfaceLight.withOpacity(0.3),
                    width: 1.5,
                  ),
                ),
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
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
              ),
            ),
            const SizedBox(width: 12),
            // View Mode Selector (sağda)
            _buildViewModeSelector(context),
          ],
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
      case 3: // Dosya Transfer
        return _buildFileTransferSection();
      case 4: // Ekran Kayıtları
        return _buildScreenRecordingsSection();
      case 5: // Ayarlar
        return _buildSettingsSection();
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
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Section Header
        Row(
          children: [
            Icon(
              Icons.dashboard_rounded,
              color: AppTheme.primaryBlue,
              size: 18,
            ),
            const SizedBox(width: 8),
            Text(
              'Kısayollar',
              style: TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.3,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        // Tüm kartlar tek satırda, responsive olarak boyutlandırılacak
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Ekranı Kaydet - Kırmızı
              SizedBox(width: 260, child: _ScreenRecordCard()),
              const SizedBox(width: 12),
              // Dosya Transfer Et - Mavi
              SizedBox(width: 260, child: _FileTransferShortcutCard()),
              const SizedBox(width: 12),
              // Ayarlar - Gri
              SizedBox(width: 260, child: _SettingsShortcutCard()),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildViewModeSelector(BuildContext context) {
    return Consumer(
      builder: (context, ref, child) {
        final currentMode = ref.watch(globalViewModeProvider);
        
        return Container(
          height: 36,
          decoration: BoxDecoration(
            color: AppTheme.surfaceDark.withOpacity(0.5),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(
              color: AppTheme.surfaceLight.withOpacity(0.15),
              width: 1,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _buildViewModeButton(context, ref, 1, currentMode, Icons.format_list_bulleted_rounded, 'Liste'),
              Container(width: 1, height: 24, color: AppTheme.surfaceLight.withOpacity(0.15)),
              _buildViewModeButton(context, ref, 2, currentMode, Icons.grid_view_rounded, 'Kompakt'),
              Container(width: 1, height: 24, color: AppTheme.surfaceLight.withOpacity(0.15)),
              _buildViewModeButton(context, ref, 3, currentMode, Icons.view_module_rounded, 'Geniş'),
            ],
          ),
        );
      },
    );
  }

  Widget _buildViewModeButton(BuildContext context, WidgetRef ref, int mode, int currentMode, IconData icon, String tooltip) {
    final isSelected = currentMode == mode;
    bool isHovered = false;
    
    return StatefulBuilder(
      builder: (context, setState) {
        return Tooltip(
          message: tooltip,
          child: MouseRegion(
            onEnter: (_) {
              Future.microtask(() {
                setState(() => isHovered = true);
              });
            },
            onExit: (_) {
              Future.microtask(() {
                setState(() => isHovered = false);
              });
            },
            cursor: SystemMouseCursors.click,
            child: GestureDetector(
              onTap: () => ref.read(globalViewModeProvider.notifier).state = mode,
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  gradient: isSelected ? LinearGradient(
                    colors: [AppTheme.primaryBlue, AppTheme.primaryBlueDark],
                  ) : null,
                  color: isSelected ? null : (isHovered ? AppTheme.surfaceLight.withOpacity(0.1) : Colors.transparent),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, size: 16, color: isSelected ? Colors.white : (isHovered ? AppTheme.primaryBlue : AppTheme.textSecondary)),
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _buildFavoritesSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Section Header
        Row(
          children: [
            Icon(
              Icons.star_rounded,
              color: AppTheme.primaryBlue,
              size: 18,
            ),
            const SizedBox(width: 8),
            Text(
              'Favoriler',
              style: TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.3,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        // Content
        Consumer(
          builder: (context, ref, child) {
            final favorites = ref.watch(favoritesProvider).favorites;
            final viewMode = ref.watch(globalViewModeProvider);
            
            if (favorites.isEmpty) {
              return Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      Icons.star_outline,
                      size: 64,
                      color: AppTheme.textTertiary.withOpacity(0.5),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'Henüz favori yok',
                      style: TextStyle(
                        color: AppTheme.textSecondary,
                        fontSize: 16,
                      ),
                    ),
                  ],
                ),
              );
            }

            final cards = favorites.map((favorite) {
              return RecentSessionCard(
                deviceId: favorite.deviceId,
                deviceName: favorite.deviceName,
                isFavorite: true,
                isOnline: favorite.isOnline,
                lastConnected: favorite.lastConnected,
                backgroundType: favorite.backgroundType,
              );
            }).toList();

            if (viewMode == 1) {
              // Liste görünümü
              return Column(
                children: cards.map((card) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: _buildListSessionItem(card),
                )).toList(),
              );
            } else {
              // Grid görünümü
              final crossAxisCount = viewMode == 2 ? 4 : // Kompakt mod: 4 sütun (daha küçük kartlar)
                  (MediaQuery.of(context).size.width < 600 ? 2 :
                   MediaQuery.of(context).size.width < 900 ? 4 :
                   MediaQuery.of(context).size.width < 1200 ? 5 : 6);
              
              return GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: crossAxisCount,
                crossAxisSpacing: 10,
                mainAxisSpacing: 10,
                childAspectRatio: viewMode == 2 ? 2.2 : 1.8, // Kompakt mod: daha geniş ama kısa kartlar
                children: cards,
              );
            }
          },
        ),
      ],
    );
  }

  Widget _buildRecentSessionsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Section Header
        Row(
          children: [
            Icon(
              Icons.history_rounded,
              color: AppTheme.primaryBlue,
              size: 18,
            ),
            const SizedBox(width: 8),
            Text(
              'Son Oturumlar',
              style: TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.3,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        // Content
        Consumer(
          builder: (context, ref, child) {
            final favorites = ref.watch(favoritesProvider);
            final viewMode = ref.watch(globalViewModeProvider);
            
            final cards = [
              RecentSessionCard(
                deviceId: '123456789',
                deviceName: 'Ana Bilgisayar',
                isFavorite: favorites.isFavorite('123456789'),
                isOnline: true,
                lastConnected: '2 saat önce',
                backgroundType: 0,
              ),
              RecentSessionCard(
                deviceId: '987654321',
                deviceName: 'Laptop',
                isFavorite: favorites.isFavorite('987654321'),
                isOnline: false,
                lastConnected: '1 gün önce',
                backgroundType: 1,
              ),
              RecentSessionCard(
                deviceId: '280969031',
                deviceName: 'Sunucu',
                isFavorite: favorites.isFavorite('280969031'),
                isOnline: true,
                lastConnected: '30 dakika önce',
                backgroundType: 2,
              ),
              RecentSessionCard(
                deviceId: '456789123',
                deviceName: 'Tablet',
                isFavorite: favorites.isFavorite('456789123'),
                isOnline: false,
                lastConnected: '3 gün önce',
                backgroundType: 3,
              ),
            ];

            if (viewMode == 1) {
              // Liste görünümü
              return Column(
                children: cards.map((card) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: _buildListSessionItem(card),
                )).toList(),
              );
            } else {
              // Grid görünümü
              final crossAxisCount = viewMode == 2 ? 4 : // Kompakt mod: 4 sütun (daha küçük kartlar)
                  (MediaQuery.of(context).size.width < 600 ? 2 :
                   MediaQuery.of(context).size.width < 900 ? 4 :
                   MediaQuery.of(context).size.width < 1200 ? 5 : 6);
              
              return GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: crossAxisCount,
                crossAxisSpacing: 10,
                mainAxisSpacing: 10,
                childAspectRatio: viewMode == 2 ? 2.2 : 1.8, // Kompakt mod: daha geniş ama kısa kartlar
                children: cards,
              );
            }
          },
        ),
      ],
    );
  }

  Widget _buildListSessionItem(RecentSessionCard card) {
    return Consumer(
      builder: (context, ref, child) {
        final isFavorite = ref.watch(favoritesProvider).isFavorite(card.deviceId);
        
        return GestureDetector(
          onSecondaryTapDown: (details) {
            _showListContextMenu(context, ref, card, isFavorite, details.globalPosition);
          },
          child: Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppTheme.surfaceDark,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: AppTheme.surfaceLight.withOpacity(0.2),
                width: 1,
              ),
            ),
            child: Row(
              children: [
                // Thumbnail
                Container(
                  width: 60,
                  height: 60,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(6),
                    color: AppTheme.surfaceDark,
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(6),
                    child: Stack(
                      children: [
                        Positioned.fill(
                          child: _buildCardBackground(card.backgroundType),
                        ),
                        Positioned(
                          top: 4,
                          left: 4,
                          child: Container(
                            width: 10,
                            height: 10,
                            decoration: BoxDecoration(
                              color: card.isOnline ? AppTheme.successGreen : Colors.red,
                              shape: BoxShape.circle,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                // Device name/ID
                Expanded(
                  child: Builder(
                    builder: (context) {
                      final customName = ref.watch(customNamesProvider).getCustomName(card.deviceId);
                      final displayName = customName ?? _formatDeviceId(card.deviceId);
                      final showIdBelow = customName != null;
                      
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            displayName,
                            style: TextStyle(
                              color: AppTheme.textPrimary,
                              fontSize: 14,
                              fontWeight: FontWeight.w600,
                              fontFamily: customName != null ? null : 'monospace',
                            ),
                          ),
                          if (showIdBelow) ...[
                            const SizedBox(height: 4),
                            Text(
                              _formatDeviceId(card.deviceId),
                              style: TextStyle(
                                color: AppTheme.textSecondary,
                                fontSize: 12,
                                fontFamily: 'monospace',
                              ),
                            ),
                          ],
                        ],
                      );
                    },
                  ),
                ),
                // Favorite star
                IconButton(
                  icon: Icon(
                    isFavorite ? Icons.star_rounded : Icons.star_outline_rounded,
                    color: isFavorite ? const Color(0xFFFFD700) : AppTheme.textSecondary,
                    size: 20,
                  ),
                  onPressed: () {
                    final favoritesNotifier = ref.read(favoritesProvider.notifier);
                    if (isFavorite) {
                      favoritesNotifier.removeFavorite(card.deviceId);
                    } else {
                      favoritesNotifier.addFavorite(
                        SessionCardData(
                          deviceId: card.deviceId,
                          deviceName: card.deviceName,
                          isOnline: card.isOnline,
                          lastConnected: card.lastConnected,
                          backgroundType: card.backgroundType,
                        ),
                      );
                    }
                  },
                ),
                // Action icons
                IconButton(
                  icon: Icon(Icons.arrow_forward_rounded, size: 18, color: AppTheme.textSecondary),
                  onPressed: () {},
                ),
                IconButton(
                  icon: Icon(Icons.insert_drive_file_rounded, size: 18, color: AppTheme.textSecondary),
                  onPressed: () {},
                ),
                Builder(
                  builder: (context) => IconButton(
                    icon: Icon(Icons.more_vert_rounded, size: 18, color: AppTheme.textSecondary),
                    onPressed: () {
                      final RenderBox? renderBox = context.findRenderObject() as RenderBox?;
                      if (renderBox != null) {
                        final Offset localToGlobal = renderBox.localToGlobal(Offset.zero);
                        _showListContextMenu(
                          context,
                          ref,
                          card,
                          isFavorite,
                          Offset(
                            localToGlobal.dx + renderBox.size.width,
                            localToGlobal.dy + renderBox.size.height,
                          ),
                        );
                      }
                    },
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  void _showListContextMenu(
    BuildContext context,
    WidgetRef ref,
    RecentSessionCard card,
    bool isFavorite,
    Offset position,
  ) {
    final RenderBox? overlay = Overlay.of(context).context.findRenderObject() as RenderBox?;
    if (overlay == null) return;

    const double menuWidth = 220.0;
    const double menuItemHeight = 32.0;
    const int menuItemCount = 9;
    final double menuHeight = menuItemHeight * menuItemCount;
    
    // Ekran sınırlarını kontrol et
    double menuLeft = position.dx;
    double menuTop = position.dy;
    
    // Sağdan taşmasını önle
    if (menuLeft + menuWidth > overlay.size.width) {
      menuLeft = overlay.size.width - menuWidth - 8;
    }
    
    // Soldan taşmasını önle
    if (menuLeft < 8) {
      menuLeft = 8;
    }
    
    // Alttan taşmasını önle - yukarı aç
    if (menuTop + menuHeight > overlay.size.height) {
      menuTop = position.dy - menuHeight;
    }
    
    // Üstten taşmasını önle
    if (menuTop < 8) {
      menuTop = 8;
    }

    showGeneralDialog(
      context: context,
      barrierDismissible: true,
      barrierLabel: 'Context Menu',
      barrierColor: Colors.transparent,
      transitionDuration: Duration.zero,
      useRootNavigator: true,
      pageBuilder: (context, animation, secondaryAnimation) {
        return Stack(
          children: [
            Positioned(
              left: menuLeft,
              top: menuTop,
              child: Material(
                color: Colors.transparent,
                elevation: 1000,
                child: Container(
                  width: menuWidth,
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceDark,
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(
                      color: AppTheme.surfaceLight.withOpacity(0.2),
                      width: 1,
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.3),
                        blurRadius: 12,
                        spreadRadius: 0,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      _buildListMenuButton(
                        icon: Icons.arrow_forward_rounded,
                        text: 'Bağla',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Connect to session
                        },
                      ),
                      _buildListMenuButton(
                        icon: Icons.upload_rounded,
                        text: 'Davet Et',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Invite
                        },
                      ),
                      _buildListMenuButton(
                        icon: Icons.insert_drive_file_rounded,
                        text: 'Dosya aktarımını başlat',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Start file transfer
                        },
                      ),
                      const Divider(
                        height: 8,
                        thickness: 1,
                        color: AppTheme.surfaceLight,
                      ),
                      _buildListMenuButton(
                        icon: Icons.compare_arrows_rounded,
                        text: 'TCP tüneli oluştur',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Create TCP tunnel
                        },
                      ),
                      const Divider(
                        height: 8,
                        thickness: 1,
                        color: AppTheme.surfaceLight,
                      ),
                      _buildListMenuButton(
                        icon: Icons.copy_rounded,
                        text: 'Kopyala',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Copy device ID
                        },
                      ),
                      _buildListMenuButton(
                        icon: isFavorite ? Icons.star_rounded : Icons.star_outline_rounded,
                        text: isFavorite ? 'Favorilerden Çıkart' : 'Favorilere Ekle',
                        iconColor: const Color(0xFFFFD700),
                        onTap: () {
                          Navigator.pop(context);
                          final favoritesNotifier = ref.read(favoritesProvider.notifier);
                          if (isFavorite) {
                            favoritesNotifier.removeFavorite(card.deviceId);
                          } else {
                            favoritesNotifier.addFavorite(
                              SessionCardData(
                                deviceId: card.deviceId,
                                deviceName: card.deviceName,
                                isOnline: card.isOnline,
                                lastConnected: card.lastConnected,
                                backgroundType: card.backgroundType,
                              ),
                            );
                          }
                        },
                      ),
                      _buildListMenuButton(
                        icon: Icons.desktop_windows_rounded,
                        text: 'Masaüstüne bırak',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Drop to desktop
                        },
                      ),
                      _buildListMenuButton(
                        icon: Icons.edit_rounded,
                        text: 'Adını değiştir',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Show rename dialog
                        },
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        );
      },
    );
  }

  Widget _buildListMenuButton({
    required IconData icon,
    required String text,
    required VoidCallback onTap,
    Color? iconColor,
  }) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Container(
          height: 32,
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: Row(
            children: [
              Icon(
                icon,
                size: 16,
                color: iconColor ?? AppTheme.textPrimary,
              ),
              const SizedBox(width: 10),
              Text(
                text,
                style: const TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 13,
                  fontWeight: FontWeight.w400,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildCardBackground(int backgroundType) {
    switch (backgroundType) {
      case 0:
        return Container(decoration: BoxDecoration(gradient: LinearGradient(colors: [const Color(0xFF6B46C1), const Color(0xFFDC2626), const Color(0xFFF97316)])));
      case 1:
        return Container(decoration: BoxDecoration(gradient: LinearGradient(colors: [const Color(0xFF1E3A8A), const Color(0xFF3B82F6), const Color(0xFF60A5FA)])));
      case 2:
        return Container(decoration: BoxDecoration(gradient: LinearGradient(colors: [const Color(0xFF9CA3AF), const Color(0xFF6B7280), const Color(0xFF4B5563)])));
      case 3:
        return Container(color: Colors.black);
      case 4:
        return Container(decoration: BoxDecoration(gradient: LinearGradient(colors: [const Color(0xFFDC2626), const Color(0xFFF97316), const Color(0xFFFBBF24)])));
      case 5:
        return Container(decoration: BoxDecoration(gradient: LinearGradient(colors: [const Color(0xFF0067C0), const Color(0xFF0078D4), const Color(0xFF00BCF2)])));
      default:
        return Container(color: AppTheme.surfaceDark);
    }
  }

  String _formatDeviceId(String deviceId) {
    final cleanId = deviceId.replaceAll(RegExp(r'\s'), '');
    final buffer = StringBuffer();
    for (int i = 0; i < cleanId.length; i++) {
      if (i > 0 && i % 3 == 0) buffer.write(' ');
      buffer.write(cleanId[i]);
    }
    return buffer.toString();
  }

  Widget _buildFileTransferSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Section Header
        Row(
          children: [
            Icon(
              Icons.folder_open_rounded,
              color: AppTheme.primaryBlue,
              size: 18,
            ),
            const SizedBox(width: 8),
            Text(
              'Dosya Transfer',
              style: TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.3,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        
        // File Transfer List
        Builder(
          builder: (context) {
            final screenWidth = MediaQuery.of(context).size.width;
            final crossAxisCount = screenWidth < 600
                ? 1
                : screenWidth < 900
                    ? 2
                    : screenWidth < 1200
                        ? 3
                        : screenWidth < 1600
                            ? 4
                            : 5;
            
            final spacing = 12.0;
            
            return GridView.count(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisCount: crossAxisCount,
              crossAxisSpacing: spacing,
              mainAxisSpacing: spacing,
              childAspectRatio: 2.2,
              children: [
                _FileTransferCard(
                  fileName: 'Rapor_2024.pdf',
                  fileSize: '2.4 MB',
                  transferType: 'Gönderildi',
                  status: 'Tamamlandı',
                  date: '2 saat önce',
                  deviceName: 'Ana Bilgisayar',
                ),
                _FileTransferCard(
                  fileName: 'Resimler.zip',
                  fileSize: '15.8 MB',
                  transferType: 'Alındı',
                  status: 'Tamamlandı',
                  date: '1 gün önce',
                  deviceName: 'Laptop',
                ),
                _FileTransferCard(
                  fileName: 'Döküman.docx',
                  fileSize: '856 KB',
                  transferType: 'Gönderildi',
                  status: 'İptal Edildi',
                  date: '3 gün önce',
                  deviceName: 'Sunucu',
                ),
              ],
            );
          },
        ),
      ],
    );
  }

  Widget _buildScreenRecordingsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        // Section Header
        Row(
          children: [
            Icon(
              Icons.video_library_rounded,
              color: AppTheme.primaryBlue,
              size: 18,
            ),
            const SizedBox(width: 8),
            Text(
              'Ekran Kayıtları',
              style: TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 16,
                fontWeight: FontWeight.w700,
                letterSpacing: -0.3,
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        
        // Recordings List
        Builder(
          builder: (context) {
            final screenWidth = MediaQuery.of(context).size.width;
            final crossAxisCount = screenWidth < 600
                ? 1
                : screenWidth < 900
                    ? 2
                    : screenWidth < 1200
                        ? 3
                        : screenWidth < 1600
                            ? 4
                            : 5;
            
            final spacing = 12.0;
            
            return GridView.count(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisCount: crossAxisCount,
              crossAxisSpacing: spacing,
              mainAxisSpacing: spacing,
              childAspectRatio: 2.2,
              children: [
                _RecordingCard(
                  title: 'Destek Oturumu - 15.01.2024',
                  duration: '45:32',
                  fileSize: '125 MB',
                  date: '2 gün önce',
                  deviceName: 'Ana Bilgisayar',
                ),
                _RecordingCard(
                  title: 'Eğitim Oturumu',
                  duration: '1:23:15',
                  fileSize: '342 MB',
                  date: '1 hafta önce',
                  deviceName: 'Laptop',
                ),
                _RecordingCard(
                  title: 'Sunucu Yapılandırması',
                  duration: '28:45',
                  fileSize: '89 MB',
                  date: '2 hafta önce',
                  deviceName: 'Sunucu',
                ),
              ],
            );
          },
        ),
      ],
    );
  }


  Widget _buildSettingsSection() {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          // Section Header
          Row(
            children: [
              Icon(
                Icons.settings_rounded,
                color: AppTheme.primaryBlue,
                size: 18,
              ),
              const SizedBox(width: 8),
              Text(
                'Ayarlar',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 16,
                  fontWeight: FontWeight.w700,
                  letterSpacing: -0.3,
                ),
              ),
            ],
          ),
          const SizedBox(height: 24),
          
          // Settings Categories
          _SettingsCategory(
            title: 'Bağlantı Ayarları',
            icon: Icons.link_rounded,
            iconColor: AppTheme.primaryBlue,
            children: [
              _SettingsItem(
                title: 'Otomatik Yeniden Bağlan',
                description: 'Bağlantı kesildiğinde otomatik olarak yeniden bağlanmayı dene',
                trailing: Switch(value: true, onChanged: (val) {}),
              ),
              _SettingsItem(
                title: 'Varsayılan Kalite',
                description: 'Yeni bağlantılar için varsayılan video kalitesi',
                trailing: DropdownButton<String>(
                  value: 'Yüksek',
                  items: ['Düşük', 'Orta', 'Yüksek', 'Otomatik'].map((e) => 
                    DropdownMenuItem(value: e, child: Text(e))
                  ).toList(),
                  onChanged: (val) {},
                ),
              ),
              _SettingsItem(
                title: 'Bağlantı Zaman Aşımı',
                description: 'Bağlantı kurulmazsa bekleme süresi (saniye)',
                trailing: SizedBox(
                  width: 80,
                  child: TextField(
                    decoration: InputDecoration(
                      hintText: '30',
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
          
          const SizedBox(height: 20),
          
          _SettingsCategory(
            title: 'Görünüm Ayarları',
            icon: Icons.palette_rounded,
            iconColor: AppTheme.successGreen,
            children: [
              _SettingsItem(
                title: 'Karanlık Mod',
                description: 'Uygulama temasını değiştir',
                trailing: Switch(value: true, onChanged: (val) {}),
              ),
              _SettingsItem(
                title: 'Animasyonlar',
                description: 'Arayüz animasyonlarını etkinleştir',
                trailing: Switch(value: true, onChanged: (val) {}),
              ),
              _SettingsItem(
                title: 'Varsayılan Görünüm Modu',
                description: 'Cihaz listeleri için varsayılan görünüm',
                trailing: DropdownButton<String>(
                  value: 'Geniş Grid',
                  items: ['Liste', 'Kompakt Grid', 'Geniş Grid'].map((e) => 
                    DropdownMenuItem(value: e, child: Text(e))
                  ).toList(),
                  onChanged: (val) {},
                ),
              ),
            ],
          ),
          
          const SizedBox(height: 20),
          
          _SettingsCategory(
            title: 'Güvenlik',
            icon: Icons.security_rounded,
            iconColor: AppTheme.errorRed,
            children: [
              _SettingsItem(
                title: 'Bağlantı Onayı',
                description: 'Gelen bağlantıları otomatik olarak kabul etme',
                trailing: Switch(value: false, onChanged: (val) {}),
              ),
              _SettingsItem(
                title: 'Oturum Şifresi',
                description: 'Uzak oturumlar için şifre gerektir',
                trailing: Switch(value: true, onChanged: (val) {}),
              ),
              _SettingsItem(
                title: 'Oturum Geçmişi',
                description: 'Bağlantı geçmişini sakla',
                trailing: Switch(value: true, onChanged: (val) {}),
              ),
            ],
          ),
          
          const SizedBox(height: 20),
          
          _SettingsCategory(
            title: 'Gelişmiş',
            icon: Icons.tune_rounded,
            iconColor: AppTheme.textSecondary,
            children: [
              _SettingsItem(
                title: 'STUN Sunucu',
                description: 'NAT traversal için STUN sunucu adresi',
                trailing: SizedBox(
                  width: 200,
                  child: TextField(
                    decoration: InputDecoration(
                      hintText: 'stun:stun.l.google.com:19302',
                      border: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                      ),
                    ),
                  ),
                ),
              ),
              _SettingsItem(
                title: 'Log Seviyesi',
                description: 'Uygulama loglama seviyesi',
                trailing: DropdownButton<String>(
                  value: 'Bilgi',
                  items: ['Hata', 'Uyarı', 'Bilgi', 'Hata Ayıklama'].map((e) => 
                    DropdownMenuItem(value: e, child: Text(e))
                  ).toList(),
                  onChanged: (val) {},
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _FileTransferCard extends StatefulWidget {
  final String fileName;
  final String fileSize;
  final String transferType;
  final String status;
  final String date;
  final String deviceName;

  const _FileTransferCard({
    required this.fileName,
    required this.fileSize,
    required this.transferType,
    required this.status,
    required this.date,
    required this.deviceName,
  });

  @override
  State<_FileTransferCard> createState() => _FileTransferCardState();
}

class _FileTransferCardState extends State<_FileTransferCard> {
  bool _isHovered = false;

  Color _getStatusColor() {
    switch (widget.status) {
      case 'Tamamlandı':
        return AppTheme.successGreen;
      case 'İptal Edildi':
        return AppTheme.errorRed;
      case 'Devam Ediyor':
        return AppTheme.primaryBlue;
      default:
        return AppTheme.textSecondary;
    }
  }

  IconData _getFileIcon() {
    if (widget.fileName.endsWith('.pdf')) return Icons.picture_as_pdf;
    if (widget.fileName.endsWith('.zip') || widget.fileName.endsWith('.rar')) return Icons.folder_zip;
    if (widget.fileName.endsWith('.docx') || widget.fileName.endsWith('.doc')) return Icons.description;
    if (widget.fileName.endsWith('.jpg') || widget.fileName.endsWith('.png')) return Icons.image;
    return Icons.insert_drive_file;
  }

  @override
  Widget build(BuildContext context) {
    final isUpload = widget.transferType == 'Gönderildi';
    
    return MouseRegion(
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
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: () {
          // TODO: Dosya detaylarını göster
        },
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: AppTheme.surfaceDark,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(
              color: _isHovered
                  ? AppTheme.primaryBlue.withOpacity(0.4)
                  : AppTheme.surfaceLight.withOpacity(0.2),
              width: _isHovered ? 1.5 : 1,
            ),
          ),
          transform: Matrix4.identity()
            ..translate(0.0, _isHovered ? -1.0 : 0.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              // File Icon and Status Row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // File Icon - Larger compact square design
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      color: AppTheme.primaryBlue.withOpacity(0.12),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Center(
                      child: Icon(
                        _getFileIcon(),
                        size: 20,
                        color: AppTheme.primaryBlue,
                      ),
                    ),
                  ),
                  // Status Badge
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 3),
                    decoration: BoxDecoration(
                      color: _getStatusColor(),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    child: Text(
                      widget.status,
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 10,
                        fontWeight: FontWeight.w600,
                        letterSpacing: 0.1,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              
              // File Name
              Text(
                widget.fileName,
                style: const TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 15,
                  fontWeight: FontWeight.w600,
                  letterSpacing: -0.2,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              const SizedBox(height: 4),
              
              // File Size with Direction Arrow
              Row(
                children: [
                  Icon(
                    isUpload ? Icons.arrow_upward_rounded : Icons.arrow_downward_rounded,
                    size: 13,
                    color: AppTheme.successGreen,
                  ),
                  const SizedBox(width: 4),
                  Text(
                    widget.fileSize,
                    style: TextStyle(
                      color: AppTheme.textSecondary,
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ],
              ),
              const Spacer(),
              
              // Device and Date Row
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(
                    child: Text(
                      widget.deviceName,
                      style: TextStyle(
                        color: AppTheme.textTertiary,
                        fontSize: 11,
                        fontWeight: FontWeight.w400,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    widget.date,
                    style: TextStyle(
                      color: AppTheme.textTertiary,
                      fontSize: 11,
                      fontWeight: FontWeight.w400,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 2),
            ],
          ),
        ),
      ),
    );
  }
}

class _RecordingCard extends StatefulWidget {
  final String title;
  final String duration;
  final String fileSize;
  final String date;
  final String deviceName;

  const _RecordingCard({
    required this.title,
    required this.duration,
    required this.fileSize,
    required this.date,
    required this.deviceName,
  });

  @override
  State<_RecordingCard> createState() => _RecordingCardState();
}

class _RecordingCardState extends State<_RecordingCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
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
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: () {
          // TODO: Kaydı oynat
        },
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(10),
            border: Border.all(
              color: _isHovered
                  ? AppTheme.primaryBlue.withOpacity(0.4)
                  : AppTheme.surfaceLight.withOpacity(0.2),
              width: _isHovered ? 1.5 : 1,
            ),
          ),
          transform: Matrix4.identity()
            ..translate(0.0, _isHovered ? -1.0 : 0.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              // Thumbnail Area with Gradient
              Expanded(
                child: Container(
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [
                        AppTheme.primaryBlue.withOpacity(0.25),
                        AppTheme.primaryBlueDark.withOpacity(0.15),
                      ],
                    ),
                    borderRadius: const BorderRadius.only(
                      topLeft: Radius.circular(9),
                      topRight: Radius.circular(9),
                    ),
                  ),
                  child: Stack(
                    children: [
                      // Play Icon - Large and centered
                      Center(
                        child: Container(
                          width: 48,
                          height: 48,
                          decoration: BoxDecoration(
                            color: Colors.white.withOpacity(0.25),
                            shape: BoxShape.circle,
                            border: Border.all(
                              color: Colors.white.withOpacity(0.4),
                              width: 2,
                            ),
                          ),
                          child: const Icon(
                            Icons.play_arrow_rounded,
                            size: 28,
                            color: Colors.white,
                          ),
                        ),
                      ),
                      
                      // Duration Badge - Bottom right
                      Positioned(
                        bottom: 6,
                        right: 6,
                        child: Container(
                          padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
                          decoration: BoxDecoration(
                            color: Colors.black.withOpacity(0.75),
                            borderRadius: BorderRadius.circular(5),
                          ),
                          child: Text(
                            widget.duration,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 9,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              
              // Info Area - Compact
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceDark,
                  borderRadius: const BorderRadius.only(
                    bottomLeft: Radius.circular(9),
                    bottomRight: Radius.circular(9),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // Title
                    Text(
                      widget.title,
                      style: const TextStyle(
                        color: AppTheme.textPrimary,
                        fontSize: 14,
                        fontWeight: FontWeight.w600,
                        letterSpacing: -0.2,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 5),
                    // File Size and Date
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Icon(
                              Icons.storage_rounded,
                              size: 12,
                              color: AppTheme.textTertiary,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              widget.fileSize,
                              style: TextStyle(
                                color: AppTheme.textTertiary,
                                fontSize: 11,
                                fontWeight: FontWeight.w400,
                              ),
                            ),
                          ],
                        ),
                        Text(
                          widget.date,
                          style: TextStyle(
                            color: AppTheme.textTertiary,
                            fontSize: 11,
                            fontWeight: FontWeight.w400,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
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
                    Flexible(
                      fit: FlexFit.loose,
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
                      onEnter: (_) {
                        if (mounted) {
                          Future.microtask(() {
                            if (mounted) {
                              setState(() => _isButtonHovered = true);
                            }
                          });
                        }
                      },
                      onExit: (_) {
                        if (mounted) {
                          Future.microtask(() {
                            if (mounted) {
                              setState(() => _isButtonHovered = false);
                            }
                          });
                        }
                      },
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


class _SettingsCategory extends StatelessWidget {
  final String title;
  final IconData icon;
  final Color iconColor;
  final List<Widget> children;

  const _SettingsCategory({
    required this.title,
    required this.icon,
    required this.iconColor,
    required this.children,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppTheme.surfaceDark,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: AppTheme.surfaceLight.withOpacity(0.3),
          width: 1.5,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              border: Border(
                bottom: BorderSide(
                  color: AppTheme.surfaceLight.withOpacity(0.1),
                  width: 1,
                ),
              ),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: iconColor.withOpacity(0.15),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(
                    icon,
                    size: 20,
                    color: iconColor,
                  ),
                ),
                const SizedBox(width: 12),
                Text(
                  title,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.3,
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: children,
            ),
          ),
        ],
      ),
    );
  }
}

class _SettingsItem extends StatelessWidget {
  final String title;
  final String description;
  final Widget trailing;

  const _SettingsItem({
    required this.title,
    required this.description,
    required this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 14,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 0.2,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  description,
                  style: TextStyle(
                    color: AppTheme.textSecondary,
                    fontSize: 12,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 16),
          trailing,
        ],
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
    return GestureDetector(
      onTap: () {
        // TODO: Implement shortcut action
      },
      child: MouseRegion(
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
        cursor: SystemMouseCursors.click,
        child: Stack(
          children: [
            // Background with border
            Container(
              width: 320,
              height: 240,
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [
                    AppTheme.surfaceMedium.withOpacity(0.8),
                    AppTheme.surfaceDark.withOpacity(0.9),
                  ],
                ),
                borderRadius: BorderRadius.circular(18),
              ),
            ),
            // Animated border overlay
            Positioned.fill(
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(18),
                  border: Border.all(
                    color: _isHovered
                        ? widget.iconColor.withOpacity(0.4)
                        : AppTheme.surfaceLight.withOpacity(0.1),
                    width: _isHovered ? 2 : 1,
                  ),
                ),
              ),
            ),
            // Content - Static, no animation
            ClipRRect(
              borderRadius: BorderRadius.circular(18),
              child: Stack(
                children: [
                  // Subtle accent glow
                  if (_isHovered)
                    Positioned(
                      top: -20,
                      right: -20,
                      child: Container(
                        width: 80,
                        height: 80,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          gradient: RadialGradient(
                            colors: [
                              widget.iconColor.withOpacity(0.15),
                              Colors.transparent,
                            ],
                          ),
                        ),
                      ),
                    ),
                  
                  // Content - Static, no animation
                  Padding(
                    padding: const EdgeInsets.all(20),
                    child: SizedBox(
                      height: 200, // 240 - (20 * 2) = 200
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        mainAxisAlignment: MainAxisAlignment.center,
                        mainAxisSize: MainAxisSize.max,
                        children: [
                        // Icon with modern design
                        Container(
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            gradient: LinearGradient(
                              begin: Alignment.topLeft,
                              end: Alignment.bottomRight,
                              colors: [
                                widget.iconColor.withOpacity(0.2),
                                widget.iconColor.withOpacity(0.1),
                              ],
                            ),
                            borderRadius: BorderRadius.circular(18),
                            border: Border.all(
                              color: widget.iconColor.withOpacity(0.25),
                              width: 1,
                            ),
                          ),
                          child: Icon(
                            widget.icon,
                            size: 32,
                            color: widget.iconColor,
                          ),
                        ),
                        
                        const SizedBox(height: 16),
                        
                        // Title
                        Text(
                          widget.title,
                          textAlign: TextAlign.center,
                          style: const TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 17,
                            fontWeight: FontWeight.w700,
                            letterSpacing: -0.3,
                            height: 1.2,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        
                        const SizedBox(height: 6),
                        
                        // Description
                        Text(
                          widget.description,
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 13,
                            height: 1.4,
                            fontWeight: FontWeight.w400,
                            letterSpacing: 0.1,
                          ),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        
                        const Spacer(),

                        // Shortcut Key Badge - Modern pill design
                        Center(
                          child: Container(
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                            decoration: BoxDecoration(
                              color: AppTheme.surfaceDark.withOpacity(0.6),
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(
                                color: widget.iconColor.withOpacity(0.25),
                                width: 1,
                              ),
                            ),
                            child: Row(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                Icon(
                                  Icons.keyboard_rounded,
                                  size: 15,
                                  color: widget.iconColor.withOpacity(0.7),
                                ),
                                const SizedBox(width: 8),
                                Text(
                                  widget.shortcutKey,
                                  style: TextStyle(
                                    color: widget.iconColor,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w600,
                                    fontFamily: 'monospace',
                                    letterSpacing: 0.8,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ],
                    ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Screen Record Card with special design
class _ScreenRecordCard extends StatefulWidget {
  const _ScreenRecordCard();

  @override
  State<_ScreenRecordCard> createState() => _ScreenRecordCardState();
}

class _ScreenRecordCardState extends State<_ScreenRecordCard> {
  bool _isHovered = false;
  bool _isRecording = false;
  bool _audioMuted = true;
  bool _micMuted = true;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
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
      cursor: SystemMouseCursors.click,
      child: Stack(
        children: [
          // Background
          Container(
              width: 260,
              height: 240,
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [
                    AppTheme.errorRed.withOpacity(0.2),
                    AppTheme.errorRed.withOpacity(0.15),
                  ],
                ),
                borderRadius: BorderRadius.circular(16),
              ),
          ),
          // Animated border overlay
          Positioned.fill(
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: _isHovered
                      ? AppTheme.errorRed.withOpacity(0.4)
                      : AppTheme.surfaceLight.withOpacity(0.1),
                  width: _isHovered ? 2 : 1,
                ),
              ),
            ),
          ),
          // Content - Static, no animation
          ClipRRect(
            borderRadius: BorderRadius.circular(16),
              child: Padding(
              padding: const EdgeInsets.all(14),
              child: SizedBox(
                width: double.infinity,
                height: 212, // 240 - (14 * 2) = 212
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // Header with icon
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(6),
                              decoration: BoxDecoration(
                                color: AppTheme.errorRed.withOpacity(0.2),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: const Icon(
                                Icons.videocam_rounded,
                                color: AppTheme.errorRed,
                                size: 18,
                              ),
                            ),
                            const SizedBox(width: 8),
                            const Text(
                              'Ekranı Kaydet',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                letterSpacing: -0.3,
                              ),
                            ),
                          ],
                        ),
                        IconButton(
                          icon: const Icon(
                            Icons.folder_outlined,
                            color: AppTheme.textSecondary,
                            size: 16,
                          ),
                          onPressed: () {
                            // TODO: Open recordings folder
                          },
                          tooltip: 'Kayıtlar',
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                          alignment: Alignment.centerRight,
                        ),
                      ],
                    ),
                    
                    const SizedBox(height: 12),
                  
                    // Control Section
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceDark.withOpacity(0.5),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                        children: [
                        // Monitor icon
                        Icon(
                          Icons.desktop_windows_rounded,
                          color: AppTheme.textSecondary,
                          size: 18,
                        ),
                        Container(
                          width: 1,
                          height: 18,
                          color: AppTheme.surfaceLight.withOpacity(0.2),
                        ),
                        // Audio muted
                        IconButton(
                          icon: Icon(
                            _audioMuted ? Icons.volume_off_rounded : Icons.volume_up_rounded,
                            color: _audioMuted ? AppTheme.textTertiary : AppTheme.textSecondary,
                            size: 18,
                          ),
                          onPressed: () {
                            setState(() {
                              _audioMuted = !_audioMuted;
                            });
                          },
                          tooltip: _audioMuted ? 'Ses aç' : 'Ses kapat',
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                        ),
                        Container(
                          width: 1,
                          height: 18,
                          color: AppTheme.surfaceLight.withOpacity(0.2),
                        ),
                        // Mic muted
                        IconButton(
                          icon: Icon(
                            _micMuted ? Icons.mic_off_rounded : Icons.mic_rounded,
                            color: _micMuted ? AppTheme.textTertiary : AppTheme.textSecondary,
                            size: 18,
                          ),
                          onPressed: () {
                            setState(() {
                              _micMuted = !_micMuted;
                            });
                          },
                          tooltip: _micMuted ? 'Mikrofon aç' : 'Mikrofon kapat',
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                        ),
                        Container(
                          width: 1,
                          height: 18,
                          color: AppTheme.surfaceLight.withOpacity(0.2),
                        ),
                        // Record button
                        GestureDetector(
                          onTap: () {
                            setState(() {
                              _isRecording = !_isRecording;
                            });
                          },
                          child: Container(
                            width: 32,
                            height: 32,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              color: AppTheme.errorRed,
                              border: Border.all(
                                color: Colors.white,
                                width: 2,
                              ),
                            ),
                            child: Center(
                              child: Container(
                                width: 12,
                                height: 12,
                                decoration: const BoxDecoration(
                                  shape: BoxShape.circle,
                                  color: Colors.white,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                    ),
                    
                    const SizedBox(height: 10),
                  
                    // Recordings List
                    Expanded(
                      child: Container(
                        constraints: const BoxConstraints(minHeight: 80),
                        decoration: BoxDecoration(
                          color: AppTheme.surfaceDark.withOpacity(0.3),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: SingleChildScrollView(
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              _RecordingItem(
                                name: 'screen_recording_20251206_2249',
                                date: '6.12.2025 22:49',
                                duration: '00:00',
                              ),
                              _RecordingItem(
                                name: 'screen_recording_20251206_2242',
                                date: '6.12.2025 22:42',
                                duration: '00:08',
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Recording list item
class _RecordingItem extends StatelessWidget {
  final String name;
  final String date;
  final String duration;

  const _RecordingItem({
    required this.name,
    required this.date,
    required this.duration,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  name,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 10,
                    fontWeight: FontWeight.w500,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                const SizedBox(height: 2),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      date,
                      style: TextStyle(
                        color: AppTheme.textSecondary,
                        fontSize: 9,
                      ),
                    ),
                    const SizedBox(width: 4),
                    Text(
                      duration,
                      style: TextStyle(
                        color: AppTheme.textSecondary,
                        fontSize: 9,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          IconButton(
            icon: const Icon(
              Icons.visibility_outlined,
              color: AppTheme.textSecondary,
              size: 14,
            ),
            onPressed: () {
              // TODO: View/Play recording
            },
            tooltip: 'Görüntüle',
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(),
          ),
        ],
      ),
    );
  }
}

/// File Transfer Shortcut Card with blue design
class _FileTransferShortcutCard extends StatefulWidget {
  const _FileTransferShortcutCard();

  @override
  State<_FileTransferShortcutCard> createState() => _FileTransferShortcutCardState();
}

class _FileTransferShortcutCardState extends State<_FileTransferShortcutCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
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
      cursor: SystemMouseCursors.click,
      child: Stack(
        children: [
          // Background
          Container(
            width: 260,
            height: 240,
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  AppTheme.primaryBlue.withOpacity(0.2),
                  AppTheme.primaryBlueDark.withOpacity(0.15),
                ],
              ),
              borderRadius: BorderRadius.circular(16),
            ),
          ),
          // Animated border overlay
          Positioned.fill(
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: _isHovered
                      ? AppTheme.primaryBlue.withOpacity(0.4)
                      : AppTheme.surfaceLight.withOpacity(0.1),
                  width: _isHovered ? 2 : 1,
                ),
              ),
            ),
          ),
          // Content
          ClipRRect(
            borderRadius: BorderRadius.circular(16),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: SizedBox(
                width: double.infinity,
                height: 212,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // Header with icon
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(6),
                              decoration: BoxDecoration(
                                color: AppTheme.primaryBlue.withOpacity(0.2),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: const Icon(
                                Icons.folder_rounded,
                                color: AppTheme.primaryBlue,
                                size: 18,
                              ),
                            ),
                            const SizedBox(width: 8),
                            const Text(
                              'Dosya Transfer Et',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                letterSpacing: -0.3,
                              ),
                            ),
                          ],
                        ),
                        IconButton(
                          icon: const Icon(
                            Icons.folder_outlined,
                            color: AppTheme.textSecondary,
                            size: 16,
                          ),
                          onPressed: () {
                            // TODO: Open files folder
                          },
                          tooltip: 'Dosyalar',
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                          alignment: Alignment.centerRight,
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    // Action Section
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceDark.withOpacity(0.5),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                        children: [
                          // Upload button
                          IconButton(
                            icon: const Icon(
                              Icons.upload_rounded,
                              color: AppTheme.textSecondary,
                              size: 18,
                            ),
                            onPressed: () {
                              // TODO: Open file upload dialog
                            },
                            tooltip: 'Dosya Yükle',
                            padding: EdgeInsets.zero,
                            constraints: const BoxConstraints(),
                          ),
                          Container(
                            width: 1,
                            height: 18,
                            color: AppTheme.surfaceLight.withOpacity(0.2),
                          ),
                          // Download button
                          IconButton(
                            icon: const Icon(
                              Icons.download_rounded,
                              color: AppTheme.textSecondary,
                              size: 18,
                            ),
                            onPressed: () {
                              // TODO: Open download folder
                            },
                            tooltip: 'İndirilenler',
                            padding: EdgeInsets.zero,
                            constraints: const BoxConstraints(),
                          ),
                          Container(
                            width: 1,
                            height: 18,
                            color: AppTheme.surfaceLight.withOpacity(0.2),
                          ),
                          // Send button
                          GestureDetector(
                            onTap: () {
                              // TODO: Open file transfer dialog
                            },
                            child: Container(
                              width: 32,
                              height: 32,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: AppTheme.primaryBlue,
                                border: Border.all(
                                  color: Colors.white,
                                  width: 2,
                                ),
                              ),
                              child: const Center(
                                child: Icon(
                                  Icons.send_rounded,
                                  color: Colors.white,
                                  size: 16,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 10),
                    // Files List
                    Expanded(
                      child: Container(
                        constraints: const BoxConstraints(minHeight: 80),
                        decoration: BoxDecoration(
                          color: AppTheme.surfaceDark.withOpacity(0.3),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: SingleChildScrollView(
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              _FileItem(
                                name: 'Sunum_Dosyası.pdf',
                                date: 'Bugün 14:30',
                                size: '2.4 MB',
                                status: 'Tamamlandı',
                              ),
                              _FileItem(
                                name: 'Ekran_Görüntüsü.png',
                                date: 'Dün 18:15',
                                size: '1.8 MB',
                                status: 'Tamamlandı',
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Settings Shortcut Card with gray design
class _SettingsShortcutCard extends StatefulWidget {
  const _SettingsShortcutCard();

  @override
  State<_SettingsShortcutCard> createState() => _SettingsShortcutCardState();
}

class _SettingsShortcutCardState extends State<_SettingsShortcutCard> {
  bool _isHovered = false;

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
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
      cursor: SystemMouseCursors.click,
      child: Stack(
        children: [
          // Background
          Container(
            width: 260,
            height: 240,
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [
                  AppTheme.surfaceMedium.withOpacity(0.8),
                  AppTheme.surfaceDark.withOpacity(0.9),
                ],
              ),
              borderRadius: BorderRadius.circular(16),
            ),
          ),
          // Animated border overlay
          Positioned.fill(
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 200),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: _isHovered
                      ? AppTheme.textSecondary.withOpacity(0.4)
                      : AppTheme.surfaceLight.withOpacity(0.1),
                  width: _isHovered ? 2 : 1,
                ),
              ),
            ),
          ),
          // Content
          ClipRRect(
            borderRadius: BorderRadius.circular(16),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: SizedBox(
                width: double.infinity,
                height: 212,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    // Header with icon
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(6),
                              decoration: BoxDecoration(
                                color: AppTheme.textSecondary.withOpacity(0.2),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: const Icon(
                                Icons.settings_rounded,
                                color: AppTheme.textSecondary,
                                size: 18,
                              ),
                            ),
                            const SizedBox(width: 8),
                            const Text(
                              'Ayarlar',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 14,
                                fontWeight: FontWeight.w700,
                                letterSpacing: -0.3,
                              ),
                            ),
                          ],
                        ),
                        IconButton(
                          icon: const Icon(
                            Icons.info_outline_rounded,
                            color: AppTheme.textSecondary,
                            size: 16,
                          ),
                          onPressed: () {
                            // TODO: Show settings info
                          },
                          tooltip: 'Bilgi',
                          padding: EdgeInsets.zero,
                          constraints: const BoxConstraints(),
                          alignment: Alignment.centerRight,
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    // Settings Section
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
                      decoration: BoxDecoration(
                        color: AppTheme.surfaceDark.withOpacity(0.5),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                        children: [
                          // General settings button
                          IconButton(
                            icon: const Icon(
                              Icons.tune_rounded,
                              color: AppTheme.textSecondary,
                              size: 18,
                            ),
                            onPressed: () {
                              // TODO: Open general settings
                            },
                            tooltip: 'Genel Ayarlar',
                            padding: EdgeInsets.zero,
                            constraints: const BoxConstraints(),
                          ),
                          Container(
                            width: 1,
                            height: 18,
                            color: AppTheme.surfaceLight.withOpacity(0.2),
                          ),
                          // Network settings button
                          IconButton(
                            icon: const Icon(
                              Icons.network_check_rounded,
                              color: AppTheme.textSecondary,
                              size: 18,
                            ),
                            onPressed: () {
                              // TODO: Open network settings
                            },
                            tooltip: 'Ağ Ayarları',
                            padding: EdgeInsets.zero,
                            constraints: const BoxConstraints(),
                          ),
                          Container(
                            width: 1,
                            height: 18,
                            color: AppTheme.surfaceLight.withOpacity(0.2),
                          ),
                          // Settings button
                          GestureDetector(
                            onTap: () {
                              // TODO: Open settings dialog
                            },
                            child: Container(
                              width: 32,
                              height: 32,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                color: AppTheme.textSecondary,
                                border: Border.all(
                                  color: Colors.white,
                                  width: 2,
                                ),
                              ),
                              child: const Center(
                                child: Icon(
                                  Icons.settings_rounded,
                                  color: Colors.white,
                                  size: 16,
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 10),
                    // Settings List
                    Expanded(
                      child: Container(
                        constraints: const BoxConstraints(minHeight: 80),
                        decoration: BoxDecoration(
                          color: AppTheme.surfaceDark.withOpacity(0.3),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: SingleChildScrollView(
                          child: Column(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              _SettingItem(
                                name: 'Genel Ayarlar',
                                description: 'Temel yapılandırma',
                                icon: Icons.tune_rounded,
                              ),
                              _SettingItem(
                                name: 'Ağ Ayarları',
                                description: 'Bağlantı yapılandırması',
                                icon: Icons.network_check_rounded,
                              ),
                              _SettingItem(
                                name: 'Güvenlik',
                                description: 'Şifre ve izinler',
                                icon: Icons.security_rounded,
                              ),
                              _SettingItem(
                                name: 'Görünüm',
                                description: 'Tema ve görünüm',
                                icon: Icons.palette_rounded,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// File list item
class _FileItem extends StatelessWidget {
  final String name;
  final String date;
  final String size;
  final String status;

  const _FileItem({
    required this.name,
    required this.date,
    required this.size,
    required this.status,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () {
        // TODO: Open file or show file details
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    name,
                    style: const TextStyle(
                      color: AppTheme.textPrimary,
                      fontSize: 10,
                      fontWeight: FontWeight.w500,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  const SizedBox(height: 2),
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        date,
                        style: TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: 9,
                        ),
                      ),
                      const SizedBox(width: 4),
                      Text(
                        size,
                        style: TextStyle(
                          color: AppTheme.textSecondary,
                          fontSize: 9,
                        ),
                      ),
                      const SizedBox(width: 4),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                        decoration: BoxDecoration(
                          color: AppTheme.primaryBlue.withOpacity(0.2),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          status,
                          style: TextStyle(
                            color: AppTheme.primaryBlue,
                            fontSize: 8,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            IconButton(
              icon: const Icon(
                Icons.open_in_new_rounded,
                color: AppTheme.textSecondary,
                size: 14,
              ),
              onPressed: () {
                // TODO: Open file location
              },
              tooltip: 'Konumu Aç',
              padding: EdgeInsets.zero,
              constraints: const BoxConstraints(),
            ),
          ],
        ),
      ),
    );
  }
}

/// Setting list item
class _SettingItem extends StatelessWidget {
  final String name;
  final String description;
  final IconData icon;

  const _SettingItem({
    required this.name,
    required this.description,
    required this.icon,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () {
        // TODO: Open settings category
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Flexible(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    icon,
                    size: 14,
                    color: AppTheme.textSecondary,
                  ),
                  const SizedBox(width: 6),
                  Flexible(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisAlignment: MainAxisAlignment.center,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          name,
                          style: const TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 10,
                            fontWeight: FontWeight.w500,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 2),
                        Text(
                          description,
                          style: TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 9,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            IconButton(
              icon: const Icon(
                Icons.arrow_forward_ios_rounded,
                color: AppTheme.textSecondary,
                size: 14,
              ),
              onPressed: () {
                // TODO: Open setting
              },
              tooltip: 'Aç',
              padding: EdgeInsets.zero,
              constraints: const BoxConstraints(),
            ),
          ],
        ),
      ),
    );
  }
}

