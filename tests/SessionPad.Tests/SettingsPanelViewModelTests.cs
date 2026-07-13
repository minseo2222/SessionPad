using SessionPad.App.Models;
using SessionPad.App.Services;
using SessionPad.App.ViewModels;

namespace SessionPad.Tests;

public class SettingsPanelViewModelTests
{
    private sealed class FakeSettingsService : ISettingsService
    {
        public string StoredTheme { get; set; } = "Dark";
        public bool StoredAutoTrack { get; set; }
        public string StoredHotkey { get; set; } = "Ctrl+Alt+N";
        public bool StoredAttachHintDismissed { get; set; }

        public string? SavedTheme { get; private set; }
        public bool? SavedAutoTrack { get; private set; }
        public string? SavedHotkey { get; private set; }
        public bool? SavedAttachHintDismissed { get; private set; }

        public string LoadTheme() => StoredTheme;
        public void SaveTheme(string theme) => SavedTheme = theme;
        public bool LoadAutoTrackForeground() => StoredAutoTrack;
        public void SaveAutoTrackForeground(bool value) => SavedAutoTrack = value;
        public string LoadHotkey() => StoredHotkey;
        public void SaveHotkey(string token) => SavedHotkey = token;
        public bool LoadAttachHintDismissed() => StoredAttachHintDismissed;
        public void SaveAttachHintDismissed(bool value) => SavedAttachHintDismissed = value;
    }

    private sealed class FakeStartupService : IStartupService
    {
        public bool Enabled { get; set; }
        public bool EnableSucceeds { get; set; } = true;
        public string? FailureError { get; set; }
        public int EnableCalls { get; private set; }
        public int DisableCalls { get; private set; }

        public bool IsEnabled() => Enabled;

        public bool Enable(out string? error)
        {
            EnableCalls++;
            if (EnableSucceeds)
            {
                Enabled = true;
                error = null;
                return true;
            }

            error = FailureError;
            return false;
        }

        public bool Disable(out string? error)
        {
            DisableCalls++;
            Enabled = false;
            error = null;
            return true;
        }
    }

    private static SettingsPanelViewModel Panel(
        FakeSettingsService? settings = null,
        FakeStartupService? startup = null)
    {
        return new SettingsPanelViewModel(
            settings ?? new FakeSettingsService(),
            startup ?? new FakeStartupService(),
            new ThemeService());
    }

    [Fact]
    public void Attach_hint_starts_visible_and_dismissing_persists()
    {
        var settings = new FakeSettingsService();
        var panel = Panel(settings);

        Assert.False(panel.IsAttachHintDismissed);

        panel.DismissAttachHint();

        Assert.True(panel.IsAttachHintDismissed);
        Assert.True(settings.SavedAttachHintDismissed);
    }

    [Fact]
    public void Previously_dismissed_attach_hint_stays_dismissed()
    {
        var settings = new FakeSettingsService { StoredAttachHintDismissed = true };
        var panel = Panel(settings);

        Assert.True(panel.IsAttachHintDismissed);

        // Dismissing again is a no-op and must not rewrite settings.
        panel.DismissAttachHint();
        Assert.Null(settings.SavedAttachHintDismissed);
    }

    [Fact]
    public void Hotkey_status_warning_tracks_failures_and_clears_on_success()
    {
        var panel = Panel();
        Assert.False(panel.IsHotkeyStatusWarning);

        panel.NotifyHotkeyUnavailable("it may be in use by another app");
        Assert.True(panel.IsHotkeyStatusWarning);

        panel.NotifyHotkeyApplied();
        Assert.False(panel.IsHotkeyStatusWarning);
    }

    [Fact]
    public void Applying_a_different_hotkey_raises_a_change_request()
    {
        var panel = Panel();
        var target = panel.HotkeyOptions.First(o => o.Token == "Ctrl+Alt+S");
        panel.SelectedHotkey = target;
        HotkeyOption? requested = null;
        panel.HotkeyChangeRequested += (_, option) => requested = option;

        panel.ApplyHotkeyCommand.Execute(null);

        Assert.Equal(target, requested);
    }

    [Fact]
    public void Reapplying_the_same_hotkey_recovers_when_none_is_active()
    {
        var panel = Panel(); // AppliedHotkey defaults to Ctrl+Alt+N
        Assert.Equal("Ctrl+Alt+N", panel.AppliedHotkey.Token);

        // A registration attempt left no attach shortcut active; the selection is reset
        // back to the applied hotkey (Ctrl+Alt+N).
        panel.NotifyHotkeyUnavailable("it may be in use by another app");
        Assert.Equal(panel.AppliedHotkey.Token, panel.SelectedHotkey.Token);

        var fireCount = 0;
        HotkeyOption? requested = null;
        panel.HotkeyChangeRequested += (_, option) =>
        {
            fireCount++;
            requested = option;
        };

        // The user presses Apply without changing the selection. Because nothing is
        // active, this must re-request registration rather than no-op.
        panel.ApplyHotkeyCommand.Execute(null);

        Assert.Equal(1, fireCount);
        Assert.Equal("Ctrl+Alt+N", requested!.Token);
    }

    [Fact]
    public void Reapplying_the_same_hotkey_no_ops_while_it_is_active()
    {
        var panel = Panel(); // Ctrl+Alt+N applied and active
        var fired = false;
        panel.HotkeyChangeRequested += (_, _) => fired = true;

        panel.ApplyHotkeyCommand.Execute(null);

        Assert.False(fired);
    }

    [Fact]
    public void Confirming_a_hotkey_persists_the_token_and_updates_state()
    {
        var settings = new FakeSettingsService();
        var panel = Panel(settings);
        var target = panel.HotkeyOptions.First(o => o.Token == "Ctrl+Alt+S");
        panel.SelectedHotkey = target;

        panel.NotifyHotkeyApplied();

        Assert.Equal("Ctrl+Alt+S", settings.SavedHotkey);
        Assert.Equal(target, panel.AppliedHotkey);
        Assert.Contains("Hotkey set to", panel.HotkeyStatus);
    }

    [Fact]
    public void Toggling_theme_applies_and_saves_the_theme()
    {
        var settings = new FakeSettingsService { StoredTheme = "Dark" };
        var panel = Panel(settings);
        Assert.True(panel.IsDarkTheme); // seeded from ThemeService (no WPF app => Dark)

        panel.IsDarkTheme = false;

        Assert.False(panel.IsDarkTheme);
        Assert.Equal("Light", settings.SavedTheme);
    }

    [Fact]
    public void Enabling_start_on_login_calls_the_startup_service()
    {
        var startup = new FakeStartupService { Enabled = false, EnableSucceeds = true };
        var panel = Panel(startup: startup);

        panel.StartOnLogin = true;

        Assert.Equal(1, startup.EnableCalls);
        Assert.True(panel.StartOnLogin);
        Assert.Equal("Enabled", panel.StartupStatus);
    }

    [Fact]
    public void Failed_start_on_login_reverts_and_reports_the_error()
    {
        var startup = new FakeStartupService
        {
            Enabled = false,
            EnableSucceeds = false,
            FailureError = "registry blocked"
        };
        var panel = Panel(startup: startup);

        panel.StartOnLogin = true;

        Assert.False(panel.StartOnLogin);              // reverted to the real state
        Assert.Contains("Failed to enable", panel.StartupStatus);
        Assert.Contains("registry blocked", panel.StartupStatus);
    }
}
