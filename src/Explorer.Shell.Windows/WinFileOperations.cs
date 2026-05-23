using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Shell.Windows;

/// <summary>
/// File operations using System.IO for now.
/// TODO: Replace with IFileOperation COM wrapper (Vanara) for
/// recycle bin support, undo, progress UI, and conflict resolution.
/// </summary>
public sealed class WinFileOperations : IFileOperations
{
    public IOperationHandle Copy(IReadOnlyList<PathRef> sources, PathRef destination)
    {
        return new SimpleOperationHandle("Copy", () =>
        {
            foreach (var source in sources)
            {
                var destPath = Path.Combine(destination.FullPath, Path.GetFileName(source.FullPath));
                if (source.IsDirectory)
                    CopyDirectory(source.FullPath, destPath);
                else
                    File.Copy(source.FullPath, destPath, overwrite: false);
            }
        });
    }

    public IOperationHandle Move(IReadOnlyList<PathRef> sources, PathRef destination)
    {
        return new SimpleOperationHandle("Move", () =>
        {
            foreach (var source in sources)
            {
                var destPath = Path.Combine(destination.FullPath, Path.GetFileName(source.FullPath));
                if (source.IsDirectory)
                    Directory.Move(source.FullPath, destPath);
                else
                    File.Move(source.FullPath, destPath);
            }
        });
    }

    public IOperationHandle Delete(IReadOnlyList<PathRef> items, bool permanent = false)
    {
        return new SimpleOperationHandle("Delete", () =>
        {
            foreach (var item in items)
            {
                // TODO: Use SHFileOperation or IFileOperation for recycle bin
                if (item.IsDirectory)
                    Directory.Delete(item.FullPath, recursive: true);
                else
                    File.Delete(item.FullPath);
            }
        });
    }

    public IOperationHandle CreateFolder(PathRef parent, string name)
    {
        return new SimpleOperationHandle("New Folder", () =>
        {
            Directory.CreateDirectory(Path.Combine(parent.FullPath, name));
        });
    }

    public IOperationHandle Rename(PathRef item, string newName)
    {
        return new SimpleOperationHandle("Rename", () =>
        {
            var dir = Path.GetDirectoryName(item.FullPath)!;
            var newPath = Path.Combine(dir, newName);
            if (item.IsDirectory)
                Directory.Move(item.FullPath, newPath);
            else
                File.Move(item.FullPath, newPath);
        });
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }
}

/// <summary>
/// Minimal IOperationHandle for synchronous operations.
/// Runs the action immediately on construction.
/// Will be replaced with async + progress in later milestones.
/// </summary>
internal sealed class SimpleOperationHandle : IOperationHandle
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Description { get; }
    public OperationStatus Status { get; private set; } = OperationStatus.Queued;
    public double Progress => Status == OperationStatus.Completed ? 1.0 : 0.0;
    public long BytesTransferred => 0;
    public long TotalBytes => 0;
    public TimeSpan? EstimatedRemaining => null;

    public event EventHandler<OperationProgressEventArgs>? ProgressChanged;
    public event EventHandler<OperationConflictEventArgs>? ConflictEncountered;
    public event EventHandler<OperationCompletedEventArgs>? Completed;

    public SimpleOperationHandle(string description, Action action)
    {
        Description = description;
        try
        {
            Status = OperationStatus.Running;
            action();
            Status = OperationStatus.Completed;
            Completed?.Invoke(this, new OperationCompletedEventArgs { FinalStatus = OperationStatus.Completed });
        }
        catch (Exception ex)
        {
            Status = OperationStatus.Failed;
            Completed?.Invoke(this, new OperationCompletedEventArgs { FinalStatus = OperationStatus.Failed, Error = ex });
            throw;
        }
    }

    public void Pause() { }
    public void Resume() { }
    public void Cancel() { }
    public void Dispose() { }
}
