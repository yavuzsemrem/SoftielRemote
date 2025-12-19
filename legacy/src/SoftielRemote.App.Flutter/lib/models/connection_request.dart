import 'connection_status.dart';

/// Connection request model
/// Backend API'ye gönderilecek bağlantı isteği
class ConnectionRequest {
  final String targetDeviceId;
  final String? requesterId;
  final String? requesterName;
  final QualityLevel qualityLevel;
  
  ConnectionRequest({
    required this.targetDeviceId,
    this.requesterId,
    this.requesterName,
    this.qualityLevel = QualityLevel.medium,
  });
  
  Map<String, dynamic> toJson() {
    return {
      'targetDeviceId': targetDeviceId,
      if (requesterId != null) 'requesterId': requesterId,
      if (requesterName != null) 'requesterName': requesterName,
      'qualityLevel': qualityLevel.index,
    };
  }
}

/// Quality level enum
enum QualityLevel {
  low,
  medium,
  high,
  auto,
}






