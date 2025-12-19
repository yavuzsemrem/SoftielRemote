import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:signalr_netcore/signalr_client.dart';
import '../models/webrtc_signaling_message.dart';

/// SignalR client service
/// WebRTC signaling mesajlarını Backend üzerinden alır ve gönderir
class SignalRService {
  HubConnection? _connection;
  final String _backendUrl;
  final String _deviceId;
  
  // Event handlers
  final StreamController<WebRTCSignalingMessage> _onSignalingMessage = StreamController<WebRTCSignalingMessage>.broadcast();
  final StreamController<Map<String, dynamic>> _onConnectionResponse = StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<String> _onSignalingError = StreamController<String>.broadcast();
  
  Stream<WebRTCSignalingMessage> get onSignalingMessage => _onSignalingMessage.stream;
  Stream<Map<String, dynamic>> get onConnectionResponse => _onConnectionResponse.stream;
  Stream<String> get onSignalingError => _onSignalingError.stream;
  
  SignalRService({
    required String backendUrl,
    required String deviceId,
  })  : _backendUrl = backendUrl,
        _deviceId = deviceId;
  
  /// SignalR bağlantısını başlatır
  Future<bool> connect() async {
    try {
      if (_connection != null && _connection?.state == HubConnectionState.Connected) {
        debugPrint('🔵 SignalR zaten bağlı');
        return true;
      }
      
      final hubUrl = '${_backendUrl.replaceAll(RegExp(r'/$'), '')}/hubs/connection?deviceId=$_deviceId';
      debugPrint('🔵 SignalR bağlantısı kuruluyor: $hubUrl');
      
      _connection = HubConnectionBuilder()
          .withUrl(hubUrl)
          .withAutomaticReconnect()
          .build();
      
      // Event handlers
      _connection?.on('WebRTCSignaling', (List<Object?>? args) {
        if (args != null && args.isNotEmpty) {
          try {
            final messageJson = args[0] as Map<String, dynamic>;
            final message = WebRTCSignalingMessage.fromJson(messageJson);
            debugPrint('🔵 WebRTC signaling mesajı alındı: ${message.type}');
            _onSignalingMessage.add(message);
          } catch (e) {
            debugPrint('❌ WebRTC signaling mesajı parse edilemedi: $e');
          }
        }
      });
      
      _connection?.on('ConnectionResponse', (List<Object?>? args) {
        if (args != null && args.isNotEmpty) {
          try {
            final responseJson = args[0] as Map<String, dynamic>;
            debugPrint('🔵 Connection response alındı: $responseJson');
            _onConnectionResponse.add(responseJson);
          } catch (e) {
            debugPrint('❌ Connection response parse edilemedi: $e');
          }
        }
      });
      
      _connection?.on('SignalingError', (List<Object?>? args) {
        if (args != null && args.isNotEmpty) {
          final error = args[0] as String;
          debugPrint('❌ Signaling error: $error');
          _onSignalingError.add(error);
        }
      });
      
      // Connection state listeners
      _connection?.onclose(({Exception? error}) {
        debugPrint('⚠️ SignalR bağlantısı kapandı: ${error?.toString() ?? 'Normal'}');
      });
      
      await _connection?.start();
      debugPrint('✅ SignalR bağlantısı kuruldu');
      
      // Device ID'yi kaydet
      await _connection?.invoke('RegisterDevice', args: [_deviceId]);
      debugPrint('✅ Device ID kaydedildi: $_deviceId');
      
      return true;
    } catch (e) {
      debugPrint('❌ SignalR bağlantı hatası: $e');
      return false;
    }
  }
  
  /// SignalR bağlantısını kapatır
  Future<void> disconnect() async {
    try {
      if (_connection != null) {
        await _connection?.stop();
        debugPrint('🔵 SignalR bağlantısı kapatıldı');
      }
    } catch (e) {
      debugPrint('❌ SignalR disconnect hatası: $e');
    }
  }
  
  /// WebRTC signaling mesajı gönderir
  Future<void> sendWebRTCSignaling(WebRTCSignalingMessage message) async {
    try {
      if (_connection?.state != HubConnectionState.Connected) {
        debugPrint('⚠️ SignalR bağlı değil, mesaj gönderilemedi');
        return;
      }
      
      await _connection?.invoke('SendWebRTCSignaling', args: [message.toJson()]);
      debugPrint('🔵 WebRTC signaling mesajı gönderildi: ${message.type}');
    } catch (e) {
      debugPrint('❌ WebRTC signaling mesajı gönderilemedi: $e');
    }
  }
  
  /// Bağlantı durumunu kontrol eder
  bool get isConnected => _connection?.state == HubConnectionState.Connected;
  
  /// Dispose
  void dispose() {
    disconnect();
    _onSignalingMessage.close();
    _onConnectionResponse.close();
    _onSignalingError.close();
  }
}






