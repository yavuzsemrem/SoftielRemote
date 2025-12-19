import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// Notification tipi
enum NotificationType {
  success,
  error,
  info,
  warning,
}

/// Notification modeli
class NotificationModel {
  final String id;
  final String message;
  final NotificationType type;
  final Duration duration;
  final IconData? icon;

  NotificationModel({
    required this.id,
    required this.message,
    required this.type,
    this.duration = const Duration(seconds: 3),
    this.icon,
  });
}

/// Notification state
class NotificationState {
  final List<NotificationModel> notifications;

  NotificationState({List<NotificationModel>? notifications})
      : notifications = notifications ?? [];

  NotificationState copyWith({
    List<NotificationModel>? notifications,
  }) {
    return NotificationState(
      notifications: notifications ?? this.notifications,
    );
  }
}

/// Notification provider
final notificationProvider =
    StateNotifierProvider<NotificationNotifier, NotificationState>(
  (ref) => NotificationNotifier(),
);

/// Notification notifier
class NotificationNotifier extends StateNotifier<NotificationState> {
  NotificationNotifier() : super(NotificationState());

  static int _notificationCounter = 0;
  
  /// Yeni notification ekle
  void show({
    required String message,
    required NotificationType type,
    Duration? duration,
    IconData? icon,
  }) {
    // Unique ID üret (timestamp + counter)
    _notificationCounter++;
    final uniqueId = '${DateTime.now().millisecondsSinceEpoch}_$_notificationCounter';
    
    final notification = NotificationModel(
      id: uniqueId,
      message: message,
      type: type,
      duration: duration ?? const Duration(seconds: 3),
      icon: icon,
    );

    state = state.copyWith(
      notifications: [notification, ...state.notifications],
    );

    // Otomatik kaldırma artık widget içinde animasyonlu olarak yapılıyor
    // Burada kaldırıldı çünkü animasyonlu kapanma için widget'ın kendi timer'ı kullanılıyor
  }

  /// Notification kaldır
  void remove(String id) {
    state = state.copyWith(
      notifications: state.notifications
          .where((n) => n.id != id)
          .toList(),
    );
  }

  /// Tüm notification'ları temizle
  void clear() {
    state = NotificationState();
  }

  // Helper methods
  void showSuccess(String message, {Duration? duration, IconData? icon}) {
    show(
      message: message,
      type: NotificationType.success,
      duration: duration,
      icon: icon ?? Icons.check_circle_rounded,
    );
  }

  void showError(String message, {Duration? duration, IconData? icon}) {
    show(
      message: message,
      type: NotificationType.error,
      duration: duration,
      icon: icon ?? Icons.error_rounded,
    );
  }

  void showInfo(String message, {Duration? duration, IconData? icon}) {
    show(
      message: message,
      type: NotificationType.info,
      duration: duration,
      icon: icon ?? Icons.info_rounded,
    );
  }

  void showWarning(String message, {Duration? duration, IconData? icon}) {
    show(
      message: message,
      type: NotificationType.warning,
      duration: duration,
      icon: icon ?? Icons.warning_rounded,
    );
  }
}

