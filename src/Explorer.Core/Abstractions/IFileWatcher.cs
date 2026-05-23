using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Monitors directories for filesystem changes (create, modify, delete, rename).
/// </summary>
public interface IFileWatcher : IDisposable
{
    void Watch(PathRef directory);
    void Unwatch(PathRef directory);
    event EventHandler<FileChangeEventArgs> Changed;
}
