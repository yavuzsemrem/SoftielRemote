import 'connection_status.dart';

/// Connection information model
class ConnectionInfo {
  final String remoteDeviceId;
  final ConnectionStatus status;
  final int? latency;
  final double? bitrate;
  final int? fps;
  final String? errorMessage;

  ConnectionInfo({
    required this.remoteDeviceId,
    this.status = ConnectionStatus.disconnected,
    this.latency,
    this.bitrate,
    this.fps,
    this.errorMessage,
  });

  ConnectionInfo copyWith({
    String? remoteDeviceId,
    ConnectionStatus? status,
    int? latency,
    double? bitrate,
    int? fps,
    String? errorMessage,
  }) {
    return ConnectionInfo(
      remoteDeviceId: remoteDeviceId ?? this.remoteDeviceId,
      status: status ?? this.status,
      latency: latency ?? this.latency,
      bitrate: bitrate ?? this.bitrate,
      fps: fps ?? this.fps,
      errorMessage: errorMessage ?? this.errorMessage,
    );
  }
}

