/// Connection status enum
enum ConnectionStatus {
  disconnected,
  connecting,
  connected,
  error,
}

extension ConnectionStatusExtension on ConnectionStatus {
  String get displayName {
    switch (this) {
      case ConnectionStatus.disconnected:
        return 'Bağlantı Yok';
      case ConnectionStatus.connecting:
        return 'Bağlanıyor...';
      case ConnectionStatus.connected:
        return 'Bağlı';
      case ConnectionStatus.error:
        return 'Hata';
    }
  }
}

