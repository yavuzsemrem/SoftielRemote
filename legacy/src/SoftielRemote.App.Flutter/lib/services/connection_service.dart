import 'dart:async';
import 'package:flutter/foundation.dart';
import '../services/backend_api_service.dart';
import '../services/signalr_service.dart';
import '../services/webrtc_service.dart';
import '../services/device_id_service.dart';
import '../models/connection_request.dart';
import '../models/webrtc_signaling_message.dart';
import '../models/connection_status.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';

/// Connection service
/// Tüm bağlantı mantığını yönetir (Backend API, SignalR, WebRTC)
class ConnectionService {
  final BackendApiService _backendApi;
  SignalRService? _signalR;
  WebRTCService? _webRTC;
  
  String? _currentConnectionId;
  String? _currentTargetDeviceId;
  String? _currentDeviceId;
  ConnectionStatus _currentStatus = ConnectionStatus.disconnected;
  Timer? _connectionTimeoutTimer;
  
  // Event handlers
  final StreamController<ConnectionStatus> _onStatusChange = StreamController<ConnectionStatus>.broadcast();
  final StreamController<String> _onError = StreamController<String>.broadcast();
  final StreamController<MediaStream> _onRemoteStream = StreamController<MediaStream>.broadcast();
  
  Stream<ConnectionStatus> get onStatusChange => _onStatusChange.stream;
  Stream<String> get onError => _onError.stream;
  Stream<MediaStream> get onRemoteStream => _onRemoteStream.stream;
  
  ConnectionService(this._backendApi);
  
  /// Bağlantı kurar
  Future<bool> connect(String targetDeviceId, {String? requesterName}) async {
    try {
      debugPrint('🔵 Bağlantı kuruluyor: $targetDeviceId');
      
      // Device ID'yi al
      _currentDeviceId = await DeviceIdService.loadDeviceId();
      _currentTargetDeviceId = targetDeviceId;
      
      _currentStatus = ConnectionStatus.pending;
      _onStatusChange.add(ConnectionStatus.pending);
      
      // 1. Backend API'ye connection request gönder
      final request = ConnectionRequest(
        targetDeviceId: targetDeviceId,
        requesterId: _currentDeviceId,
        requesterName: requesterName,
      );
      
      final response = await _backendApi.requestConnection(request);
      
      if (!response.success) {
        _currentStatus = ConnectionStatus.error;
        _onError.add(response.errorMessage ?? 'Bağlantı isteği başarısız');
        _onStatusChange.add(ConnectionStatus.error);
        return false;
      }
      
      _currentConnectionId = response.connectionId;
      _currentStatus = response.status;
      _onStatusChange.add(response.status);
      
      // 2. SignalR bağlantısını başlat
      _signalR = SignalRService(
        backendUrl: _backendApi.backendUrl,
        deviceId: _currentDeviceId!,
      );
      
      final signalRConnected = await _signalR!.connect();
      if (!signalRConnected) {
        _onError.add('SignalR bağlantısı kurulamadı');
        _onStatusChange.add(ConnectionStatus.error);
        return false;
      }
      
      // SignalR event handlers
      _signalR!.onConnectionResponse.listen((response) {
        _handleConnectionResponse(response);
      });
      
      _signalR!.onSignalingMessage.listen((message) {
        _handleSignalingMessage(message);
      });
      
      _signalR!.onSignalingError.listen((error) {
        _onError.add(error);
        _onStatusChange.add(ConnectionStatus.error);
      });
      
      // 3. Connection response timeout (30 saniye)
      // Eğer Agent yanıt vermezse timeout ver
      _connectionTimeoutTimer?.cancel();
      _connectionTimeoutTimer = Timer(const Duration(seconds: 30), () {
        if (_currentStatus == ConnectionStatus.pending || 
            _currentStatus == ConnectionStatus.connecting) {
          debugPrint('⚠️ Connection response timeout - Agent yanıt vermedi');
          _currentStatus = ConnectionStatus.error;
          _onError.add('Bağlantı zaman aşımına uğradı. Agent yanıt vermedi.');
          _onStatusChange.add(ConnectionStatus.error);
        }
      });
      
      return true;
    } catch (e) {
      debugPrint('❌ Connection hatası: $e');
      _onError.add(e.toString());
      _onStatusChange.add(ConnectionStatus.error);
      return false;
    }
  }
  
  /// Connection response'u işler (Agent onayladığında)
  void _handleConnectionResponse(Map<String, dynamic> response) {
    try {
      final accepted = response['accepted'] as bool? ?? false;
      final status = response['status'] as String? ?? '';
      final agentEndpoint = response['agentEndpoint'] as String?;
      
      debugPrint('🔵 Connection response alındı: accepted=$accepted, status=$status, agentEndpoint=$agentEndpoint');
      
      // Status'u güncelle
      _currentStatus = accepted ? ConnectionStatus.connecting : ConnectionStatus.rejected;
      
      if (!accepted) {
        _onStatusChange.add(ConnectionStatus.rejected);
        _onError.add('Bağlantı reddedildi');
        return;
      }
      
      // Status string'ini parse et
      ConnectionStatus parsedStatus = ConnectionStatus.connecting;
      if (status.toLowerCase() == 'connecting') {
        parsedStatus = ConnectionStatus.connecting;
      } else if (status.toLowerCase() == 'connected') {
        parsedStatus = ConnectionStatus.connected;
      } else if (status.toLowerCase() == 'rejected') {
        parsedStatus = ConnectionStatus.rejected;
      }
      
      _currentStatus = parsedStatus;
      _onStatusChange.add(parsedStatus);
      
      // Timeout timer'ı iptal et (response geldi)
      _connectionTimeoutTimer?.cancel();
      _connectionTimeoutTimer = null;
      
      if (parsedStatus == ConnectionStatus.connecting && agentEndpoint != null) {
        _startWebRTCConnection(agentEndpoint);
      } else if (parsedStatus == ConnectionStatus.connecting) {
        // AgentEndpoint yoksa bile connecting durumunda kal
        debugPrint('⚠️ AgentEndpoint null, ancak connecting durumunda kalınıyor');
      }
    } catch (e) {
      debugPrint('❌ Connection response işleme hatası: $e');
      _onError.add(e.toString());
      _onStatusChange.add(ConnectionStatus.error);
    }
  }
  
  /// WebRTC bağlantısını başlatır
  Future<void> _startWebRTCConnection(String agentEndpoint) async {
    try {
      debugPrint('🔵 WebRTC bağlantısı başlatılıyor...');
      
      // WebRTC service oluştur
      _webRTC = WebRTCService(
        deviceId: _currentDeviceId!,
        targetDeviceId: _currentTargetDeviceId!,
        connectionId: _currentConnectionId!,
        turnServerUrl: null, // TODO: TURN server URL'i config'den al
      );
      
      final initialized = await _webRTC!.initialize();
      if (!initialized) {
        _onError.add('WebRTC başlatılamadı');
        _onStatusChange.add(ConnectionStatus.error);
        return;
      }
      
      // WebRTC event handlers
      _webRTC!.onRemoteStream.listen((stream) {
        _onRemoteStream.add(stream);
        _onStatusChange.add(ConnectionStatus.connected);
      });
      
      _webRTC!.onIceCandidate.listen((candidate) {
        final message = _webRTC!.iceCandidateToMessage(candidate);
        _signalR?.sendWebRTCSignaling(message);
      });
      
      _webRTC!.onConnectionStateChange.listen((state) {
        debugPrint('🔵 WebRTC connection state: $state');
        if (state == RTCPeerConnectionState.RTCPeerConnectionStateConnected) {
          _onStatusChange.add(ConnectionStatus.connected);
        } else if (state == RTCPeerConnectionState.RTCPeerConnectionStateFailed ||
                   state == RTCPeerConnectionState.RTCPeerConnectionStateDisconnected) {
          _onStatusChange.add(ConnectionStatus.error);
        }
      });
      
      // SDP offer oluştur ve gönder
      final offerSdp = await _webRTC!.createOffer();
      if (offerSdp != null) {
        final offerMessage = WebRTCSignalingMessage(
          type: 'offer',
          targetDeviceId: _currentTargetDeviceId!,
          senderDeviceId: _currentDeviceId!,
          connectionId: _currentConnectionId!,
          sdp: offerSdp,
        );
        
        await _signalR?.sendWebRTCSignaling(offerMessage);
        debugPrint('✅ SDP offer gönderildi');
      }
    } catch (e) {
      debugPrint('❌ WebRTC bağlantı hatası: $e');
      _onError.add(e.toString());
      _onStatusChange.add(ConnectionStatus.error);
    }
  }
  
  /// Signaling mesajını işler
  void _handleSignalingMessage(WebRTCSignalingMessage message) {
    try {
      debugPrint('🔵 Signaling mesajı işleniyor: ${message.type}');
      
      if (_webRTC == null) {
        debugPrint('⚠️ WebRTC service null');
        return;
      }
      
      switch (message.type) {
        case 'answer':
          if (message.sdp != null) {
            _webRTC!.setRemoteAnswer(message.sdp!);
          }
          break;
          
        case 'ice-candidate':
          if (message.iceCandidate != null) {
            final candidate = RTCIceCandidate(
              message.iceCandidate!.candidate,
              message.iceCandidate!.sdpMid,
              message.iceCandidate!.sdpMLineIndex,
            );
            _webRTC!.addIceCandidate(candidate);
          }
          break;
          
        default:
          debugPrint('⚠️ Bilinmeyen signaling mesaj tipi: ${message.type}');
      }
    } catch (e) {
      debugPrint('❌ Signaling mesajı işleme hatası: $e');
      _onError.add(e.toString());
    }
  }
  
  /// Bağlantıyı kapatır
  Future<void> disconnect() async {
    _connectionTimeoutTimer?.cancel();
    _connectionTimeoutTimer = null;
    _currentStatus = ConnectionStatus.disconnected;
    try {
      debugPrint('🔵 Bağlantı kapatılıyor...');
      
      await _webRTC?.close();
      await _signalR?.disconnect();
      
      _webRTC = null;
      _signalR = null;
      _currentConnectionId = null;
      _currentTargetDeviceId = null;
      
      _onStatusChange.add(ConnectionStatus.disconnected);
    } catch (e) {
      debugPrint('❌ Disconnect hatası: $e');
    }
  }
  
  /// Remote renderer'ı döner
  RTCVideoRenderer? get remoteRenderer => _webRTC?.remoteRenderer;
  
  /// Dispose
  void dispose() {
    disconnect();
    _onStatusChange.close();
    _onError.close();
    _onRemoteStream.close();
  }
}

