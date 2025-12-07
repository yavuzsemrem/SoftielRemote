/// Device information model
class DeviceInfo {
  final String deviceId;
  final bool isRegistered;
  final DateTime? registeredAt;

  DeviceInfo({
    required this.deviceId,
    this.isRegistered = false,
    this.registeredAt,
  });

  DeviceInfo copyWith({
    String? deviceId,
    bool? isRegistered,
    DateTime? registeredAt,
  }) {
    return DeviceInfo(
      deviceId: deviceId ?? this.deviceId,
      isRegistered: isRegistered ?? this.isRegistered,
      registeredAt: registeredAt ?? this.registeredAt,
    );
  }
}

