import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter_webrtc/flutter_webrtc.dart';
import '../models/webrtc_signaling_message.dart';

/// WebRTC client service
/// Video stream almak ve input göndermek için
class WebRTCService {
  RTCPeerConnection? _peerConnection;
  MediaStream? _remoteStream;
  RTCVideoRenderer? _remoteRenderer;
  
  final String _deviceId;
  final String _targetDeviceId;
  final String _connectionId;
  final String? _turnServerUrl;
  
  // Event handlers
  final StreamController<MediaStream> _onRemoteStream = StreamController<MediaStream>.broadcast();
  final StreamController<RTCIceCandidate> _onIceCandidate = StreamController<RTCIceCandidate>.broadcast();
  final StreamController<RTCPeerConnectionState> _onConnectionStateChange = StreamController<RTCPeerConnectionState>.broadcast();
  
  Stream<MediaStream> get onRemoteStream => _onRemoteStream.stream;
  Stream<RTCIceCandidate> get onIceCandidate => _onIceCandidate.stream;
  Stream<RTCPeerConnectionState> get onConnectionStateChange => _onConnectionStateChange.stream;
  
  WebRTCService({
    required String deviceId,
    required String targetDeviceId,
    required String connectionId,
    String? turnServerUrl,
  })  : _deviceId = deviceId,
        _targetDeviceId = targetDeviceId,
        _connectionId = connectionId,
        _turnServerUrl = turnServerUrl;
  
  /// WebRTC peer connection'ı başlatır
  Future<bool> initialize() async {
    try {
      debugPrint('🔵 WebRTC peer connection başlatılıyor...');
      
      // ICE servers
      final iceServers = <Map<String, dynamic>>[
        {
          'urls': [
            'stun:stun.l.google.com:19302',
            'stun:stun1.l.google.com:19302',
          ],
        },
      ];
      
      // TURN server ekle (eğer varsa)
      if (_turnServerUrl != null && _turnServerUrl!.isNotEmpty) {
        iceServers.add({
          'urls': [_turnServerUrl!],
        });
      }
      
      // Peer connection oluştur
      _peerConnection = await createPeerConnection({
        'iceServers': iceServers,
        'iceCandidatePoolSize': 10,
      });
      
      // Event handlers
      _peerConnection!.onIceCandidate = (RTCIceCandidate candidate) {
        debugPrint('🔵 ICE candidate alındı: ${candidate.candidate}');
        _onIceCandidate.add(candidate);
      };
      
      _peerConnection!.onConnectionState = (RTCPeerConnectionState state) {
        debugPrint('🔵 WebRTC connection state: $state');
        _onConnectionStateChange.add(state);
      };
      
      _peerConnection!.onTrack = (RTCTrackEvent event) {
        if (event.streams.isNotEmpty) {
          _remoteStream = event.streams[0];
          debugPrint('🔵 Remote stream alındı: ${_remoteStream!.id}');
          _onRemoteStream.add(_remoteStream!);
        }
      };
      
      // Remote renderer oluştur
      _remoteRenderer = RTCVideoRenderer();
      await _remoteRenderer!.initialize();
      
      debugPrint('✅ WebRTC peer connection başlatıldı');
      return true;
    } catch (e) {
      debugPrint('❌ WebRTC initialize hatası: $e');
      return false;
    }
  }
  
  /// SDP offer oluşturur ve gönderir
  Future<String?> createOffer() async {
    try {
      if (_peerConnection == null) {
        debugPrint('⚠️ Peer connection null');
        return null;
      }
      
      debugPrint('🔵 SDP offer oluşturuluyor...');
      
      // Data channel oluştur (input göndermek için)
      final dataChannel = await _peerConnection!.createDataChannel(
        'input',
        RTCDataChannelInit(),
      );
      
      dataChannel.onMessage = (RTCDataChannelMessage message) {
        debugPrint('🔵 Data channel mesajı alındı: ${message.text}');
      };
      
      // Offer oluştur
      final offer = await _peerConnection!.createOffer({
        'offerToReceiveVideo': true,
        'offerToReceiveAudio': false,
      });
      
      await _peerConnection!.setLocalDescription(offer);
      debugPrint('✅ SDP offer oluşturuldu: ${offer.sdp}');
      
      return offer.sdp;
    } catch (e) {
      debugPrint('❌ SDP offer oluşturma hatası: $e');
      return null;
    }
  }
  
  /// SDP answer'ı işler
  Future<bool> setRemoteAnswer(String sdp) async {
    try {
      if (_peerConnection == null) {
        debugPrint('⚠️ Peer connection null');
        return false;
      }
      
      debugPrint('🔵 SDP answer ayarlanıyor...');
      
      final answer = RTCSessionDescription(sdp, 'answer');
      await _peerConnection!.setRemoteDescription(answer);
      
      debugPrint('✅ SDP answer ayarlandı');
      return true;
    } catch (e) {
      debugPrint('❌ SDP answer ayarlama hatası: $e');
      return false;
    }
  }
  
  /// ICE candidate ekler
  Future<void> addIceCandidate(RTCIceCandidate candidate) async {
    try {
      if (_peerConnection == null) {
        debugPrint('⚠️ Peer connection null');
        return;
      }
      
      await _peerConnection!.addCandidate(candidate);
      debugPrint('✅ ICE candidate eklendi: ${candidate.candidate}');
    } catch (e) {
      debugPrint('❌ ICE candidate ekleme hatası: $e');
    }
  }
  
  /// ICE candidate'ı WebRTCSignalingMessage'a çevirir
  WebRTCSignalingMessage iceCandidateToMessage(RTCIceCandidate candidate) {
    return WebRTCSignalingMessage(
      type: 'ice-candidate',
      targetDeviceId: _targetDeviceId,
      senderDeviceId: _deviceId,
      connectionId: _connectionId,
      iceCandidate: IceCandidate(
        candidate: candidate.candidate ?? '',
        sdpMLineIndex: candidate.sdpMLineIndex ?? 0,
        sdpMid: candidate.sdpMid,
      ),
    );
  }
  
  /// Remote renderer'ı döner
  RTCVideoRenderer? get remoteRenderer => _remoteRenderer;
  
  /// Bağlantıyı kapatır
  Future<void> close() async {
    try {
      await _remoteStream?.dispose();
      await _remoteRenderer?.dispose();
      await _peerConnection?.close();
      _peerConnection = null;
      _remoteStream = null;
      _remoteRenderer = null;
      debugPrint('🔵 WebRTC bağlantısı kapatıldı');
    } catch (e) {
      debugPrint('❌ WebRTC close hatası: $e');
    }
  }
  
  /// Dispose
  void dispose() {
    close();
    _onRemoteStream.close();
    _onIceCandidate.close();
    _onConnectionStateChange.close();
  }
}






