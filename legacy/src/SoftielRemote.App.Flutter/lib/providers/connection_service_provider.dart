import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../services/connection_service.dart';
import '../services/backend_api_service.dart';

/// Backend API service provider
final backendApiServiceProvider = Provider<BackendApiService>((ref) {
  // Lazy initialization - ilk API çağrısında otomatik olarak initialize edilecek
  return BackendApiService();
});

/// Connection service provider
final connectionServiceProvider = Provider<ConnectionService>((ref) {
  final backendApi = ref.watch(backendApiServiceProvider);
  return ConnectionService(backendApi);
});

