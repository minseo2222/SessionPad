using SessionPad.App.Models;
using SessionPad.App.Services;

namespace SessionPad.Tests;

public class HotkeyRegistrationCoordinatorTests
{
    private static readonly HotkeyOption Requested =
        new("Ctrl+Alt+S", "Ctrl + Alt + S", 0x1 | 0x4, 0x53);

    private static readonly HotkeyOption Previous =
        new("Ctrl+Alt+N", "Ctrl + Alt + N", 0x1 | 0x4, 0x4E);

    /// <summary>A registrar that only "registers" a configured set of key combinations.</summary>
    private sealed class FakeRegistrar : IHotkeyRegistrar
    {
        private readonly HashSet<(uint Modifiers, uint VirtualKey)> _registrable;
        private readonly int _errorOnFailure;

        public FakeRegistrar(int errorOnFailure, params HotkeyOption[] registrable)
        {
            _errorOnFailure = errorOnFailure;
            _registrable = registrable.Select(o => (o.Modifiers, o.VirtualKey)).ToHashSet();
        }

        public int RegisterCalls { get; private set; }

        public int UnregisterCalls { get; private set; }

        public int LastError { get; private set; }

        public bool Register(uint modifiers, uint virtualKey)
        {
            RegisterCalls++;
            if (_registrable.Contains((modifiers, virtualKey)))
            {
                LastError = 0;
                return true;
            }

            LastError = _errorOnFailure;
            return false;
        }

        public void Unregister() => UnregisterCalls++;
    }

    [Fact]
    public void New_hotkey_succeeds()
    {
        var registrar = new FakeRegistrar(0, Requested);
        var coordinator = new HotkeyRegistrationCoordinator(registrar);

        var result = coordinator.Apply(Requested, Previous, currentlyRegistered: true);

        Assert.Equal(HotkeyApplyOutcome.Applied, result.Outcome);
        Assert.True(result.HotkeyRegistered);
        Assert.Equal(1, registrar.UnregisterCalls); // the old hotkey was released first
    }

    [Fact]
    public void New_hotkey_fails_but_previous_restore_succeeds()
    {
        // Only the previous combination can be registered.
        var registrar = new FakeRegistrar(1400, Previous);
        var coordinator = new HotkeyRegistrationCoordinator(registrar);

        var result = coordinator.Apply(Requested, Previous, currentlyRegistered: true);

        Assert.Equal(HotkeyApplyOutcome.RevertedToPrevious, result.Outcome);
        Assert.True(result.HotkeyRegistered);
        Assert.Equal(1400, result.Error);
    }

    [Fact]
    public void New_hotkey_fails_and_previous_restore_also_fails()
    {
        // Nothing can be registered.
        var registrar = new FakeRegistrar(1400);
        var coordinator = new HotkeyRegistrationCoordinator(registrar);

        var result = coordinator.Apply(Requested, Previous, currentlyRegistered: true);

        Assert.Equal(HotkeyApplyOutcome.NoHotkeyActive, result.Outcome);
        Assert.False(result.HotkeyRegistered);
        Assert.Equal(1400, result.Error);
    }
}
