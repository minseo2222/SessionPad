namespace SessionPad.App.Models;

public sealed record NoteSections
{
    public List<PinnedItem> Pinned { get; init; } = new();

    public List<TodoItem> Todo { get; init; } = new();

    public List<CommandItem> Commands { get; init; } = new();

    public List<NoteTextItem> Notes { get; init; } = new();
}
