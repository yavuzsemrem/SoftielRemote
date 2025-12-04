using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SoftielRemote.Core.Messages;

namespace SoftielRemote.Agent.InputInjection;

/// <summary>
/// Windows SendInput API kullanarak input injection servisi (Production-ready).
/// </summary>
public class WindowsInputInjectionService : IInputInjectionService
{
    private readonly ILogger<WindowsInputInjectionService> _logger;
    private bool _isEnabled = false;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            _logger.LogInformation("Input injection {Status}", value ? "aktif" : "devre dışı");
        }
    }

    public WindowsInputInjectionService(ILogger<WindowsInputInjectionService> logger)
    {
        _logger = logger;
    }

    public Task<bool> InjectInputAsync(RemoteInputMessage inputMessage)
    {
        if (!_isEnabled)
        {
            _logger.LogWarning("Input injection devre dışı, mesaj yok sayıldı");
            return Task.FromResult(false);
        }

        try
        {
            switch (inputMessage.Type)
            {
                case Core.Messages.InputType.MouseMove:
                    InjectMouseMove(inputMessage.MouseX ?? 0, inputMessage.MouseY ?? 0);
                    break;

                case Core.Messages.InputType.MouseClick:
                    if (inputMessage.MouseButtonState == Core.Messages.MouseButtonState.Down)
                    {
                        InjectMouseButton(GetButtonString(inputMessage.MouseButton ?? Core.Messages.MouseButton.Left), true);
                    }
                    else if (inputMessage.MouseButtonState == Core.Messages.MouseButtonState.Up)
                    {
                        InjectMouseButton(GetButtonString(inputMessage.MouseButton ?? Core.Messages.MouseButton.Left), false);
                    }
                    break;

                case Core.Messages.InputType.MouseWheel:
                    // WheelDelta için RemoteInputMessage'da property yok, şimdilik 0
                    InjectMouseWheel(0);
                    break;

                case Core.Messages.InputType.KeyPress:
                    if (inputMessage.KeyState == Core.Messages.KeyState.Down)
                    {
                        InjectKey(GetKeyString(inputMessage.KeyCode ?? 0), true);
                    }
                    else if (inputMessage.KeyState == Core.Messages.KeyState.Up)
                    {
                        InjectKey(GetKeyString(inputMessage.KeyCode ?? 0), false);
                    }
                    break;

                default:
                    _logger.LogWarning("Bilinmeyen input tipi: {Type}", inputMessage.Type);
                    return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input injection hatası: {Type}", inputMessage.Type);
            return Task.FromResult(false);
        }
    }

    private void InjectMouseMove(int x, int y)
    {
        var input = new INPUT
        {
            type = INPUT_TYPE.MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = x,
                    dy = y,
                    dwFlags = MOUSEEVENTF.MOVE | MOUSEEVENTF.ABSOLUTE,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }

    private void InjectMouseButton(string button, bool isDown)
    {
        MOUSEEVENTF flags = 0;
        if (button.Equals("left", StringComparison.OrdinalIgnoreCase))
        {
            flags = isDown ? MOUSEEVENTF.LEFTDOWN : MOUSEEVENTF.LEFTUP;
        }
        else if (button.Equals("right", StringComparison.OrdinalIgnoreCase))
        {
            flags = isDown ? MOUSEEVENTF.RIGHTDOWN : MOUSEEVENTF.RIGHTUP;
        }
        else if (button.Equals("middle", StringComparison.OrdinalIgnoreCase))
        {
            flags = isDown ? MOUSEEVENTF.MIDDLEDOWN : MOUSEEVENTF.MIDDLEUP;
        }

        if (flags != 0)
        {
            var input = new INPUT
            {
                type = INPUT_TYPE.MOUSE,
                u = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dwFlags = flags,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }

    private void InjectMouseWheel(int delta)
    {
        var input = new INPUT
        {
            type = INPUT_TYPE.MOUSE,
            u = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dwFlags = MOUSEEVENTF.WHEEL,
                    mouseData = delta,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }

    private void InjectKey(string key, bool isDown)
    {
        // Virtual key code'u al (basit mapping)
        ushort vkCode = GetVirtualKeyCode(key);
        if (vkCode == 0)
        {
            _logger.LogWarning("Bilinmeyen tuş: {Key}", key);
            return;
        }

        var input = new INPUT
        {
            type = INPUT_TYPE.KEYBOARD,
            u = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vkCode,
                    wScan = 0,
                    dwFlags = isDown ? 0 : KEYEVENTF.KEYUP,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

        SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
    }

    private string GetButtonString(Core.Messages.MouseButton button)
    {
        return button switch
        {
            Core.Messages.MouseButton.Left => "left",
            Core.Messages.MouseButton.Right => "right",
            Core.Messages.MouseButton.Middle => "middle",
            _ => "left"
        };
    }

    private string GetKeyString(int keyCode)
    {
        // Virtual key code'u string'e çevir (basit mapping)
        return keyCode switch
        {
            0x0D => "ENTER",
            0x1B => "ESCAPE",
            0x20 => "SPACE",
            0x09 => "TAB",
            0x08 => "BACKSPACE",
            0x2E => "DELETE",
            0x26 => "ARROWUP",
            0x28 => "ARROWDOWN",
            0x25 => "ARROWLEFT",
            0x27 => "ARROWRIGHT",
            0x11 => "CTRL",
            0x12 => "ALT",
            0x10 => "SHIFT",
            0x5B => "WIN",
            _ => keyCode > 0 && keyCode < 256 ? ((char)keyCode).ToString() : string.Empty
        };
    }

    private ushort GetVirtualKeyCode(string key)
    {
        // Basit key mapping (ileride genişletilebilir)
        return key.ToUpper() switch
        {
            "ENTER" => 0x0D,
            "ESCAPE" => 0x1B,
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "BACKSPACE" => 0x08,
            "DELETE" => 0x2E,
            "ARROWUP" => 0x26,
            "ARROWDOWN" => 0x28,
            "ARROWLEFT" => 0x25,
            "ARROWRIGHT" => 0x27,
            "CTRL" => 0x11,
            "ALT" => 0x12,
            "SHIFT" => 0x10,
            "WIN" => 0x5B,
            _ => key.Length == 1 ? (ushort)key[0] : (ushort)0
        };
    }

    #region Win32 API Declarations

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const int INPUT_MOUSE = 0;
    private const int INPUT_KEYBOARD = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public INPUT_TYPE type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public MOUSEEVENTF dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public KEYEVENTF dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private enum INPUT_TYPE : uint
    {
        MOUSE = INPUT_MOUSE,
        KEYBOARD = INPUT_KEYBOARD
    }

    [Flags]
    private enum MOUSEEVENTF : uint
    {
        MOVE = 0x0001,
        LEFTDOWN = 0x0002,
        LEFTUP = 0x0004,
        RIGHTDOWN = 0x0008,
        RIGHTUP = 0x0010,
        MIDDLEDOWN = 0x0020,
        MIDDLEUP = 0x0040,
        WHEEL = 0x0800,
        ABSOLUTE = 0x8000
    }

    [Flags]
    private enum KEYEVENTF : uint
    {
        KEYUP = 0x0002
    }

    #endregion
}

