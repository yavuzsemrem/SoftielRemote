import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Session card data model
class SessionCardData {
  final String deviceId;
  final String deviceName;
  final bool isOnline;
  final String lastConnected;
  final int backgroundType;

  SessionCardData({
    required this.deviceId,
    required this.deviceName,
    required this.isOnline,
    required this.lastConnected,
    this.backgroundType = 0,
  });

  SessionCardData copyWith({
    String? deviceId,
    String? deviceName,
    bool? isOnline,
    String? lastConnected,
    int? backgroundType,
  }) {
    return SessionCardData(
      deviceId: deviceId ?? this.deviceId,
      deviceName: deviceName ?? this.deviceName,
      isOnline: isOnline ?? this.isOnline,
      lastConnected: lastConnected ?? this.lastConnected,
      backgroundType: backgroundType ?? this.backgroundType,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'deviceId': deviceId,
      'deviceName': deviceName,
      'isOnline': isOnline,
      'lastConnected': lastConnected,
      'backgroundType': backgroundType,
    };
  }

  factory SessionCardData.fromJson(Map<String, dynamic> json) {
    return SessionCardData(
      deviceId: json['deviceId'] as String,
      deviceName: json['deviceName'] as String,
      isOnline: json['isOnline'] as bool,
      lastConnected: json['lastConnected'] as String,
      backgroundType: json['backgroundType'] as int? ?? 0,
    );
  }
}

/// Favorites state
class FavoritesState {
  final List<SessionCardData> favorites;

  FavoritesState({required this.favorites});

  FavoritesState copyWith({List<SessionCardData>? favorites}) {
    return FavoritesState(
      favorites: favorites ?? this.favorites,
    );
  }

  bool isFavorite(String deviceId) {
    return favorites.any((f) => f.deviceId == deviceId);
  }
}

/// Favorites provider
final favoritesProvider = StateNotifierProvider<FavoritesNotifier, FavoritesState>(
  (ref) => FavoritesNotifier(),
);

class FavoritesNotifier extends StateNotifier<FavoritesState> {
  static const String _favoritesKey = 'favorites_list';

  FavoritesNotifier() : super(FavoritesState(favorites: [])) {
    _loadFavorites();
  }

  Future<void> _loadFavorites() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = prefs.getString(_favoritesKey);
      
      if (favoritesJson != null) {
        final List<dynamic> favoritesList = json.decode(favoritesJson);
        final favorites = favoritesList
            .map((json) => SessionCardData.fromJson(json as Map<String, dynamic>))
            .toList();
        
        state = FavoritesState(favorites: favorites);
      }
    } catch (e) {
      // Hata durumunda boş liste ile devam et
      state = FavoritesState(favorites: []);
    }
  }

  Future<void> _saveFavorites() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final favoritesJson = json.encode(
        state.favorites.map((f) => f.toJson()).toList(),
      );
      await prefs.setString(_favoritesKey, favoritesJson);
    } catch (e) {
      // Hata durumunda sessizce devam et
    }
  }

  void addFavorite(SessionCardData session) {
    if (!state.favorites.any((f) => f.deviceId == session.deviceId)) {
      state = state.copyWith(
        favorites: [...state.favorites, session],
      );
      _saveFavorites();
    }
  }

  void removeFavorite(String deviceId) {
    state = state.copyWith(
      favorites: state.favorites.where((f) => f.deviceId != deviceId).toList(),
    );
    _saveFavorites();
  }

  bool isFavorite(String deviceId) {
    return state.favorites.any((f) => f.deviceId == deviceId);
  }
}

/// Custom names state - Device ID'ye göre özel isimleri saklar
class CustomNamesState {
  final Map<String, String> customNames; // deviceId -> customName

  CustomNamesState({Map<String, String>? customNames})
      : customNames = customNames ?? {};

  CustomNamesState copyWith({Map<String, String>? customNames}) {
    return CustomNamesState(
      customNames: customNames ?? this.customNames,
    );
  }

  String? getCustomName(String deviceId) {
    return customNames[deviceId];
  }
}

/// Custom names provider
final customNamesProvider = StateNotifierProvider<CustomNamesNotifier, CustomNamesState>(
  (ref) => CustomNamesNotifier(),
);

class CustomNamesNotifier extends StateNotifier<CustomNamesState> {
  static const String _customNamesKey = 'custom_names_map';

  CustomNamesNotifier() : super(CustomNamesState()) {
    _loadCustomNames();
  }

  Future<void> _loadCustomNames() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final customNamesJson = prefs.getString(_customNamesKey);
      
      if (customNamesJson != null) {
        final Map<String, dynamic> customNamesMap = json.decode(customNamesJson);
        final customNames = customNamesMap.map(
          (key, value) => MapEntry(key, value as String),
        );
        
        state = CustomNamesState(customNames: customNames);
      }
    } catch (e) {
      // Hata durumunda boş map ile devam et
      state = CustomNamesState();
    }
  }

  Future<void> _saveCustomNames() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final customNamesJson = json.encode(state.customNames);
      await prefs.setString(_customNamesKey, customNamesJson);
    } catch (e) {
      // Hata durumunda sessizce devam et
    }
  }

  void setCustomName(String deviceId, String customName) {
    final newNames = Map<String, String>.from(state.customNames);
    if (customName.trim().isEmpty) {
      newNames.remove(deviceId);
    } else {
      newNames[deviceId] = customName.trim();
    }
    state = state.copyWith(customNames: newNames);
    _saveCustomNames();
  }

  void removeCustomName(String deviceId) {
    final newNames = Map<String, String>.from(state.customNames);
    newNames.remove(deviceId);
    state = state.copyWith(customNames: newNames);
    _saveCustomNames();
  }

  String? getCustomName(String deviceId) {
    return state.getCustomName(deviceId);
  }
}

/// Global view mode provider for all sections
/// 1 = List view (single column)
/// 2 = Grid view (2 columns - compact)
/// 3 = Grid view (3+ columns - wide)
final globalViewModeProvider = StateProvider<int>((ref) => 3);

