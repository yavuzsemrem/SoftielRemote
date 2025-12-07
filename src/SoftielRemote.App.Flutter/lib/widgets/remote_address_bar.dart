import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:window_manager/window_manager.dart';
import '../providers/app_state_provider.dart';
import '../models/connection_status.dart';
import '../utils/app_theme.dart';

/// Custom painter for 4-line hamburger menu icon
class _FourLineMenuPainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = AppTheme.textSecondary
      ..strokeWidth = 1.5
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final lineSpacing = size.height / 5;
    final lineWidth = size.width * 0.7;
    final startX = (size.width - lineWidth) / 2;

    // Draw 4 horizontal lines
    for (int i = 0; i < 4; i++) {
      final y = lineSpacing * (i + 1);
      canvas.drawLine(
        Offset(startX, y),
        Offset(startX + lineWidth, y),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(_FourLineMenuPainter oldDelegate) => false;
}

/// Remote address input bar (below title bar)
class RemoteAddressBar extends ConsumerStatefulWidget {
  const RemoteAddressBar({super.key});

  @override
  ConsumerState<RemoteAddressBar> createState() => _RemoteAddressBarState();
}

class _RemoteAddressBarState extends ConsumerState<RemoteAddressBar> {
  final TextEditingController _controller = TextEditingController();
  final FocusNode _focusNode = FocusNode();
  final GlobalKey _textFieldKey = GlobalKey();
  OverlayEntry? _overlayEntry;
  bool _isFocused = false;

  @override
  void initState() {
    super.initState();
    _focusNode.addListener(_onFocusChange);
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.removeListener(_onFocusChange);
    _focusNode.dispose();
    _removeOverlay();
    super.dispose();
  }

  void _onFocusChange() {
    if (_focusNode.hasFocus && !_isFocused) {
      _isFocused = true;
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && _focusNode.hasFocus) {
          _showShortcuts();
        }
      });
    } else if (!_focusNode.hasFocus && _isFocused) {
      _isFocused = false;
      _removeOverlay();
    }
  }

  void _showShortcuts() {
    _removeOverlay();
    
    final BuildContext? textFieldContext = _textFieldKey.currentContext;
    if (textFieldContext == null) return;
    
    final RenderBox? renderBox = textFieldContext.findRenderObject() as RenderBox?;
    if (renderBox == null || !renderBox.attached) return;

    final OverlayState? overlayState = Overlay.maybeOf(context);
    if (overlayState == null) return;

    final Size size = renderBox.size;
    final Offset offset = renderBox.localToGlobal(Offset.zero);

    _overlayEntry = OverlayEntry(
      maintainState: false,
      opaque: false,
      builder: (context) => Stack(
        children: [
          // Tıklanabilir arka plan - overlay'i kapatmak için
          Positioned.fill(
            child: GestureDetector(
              onTap: () {
                _focusNode.unfocus();
              },
              child: Container(color: Colors.transparent),
            ),
          ),
          // Dropdown menü
          Positioned(
            left: offset.dx,
            top: offset.dy + size.height + 4,
            width: size.width,
            child: Material(
              elevation: 8,
              color: Colors.transparent,
              child: Container(
                decoration: BoxDecoration(
                  color: AppTheme.surfaceMedium,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: AppTheme.surfaceLight.withOpacity(0.5),
                    width: 1,
                  ),
                ),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    _ShortcutItem(
                      icon: Icons.folder_outlined,
                      label: 'Dosya Transfer Et',
                      shortcut: _KeyboardShortcutBadge(
                        keys: ['Ctrl', 'F'],
                        label: '',
                      ),
                      onTap: () {
                        _handleMenuSelection('file_transfer');
                        _focusNode.unfocus();
                      },
                    ),
                    _ShortcutItem(
                      icon: Icons.videocam_outlined,
                      label: 'Ekran Kaydını Başlat',
                      shortcut: _KeyboardShortcutBadge(
                        keys: ['Ctrl', 'R'],
                        label: '',
                      ),
                      onTap: () {
                        _handleMenuSelection('session_recordings');
                        _focusNode.unfocus();
                      },
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );

    overlayState.insert(_overlayEntry!);
  }

  void _removeOverlay() {
    _overlayEntry?.remove();
    _overlayEntry = null;
  }

  void _handleConnect() {
    final deviceId = _controller.text.trim();
    if (deviceId.isNotEmpty) {
      ref.read(appStateProvider.notifier).setRemoteDeviceId(deviceId);
      // TODO: Trigger connection
    }
  }

  void _showMenu(BuildContext context) {
    final RenderBox? button = context.findRenderObject() as RenderBox?;
    if (button == null) return;
    
    final RenderBox overlay = Overlay.of(context).context.findRenderObject() as RenderBox;
    final Offset buttonPosition = button.localToGlobal(Offset.zero, ancestor: overlay);
    
    const double menuWidth = 220.0;
    const double menuItemHeight = 36.0;
    const int menuItemCount = 8;
    final double menuHeight = menuItemHeight * menuItemCount;
    
    // Menüyü butonun altında aç (üzerinde değil)
    double menuLeft = buttonPosition.dx;
    double menuTop = buttonPosition.dy + button.size.height + 4;
    
    // Sağdan taşmasını önle - sağa hizala
    if (menuLeft + menuWidth > overlay.size.width) {
      menuLeft = overlay.size.width - menuWidth - 8; // 8px margin
    }
    
    // Soldan taşmasını önle
    if (menuLeft < 8) {
      menuLeft = 8;
    }
    
    // Alttan taşmasını önle - yukarı aç
    if (menuTop + menuHeight > overlay.size.height) {
      menuTop = buttonPosition.dy - menuHeight - 4; // Butonun üstünde
    }

    showGeneralDialog<String>(
      context: context,
      barrierDismissible: true,
      barrierLabel: 'Menü',
      barrierColor: Colors.transparent,
      transitionDuration: Duration.zero, // Animasyon yok
      pageBuilder: (context, animation, secondaryAnimation) {
        return Stack(
          children: [
            Positioned(
              left: menuLeft,
              top: menuTop,
              child: Material(
                color: Colors.transparent,
                child: Container(
                  width: menuWidth,
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceMedium,
                    borderRadius: BorderRadius.circular(6),
                    border: Border.all(
                      color: AppTheme.surfaceLight.withOpacity(0.2),
                      width: 1,
                    ),
                    boxShadow: [
                      BoxShadow(
                        color: Colors.black.withOpacity(0.3),
                        blurRadius: 8,
                        spreadRadius: 0,
                        offset: const Offset(0, 2),
                      ),
                    ],
                  ),
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      _buildMenuItem(
                        icon: Icons.settings,
                        text: 'Ayarlar',
                        value: 'settings',
                        onTap: () => Navigator.pop(context, 'settings'),
                      ),
                      _buildMenuItem(
                        icon: Icons.lock,
                        text: 'Çalışma Alanı parolasını değiştir...',
                        value: 'change_password',
                        onTap: () => Navigator.pop(context, 'change_password'),
                      ),
                      _buildMenuItem(
                        icon: Icons.contacts,
                        text: 'Adres defteri',
                        value: 'address_book',
                        onTap: () => Navigator.pop(context, 'address_book'),
                      ),
                      _buildMenuItem(
                        icon: Icons.play_circle_outline,
                        text: 'Oturum kayıtları',
                        value: 'session_recordings',
                        onTap: () => Navigator.pop(context, 'session_recordings'),
                      ),
                      _buildMenuItem(
                        icon: Icons.vpn_key,
                        text: 'Lisans anahtarını değiştir...',
                        value: 'change_license',
                        onTap: () => Navigator.pop(context, 'change_license'),
                      ),
                      _buildMenuItem(
                        icon: Icons.help_outline,
                        text: 'Yardım',
                        value: 'help',
                        onTap: () => Navigator.pop(context, 'help'),
                      ),
                      _buildMenuItem(
                        icon: Icons.info_outline,
                        text: 'SoftielRemote Hakkında',
                        value: 'about',
                        onTap: () => Navigator.pop(context, 'about'),
                      ),
                      _buildMenuItem(
                        icon: Icons.close,
                        text: 'Sonlandır',
                        value: 'exit',
                        onTap: () => Navigator.pop(context, 'exit'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        );
      },
    ).then((value) {
      if (value != null) {
        _handleMenuSelection(value);
      }
    });
  }

  Widget _buildMenuItem({
    required IconData icon,
    required String text,
    required String value,
    required VoidCallback onTap,
  }) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Row(
            children: [
              Icon(icon, size: 16, color: AppTheme.textSecondary),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  text,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 13,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _handleMenuSelection(String value) {
    switch (value) {
      case 'settings':
        // TODO: Open settings
        break;
      case 'change_password':
        // TODO: Change workspace password
        break;
      case 'file_transfer':
        // TODO: Open file transfer section
        // Şimdilik sadece bir placeholder - ileride ContentSectionsWidget'teki section'ı değiştirecek
        break;
      case 'address_book':
        // TODO: Open address book
        break;
      case 'session_recordings':
        // TODO: Open session recordings
        break;
      case 'change_license':
        // TODO: Change license key
        break;
      case 'help':
        // TODO: Open help
        break;
      case 'about':
        // TODO: Show about dialog
        break;
      case 'exit':
        _exitApplication();
        break;
    }
  }

  Future<void> _exitApplication() async {
    if (!kIsWeb) {
      try {
        await windowManager.close();
      } catch (e) {
        // Fallback: Use SystemNavigator if window_manager fails
        // ignore: avoid_print
        debugPrint('Window manager close failed: $e');
      }
    }
    // For web or fallback, use SystemNavigator
    // SystemNavigator.pop();
  }

  @override
  Widget build(BuildContext context) {
    final connectionInfo = ref.watch(appStateProvider).connectionInfo;
    final isConnected = connectionInfo.status == ConnectionStatus.connected;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 0),
      decoration: BoxDecoration(
        color: AppTheme.backgroundDark,
        border: Border(
          bottom: BorderSide(
            color: AppTheme.surfaceLight.withOpacity(0.35),
            width: 1,
          ),
        ),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          // Status indicator (green checkmark)
          Container(
            width: 20,
            height: 20,
            decoration: const BoxDecoration(
              color: AppTheme.successGreen,
              shape: BoxShape.circle,
            ),
            child: const Icon(
              Icons.check,
              size: 12,
              color: Colors.white,
            ),
          ),
          const SizedBox(width: 15),
          
          // Remote address input with buttons inside (but hamburger menu outside)
          Flexible(
            flex: 4,
            child: Container(
              key: _textFieldKey,
              height: 40,
              decoration: BoxDecoration(
                color: AppTheme.surfaceMedium,
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                  color: AppTheme.surfaceLight,
                  width: 1,
                ),
              ),
              child: Shortcuts(
                shortcuts: {
                  // Ctrl+Enter veya Cmd+Enter (Bağlan)
                  const SingleActivator(LogicalKeyboardKey.enter, control: true): _ConnectIntent(),
                  const SingleActivator(LogicalKeyboardKey.enter, meta: true): _ConnectIntent(),
                  // Ctrl+F veya Cmd+F (Dosya Transfer Et)
                  const SingleActivator(LogicalKeyboardKey.keyF, control: true): _FileTransferIntent(),
                  const SingleActivator(LogicalKeyboardKey.keyF, meta: true): _FileTransferIntent(),
                  // Ctrl+R veya Cmd+R (Ekran Kaydını Başlat)
                  const SingleActivator(LogicalKeyboardKey.keyR, control: true): _RecordIntent(),
                  const SingleActivator(LogicalKeyboardKey.keyR, meta: true): _RecordIntent(),
                },
                child: Actions(
                  actions: {
                    _ConnectIntent: CallbackAction<_ConnectIntent>(
                      onInvoke: (_) => _handleConnect(),
                    ),
                    _FileTransferIntent: CallbackAction<_FileTransferIntent>(
                      onInvoke: (_) {
                        _handleMenuSelection('file_transfer');
                        return null;
                      },
                    ),
                    _RecordIntent: CallbackAction<_RecordIntent>(
                      onInvoke: (_) {
                        _handleMenuSelection('session_recordings');
                        return null;
                      },
                    ),
                  },
                  child: Focus(
                    onKeyEvent: (node, event) {
                      // Kısayolları handle et
                      if (event is KeyDownEvent) {
                        final isControl = event.logicalKey == LogicalKeyboardKey.controlLeft ||
                            event.logicalKey == LogicalKeyboardKey.controlRight;
                        final isMeta = event.logicalKey == LogicalKeyboardKey.metaLeft ||
                            event.logicalKey == LogicalKeyboardKey.metaRight;
                        
                        // Ctrl+V veya Cmd+V için Flutter otomatik yapıştırma yapar
                        // Diğer kısayollar Shortcuts widget'ı tarafından handle edilir
                        return KeyEventResult.ignored;
                      }
                      return KeyEventResult.ignored;
                    },
                    child: TextField(
                      controller: _controller,
                      focusNode: _focusNode,
                      enabled: !isConnected,
                      style: const TextStyle(
                        color: AppTheme.textPrimary,
                        fontSize: 14,
                      ),
                      decoration: InputDecoration(
                        hintText: 'Bağlantı Kodu Giriniz',
                        hintStyle: const TextStyle(
                          color: AppTheme.textTertiary,
                          fontSize: 14,
                        ),
                        border: InputBorder.none,
                        contentPadding: const EdgeInsets.symmetric(vertical: 12, horizontal: 12),
                        isDense: false,
                        suffixIconConstraints: const BoxConstraints(
                          minWidth: 72,
                          minHeight: 40,
                        ),
                        suffixIcon: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            // File transfer button
                            Material(
                              color: Colors.transparent,
                              child: InkWell(
                                onTap: () {},
                                borderRadius: BorderRadius.circular(6),
                                child: Container(
                                  width: 36,
                                  height: 36,
                                  alignment: Alignment.center,
                                  child: const Icon(
                                    Icons.upload_file,
                                    size: 20,
                                    color: AppTheme.textSecondary,
                                  ),
                                ),
                              ),
                            ),
                            
                            // Connect button
                            Material(
                              color: Colors.transparent,
                              child: InkWell(
                                onTap: isConnected ? null : _handleConnect,
                                borderRadius: BorderRadius.circular(6),
                                child: Container(
                                  width: 36,
                                  height: 36,
                                  alignment: Alignment.center,
                                  child: Icon(
                                    Icons.arrow_forward,
                                    size: 20,
                                    color: isConnected 
                                        ? AppTheme.textTertiary 
                                        : AppTheme.textSecondary,
                                  ),
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      onSubmitted: (_) => _handleConnect(),
                    ),
                  ),
                ),
              ),
            ),
          ),
          
          const SizedBox(width: 15),
          
          // Menu button (outside textbox) - 4-line hamburger icon with dropdown
          Builder(
            builder: (context) => IconButton(
              icon: CustomPaint(
                size: const Size(24, 24),
                painter: _FourLineMenuPainter(),
              ),
              color: AppTheme.textSecondary,
              onPressed: () => _showMenu(context),
              tooltip: 'Menü',
            ),
          ),
        ],
      ),
    );
  }
}

/// Intent class for connect action
class _ConnectIntent extends Intent {
  const _ConnectIntent();
}

/// Intent class for file transfer action
class _FileTransferIntent extends Intent {
  const _FileTransferIntent();
}

/// Intent class for record action
class _RecordIntent extends Intent {
  const _RecordIntent();
}

/// Shortcut item widget for the shortcuts overlay
class _ShortcutItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final Widget shortcut;
  final VoidCallback onTap;

  const _ShortcutItem({
    required this.icon,
    required this.label,
    required this.shortcut,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return MouseRegion(
      cursor: SystemMouseCursors.click,
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(8),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                Icon(
                  icon,
                  size: 20,
                  color: AppTheme.textSecondary,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    label,
                    style: const TextStyle(
                      color: AppTheme.textPrimary,
                      fontSize: 14,
                    ),
                  ),
                ),
                shortcut,
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// Keyboard shortcut badge widget
class _KeyboardShortcutBadge extends StatelessWidget {
  final List<String> keys;
  final String label;

  const _KeyboardShortcutBadge({
    required this.keys,
    required this.label,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        // Kısayol tuşları
        Row(
          mainAxisSize: MainAxisSize.min,
          children: keys.asMap().entries.map((entry) {
            final isLast = entry.key == keys.length - 1;
            return Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: AppTheme.surfaceLight.withOpacity(0.6),
                    borderRadius: BorderRadius.circular(3),
                    border: Border.all(
                      color: AppTheme.surfaceLight.withOpacity(0.8),
                      width: 1,
                    ),
                  ),
                  child: Text(
                    entry.value,
                    style: const TextStyle(
                      color: AppTheme.textSecondary,
                      fontSize: 11,
                      fontWeight: FontWeight.w500,
                      letterSpacing: 0.5,
                    ),
                  ),
                ),
                if (!isLast)
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 2),
                    child: Text(
                      '+',
                      style: TextStyle(
                        color: AppTheme.textTertiary,
                        fontSize: 10,
                        fontWeight: FontWeight.w400,
                      ),
                    ),
                  ),
              ],
            );
          }).toList(),
        ),
        if (label.isNotEmpty) ...[
          const SizedBox(width: 6),
          // Label
          Text(
            label,
            style: const TextStyle(
              color: AppTheme.textTertiary,
              fontSize: 11,
              fontWeight: FontWeight.w400,
            ),
          ),
        ],
      ],
    );
  }
}

