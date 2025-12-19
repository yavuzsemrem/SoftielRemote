namespace SoftielRemote.Core.Messages;

/// <summary>
/// Controller'dan Agent'a gönderilen input (mouse/klavye) mesajı.
/// </summary>
public class RemoteInputMessage
{
    /// <summary>
    /// Input tipi (MouseMove, MouseClick, KeyPress vb.).
    /// </summary>
    public InputType Type { get; set; }

    /// <summary>
    /// Mouse X koordinatı (Type = MouseMove veya MouseClick ise).
    /// </summary>
    public int? MouseX { get; set; }

    /// <summary>
    /// Mouse Y koordinatı (Type = MouseMove veya MouseClick ise).
    /// </summary>
    public int? MouseY { get; set; }

    /// <summary>
    /// Mouse butonu (Left, Right, Middle) - Type = MouseClick ise.
    /// </summary>
    public MouseButton? MouseButton { get; set; }

    /// <summary>
    /// Mouse buton durumu (Down, Up) - Type = MouseClick ise.
    /// </summary>
    public MouseButtonState? MouseButtonState { get; set; }

    /// <summary>
    /// Klavye tuş kodu (Type = KeyPress ise).
    /// </summary>
    public int? KeyCode { get; set; }

    /// <summary>
    /// Klavye tuş durumu (Down, Up) - Type = KeyPress ise.
    /// </summary>
    public KeyState? KeyState { get; set; }

    /// <summary>
    /// Mesaj zaman damgası (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Input mesaj tipi.
/// </summary>
public enum InputType
{
    MouseMove = 0,
    MouseClick = 1,
    MouseWheel = 2,
    KeyPress = 3
}

/// <summary>
/// Mouse butonu.
/// </summary>
public enum MouseButton
{
    Left = 0,
    Right = 1,
    Middle = 2
}

/// <summary>
/// Mouse buton durumu.
/// </summary>
public enum MouseButtonState
{
    Down = 0,
    Up = 1
}

/// <summary>
/// Klavye tuş durumu.
/// </summary>
public enum KeyState
{
    Down = 0,
    Up = 1
}

