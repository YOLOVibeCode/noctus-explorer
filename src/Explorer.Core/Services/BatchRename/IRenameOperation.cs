using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

/// <summary>
/// A single transformation step in a batch rename pipeline.
/// Operations are chained: each receives the result of the previous one.
/// </summary>
public interface IRenameOperation
{
    /// <summary>
    /// Returns the transformed name for an entry.
    /// </summary>
    /// <param name="currentName">The name after previous operations (filename + extension).</param>
    /// <param name="index">Zero-based position of the entry within the batch.</param>
    /// <param name="entry">The original file entry (for date-stamp etc. that need source metadata).</param>
    string Apply(string currentName, int index, FileEntry entry);
}
