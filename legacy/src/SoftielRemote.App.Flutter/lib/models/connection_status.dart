/// Connection status enum
enum ConnectionStatus {
  disconnected,
  pending,
  connecting,
  connected,
  rejected,
  error,
}

/// ConnectionStatus extension - Backend'den gelen int değerini ConnectionStatus'a çevir
extension ConnectionStatusFromInt on ConnectionStatus {
  /// Backend'den gelen int değerini ConnectionStatus'a çevir
  static ConnectionStatus fromInt(int value) {
    switch (value) {
      case 0:
        return ConnectionStatus.disconnected;
      case 1:
        return ConnectionStatus.pending;
      case 2:
        return ConnectionStatus.connecting;
      case 3:
        return ConnectionStatus.connected;
      case 4:
        return ConnectionStatus.rejected;
      case 5:
        return ConnectionStatus.error;
      default:
        return ConnectionStatus.disconnected;
    }
  }
}

extension ConnectionStatusExtension on ConnectionStatus {
  String get displayName {
    switch (this) {
      case ConnectionStatus.disconnected:
        return 'Bağlantı Yok';
      case ConnectionStatus.pending:
        return 'Bekleniyor...';
      case ConnectionStatus.connecting:
        return 'Bağlanıyor...';
      case ConnectionStatus.connected:
        return 'Bağlı';
      case ConnectionStatus.rejected:
        return 'Reddedildi';
      case ConnectionStatus.error:
        return 'Hata';
    }
  }
}

