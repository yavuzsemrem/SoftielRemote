import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter/foundation.dart';
import '../models/connection_request.dart';
import '../models/connection_response.dart';
import '../models/connection_status.dart';

/// Backend API client service
/// Connection request gönderme ve diğer API işlemleri için
class BackendApiService {
  static const String _defaultBackendUrl = 'http://localhost:5000';
  
  String _backendUrl = _defaultBackendUrl;
  bool _isInitialized = false;
  Future<void>? _initializeFuture;
  
  String get backendUrl => _backendUrl;
  
  /// Backend URL'ini ayarlar
  void setBackendUrl(String url) {
    _backendUrl = url.trim().replaceAll(RegExp(r'/$'), '');
    debugPrint('🔵 Backend URL ayarlandı: $_backendUrl');
  }
  
  /// Backend URL'ini environment variable'dan veya varsayılan değerden alır
  /// Lazy initialization - ilk çağrıldığında initialize eder
  Future<void> initialize() async {
    if (_isInitialized) {
      return;
    }
    
    // Eğer zaten initialize ediliyorsa, o future'ı bekle
    if (_initializeFuture != null) {
      await _initializeFuture;
      return;
    }
    
    _initializeFuture = _doInitialize();
    await _initializeFuture;
  }
  
  Future<void> _doInitialize() async {
    // 1. Environment variable'dan oku (eğer varsa) - en yüksek öncelik
    const envBackendUrl = String.fromEnvironment('SOFTIELREMOTE_BACKEND_URL');
    if (envBackendUrl.isNotEmpty) {
      if (await _tryBackendUrl(envBackendUrl)) {
        _backendUrl = envBackendUrl.trim().replaceAll(RegExp(r'/$'), '');
        _isInitialized = true;
        debugPrint('🔵 Backend URL environment variable\'dan alındı: $_backendUrl');
        return;
      }
    }
    
    // 2. Supabase REST API'den aktif Backend URL'lerini çek (otomatik discovery)
    // Önce environment variable'dan oku, yoksa hardcode değerleri kullan
    var supabaseProjectUrl = const String.fromEnvironment('SOFTIELREMOTE_SUPABASE_PROJECT_URL');
    var supabaseAnonKey = const String.fromEnvironment('SOFTIELREMOTE_SUPABASE_ANON_KEY');
    
    // Environment variable yoksa, hardcode değerleri kullan (production için)
    if (supabaseProjectUrl.isEmpty) {
      supabaseProjectUrl = 'https://yfyfeymjqcmrontajwco.supabase.co';
    }
    if (supabaseAnonKey.isEmpty) {
      // Production için hardcode Supabase Anon Key (Agent ile aynı)
      supabaseAnonKey = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InlmeWZleW1qcWNtcm9udGFqd2NvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ3NjI4ODYsImV4cCI6MjA4MDMzODg4Nn0.M72mLMJCPfxqgwse3ZPpZIoaxbl_nv13WBJ3YgK0eaE';
    }
    
    if (supabaseProjectUrl.isNotEmpty && supabaseAnonKey.isNotEmpty) {
      try {
        final apiUrl = Uri.parse('$supabaseProjectUrl/rest/v1/BackendRegistry?IsActive=eq.true&LastSeen=gte.${DateTime.now().subtract(const Duration(minutes: 5)).toIso8601String()}&select=PublicUrl,LocalIp&order=LastSeen.desc');
        
        final response = await http
            .get(apiUrl, headers: {
              'apikey': supabaseAnonKey,
              'Authorization': 'Bearer $supabaseAnonKey',
            })
            .timeout(const Duration(seconds: 10));
        
        if (response.statusCode == 200) {
          final backendData = (jsonDecode(response.body) as List)
              .cast<Map<String, dynamic>>();
          
          // PublicUrl'leri önce dene
          for (final backend in backendData) {
            final publicUrl = backend['PublicUrl']?.toString();
            if (publicUrl != null && publicUrl.isNotEmpty) {
              if (await _tryBackendUrl(publicUrl)) {
                _backendUrl = publicUrl.trim().replaceAll(RegExp(r'/$'), '');
                _isInitialized = true;
                debugPrint('🔵 Backend URL Supabase\'den bulundu: $_backendUrl');
                return;
              }
            }
          }
        }
      } catch (e) {
        debugPrint('⚠️ Supabase\'den Backend listesi alınamadı: $e');
      }
    }
    
    // 3. Discovery URL'lerini dene (merkezi discovery servisi)
    const discoveryUrl = String.fromEnvironment('SOFTIELREMOTE_DISCOVERY_URL');
    if (discoveryUrl.isNotEmpty) {
      try {
        final response = await http
            .get(Uri.parse('$discoveryUrl/api/backendregistry/active'))
            .timeout(const Duration(seconds: 5));
        
        if (response.statusCode == 200) {
          final backendUrls = (jsonDecode(response.body) as List)
              .map((e) => e.toString())
              .where((url) => url.isNotEmpty)
              .toList();
          
          for (final url in backendUrls) {
            if (await _tryBackendUrl(url)) {
              _backendUrl = url;
              _isInitialized = true;
              debugPrint('🔵 Backend URL discovery servisinden bulundu: $_backendUrl');
              return;
            }
          }
        }
      } catch (e) {
        debugPrint('⚠️ Discovery URL\'den Backend listesi alınamadı: $e');
      }
    }
    
    _isInitialized = true;
    debugPrint('⚠️ Backend URL bulunamadı. Varsayılan Backend URL kullanılıyor: $_backendUrl (muhtemelen çalışmayacak)');
  }
  
  /// İlk API çağrısından önce initialize edilmesini sağlar
  Future<void> _ensureInitialized() async {
    if (!_isInitialized) {
      await initialize();
    }
  }
  
  /// Backend URL'inin çalışıp çalışmadığını kontrol eder
  Future<bool> _tryBackendUrl(String url) async {
    try {
      // Health endpoint'ini dene
      final healthResponse = await http
          .get(Uri.parse('$url/health'))
          .timeout(const Duration(seconds: 2));
      
      if (healthResponse.statusCode == 200) {
        return true;
      }
      
      // Health endpoint yoksa, agents endpoint'ini dene
      final agentsResponse = await http
          .get(Uri.parse('$url/api/agents'))
          .timeout(const Duration(seconds: 2));
      
      return agentsResponse.statusCode == 200 || agentsResponse.statusCode == 401 || agentsResponse.statusCode == 403;
    } catch (e) {
      return false;
    }
  }
  
  /// Connection request gönderir
  /// POST /api/connections/request
  Future<ConnectionResponse> requestConnection(ConnectionRequest request) async {
    try {
      // İlk kullanımda initialize et
      await _ensureInitialized();
      
      final url = Uri.parse('$_backendUrl/api/connections/request');
      
      debugPrint('🔵 Connection request gönderiliyor: ${request.targetDeviceId}');
      
      final response = await http.post(
        url,
        headers: {
          'Content-Type': 'application/json',
        },
        body: jsonEncode(request.toJson()),
      ).timeout(const Duration(seconds: 10));
      
      if (response.statusCode == 200) {
        final json = jsonDecode(response.body) as Map<String, dynamic>;
        debugPrint('🔵 Connection response JSON: $json');
        final connectionResponse = ConnectionResponse.fromJson(json);
        debugPrint('✅ Connection request başarılı: success=${connectionResponse.success}, status=${connectionResponse.status}, connectionId=${connectionResponse.connectionId}, agentEndpoint=${connectionResponse.agentEndpoint}, errorMessage=${connectionResponse.errorMessage}');
        return connectionResponse;
      } else if (response.statusCode == 429) {
        debugPrint('⚠️ Rate limit aşıldı');
        return ConnectionResponse(
          success: false,
          status: ConnectionStatus.error,
          errorMessage: 'Rate limit aşıldı. Lütfen birkaç saniye sonra tekrar deneyin.',
        );
      } else {
        final errorBody = response.body;
        debugPrint('❌ Connection request hatası: ${response.statusCode} - $errorBody');
        return ConnectionResponse(
          success: false,
          status: ConnectionStatus.error,
          errorMessage: 'Bağlantı isteği gönderilemedi: ${response.statusCode}',
        );
      }
    } catch (e) {
      debugPrint('❌ Connection request exception: $e');
      return ConnectionResponse(
        success: false,
        status: ConnectionStatus.error,
        errorMessage: 'Bağlantı hatası: ${e.toString()}',
      );
    }
  }
  
  /// Health check endpoint'i
  Future<bool> checkHealth() async {
    try {
      // İlk kullanımda initialize et
      await _ensureInitialized();
      
      final response = await http
          .get(Uri.parse('$_backendUrl/health'))
          .timeout(const Duration(seconds: 2));
      return response.statusCode == 200;
    } catch (e) {
      return false;
    }
  }
}

