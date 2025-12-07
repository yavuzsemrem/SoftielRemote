import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/notification_provider.dart';
import '../utils/app_theme.dart';

/// Sağ üstte gösterilecek modern notification overlay
class NotificationOverlay extends ConsumerWidget {
  const NotificationOverlay({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final notifications = ref.watch(notificationProvider).notifications;

    if (notifications.isEmpty) {
      return const SizedBox.shrink();
    }

    return Positioned(
      top: 50,
      right: 16,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        mainAxisSize: MainAxisSize.min,
        children: notifications.map((notification) {
          return Padding(
            key: ValueKey(notification.id),
            padding: const EdgeInsets.only(bottom: 12),
            child: _NotificationItem(
              key: ValueKey(notification.id),
              notification: notification,
              onDismiss: () {
                ref.read(notificationProvider.notifier).remove(notification.id);
              },
            ),
          );
        }).toList(),
      ),
    );
  }
}

/// Tek bir notification item widget'ı
class _NotificationItem extends StatefulWidget {
  final NotificationModel notification;
  final VoidCallback onDismiss;

  const _NotificationItem({
    super.key,
    required this.notification,
    required this.onDismiss,
  });

  @override
  State<_NotificationItem> createState() => _NotificationItemState();
}

class _NotificationItemState extends State<_NotificationItem>
    with TickerProviderStateMixin {
  late AnimationController _enterController;
  late AnimationController _exitController;
  late Animation<Offset> _slideAnimation;
  late Animation<double> _fadeAnimation;
  bool _isDismissing = false;
  Timer? _autoDismissTimer;

  @override
  void initState() {
    super.initState();
    
    // Giriş animasyonu için controller - hızlandırıldı
    _enterController = AnimationController(
      duration: const Duration(milliseconds: 400), // Hızlı ve yumuşak giriş
      vsync: this,
    );

    // Çıkış animasyonu için ayrı controller - hızlandırıldı
    _exitController = AnimationController(
      duration: const Duration(milliseconds: 350), // Hızlı çıkış
      vsync: this,
    );

    // Sağdan sola kayarak gelme animasyonu - hızlı ve smooth curve
    _slideAnimation = Tween<Offset>(
      begin: const Offset(1.0, 0.0), // Sağdan başla
      end: Offset.zero, // Normal pozisyon
    ).animate(CurvedAnimation(
      parent: _enterController,
      curve: Curves.easeOutCubic, // Yumuşak ve hızlı giriş
    ));

    _fadeAnimation = Tween<double>(
      begin: 0.0,
      end: 1.0,
    ).animate(CurvedAnimation(
      parent: _enterController,
      curve: Curves.easeOutCubic, // Aynı smooth curve
    ));

    // Giriş animasyonunu başlat
    _enterController.forward();

    // Otomatik kapanma için timer - animasyonlu kapanma
    if (widget.notification.duration.inMilliseconds > 0) {
      _autoDismissTimer = Timer(widget.notification.duration, () {
        if (mounted && !_isDismissing) {
          _dismiss();
        }
      });
    }
  }

  @override
  void dispose() {
    _autoDismissTimer?.cancel();
    _enterController.dispose();
    _exitController.dispose();
    super.dispose();
  }

  /// Kapatma animasyonu (sağa kayarak + fade)
  Future<void> _dismiss() async {
    if (_isDismissing) return;
    _isDismissing = true;

    // Çıkış için yeni animasyonlar oluştur - hem sağa kayma hem fade
    final exitSlideAnimation = Tween<Offset>(
      begin: Offset.zero,
      end: const Offset(1.0, 0.0), // Sağa kay
    ).animate(CurvedAnimation(
      parent: _exitController,
      curve: Curves.easeInCubic, // Yumuşak ama hızlı çıkış
    ));

    final exitFadeAnimation = Tween<double>(
      begin: 1.0,
      end: 0.0,
    ).animate(CurvedAnimation(
      parent: _exitController,
      curve: Curves.easeInCubic, // Fade out ile birlikte
    ));

    // Animasyonları güncelle
    setState(() {
      _slideAnimation = exitSlideAnimation;
      _fadeAnimation = exitFadeAnimation;
    });

    // Controller'ı reset edip çıkış animasyonunu başlat
    _exitController.reset();
    await _exitController.forward();
    
    if (mounted) {
      widget.onDismiss();
    }
  }

  Color _getBackgroundColor() {
    switch (widget.notification.type) {
      case NotificationType.success:
        return AppTheme.successGreen;
      case NotificationType.error:
        return Colors.red.shade600;
      case NotificationType.info:
        return AppTheme.primaryBlue;
      case NotificationType.warning:
        return Colors.orange.shade600;
    }
  }

  Color _getIconColor() {
    return Colors.white;
  }

  IconData _getDefaultIcon() {
    if (widget.notification.icon != null) {
      return widget.notification.icon!;
    }

    switch (widget.notification.type) {
      case NotificationType.success:
        return Icons.check_circle_rounded;
      case NotificationType.error:
        return Icons.error_rounded;
      case NotificationType.info:
        return Icons.info_rounded;
      case NotificationType.warning:
        return Icons.warning_rounded;
    }
  }

  @override
  Widget build(BuildContext context) {
    return SlideTransition(
      position: _slideAnimation,
      child: FadeTransition(
        opacity: _fadeAnimation,
        child: Material(
          elevation: 0,
          color: Colors.transparent,
          child: Container(
            constraints: const BoxConstraints(
              maxWidth: 400,
              minWidth: 300,
            ),
            decoration: BoxDecoration(
              color: _getBackgroundColor(),
              borderRadius: BorderRadius.circular(16),
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.3),
                  blurRadius: 20,
                  spreadRadius: 0,
                  offset: const Offset(0, 8),
                ),
                BoxShadow(
                  color: _getBackgroundColor().withOpacity(0.3),
                  blurRadius: 15,
                  spreadRadius: -5,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(16),
              child: Stack(
                children: [
                  // Gradient overlay
                  Positioned.fill(
                    child: Container(
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                          colors: [
                            _getBackgroundColor(),
                            _getBackgroundColor().withOpacity(0.9),
                          ],
                        ),
                      ),
                    ),
                  ),
                  // Content
                  Padding(
                    padding: const EdgeInsets.all(16),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        // Icon
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(
                            color: Colors.white.withOpacity(0.2),
                            borderRadius: BorderRadius.circular(10),
                          ),
                          child: Icon(
                            _getDefaultIcon(),
                            color: _getIconColor(),
                            size: 24,
                          ),
                        ),
                        const SizedBox(width: 12),
                        // Message
                        Expanded(
                          child: Text(
                            widget.notification.message,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 14.5,
                              fontWeight: FontWeight.w600,
                              letterSpacing: 0.2,
                              height: 1.4,
                            ),
                            maxLines: 3,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        const SizedBox(width: 8),
                        // Close button
                        Material(
                          color: Colors.transparent,
                          child: InkWell(
                            onTap: _dismiss,
                            borderRadius: BorderRadius.circular(8),
                            child: Container(
                              padding: const EdgeInsets.all(4),
                              child: Icon(
                                Icons.close_rounded,
                                color: Colors.white.withOpacity(0.9),
                                size: 18,
                              ),
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
      ),
    );
  }
}

