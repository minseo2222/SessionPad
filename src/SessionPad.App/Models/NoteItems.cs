namespace SessionPad.App.Models;

public sealed record PinnedItem
{
    public string Id { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed record TodoItem
{
    public string Id { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public bool IsDone { get; init; }

    public int SortOrder { get; init; }
}

public sealed record CommandItem
{
    public string Id { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed record NoteTextItem
{
    public string Id { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}
