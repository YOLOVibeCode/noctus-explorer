using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

public sealed record RenamePreview(FileEntry Original, string NewName, bool IsValid, string? ValidationError);

public sealed class BatchRenameEngine
{
    /// <summary>
    /// Runs the operation chain over the entries and returns a (original, newName) preview list.
    /// Operations are applied in order; each receives the previous result.
    /// Validation flags duplicates, empty names, invalid characters, and no-op renames.
    /// </summary>
    public IReadOnlyList<RenamePreview> Preview(
        IReadOnlyList<FileEntry> entries,
        IReadOnlyList<IRenameOperation> operations)
    {
        var results = new List<RenamePreview>(entries.Count);
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var name = entry.Name;
            foreach (var op in operations)
                name = op.Apply(name, i, entry);

            var (isValid, error) = Validate(name, entry, seenNames);
            results.Add(new RenamePreview(entry, name, isValid, error));
            seenNames.Add(name);
        }

        return results;
    }

    private static (bool isValid, string? error) Validate(string newName, FileEntry original, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return (false, "Name is empty");

        if (newName.IndexOfAny(InvalidNameChars) >= 0)
            return (false, "Name contains invalid characters");

        if (seen.Contains(newName))
            return (false, "Duplicate name within batch");

        if (string.Equals(newName, original.Name, StringComparison.Ordinal))
            return (true, "Unchanged");

        return (true, null);
    }

    // Conservative cross-platform set: characters that are invalid on Windows + control chars.
    // / and \ are excluded everywhere; : * ? " < > | are Windows-only but safer to reject universally.
    private static readonly char[] InvalidNameChars =
        ['\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0'];
}
