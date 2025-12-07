import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../utils/app_theme.dart';
import '../widgets/custom_title_bar.dart';
import '../widgets/device_id_section.dart';
import '../widgets/remote_address_bar.dart';
import '../widgets/content_sections_widget.dart';
import '../services/device_id_service.dart';
import '../providers/app_state_provider.dart';
import '../models/device_info.dart';

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
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Row(
              children: [
                Icon(
                  Icons.history,
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
                // TODO: Show all sessions
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
        const SizedBox(height: 16),
        
        // Sessions Grid
        LayoutBuilder(
          builder: (context, constraints) {
            final crossAxisCount = constraints.maxWidth < 600
                ? 1
                : constraints.maxWidth < 900
                    ? 2
                    : constraints.maxWidth < 1200
                        ? 3
                        : 4;
            
            final spacing = 12.0;
            
            return GridView.count(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              crossAxisCount: crossAxisCount,
              crossAxisSpacing: spacing,
              mainAxisSpacing: spacing,
              childAspectRatio: 1.6,
              children: [
                _RecentSessionCard(
                  deviceId: '499415805',
                  deviceName: 'Ana Bilgisayar',
                  isFavorite: true,
                  isOnline: true,
                  lastConnected: '2 saat önce',
                  backgroundType: 0, // Purple-red gradient
                ),
                _RecentSessionCard(
                  deviceId: '1199642539',
                  deviceName: 'Laptop',
                  isFavorite: false,
                  isOnline: false,
                  lastConnected: '1 gün önce',
                  backgroundType: 1, // Blue swirl
                ),
                _RecentSessionCard(
                  deviceId: '1464295972',
                  deviceName: 'Sunucu',
                  isFavorite: false,
                  isOnline: false,
                  lastConnected: '30 dakika önce',
                  backgroundType: 2, // Metal/TUF
                ),
                _RecentSessionCard(
                  deviceId: '425621472',
                  deviceName: 'Tablet',
                  isFavorite: false,
                  isOnline: false,
                  lastConnected: '3 gün önce',
                  backgroundType: 3, // Black
                ),
                _RecentSessionCard(
                  deviceId: '140640537',
                  deviceName: 'Workstation',
                  isFavorite: false,
                  isOnline: false,
                  lastConnected: '5 saat önce',
                  backgroundType: 4, // Red-orange gradient
                ),
                _RecentSessionCard(
                  deviceId: '301252902',
                  deviceName: 'Gaming PC',
                  isFavorite: false,
                  isOnline: false,
                  lastConnected: '1 saat önce',
                  backgroundType: 5, // Windows logo
                ),
              ],
            );
          },
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppTheme.backgroundDark,
      body: Column(
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
                  children: [
                    // Device ID Section
                    const DeviceIdSection(),
                    
                    const SizedBox(height: 40),
                    
                    // Content Sections (News, Recent Sessions, etc.)
                    const ContentSectionsWidget(),
                    
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
}

class _RecentSessionCard extends StatefulWidget {
  final String deviceId;
  final String deviceName;
  final bool isFavorite;
  final bool isOnline;
  final String lastConnected;
  final int backgroundType;

  const _RecentSessionCard({
    required this.deviceId,
    required this.deviceName,
    required this.isFavorite,
    required this.isOnline,
    required this.lastConnected,
    this.backgroundType = 0,
  });

  @override
  State<_RecentSessionCard> createState() => _RecentSessionCardState();
}

class _RecentSessionCardState extends State<_RecentSessionCard> {
  bool _isHovered = false;

  Color _getBottomLineColor() {
    switch (widget.backgroundType) {
      case 0:
        return AppTheme.successGreen;
      case 1:
        return AppTheme.primaryBlue;
      case 2:
        return AppTheme.successGreen;
      case 3:
        return const Color(0xFF1A5F1A);
      case 4:
        return const Color(0xFF8B5CF6);
      case 5:
        return AppTheme.primaryBlue;
      default:
        return AppTheme.primaryBlue;
    }
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
      onEnter: (_) => setState(() => _isHovered = true),
      onExit: (_) => setState(() => _isHovered = false),
      cursor: SystemMouseCursors.click,
      child: GestureDetector(
        onTap: () {
          // TODO: Connect to session
        },
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
                Container(
                  padding: const EdgeInsets.all(8),
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
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      // Header: Status + Favorite
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          // Status Indicator
                          Container(
                            width: 10,
                            height: 10,
                            decoration: BoxDecoration(
                              color: widget.isOnline
                                  ? AppTheme.successGreen
                                  : Colors.red,
                              shape: BoxShape.circle,
                              border: Border.all(
                                color: Colors.white.withOpacity(0.3),
                                width: 1.5,
                              ),
                              boxShadow: widget.isOnline
                                  ? [
                                      BoxShadow(
                                        color: AppTheme.successGreen.withOpacity(0.6),
                                        blurRadius: 4,
                                        spreadRadius: 1,
                                      ),
                                    ]
                                  : null,
                            ),
                            child: widget.isOnline
                                ? null
                                : Center(
                                    child: Container(
                                      width: 8,
                                      height: 1.5,
                                      decoration: BoxDecoration(
                                        color: Colors.white,
                                        borderRadius: BorderRadius.circular(1),
                                      ),
                                    ),
                                  ),
                          ),
                          
                          // Favorite Icon
                          Icon(
                            widget.isFavorite
                                ? Icons.star_rounded
                                : Icons.star_outline_rounded,
                            size: 16,
                            color: widget.isFavorite
                                ? const Color(0xFFFFD700)
                                : Colors.white.withOpacity(0.6),
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
                            child: Text(
                              widget.deviceId,
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 12,
                                fontWeight: FontWeight.w600,
                                fontFamily: 'monospace',
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
                          
                          // Menu Icon (bottom right)
                          Icon(
                            Icons.more_vert_rounded,
                            size: 16,
                            color: Colors.white.withOpacity(0.7),
                          ),
                        ],
                      ),
                    ],
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

