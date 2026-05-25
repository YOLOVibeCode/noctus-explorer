using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

public enum InsertPosition
{
    AtStart,
    AtEnd,
    AtIndex,
    BeforeExtension
}

public sealed class InsertTextOperation : IRenameOperation
{
    public string Text { get; }
    public InsertPosition Position { get; }
    public int Index { get; }

    public InsertTextOperation(string text, InsertPosition position = InsertPosition.AtStart, int index = 0)
    {
        Text = text ?? "";
        Position = position;
        Index = index;
    }

    public string Apply(string currentName, int index, FileEntry entry)
    {
        if (string.IsNullOrEmpty(Text)) return currentName;

        return Position switch
        {
            InsertPosition.AtStart => Text + currentName,
            InsertPosition.AtEnd => currentName + Text,
            InsertPosition.BeforeExtension => InsertBeforeExtension(currentName, Text),
            InsertPosition.AtIndex => InsertAtIndex(currentName, Text, Index),
            _ => currentName
        };
    }

    private static string InsertAtIndex(string name, string text, int idx)
    {
        var clamped = Math.Clamp(idx, 0, name.Length);
        return name[..clamped] + text + name[clamped..];
    }

    private static string InsertBeforeExtension(string name, string text)
    {
        var dot = name.LastIndexOf('.');
        if (dot <= 0) return name + text;
        return name[..dot] + text + name[dot..];
    }
}
