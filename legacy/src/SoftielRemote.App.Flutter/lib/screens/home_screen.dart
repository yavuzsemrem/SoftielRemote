import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../utils/app_theme.dart';
import '../widgets/custom_title_bar.dart';
import '../widgets/device_id_section.dart';
import '../widgets/remote_address_bar.dart';
import '../widgets/content_sections_widget.dart';
import '../services/device_id_service.dart';
import '../providers/app_state_provider.dart';
import '../providers/favorites_provider.dart';
import '../models/device_info.dart';

/// View mode provider for grid layout (popup için)
final _viewModeProvider = StateProvider<int>((ref) => 5);

/// Ana ekran - Home screen with modern tab design
class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  bool _isLoadingDeviceId = true;

  @override
  void initState() {
    super.initState();
    _loadDeviceId();
  }

  Future<void> _loadDeviceId() async {
    try {
      final deviceId = await DeviceIdService.loadDeviceId();
      if (mounted) {
        ref.read(appStateProvider.notifier).updateDeviceInfo(
              DeviceInfo(
                deviceId: deviceId,
              ),
            );
        setState(() {
          _isLoadingDeviceId = false;
        });
      }
    } catch (e) {
      debugPrint('⚠️ Device ID yüklenirken hata: $e');
      if (mounted) {
        setState(() {
          _isLoadingDeviceId = false;
        });
      }
    }
  }

  Widget _buildRecentSessionsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Section Header
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.access_time_rounded,
                      color: AppTheme.primaryBlue,
                      size: 20,
                    ),
                    const SizedBox(width: 8),
                    Text(
                      'Son Oturumlar',
                      style: TextStyle(
                        color: AppTheme.textPrimary,
                        fontSize: 18,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 0.3,
                      ),
                    ),
                  ],
                ),
                TextButton(
                  onPressed: () {
                    _showAllSessionsDialog(context);
                  },
                  style: TextButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                    minimumSize: Size.zero,
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  child: Text(
                    'Tümünü göster',
                    style: TextStyle(
                      color: AppTheme.primaryBlue,
                      fontSize: 13,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Container(
              height: 2.0,
              decoration: BoxDecoration(
                color: AppTheme.surfaceLight.withOpacity(0.7),
              ),
            ),
          ],
        ),
        const SizedBox(height: 16),
        
        // Sessions View - List or Grid based on view mode
        Consumer(
          builder: (context, ref, child) {
            final viewMode = ref.watch(globalViewModeProvider);
            
            // Sadece ilk 12 oturumu göster
            final sessionsToShow = [
              RecentSessionCard(
                deviceId: '499415805',
                deviceName: 'Ana Bilgisayar',
                isFavorite: true,
                isOnline: true,
                lastConnected: '2 saat önce',
                backgroundType: 0,
              ),
              RecentSessionCard(
                deviceId: '1199642539',
                deviceName: 'Laptop',
                isFavorite: false,
                isOnline: false,
                lastConnected: '1 gün önce',
                backgroundType: 1,
              ),
              RecentSessionCard(
                deviceId: '1464295972',
                deviceName: 'Sunucu',
                isFavorite: false,
                isOnline: false,
                lastConnected: '30 dakika önce',
                backgroundType: 2,
              ),
              RecentSessionCard(
                deviceId: '425621472',
                deviceName: 'Tablet',
                isFavorite: false,
                isOnline: false,
                lastConnected: '3 gün önce',
                backgroundType: 3,
              ),
              RecentSessionCard(
                deviceId: '140640537',
                deviceName: 'Workstation',
                isFavorite: false,
                isOnline: false,
                lastConnected: '5 saat önce',
                backgroundType: 4,
              ),
              RecentSessionCard(
                deviceId: '301252902',
                deviceName: 'Gaming PC',
                isFavorite: false,
                isOnline: false,
                lastConnected: '1 saat önce',
                backgroundType: 5,
              ),
              RecentSessionCard(
                deviceId: '789456123',
                deviceName: 'Office PC',
                isFavorite: false,
                isOnline: true,
                lastConnected: '15 dakika önce',
                backgroundType: 0,
              ),
              RecentSessionCard(
                deviceId: '321654987',
                deviceName: 'Home Server',
                isFavorite: false,
                isOnline: true,
                lastConnected: '45 dakika önce',
                backgroundType: 1,
              ),
              RecentSessionCard(
                deviceId: '654789321',
                deviceName: 'Test Machine',
                isFavorite: false,
                isOnline: false,
                lastConnected: '2 gün önce',
                backgroundType: 2,
              ),
              RecentSessionCard(
                deviceId: '987321654',
                deviceName: 'Backup Server',
                isFavorite: false,
                isOnline: true,
                lastConnected: '1 saat önce',
                backgroundType: 3,
              ),
              RecentSessionCard(
                deviceId: '147258369',
                deviceName: 'Development PC',
                isFavorite: false,
                isOnline: true,
                lastConnected: '20 dakika önce',
                backgroundType: 4,
              ),
              RecentSessionCard(
                deviceId: '369258147',
                deviceName: 'Media Center',
                isFavorite: false,
                isOnline: false,
                lastConnected: '4 gün önce',
                backgroundType: 5,
              ),
            ];
            
            // View mode'a göre görünüm seç
            if (viewMode == 1) {
              // Liste görünümü
              return Column(
                children: sessionsToShow.map((card) {
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: _buildListSessionItem(card),
                  );
                }).toList(),
              );
            } else {
              // Grid görünümü
              final crossAxisCount = viewMode == 2 ? 4 : // Kompakt mod: 4 sütun (daha küçük kartlar)
                  (MediaQuery.of(context).size.width < 600 ? 2 :
                   MediaQuery.of(context).size.width < 900 ? 4 :
                   MediaQuery.of(context).size.width < 1200 ? 5 : 6);
              
              final spacing = 10.0;
              
              return GridView.count(
                shrinkWrap: true,
                physics: const NeverScrollableScrollPhysics(),
                crossAxisCount: crossAxisCount,
                crossAxisSpacing: spacing,
                mainAxisSpacing: spacing,
                childAspectRatio: viewMode == 2 ? 2.2 : 1.8, // Kompakt mod: daha geniş ama kısa kartlar
                children: sessionsToShow,
              );
            }
          },
        ),
      ],
    );
  }

  void _showAllSessionsDialog(BuildContext context) {
    final allSessions = [
      {'deviceId': '499415805', 'deviceName': 'Ana Bilgisayar', 'isOnline': true, 'lastConnected': '2 saat önce', 'backgroundType': 0},
      {'deviceId': '1199642539', 'deviceName': 'Laptop', 'isOnline': false, 'lastConnected': '1 gün önce', 'backgroundType': 1},
      {'deviceId': '1464295972', 'deviceName': 'Sunucu', 'isOnline': false, 'lastConnected': '30 dakika önce', 'backgroundType': 2},
      {'deviceId': '425621472', 'deviceName': 'Tablet', 'isOnline': false, 'lastConnected': '3 gün önce', 'backgroundType': 3},
      {'deviceId': '140640537', 'deviceName': 'Workstation', 'isOnline': false, 'lastConnected': '5 saat önce', 'backgroundType': 4},
      {'deviceId': '301252902', 'deviceName': 'Gaming PC', 'isOnline': false, 'lastConnected': '1 saat önce', 'backgroundType': 5},
      {'deviceId': '789456123', 'deviceName': 'Office PC', 'isOnline': true, 'lastConnected': '15 dakika önce', 'backgroundType': 0},
      {'deviceId': '321654987', 'deviceName': 'Home Server', 'isOnline': true, 'lastConnected': '45 dakika önce', 'backgroundType': 1},
      {'deviceId': '654789321', 'deviceName': 'Test Machine', 'isOnline': false, 'lastConnected': '2 gün önce', 'backgroundType': 2},
      {'deviceId': '987321654', 'deviceName': 'Backup Server', 'isOnline': true, 'lastConnected': '1 saat önce', 'backgroundType': 3},
      {'deviceId': '147258369', 'deviceName': 'Development PC', 'isOnline': true, 'lastConnected': '20 dakika önce', 'backgroundType': 4},
      {'deviceId': '369258147', 'deviceName': 'Media Center', 'isOnline': false, 'lastConnected': '4 gün önce', 'backgroundType': 5},
      {'deviceId': '258147369', 'deviceName': 'Remote Desktop', 'isOnline': true, 'lastConnected': '10 dakika önce', 'backgroundType': 0},
      {'deviceId': '741852963', 'deviceName': 'VM Server', 'isOnline': false, 'lastConnected': '6 saat önce', 'backgroundType': 1},
      {'deviceId': '963741852', 'deviceName': 'NAS Storage', 'isOnline': true, 'lastConnected': '3 saat önce', 'backgroundType': 2},
      {'deviceId': '852963741', 'deviceName': 'Web Server', 'isOnline': true, 'lastConnected': '25 dakika önce', 'backgroundType': 3},
      {'deviceId': '159753486', 'deviceName': 'Database Server', 'isOnline': false, 'lastConnected': '12 saat önce', 'backgroundType': 4},
    ];

    showDialog(
      context: context,
      barrierColor: Colors.black.withOpacity(0.75),
      builder: (context) => Dialog(
        backgroundColor: Colors.transparent,
        elevation: 0,
        insetPadding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 1200, maxHeight: 800),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                AppTheme.surfaceDark,
                AppTheme.surfaceDark.withOpacity(0.95),
              ],
            ),
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.6),
                blurRadius: 30,
                spreadRadius: 0,
                offset: const Offset(0, 10),
              ),
            ],
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Header
                Container(
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [
                        AppTheme.primaryBlue.withOpacity(0.15),
                        AppTheme.primaryBlue.withOpacity(0.05),
                      ],
                    ),
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
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                            colors: [
                              AppTheme.primaryBlue,
                              AppTheme.primaryBlueDark,
                            ],
                          ),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Icon(
                          Icons.access_time_rounded,
                          color: Colors.white,
                          size: 20,
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Tüm Oturumlar',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 22,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.8,
                              ),
                            ),
                            const SizedBox(height: 4),
                            Text(
                              '${allSessions.length} oturum bulundu',
                              style: TextStyle(
                                color: AppTheme.textSecondary,
                                fontSize: 13.5,
                                fontWeight: FontWeight.w500,
                              ),
                            ),
                          ],
                        ),
                      ),
                      IconButton(
                        onPressed: () => Navigator.pop(context),
                        icon: Icon(
                          Icons.close_rounded,
                          color: AppTheme.textSecondary,
                          size: 22,
                        ),
                        style: IconButton.styleFrom(
                          padding: const EdgeInsets.all(8),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                
                // View Mode Selector - Modern Segmented Control
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
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
                      Row(
                        children: [
                          Icon(
                            Icons.view_module_rounded,
                            size: 18,
                            color: AppTheme.textSecondary,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Görünüm',
                            style: TextStyle(
                              color: AppTheme.textSecondary,
                              fontSize: 13.5,
                              fontWeight: FontWeight.w600,
                              letterSpacing: 0.2,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(width: 20),
                      _buildModernViewModeSelector(context),
                    ],
                  ),
                ),
                
                // Content - Scrollable grid
                Expanded(
                  child: Consumer(
                    builder: (context, ref, child) {
                      final viewMode = ref.watch(_viewModeProvider);
                      return SingleChildScrollView(
                        padding: const EdgeInsets.all(24),
                        child: GridView.builder(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: viewMode,
                            crossAxisSpacing: 12,
                            mainAxisSpacing: 12,
                            childAspectRatio: viewMode == 2 ? 2.2 : 1.8, // İkili mod: daha kısa kartlar
                          ),
                          itemCount: allSessions.length,
                          itemBuilder: (context, index) {
                            final session = allSessions[index];
                            final isFavorite = ref.watch(favoritesProvider).isFavorite(session['deviceId'] as String);
                            return RecentSessionCard(
                              deviceId: session['deviceId'] as String,
                              deviceName: session['deviceName'] as String,
                              isFavorite: isFavorite,
                              isOnline: session['isOnline'] as bool,
                              lastConnected: session['lastConnected'] as String,
                              backgroundType: session['backgroundType'] as int,
                            );
                          },
                        ),
                      );
                    },
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundDark,
      body: Column(
        mainAxisSize: MainAxisSize.max,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Custom Title Bar with Tabs
          const CustomTitleBar(),
          
          // Remote Address Input Bar (navbar'a yakın, farklı arka plan ile)
          const RemoteAddressBar(),
          
          // Main Content Area
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: const [
                    AppTheme.backgroundDark,
                    AppTheme.surfaceDark,
                    AppTheme.backgroundDarker,
                  ],
                  stops: const [0.0, 0.3, 1.0],
                ),
              ),
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(20),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Device ID Section
                    const DeviceIdSection(),
                    
                    const SizedBox(height: 40),
                    
                    // Content Sections (News, Recent Sessions, etc.)
                    ContentSectionsWidget(),
                    
                    const SizedBox(height: 40),
                    
                    // Son Oturumlar Section
                    _buildRecentSessionsSection(),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildGlobalViewModeSelector(BuildContext context) {
    return Consumer(
      builder: (context, ref, child) {
        final currentMode = ref.watch(globalViewModeProvider);
        
        return Container(
          height: 36,
          decoration: BoxDecoration(
            color: AppTheme.surfaceMedium.withOpacity(0.5),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(
              color: AppTheme.surfaceLight.withOpacity(0.15),
              width: 1,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _buildGlobalSegmentedButton(
                context: context,
                ref: ref,
                mode: 1,
                currentMode: currentMode,
                icon: Icons.format_list_bulleted_rounded,
                tooltip: 'Liste Görünümü',
              ),
              Container(
                width: 1,
                height: 24,
                color: AppTheme.surfaceLight.withOpacity(0.15),
              ),
              _buildGlobalSegmentedButton(
                context: context,
                ref: ref,
                mode: 2,
                currentMode: currentMode,
                icon: Icons.grid_view_rounded,
                tooltip: 'Kompakt Grid',
              ),
              Container(
                width: 1,
                height: 24,
                color: AppTheme.surfaceLight.withOpacity(0.15),
              ),
              _buildGlobalSegmentedButton(
                context: context,
                ref: ref,
                mode: 3,
                currentMode: currentMode,
                icon: Icons.view_module_rounded,
                tooltip: 'Geniş Grid',
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildGlobalSegmentedButton({
    required BuildContext context,
    required WidgetRef ref,
    required int mode,
    required int currentMode,
    required IconData icon,
    required String tooltip,
  }) {
    final isSelected = currentMode == mode;
    bool isHovered = false;
    
    return StatefulBuilder(
      builder: (context, setState) {
        return Tooltip(
          message: tooltip,
          waitDuration: const Duration(milliseconds: 500),
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
              onTap: () {
                ref.read(globalViewModeProvider.notifier).state = mode;
              },
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                curve: Curves.easeOutCubic,
                width: 36,
                height: 36,
                decoration: BoxDecoration(
                  gradient: isSelected
                      ? LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: [
                            AppTheme.primaryBlue,
                            AppTheme.primaryBlueDark,
                          ],
                        )
                      : null,
                  color: isSelected
                      ? null
                      : isHovered
                          ? AppTheme.surfaceLight.withOpacity(0.1)
                          : Colors.transparent,
                  borderRadius: BorderRadius.circular(8),
                  boxShadow: isSelected
                      ? [
                          BoxShadow(
                            color: AppTheme.primaryBlue.withOpacity(0.3),
                            blurRadius: 8,
                            spreadRadius: 0,
                            offset: const Offset(0, 2),
                          ),
                        ]
                      : null,
                ),
                child: Icon(
                  icon,
                  size: 16,
                  color: isSelected
                      ? Colors.white
                      : isHovered
                          ? AppTheme.primaryBlue
                          : AppTheme.textSecondary,
                ),
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _buildListSessionItem(RecentSessionCard card) {
    return Consumer(
      builder: (context, ref, child) {
        final isFavorite = ref.watch(favoritesProvider).isFavorite(card.deviceId);
        
        return GestureDetector(
          onSecondaryTapDown: (details) {
            _showHomeListContextMenu(context, ref, card, isFavorite, details.globalPosition);
          },
          child: Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: AppTheme.surfaceMedium,
              borderRadius: BorderRadius.circular(8),
              border: Border.all(
                color: AppTheme.surfaceLight.withOpacity(0.2),
                width: 1,
              ),
            ),
            child: Row(
              children: [
                // Thumbnail/Icon
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
                        // Background gradient based on backgroundType
                        Positioned.fill(
                          child: _buildCardBackground(card.backgroundType),
                        ),
                        // Status indicator
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
                  onPressed: () {
                    // TODO: Connect
                  },
                ),
                IconButton(
                  icon: Icon(Icons.insert_drive_file_rounded, size: 18, color: AppTheme.textSecondary),
                  onPressed: () {
                    // TODO: File transfer
                  },
                ),
                Builder(
                  builder: (context) => IconButton(
                    icon: Icon(Icons.more_vert_rounded, size: 18, color: AppTheme.textSecondary),
                    onPressed: () {
                      final RenderBox? renderBox = context.findRenderObject() as RenderBox?;
                      if (renderBox != null) {
                        final Offset localToGlobal = renderBox.localToGlobal(Offset.zero);
                        _showHomeListContextMenu(
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

  void _showHomeListContextMenu(
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
                      _buildHomeListMenuButton(
                        icon: Icons.arrow_forward_rounded,
                        text: 'Bağla',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Connect to session
                        },
                      ),
                      _buildHomeListMenuButton(
                        icon: Icons.upload_rounded,
                        text: 'Davet Et',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Invite
                        },
                      ),
                      _buildHomeListMenuButton(
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
                      _buildHomeListMenuButton(
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
                      _buildHomeListMenuButton(
                        icon: Icons.copy_rounded,
                        text: 'Kopyala',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Copy device ID
                        },
                      ),
                      _buildHomeListMenuButton(
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
                      _buildHomeListMenuButton(
                        icon: Icons.desktop_windows_rounded,
                        text: 'Masaüstüne bırak',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Drop to desktop
                        },
                      ),
                      _buildHomeListMenuButton(
                        icon: Icons.edit_rounded,
                        text: 'Adını değiştir',
                        onTap: () {
                          Navigator.pop(context);
                          _showHomeListRenameDialog(context, ref, card);
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

  Widget _buildHomeListMenuButton({
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

  void _showHomeListRenameDialog(BuildContext context, WidgetRef ref, RecentSessionCard card) {
    final currentCustomName = ref.read(customNamesProvider).getCustomName(card.deviceId);
    final textController = TextEditingController(
      text: currentCustomName ?? _formatDeviceId(card.deviceId),
    );

    showDialog(
      context: context,
      barrierColor: Colors.black.withOpacity(0.75),
      builder: (context) => Dialog(
        backgroundColor: Colors.transparent,
        elevation: 0,
        insetPadding: const EdgeInsets.symmetric(horizontal: 24),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 480),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                AppTheme.surfaceDark,
                AppTheme.surfaceDark.withOpacity(0.95),
              ],
            ),
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.6),
                blurRadius: 30,
                spreadRadius: 0,
                offset: const Offset(0, 10),
              ),
              BoxShadow(
                color: AppTheme.primaryBlue.withOpacity(0.1),
                blurRadius: 20,
                spreadRadius: -5,
                offset: const Offset(0, 5),
              ),
            ],
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Header with gradient accent
                Container(
                  padding: const EdgeInsets.all(28),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [
                        AppTheme.primaryBlue.withOpacity(0.15),
                        AppTheme.primaryBlue.withOpacity(0.05),
                      ],
                    ),
                  ),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                            colors: [
                              AppTheme.primaryBlue,
                              AppTheme.primaryBlueDark,
                            ],
                          ),
                          borderRadius: BorderRadius.circular(14),
                        ),
                        child: Icon(
                          Icons.edit_rounded,
                          color: Colors.white,
                          size: 22,
                        ),
                      ),
                      const SizedBox(width: 18),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Adını Değiştir',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 22,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.8,
                                height: 1.2,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              'Bu cihaz için özel bir isim belirleyin',
                              style: TextStyle(
                                color: AppTheme.textSecondary,
                                fontSize: 13.5,
                                fontWeight: FontWeight.w500,
                                height: 1.3,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                
                // Content
                Padding(
                  padding: const EdgeInsets.all(28),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(
                            Icons.label_outline_rounded,
                            size: 16,
                            color: AppTheme.primaryBlue,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Yeni İsim',
                            style: TextStyle(
                              color: AppTheme.textPrimary,
                              fontSize: 13.5,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 0.3,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      Container(
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(14),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.2),
                              blurRadius: 8,
                              spreadRadius: 0,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: TextField(
                          controller: textController,
                          autofocus: true,
                          style: TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 15.5,
                            fontWeight: FontWeight.w500,
                            letterSpacing: 0.2,
                          ),
                          decoration: InputDecoration(
                            hintText: 'Örn: Ana Bilgisayar, Laptop...',
                            hintStyle: TextStyle(
                              color: AppTheme.textTertiary,
                              fontSize: 14.5,
                              fontWeight: FontWeight.w400,
                            ),
                            filled: true,
                            fillColor: AppTheme.surfaceMedium,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 18,
                              vertical: 18,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(14),
                              borderSide: BorderSide.none,
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(14),
                              borderSide: BorderSide(
                                color: AppTheme.surfaceLight.withOpacity(0.15),
                                width: 1.5,
                              ),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(14),
                              borderSide: BorderSide(
                                color: AppTheme.primaryBlue,
                                width: 2.5,
                              ),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: AppTheme.primaryBlue.withOpacity(0.08),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                            color: AppTheme.primaryBlue.withOpacity(0.15),
                            width: 1,
                          ),
                        ),
                        child: Row(
                          children: [
                            Icon(
                              Icons.info_outline_rounded,
                              size: 16,
                              color: AppTheme.primaryBlue,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                'Boş bırakırsanız cihaz ID\'si gösterilir',
                                style: TextStyle(
                                  color: AppTheme.textSecondary,
                                  fontSize: 12.5,
                                  fontWeight: FontWeight.w500,
                                  height: 1.4,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                
                // Actions
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceMedium.withOpacity(0.5),
                    border: Border(
                      top: BorderSide(
                        color: AppTheme.surfaceLight.withOpacity(0.1),
                        width: 1,
                      ),
                    ),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      TextButton(
                        onPressed: () => Navigator.pop(context),
                        style: TextButton.styleFrom(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 24,
                            vertical: 14,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                        child: Text(
                          'İptal',
                          style: TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 14.5,
                            fontWeight: FontWeight.w600,
                            letterSpacing: 0.2,
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Container(
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                            colors: [
                              AppTheme.primaryBlue,
                              AppTheme.primaryBlueDark,
                            ],
                          ),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: ElevatedButton(
                          onPressed: () {
                            final newName = textController.text.trim();
                            ref.read(customNamesProvider.notifier).setCustomName(
                                  card.deviceId,
                                  newName,
                                );
                            Navigator.pop(context);
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.transparent,
                            foregroundColor: Colors.white,
                            shadowColor: Colors.transparent,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 28,
                              vertical: 14,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                            elevation: 0,
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                Icons.check_rounded,
                                size: 18,
                                color: Colors.white,
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'Kaydet',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 14.5,
                                  fontWeight: FontWeight.w700,
                                  letterSpacing: 0.3,
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
        ),
      ),
    );
  }

  Widget _buildCardBackground(int backgroundType) {
    // RecentSessionCard'daki _buildBackground metodunun basitleştirilmiş versiyonu
    switch (backgroundType) {
      case 0:
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFF6B46C1),
                const Color(0xFFDC2626),
                const Color(0xFFF97316),
              ],
            ),
          ),
        );
      case 1:
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFF1E3A8A),
                const Color(0xFF3B82F6),
                const Color(0xFF60A5FA),
              ],
            ),
          ),
        );
      case 2:
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [
                const Color(0xFF9CA3AF),
                const Color(0xFF6B7280),
                const Color(0xFF4B5563),
              ],
            ),
          ),
        );
      case 3:
        return Container(color: Colors.black);
      case 4:
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFFDC2626),
                const Color(0xFFF97316),
                const Color(0xFFFBBF24),
              ],
            ),
          ),
        );
      case 5:
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFF0067C0),
                const Color(0xFF0078D4),
                const Color(0xFF00BCF2),
              ],
            ),
          ),
        );
      default:
        return Container(color: AppTheme.surfaceDark);
    }
  }

  Widget _buildModernViewModeSelector(BuildContext context) {
    return Consumer(
      builder: (context, ref, child) {
        final currentMode = ref.watch(_viewModeProvider);
        
        return Container(
          height: 40,
          decoration: BoxDecoration(
            color: AppTheme.surfaceMedium.withOpacity(0.5),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(
              color: AppTheme.surfaceLight.withOpacity(0.15),
              width: 1,
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              _buildSegmentedButton(
                context: context,
                ref: ref,
                mode: 2,
                currentMode: currentMode,
                icon: Icons.grid_view_rounded,
                tooltip: 'Kompakt Görünüm',
              ),
                Container(
                  width: 1,
                  height: 24,
                  color: AppTheme.surfaceLight.withOpacity(0.15),
                ),
                _buildSegmentedButton(
                  context: context,
                  ref: ref,
                  mode: 4,
                  currentMode: currentMode,
                  icon: Icons.view_module_rounded,
                  tooltip: 'Orta Görünüm',
                ),
                Container(
                  width: 1,
                  height: 24,
                  color: AppTheme.surfaceLight.withOpacity(0.15),
                ),
                _buildSegmentedButton(
                  context: context,
                  ref: ref,
                  mode: 5,
                  currentMode: currentMode,
                  icon: Icons.view_compact_rounded,
                  tooltip: 'Geniş Görünüm',
                ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildSegmentedButton({
    required BuildContext context,
    required WidgetRef ref,
    required int mode,
    required int currentMode,
    required IconData icon,
    required String tooltip,
  }) {
    final isSelected = currentMode == mode;
    bool isHovered = false;
    
    return StatefulBuilder(
      builder: (context, setState) {
        return Tooltip(
          message: tooltip,
          waitDuration: const Duration(milliseconds: 500),
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
              onTap: () {
                ref.read(_viewModeProvider.notifier).state = mode;
              },
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                curve: Curves.easeOutCubic,
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                decoration: BoxDecoration(
                  gradient: isSelected
                      ? LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: [
                            AppTheme.primaryBlue,
                            AppTheme.primaryBlueDark,
                          ],
                        )
                      : null,
                  color: isSelected
                      ? null
                      : isHovered
                          ? AppTheme.surfaceLight.withOpacity(0.1)
                          : Colors.transparent,
                  borderRadius: BorderRadius.circular(10),
                  boxShadow: isSelected
                      ? [
                          BoxShadow(
                            color: AppTheme.primaryBlue.withOpacity(0.3),
                            blurRadius: 8,
                            spreadRadius: 0,
                            offset: const Offset(0, 2),
                          ),
                        ]
                      : null,
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    AnimatedSwitcher(
                      duration: const Duration(milliseconds: 200),
                      child: Icon(
                        icon,
                        key: ValueKey('$mode-$isSelected'),
                        size: 18,
                        color: isSelected
                            ? Colors.white
                            : isHovered
                                ? AppTheme.primaryBlue
                                : AppTheme.textSecondary,
                      ),
                    ),
                    const SizedBox(width: 8),
                    AnimatedDefaultTextStyle(
                      duration: const Duration(milliseconds: 200),
                      style: TextStyle(
                        color: isSelected
                            ? Colors.white
                            : isHovered
                                ? AppTheme.primaryBlue
                                : AppTheme.textSecondary,
                        fontSize: 13,
                        fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                        letterSpacing: 0.2,
                      ),
                      child: Text(
                        '$mode',
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}

class RecentSessionCard extends ConsumerStatefulWidget {
  final String deviceId;
  final String deviceName;
  final bool isFavorite;
  final bool isOnline;
  final String lastConnected;
  final int backgroundType;

  const RecentSessionCard({
    super.key,
    required this.deviceId,
    required this.deviceName,
    required this.isFavorite,
    required this.isOnline,
    required this.lastConnected,
    this.backgroundType = 0,
  });

  @override
  ConsumerState<RecentSessionCard> createState() => _RecentSessionCardState();
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

class _RecentSessionCardState extends ConsumerState<RecentSessionCard> {
  bool _isHovered = false;

  bool get _isFavorite {
    return ref.watch(favoritesProvider).isFavorite(widget.deviceId);
  }

  String get _displayText {
    final customName = ref.watch(customNamesProvider).getCustomName(widget.deviceId);
    return customName ?? _formatDeviceId(widget.deviceId);
  }

  Color _getBottomLineColor() {
    // Aktif olanlar yeşil, aktif olmayanlar kırmızı
    return widget.isOnline 
        ? AppTheme.successGreen 
        : Colors.red;
  }

  void _showRenameDialog(BuildContext context) {
    final currentCustomName = ref.read(customNamesProvider).getCustomName(widget.deviceId);
    final textController = TextEditingController(
      text: currentCustomName ?? _formatDeviceId(widget.deviceId),
    );

    showDialog(
      context: context,
      barrierColor: Colors.black.withOpacity(0.75),
      builder: (context) => Dialog(
        backgroundColor: Colors.transparent,
        elevation: 0,
        insetPadding: const EdgeInsets.symmetric(horizontal: 24),
        child: Container(
          constraints: const BoxConstraints(maxWidth: 480),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                AppTheme.surfaceDark,
                AppTheme.surfaceDark.withOpacity(0.95),
              ],
            ),
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.6),
                blurRadius: 30,
                spreadRadius: 0,
                offset: const Offset(0, 10),
              ),
              BoxShadow(
                color: AppTheme.primaryBlue.withOpacity(0.1),
                blurRadius: 20,
                spreadRadius: -5,
                offset: const Offset(0, 5),
              ),
            ],
          ),
          child: ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Header with gradient accent
                Container(
                  padding: const EdgeInsets.all(28),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                      colors: [
                        AppTheme.primaryBlue.withOpacity(0.15),
                        AppTheme.primaryBlue.withOpacity(0.05),
                      ],
                    ),
                  ),
                  child: Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                            colors: [
                              AppTheme.primaryBlue,
                              AppTheme.primaryBlueDark,
                            ],
                          ),
                          borderRadius: BorderRadius.circular(14),
                        ),
                        child: Icon(
                          Icons.edit_rounded,
                          color: Colors.white,
                          size: 22,
                        ),
                      ),
                      const SizedBox(width: 18),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'Adını Değiştir',
                              style: TextStyle(
                                color: AppTheme.textPrimary,
                                fontSize: 22,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.8,
                                height: 1.2,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Text(
                              'Bu cihaz için özel bir isim belirleyin',
                              style: TextStyle(
                                color: AppTheme.textSecondary,
                                fontSize: 13.5,
                                fontWeight: FontWeight.w500,
                                height: 1.3,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                
                // Content
                Padding(
                  padding: const EdgeInsets.all(28),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(
                            Icons.label_outline_rounded,
                            size: 16,
                            color: AppTheme.primaryBlue,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            'Yeni İsim',
                            style: TextStyle(
                              color: AppTheme.textPrimary,
                              fontSize: 13.5,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 0.3,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      Container(
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(14),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withOpacity(0.2),
                              blurRadius: 8,
                              spreadRadius: 0,
                              offset: const Offset(0, 2),
                            ),
                          ],
                        ),
                        child: TextField(
                          controller: textController,
                          autofocus: true,
                          style: TextStyle(
                            color: AppTheme.textPrimary,
                            fontSize: 15.5,
                            fontWeight: FontWeight.w500,
                            letterSpacing: 0.2,
                          ),
                          decoration: InputDecoration(
                            hintText: 'Örn: Ana Bilgisayar, Laptop...',
                            hintStyle: TextStyle(
                              color: AppTheme.textTertiary,
                              fontSize: 14.5,
                              fontWeight: FontWeight.w400,
                            ),
                            filled: true,
                            fillColor: AppTheme.surfaceMedium,
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 18,
                              vertical: 18,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(14),
                              borderSide: BorderSide.none,
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(14),
                              borderSide: BorderSide(
                                color: AppTheme.surfaceLight.withOpacity(0.15),
                                width: 1.5,
                              ),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(14),
                              borderSide: BorderSide(
                                color: AppTheme.primaryBlue,
                                width: 2.5,
                              ),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(height: 16),
                      Container(
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: AppTheme.primaryBlue.withOpacity(0.08),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                            color: AppTheme.primaryBlue.withOpacity(0.15),
                            width: 1,
                          ),
                        ),
                        child: Row(
                          children: [
                            Icon(
                              Icons.info_outline_rounded,
                              size: 16,
                              color: AppTheme.primaryBlue,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                'Boş bırakırsanız cihaz ID\'si gösterilir',
                                style: TextStyle(
                                  color: AppTheme.textSecondary,
                                  fontSize: 12.5,
                                  fontWeight: FontWeight.w500,
                                  height: 1.4,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                
                // Actions
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceMedium.withOpacity(0.5),
                    border: Border(
                      top: BorderSide(
                        color: AppTheme.surfaceLight.withOpacity(0.1),
                        width: 1,
                      ),
                    ),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      TextButton(
                        onPressed: () => Navigator.pop(context),
                        style: TextButton.styleFrom(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 24,
                            vertical: 14,
                          ),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(10),
                          ),
                        ),
                        child: Text(
                          'İptal',
                          style: TextStyle(
                            color: AppTheme.textSecondary,
                            fontSize: 14.5,
                            fontWeight: FontWeight.w600,
                            letterSpacing: 0.2,
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Container(
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                            colors: [
                              AppTheme.primaryBlue,
                              AppTheme.primaryBlueDark,
                            ],
                          ),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: ElevatedButton(
                          onPressed: () {
                            final newName = textController.text.trim();
                            ref.read(customNamesProvider.notifier).setCustomName(
                                  widget.deviceId,
                                  newName,
                                );
                            Navigator.pop(context);
                          },
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.transparent,
                            foregroundColor: Colors.white,
                            shadowColor: Colors.transparent,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 28,
                              vertical: 14,
                            ),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(10),
                            ),
                            elevation: 0,
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Icon(
                                Icons.check_rounded,
                                size: 18,
                                color: Colors.white,
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'Kaydet',
                                style: TextStyle(
                                  color: Colors.white,
                                  fontSize: 14.5,
                                  fontWeight: FontWeight.w700,
                                  letterSpacing: 0.3,
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
        ),
      ),
    );
  }

  void _showContextMenu(BuildContext context, Offset position) {
    final RenderBox? overlay = Overlay.of(context).context.findRenderObject() as RenderBox?;
    if (overlay == null) return;

    const double menuWidth = 220.0;
    const double menuItemHeight = 32.0;
    const int menuItemCount = 9; // Menü öğelerinin sayısı
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
      useRootNavigator: true, // Root navigator kullan - çerçeve üstünde görünsün
      pageBuilder: (context, animation, secondaryAnimation) {
        return Stack(
          children: [
            Positioned(
              left: menuLeft,
              top: menuTop,
              child: Material(
                color: Colors.transparent,
                elevation: 1000, // Yüksek z-index için elevation
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
                      _buildMenuButton(
                        icon: Icons.arrow_forward_rounded,
                        text: 'Bağla',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Connect to session
                        },
                      ),
                      _buildMenuButton(
                        icon: Icons.upload_rounded,
                        text: 'Davet Et',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Invite
                        },
                      ),
                      _buildMenuButton(
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
                      _buildMenuButton(
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
                      _buildMenuButton(
                        icon: Icons.copy_rounded,
                        text: 'Kopyala',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Copy device ID
                        },
                      ),
                      _buildMenuButton(
                        icon: _isFavorite
                            ? Icons.star_rounded
                            : Icons.star_outline_rounded,
                        text: _isFavorite
                            ? 'Favorilerden Çıkart'
                            : 'Favorilere Ekle',
                        iconColor: const Color(0xFFFFD700), // Sarı renk
                        onTap: () {
                          Navigator.pop(context);
                          final favoritesNotifier = ref.read(favoritesProvider.notifier);
                          if (_isFavorite) {
                            favoritesNotifier.removeFavorite(widget.deviceId);
                          } else {
                            favoritesNotifier.addFavorite(
                              SessionCardData(
                                deviceId: widget.deviceId,
                                deviceName: widget.deviceName,
                                isOnline: widget.isOnline,
                                lastConnected: widget.lastConnected,
                                backgroundType: widget.backgroundType,
                              ),
                            );
                          }
                        },
                      ),
                      _buildMenuButton(
                        icon: Icons.desktop_windows_rounded,
                        text: 'Masaüstüne bırak',
                        onTap: () {
                          Navigator.pop(context);
                          // TODO: Drop to desktop
                        },
                      ),
                      _buildMenuButton(
                        icon: Icons.edit_rounded,
                        text: 'Adını değiştir',
                        onTap: () {
                          Navigator.pop(context);
                          _showRenameDialog(context);
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

  Widget _buildMenuButton({
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
          child: _buildMenuItem(icon: icon, text: text, iconColor: iconColor),
        ),
      ),
    );
  }

  Widget _buildMenuItem({
    required IconData icon,
    required String text,
    Color? iconColor,
  }) {
    return Row(
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
    );
  }

  Widget _buildBackground() {
    switch (widget.backgroundType) {
      case 0: // Purple-red gradient
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFF6B46C1),
                const Color(0xFFDC2626),
                const Color(0xFFF97316),
              ],
            ),
          ),
          child: Container(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.center,
                radius: 0.8,
                colors: [
                  Colors.white.withOpacity(0.1),
                  Colors.transparent,
                ],
              ),
            ),
          ),
        );
      case 1: // Blue swirl
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFF1E3A8A),
                const Color(0xFF3B82F6),
                const Color(0xFF60A5FA),
              ],
            ),
          ),
          child: CustomPaint(
            painter: _SwirlPainter(),
          ),
        );
      case 2: // Metal/TUF
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [
                const Color(0xFF9CA3AF),
                const Color(0xFF6B7280),
                const Color(0xFF4B5563),
              ],
            ),
          ),
          child: Center(
            child: Icon(
              Icons.memory,
              size: 40,
              color: Colors.black.withOpacity(0.3),
            ),
          ),
        );
      case 3: // Black
        return Container(
          color: Colors.black,
        );
      case 4: // Red-orange gradient
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFFDC2626),
                const Color(0xFFF97316),
                const Color(0xFFFBBF24),
              ],
            ),
          ),
          child: Container(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.center,
                radius: 0.8,
                colors: [
                  Colors.white.withOpacity(0.1),
                  Colors.transparent,
                ],
              ),
            ),
          ),
        );
      case 5: // Windows logo
        return Container(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [
                const Color(0xFF0067C0),
                const Color(0xFF0078D4),
                const Color(0xFF00BCF2),
              ],
            ),
          ),
          child: Center(
            child: Icon(
              Icons.window,
              size: 35,
              color: Colors.white.withOpacity(0.3),
            ),
          ),
        );
      default:
        return Container(color: AppTheme.surfaceDark);
    }
  }

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
        onSecondaryTapDown: (details) {
          _showContextMenu(context, details.globalPosition);
        },
        onTap: () {
          // TODO: Connect to session
        },
        child: SizedBox(
          width: 216, // 120 * 1.8 (childAspectRatio from GridView)
          height: 120,
          child: AnimatedContainer(
            duration: const Duration(milliseconds: 250),
            curve: Curves.easeOutCubic,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(12),
              border: Border.all(
                color: _isHovered
                    ? AppTheme.surfaceLight.withOpacity(0.4)
                    : AppTheme.surfaceLight.withOpacity(0.15),
                width: _isHovered ? 1.5 : 1,
              ),
              boxShadow: _isHovered
                  ? [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.3),
                        blurRadius: 10,
                        spreadRadius: 0,
                        offset: const Offset(0, 4),
                      ),
                    ]
                  : [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.2),
                        blurRadius: 6,
                        spreadRadius: -2,
                        offset: const Offset(0, 2),
                      ),
                    ],
            ),
            transform: Matrix4.identity()
              ..translate(0.0, _isHovered ? -2.0 : 0.0)
              ..scale(_isHovered ? 1.02 : 1.0),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(12),
              child: Stack(
                children: [
                  // Background
                  Positioned.fill(
                    child: _buildBackground(),
                  ),
                  
                  // Content Overlay
                  Positioned.fill(
                    child: Container(
                      padding: const EdgeInsets.all(6),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            Colors.black.withOpacity(0.2),
                            Colors.black.withOpacity(0.4),
                            Colors.black.withOpacity(0.6),
                          ],
                        ),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Header: Status + Favorite
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              // Status Indicator
                              Container(
                                width: 12,
                                height: 12,
                                decoration: BoxDecoration(
                                  color: widget.isOnline
                                      ? AppTheme.successGreen
                                      : Colors.red,
                                  shape: BoxShape.circle,
                                ),
                              ),
                              
                              // Favorite Icon (tıklanabilir)
                              GestureDetector(
                                onTap: () {
                                  final favoritesNotifier = ref.read(favoritesProvider.notifier);
                                  if (_isFavorite) {
                                    favoritesNotifier.removeFavorite(widget.deviceId);
                                  } else {
                                    favoritesNotifier.addFavorite(
                                      SessionCardData(
                                        deviceId: widget.deviceId,
                                        deviceName: widget.deviceName,
                                        isOnline: widget.isOnline,
                                        lastConnected: widget.lastConnected,
                                        backgroundType: widget.backgroundType,
                                      ),
                                    );
                                  }
                                },
                                child: Icon(
                                  _isFavorite
                                      ? Icons.star_rounded
                                      : Icons.star_outline_rounded,
                                  size: 18,
                                  color: _isFavorite
                                      ? const Color(0xFFFFD700)
                                      : Colors.white.withOpacity(0.6),
                                ),
                              ),
                            ],
                          ),
                          
                          const Spacer(),
                          
                          // Device ID (bottom left)
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Expanded(
                                child: Row(
                                  children: [
                                    // ID Icon
                                    Icon(
                                      Icons.computer_rounded,
                                      size: 16,
                                      color: Colors.white.withOpacity(0.7),
                                    ),
                                    const SizedBox(width: 6),
                                    // Device ID Text (or custom name)
                                    Expanded(
                                      child: Text(
                                        _displayText,
                                        style: TextStyle(
                                          color: Colors.white,
                                          fontSize: 14,
                                          fontWeight: FontWeight.w600,
                                          fontFamily: ref.watch(customNamesProvider).getCustomName(widget.deviceId) != null
                                              ? null // Custom name için monospace kullanma
                                              : 'monospace',
                                          letterSpacing: 0.5,
                                          shadows: [
                                            Shadow(
                                              color: Colors.black,
                                              blurRadius: 4,
                                              offset: Offset(0, 1),
                                            ),
                                          ],
                                        ),
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              
                              // Menu Icon (bottom right)
                              Builder(
                                builder: (context) => GestureDetector(
                                  onTapDown: (details) {
                                    final RenderBox? renderBox = context.findRenderObject() as RenderBox?;
                                    if (renderBox != null) {
                                      final Offset localToGlobal = renderBox.localToGlobal(Offset.zero);
                                      _showContextMenu(
                                        context,
                                        Offset(
                                          localToGlobal.dx + renderBox.size.width,
                                          localToGlobal.dy + renderBox.size.height,
                                        ),
                                      );
                                    }
                                  },
                                  child: Icon(
                                    Icons.more_vert_rounded,
                                    size: 20,
                                    color: Colors.white.withOpacity(0.7),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ],
                      ),
                    ),
                  ),
                  
                  // Bottom Line
                  Positioned(
                    bottom: 0,
                    left: 0,
                    right: 0,
                    child: Container(
                      height: 2,
                      color: _getBottomLineColor(),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// Custom painter for swirl effect
class _SwirlPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = Colors.white.withOpacity(0.1)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2;

    final path = Path();
    path.moveTo(size.width * 0.2, size.height * 0.3);
    path.quadraticBezierTo(
      size.width * 0.5,
      size.height * 0.2,
      size.width * 0.8,
      size.height * 0.4,
    );
    path.quadraticBezierTo(
      size.width * 0.9,
      size.height * 0.6,
      size.width * 0.7,
      size.height * 0.8,
    );
    path.quadraticBezierTo(
      size.width * 0.4,
      size.height * 0.9,
      size.width * 0.2,
      size.height * 0.7,
    );

    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

