namespace SessionPad.App.Models;

/// <summary>
/// A selectable global hotkey combination. <see cref="Token"/> is the stable
/// string persisted in settings; <see cref="Modifiers"/> and <see cref="VirtualKey"/>
/// are the Win32 values passed to RegisterHotKey.
/// </summary>
public sealed record HotkeyOption(string Token, string Display, uint Modifiers, uint VirtualKey);
