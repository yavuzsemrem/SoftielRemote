import 'connection_status.dart';

/// Connection response model
/// Backend API'den gelen bağlantı yanıtı
class ConnectionResponse {
  final bool success;
  final ConnectionStatus status;
  final String? errorMessage;
  final String? agentEndpoint;
  final String? connectionId;
  
  ConnectionResponse({
    required this.success,
    required this.status,
    this.errorMessage,
    this.agentEndpoint,
    this.connectionId,
  });
  
  factory ConnectionResponse.fromJson(Map<String, dynamic> json) {
    return ConnectionResponse(
      success: json['success'] as bool? ?? false,
      status: ConnectionStatusFromInt.fromInt(json['status'] as int? ?? 0),
      errorMessage: json['errorMessage'] as String?,
      agentEndpoint: json['agentEndpoint'] as String?,
      connectionId: json['connectionId'] as String?,
    );
  }
}

