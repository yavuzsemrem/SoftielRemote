/// WebRTC signaling message model
class WebRTCSignalingMessage {
  final String type; // 'offer', 'answer', 'ice-candidate'
  final String targetDeviceId;
  final String senderDeviceId;
  final String connectionId;
  final String? sdp;
  final IceCandidate? iceCandidate;
  
  WebRTCSignalingMessage({
    required this.type,
    required this.targetDeviceId,
    required this.senderDeviceId,
    required this.connectionId,
    this.sdp,
    this.iceCandidate,
  });
  
  factory WebRTCSignalingMessage.fromJson(Map<String, dynamic> json) {
    return WebRTCSignalingMessage(
      type: json['type'] as String? ?? '',
      targetDeviceId: json['targetDeviceId'] as String? ?? '',
      senderDeviceId: json['senderDeviceId'] as String? ?? '',
      connectionId: json['connectionId'] as String? ?? '',
      sdp: json['sdp'] as String?,
      iceCandidate: json['iceCandidate'] != null
          ? IceCandidate.fromJson(json['iceCandidate'] as Map<String, dynamic>)
          : null,
    );
  }
  
  Map<String, dynamic> toJson() {
    return {
      'type': type,
      'targetDeviceId': targetDeviceId,
      'senderDeviceId': senderDeviceId,
      'connectionId': connectionId,
      if (sdp != null) 'sdp': sdp,
      if (iceCandidate != null) 'iceCandidate': iceCandidate!.toJson(),
    };
  }
}

/// ICE candidate model
class IceCandidate {
  final String candidate;
  final int sdpMLineIndex;
  final String? sdpMid;
  
  IceCandidate({
    required this.candidate,
    required this.sdpMLineIndex,
    this.sdpMid,
  });
  
  factory IceCandidate.fromJson(Map<String, dynamic> json) {
    return IceCandidate(
      candidate: json['candidate'] as String? ?? '',
      sdpMLineIndex: json['sdpMLineIndex'] as int? ?? 0,
      sdpMid: json['sdpMid'] as String?,
    );
  }
  
  Map<String, dynamic> toJson() {
    return {
      'candidate': candidate,
      'sdpMLineIndex': sdpMLineIndex,
      if (sdpMid != null) 'sdpMid': sdpMid,
    };
  }
}






