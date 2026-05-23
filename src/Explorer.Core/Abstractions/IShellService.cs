using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Enumerates and resolves filesystem items. Read-only shell queries.
/// </summary>
public interface IShellService
{
    Task<IReadOnlyList<FileEntry>> EnumerateAsync(PathRef directory, CancellationToken ct = default);
    Task<PathRef> ResolveAsync(string path, CancellationToken ct = default);
    PathRef GetSpecialFolder(SpecialFolder folder);
    string GetDisplayName(PathRef item);
    bool Exists(PathRef item);
}
