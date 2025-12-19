# SoftielRemote App (Flutter Desktop)

Cross-platform desktop application for SoftielRemote - Windows & macOS support.

## Getting Started

### Prerequisites

- Flutter SDK 3.0+ with desktop support
- Dart SDK 3.0+

### Installation

1. Install Flutter with desktop support:
```bash
flutter config --enable-windows-desktop
flutter config --enable-macos-desktop
```

2. Get dependencies:
```bash
cd src/SoftielRemote.App.Flutter
flutter pub get
```

3. Run the app:
```bash
# Windows
flutter run -d windows

# macOS
flutter run -d macos
```

## Project Structure

```
lib/
├── main.dart                 # App entry point
├── screens/                  # Screen widgets
│   └── home_screen.dart
├── widgets/                  # Reusable UI components
│   ├── custom_title_bar.dart
│   ├── tab_bar_widget.dart
│   ├── remote_address_bar.dart
│   ├── device_id_section.dart
│   └── content_sections_widget.dart
├── services/                 # Business logic services
│   ├── backend_client_service.dart
│   ├── signalr_service.dart
│   └── webrtc_service.dart
├── providers/                # Riverpod state management
│   └── app_state_provider.dart
├── models/                   # Data models
│   ├── device_info.dart
│   ├── connection_info.dart
│   └── connection_status.dart
└── utils/                     # Utilities
    └── app_theme.dart
```

## Features

- ✅ Modern UI/UX design
- ✅ Browser-style tabs
- ✅ Cross-platform (Windows & macOS)
- ✅ State management (Riverpod)
- 🔄 Backend integration (in progress)
- 🔄 WebRTC integration (in progress)

## Dependencies

- `flutter_riverpod` - State management
- `flutter_webrtc` - WebRTC support
- `http` - REST API calls
- `web_socket_channel` - SignalR/WebSocket
- `window_manager` - Window management
- `system_tray` - System tray integration

