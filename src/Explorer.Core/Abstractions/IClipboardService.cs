using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Read/write files and text on the system clipboard.
/// </summary>
public interface IClipboardService
{
    Task SetFilesAsync(IReadOnlyList<PathRef> items, ClipboardOperation operation);
    Task<ClipboardContent?> GetFilesAsync();
    Task SetTextAsync(string text);
}

public sealed record ClipboardContent(IReadOnlyList<PathRef> Items, ClipboardOperation Operation);
