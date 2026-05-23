using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Shell.Windows;

public sealed class WinFileWatcher : IFileWatcher
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<FileChangeEventArgs>? Changed;

    public void Watch(PathRef directory)
    {
        var path = directory.FullPath;
        if (_watchers.ContainsKey(path)) return;
        if (!Directory.Exists(path)) return;

        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };

        watcher.Created += (_, e) => OnChanged(FileChangeType.Created, e.FullPath);
        watcher.Deleted += (_, e) => OnChanged(FileChangeType.Deleted, e.FullPath);
        watcher.Changed += (_, e) => OnChanged(FileChangeType.Modified, e.FullPath);
        watcher.Renamed += (_, e) => Changed?.Invoke(this, new FileChangeEventArgs
        {
            ChangeType = FileChangeType.Renamed,
            Path = new PathRef(e.FullPath),
            OldPath = new PathRef(e.OldFullPath)
        });

        _watchers[path] = watcher;
    }

    public void Unwatch(PathRef directory)
    {
        if (_watchers.Remove(directory.FullPath, out var watcher))
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    private void OnChanged(FileChangeType type, string fullPath)
    {
        Changed?.Invoke(this, new FileChangeEventArgs
        {
            ChangeType = type,
            Path = new PathRef(fullPath)
        });
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }
}
