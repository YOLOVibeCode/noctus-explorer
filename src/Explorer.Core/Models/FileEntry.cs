namespace NoctusExplorer.Core.Models;

/// <summary>
/// Immutable snapshot of a file or directory in a listing.
/// </summary>
public sealed record FileEntry(
    PathRef Path,
    string Name,
    string Extension,
    long? Size,
    DateTimeOffset DateModified,
    DateTimeOffset DateCreated,
    bool IsHidden,
    bool IsSystem,
    string Kind)
{
    public bool IsDirectory => Path.IsDirectory;
}
