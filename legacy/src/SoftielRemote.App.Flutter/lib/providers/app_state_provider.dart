import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/device_info.dart';
import '../models/connection_info.dart';
import '../models/connection_status.dart';

/// App state - Device information
class AppState {
  final DeviceInfo deviceInfo;
  final ConnectionInfo connectionInfo;

  AppState({
    required this.deviceInfo,
    required this.connectionInfo,
  });

  AppState copyWith({
    DeviceInfo? deviceInfo,
    ConnectionInfo? connectionInfo,
  }) {
    return AppState(
      deviceInfo: deviceInfo ?? this.deviceInfo,
      connectionInfo: connectionInfo ?? this.connectionInfo,
    );
  }
}

/// App state provider
final appStateProvider = StateNotifierProvider<AppStateNotifier, AppState>(
  (ref) => AppStateNotifier(),
);

class AppStateNotifier extends StateNotifier<AppState> {
  AppStateNotifier()
      : super(
          AppState(
            deviceInfo: DeviceInfo(
              deviceId: '---',
            ),
            connectionInfo: ConnectionInfo(
              remoteDeviceId: '',
            ),
          ),
        );

  void updateDeviceInfo(DeviceInfo deviceInfo) {
    state = state.copyWith(deviceInfo: deviceInfo);
  }

  void updateConnectionInfo(ConnectionInfo connectionInfo) {
    state = state.copyWith(connectionInfo: connectionInfo);
  }

  void setRemoteDeviceId(String deviceId) {
    state = state.copyWith(
      connectionInfo: state.connectionInfo.copyWith(
        remoteDeviceId: deviceId,
      ),
    );
  }

  void setConnectionStatus(ConnectionStatus status) {
    state = state.copyWith(
      connectionInfo: state.connectionInfo.copyWith(
        status: status,
      ),
    );
  }
}

