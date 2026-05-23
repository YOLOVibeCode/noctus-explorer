using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Abstractions;

/// <summary>
/// Mutating file system operations. Each returns a trackable handle.
/// </summary>
public interface IFileOperations
{
    IOperationHandle Copy(IReadOnlyList<PathRef> sources, PathRef destination);
    IOperationHandle Move(IReadOnlyList<PathRef> sources, PathRef destination);
    IOperationHandle Delete(IReadOnlyList<PathRef> items, bool permanent = false);
    IOperationHandle CreateFolder(PathRef parent, string name);
    IOperationHandle Rename(PathRef item, string newName);
}
