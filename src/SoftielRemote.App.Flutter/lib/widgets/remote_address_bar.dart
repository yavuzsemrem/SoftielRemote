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
                      icon: Icons.contacts_outlined,
                      label: 'Adres Defterini Aç',
                      shortcut: _KeyboardShortcutBadge(
                        keys: ['Ctrl', 'A'],
                        label: '',
                      ),
                      onTap: () {
                        _handleMenuSelection('address_book');
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
    final RelativeRect position = RelativeRect.fromRect(
      Rect.fromPoints(
        button.localToGlobal(Offset.zero, ancestor: overlay),
        button.localToGlobal(button.size.bottomRight(Offset.zero), ancestor: overlay),
      ),
      Offset.zero & overlay.size,
    );

    showMenu<String>(
      context: context,
      position: position,
      color: AppTheme.surfaceMedium,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(6),
        side: BorderSide(
          color: AppTheme.surfaceLight,
          width: 1,
        ),
      ),
      items: [
        PopupMenuItem<String>(
          value: 'settings',
          child: Row(
            children: [
              const Icon(Icons.settings, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Ayarlar',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'change_password',
          child: Row(
            children: [
              const Icon(Icons.lock, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Çalışma Alanı parolasını değiştir...',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'address_book',
          child: Row(
            children: [
              const Icon(Icons.contacts, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Adres defteri',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'session_recordings',
          child: Row(
            children: [
              const Icon(Icons.play_circle_outline, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Oturum kayıtları',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'change_license',
          child: Row(
            children: [
              const Icon(Icons.vpn_key, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Lisans anahtarını değiştir...',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'help',
          child: Row(
            children: [
              const Icon(Icons.help_outline, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Yardım',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'about',
          child: Row(
            children: [
              const Icon(Icons.info_outline, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'SoftielRemote Hakkında',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
        PopupMenuItem<String>(
          value: 'exit',
          child: Row(
            children: [
              const Icon(Icons.close, size: 20, color: AppTheme.textSecondary),
              const SizedBox(width: 12),
              const Text(
                'Sonlandır',
                style: TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 14,
                ),
              ),
            ],
          ),
        ),
      ],
    ).then((value) {
      if (value != null) {
        _handleMenuSelection(value);
      }
    });
  }

  void _handleMenuSelection(String value) {
    switch (value) {
      case 'settings':
        // TODO: Open settings
        break;
      case 'change_password':
        // TODO: Change workspace password
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
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 6),
      decoration: const BoxDecoration(
        color: AppTheme.backgroundDark,
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
                  // Ctrl+A veya Cmd+A (Adres Defterini Aç)
                  const SingleActivator(LogicalKeyboardKey.keyA, control: true): _AddressBookIntent(),
                  const SingleActivator(LogicalKeyboardKey.keyA, meta: true): _AddressBookIntent(),
                  // Ctrl+R veya Cmd+R (Ekran Kaydını Başlat)
                  const SingleActivator(LogicalKeyboardKey.keyR, control: true): _RecordIntent(),
                  const SingleActivator(LogicalKeyboardKey.keyR, meta: true): _RecordIntent(),
                },
                child: Actions(
                  actions: {
                    _ConnectIntent: CallbackAction<_ConnectIntent>(
                      onInvoke: (_) => _handleConnect(),
                    ),
                    _AddressBookIntent: CallbackAction<_AddressBookIntent>(
                      onInvoke: (_) {
                        _handleMenuSelection('address_book');
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
                      // Ctrl+V veya Cmd+V için Flutter otomatik yapıştırma yapar
                      // Burada özel bir işlem yapmaya gerek yok
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

/// Intent class for address book action
class _AddressBookIntent extends Intent {
  const _AddressBookIntent();
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

