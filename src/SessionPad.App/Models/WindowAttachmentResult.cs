namespace SessionPad.App.Models;

public sealed record WindowAttachmentResult
{
    public bool IsAttached { get; init; }

    public string Status { get; init; } = "Not attached";

    public string? Side { get; init; }

    public string? Error { get; init; }

    public static WindowAttachmentResult Attached(string side)
    {
        return new WindowAttachmentResult
        {
            IsAttached = true,
            Status = "Attached",
            Side = side
        };
    }

    public static WindowAttachmentResult NotAttached(string reason)
    {
        return new WindowAttachmentResult
        {
            IsAttached = false,
            Status = "Not attached",
            Error = reason
        };
    }
}
