import 'package:flutter/material.dart';
import '../utils/app_theme.dart';
import 'login_dialog.dart';
import 'signup_dialog.dart';

/// Profile dropdown menu widget
class ProfileDropdown extends StatelessWidget {
  final GlobalKey buttonKey;

  const ProfileDropdown({
    super.key,
    required this.buttonKey,
  });

  void showDropdown(BuildContext context) {
    final RenderBox? button = buttonKey.currentContext?.findRenderObject() as RenderBox?;
    if (button == null) return;
    
    final RenderBox overlay = Overlay.of(context).context.findRenderObject() as RenderBox;
    final Offset buttonPosition = button.localToGlobal(Offset.zero, ancestor: overlay);
    
    const double menuWidth = 200.0;
    const double menuItemHeight = 44.0;
    const int menuItemCount = 2;
    final double menuHeight = menuItemHeight * menuItemCount;
    
    // Menüyü butonun altında aç
    double menuLeft = buttonPosition.dx - menuWidth + button.size.width;
    double menuTop = buttonPosition.dy + button.size.height + 4;
    
    // Sağdan taşmasını önle
    if (menuLeft + menuWidth > overlay.size.width) {
      menuLeft = overlay.size.width - menuWidth - 8;
    }
    
    // Soldan taşmasını önle
    if (menuLeft < 8) {
      menuLeft = 8;
    }
    
    // Alttan taşmasını önle - yukarı aç
    if (menuTop + menuHeight > overlay.size.height) {
      menuTop = buttonPosition.dy - menuHeight - 4;
    }

    showGeneralDialog<String>(
      context: context,
      barrierDismissible: true,
      barrierLabel: 'Profil Menüsü',
      barrierColor: Colors.transparent,
      transitionDuration: const Duration(milliseconds: 150),
      pageBuilder: (context, animation, secondaryAnimation) {
        return FadeTransition(
          opacity: animation,
          child: Stack(
            children: [
              // Tıklanabilir arka plan - dropdown'ı kapatmak için
              Positioned.fill(
                child: GestureDetector(
                  onTap: () => Navigator.pop(context),
                  child: Container(color: Colors.transparent),
                ),
              ),
              // Dropdown menü
              Positioned(
                left: menuLeft,
                top: menuTop,
                child: Material(
                  color: Colors.transparent,
                  elevation: 8,
                  child: Container(
                    width: menuWidth,
                    decoration: BoxDecoration(
                      color: AppTheme.surfaceMedium,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(
                        color: AppTheme.surfaceLight.withOpacity(0.3),
                        width: 1,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withOpacity(0.4),
                          blurRadius: 16,
                          spreadRadius: 0,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        _buildMenuItem(
                          icon: Icons.login_rounded,
                          text: 'Giriş Yap',
                          onTap: () {
                            Navigator.pop(context);
                            _showLoginDialog(context);
                          },
                        ),
                        _buildMenuItem(
                          icon: Icons.person_add_rounded,
                          text: 'Kayıt Ol',
                          onTap: () {
                            Navigator.pop(context);
                            _showSignupDialog(context);
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
      },
    );
  }

  Widget _buildMenuItem({
    required IconData icon,
    required String text,
    required VoidCallback onTap,
  }) {
    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: AppTheme.surfaceLight.withOpacity(0.3),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(
                  icon,
                  size: 18,
                  color: AppTheme.primaryBlue,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  text,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 14,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
              Icon(
                Icons.chevron_right_rounded,
                size: 18,
                color: AppTheme.textTertiary,
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showLoginDialog(BuildContext context) {
    showDialog(
      context: context,
      barrierColor: Colors.black.withOpacity(0.7),
      builder: (context) => LoginDialog(),
    );
  }

  void _showSignupDialog(BuildContext context) {
    showDialog(
      context: context,
      barrierColor: Colors.black.withOpacity(0.7),
      builder: (context) => SignupDialog(),
    );
  }

  @override
  Widget build(BuildContext context) {
    return const SizedBox.shrink();
  }
}

