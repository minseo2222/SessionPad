using SessionPad.App.Models;

namespace SessionPad.App.Services;

/// <summary>
/// Abstracts the global-hotkey registration calls so the fallback decision logic in
/// <see cref="HotkeyRegistrationCoordinator"/> can be unit tested without a real window
/// handle or Win32 calls.
/// </summary>
public interface IHotkeyRegistrar
{
    bool Register(uint modifiers, uint virtualKey);

    void Unregister();

    int LastError { get; }
}

public enum HotkeyApplyOutcome
{
    /// <summary>The requested hotkey was registered and is now active.</summary>
    Applied,

    /// <summary>The requested hotkey failed; the previous hotkey was restored and is active.</summary>
    RevertedToPrevious,

    /// <summary>The requested hotkey failed and the previous hotkey could not be restored; nothing is active.</summary>
    NoHotkeyActive
}

public sealed record HotkeyApplyResult(HotkeyApplyOutcome Outcome, int Error)
{
    /// <summary>True when some hotkey (requested or previous) is currently registered.</summary>
    public bool HotkeyRegistered => Outcome != HotkeyApplyOutcome.NoHotkeyActive;
}

/// <summary>
/// Switches the global attach hotkey from a previous option to a requested one, with a
/// best-effort restore of the previous hotkey when the requested one cannot be
/// registered, so SessionPad stays usable.
/// </summary>
public sealed class HotkeyRegistrationCoordinator
{
    private readonly IHotkeyRegistrar _registrar;

    public HotkeyRegistrationCoordinator(IHotkeyRegistrar registrar)
    {
        _registrar = registrar;
    }

    public HotkeyApplyResult Apply(HotkeyOption requested, HotkeyOption previous, bool currentlyRegistered)
    {
        if (currentlyRegistered)
        {
            _registrar.Unregister();
        }

        if (_registrar.Register(requested.Modifiers, requested.VirtualKey))
        {
            return new HotkeyApplyResult(HotkeyApplyOutcome.Applied, 0);
        }

        var error = _registrar.LastError;

        // Best-effort: restore the previous hotkey so SessionPad stays usable.
        if (_registrar.Register(previous.Modifiers, previous.VirtualKey))
        {
            return new HotkeyApplyResult(HotkeyApplyOutcome.RevertedToPrevious, error);
        }

        return new HotkeyApplyResult(HotkeyApplyOutcome.NoHotkeyActive, error);
    }
}
