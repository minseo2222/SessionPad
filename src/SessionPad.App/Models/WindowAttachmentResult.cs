namespace SessionPad.App.Models;

public sealed record WindowAttachmentResult
{
    public bool IsAttached { get; init; }

    public bool ShouldContinueTracking { get; init; }

    public bool IsHiddenBecauseTargetMinimized { get; init; }

    public string Status { get; init; } = "Not attached";

    public string? Side { get; init; }

    public string? Error { get; init; }

    public string? FollowUpdateText { get; init; }

    public static WindowAttachmentResult Attached(string side)
    {
        return new WindowAttachmentResult
        {
            IsAttached = true,
            ShouldContinueTracking = true,
            Status = "Attached",
            Side = side,
            FollowUpdateText = "Attached"
        };
    }

    public static WindowAttachmentResult Following(string side, string updateText)
    {
        return new WindowAttachmentResult
        {
            IsAttached = true,
            ShouldContinueTracking = true,
            Status = "Following target",
            Side = side,
            FollowUpdateText = updateText
        };
    }

    public static WindowAttachmentResult FollowWarning(string side, string warning)
    {
        return new WindowAttachmentResult
        {
            IsAttached = true,
            ShouldContinueTracking = true,
            Status = "Following target",
            Side = side,
            Error = warning,
            FollowUpdateText = warning
        };
    }

    public static WindowAttachmentResult TargetMinimized(string? side)
    {
        return new WindowAttachmentResult
        {
            IsAttached = true,
            ShouldContinueTracking = true,
            IsHiddenBecauseTargetMinimized = true,
            Status = "Target minimized",
            Side = side,
            FollowUpdateText = "Target minimized; SessionPad hidden"
        };
    }

    public static WindowAttachmentResult IgnoredSessionPadWindow(bool keepTrackingExistingTarget, string? side)
    {
        return new WindowAttachmentResult
        {
            IsAttached = keepTrackingExistingTarget,
            ShouldContinueTracking = keepTrackingExistingTarget,
            Status = "Ignored SessionPad window",
            Side = keepTrackingExistingTarget ? side : null,
            Error = "Self window was detected; attach skipped",
            FollowUpdateText = "Self window was detected; attach skipped"
        };
    }

    public static WindowAttachmentResult NotAttached(string reason)
    {
        return new WindowAttachmentResult
        {
            IsAttached = false,
            Status = "Not attached",
            Error = reason,
            FollowUpdateText = reason
        };
    }
}
