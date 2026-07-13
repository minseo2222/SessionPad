using SessionPad.App.Models;

namespace SessionPad.Tests;

public class WindowAttachmentResultTests
{
    [Fact]
    public void Attached_state_keeps_tracking_and_is_visible()
    {
        var result = WindowAttachmentResult.Attached("Right");

        Assert.True(result.IsAttached);
        Assert.True(result.ShouldContinueTracking);
        Assert.False(result.IsHiddenBecauseTargetMinimized);
        Assert.Equal("Attached", result.Status);
        Assert.Equal("Right", result.Side);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Following_state_keeps_tracking_and_is_visible()
    {
        var result = WindowAttachmentResult.Following("Left", "Target unchanged");

        Assert.True(result.IsAttached);
        Assert.True(result.ShouldContinueTracking);
        Assert.False(result.IsHiddenBecauseTargetMinimized);
        Assert.Equal("Following target", result.Status);
        Assert.Equal("Left", result.Side);
        Assert.Equal("Target unchanged", result.FollowUpdateText);
    }

    [Fact]
    public void Target_minimized_state_keeps_tracking_and_requests_hide()
    {
        var result = WindowAttachmentResult.TargetMinimized("Clamped Right");

        Assert.True(result.IsAttached);
        Assert.True(result.ShouldContinueTracking);
        Assert.True(result.IsHiddenBecauseTargetMinimized);
        Assert.Equal("Target minimized", result.Status);
        Assert.Equal("Clamped Right", result.Side);
    }

    [Fact]
    public void Not_attached_state_stops_tracking_and_is_not_minimize_hidden()
    {
        var result = WindowAttachmentResult.NotAttached("No attached target.");

        Assert.False(result.IsAttached);
        Assert.False(result.ShouldContinueTracking);
        Assert.False(result.IsHiddenBecauseTargetMinimized);
        Assert.Equal("Not attached", result.Status);
        Assert.Null(result.Side);
        Assert.Equal("No attached target.", result.Error);
    }
}
