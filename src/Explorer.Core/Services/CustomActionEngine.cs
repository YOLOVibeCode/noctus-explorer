using System.Text.RegularExpressions;
using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services;

public sealed partial class CustomActionEngine
{
    /// <summary>
    /// Determines whether a custom action should be shown for the given selection and context.
    /// </summary>
    public bool Evaluate(CustomAction action, IReadOnlyList<FileEntry> selection, PathRef otherPaneLocation)
    {
        if (!action.Enabled) return false;
        if (selection.Count == 0) return false;

        var cond = action.Conditions;

        // File type filter
        if (cond.AppliesTo == FileType.Files && selection.Any(e => e.IsDirectory))
            return false;
        if (cond.AppliesTo == FileType.Folders && selection.Any(e => !e.IsDirectory))
            return false;

        // Extension filter
        if (cond.Extensions is { Length: > 0 })
        {
            var allowed = new HashSet<string>(cond.Extensions, StringComparer.OrdinalIgnoreCase);
            if (!selection.All(e => allowed.Contains(e.Extension)))
                return false;
        }

        // Selection count
        if (cond.SelectionCount == SelectionCount.Single && selection.Count != 1)
            return false;
        if (cond.SelectionCount == SelectionCount.Multiple && selection.Count < 2)
            return false;

        // Path contains
        if (!string.IsNullOrEmpty(cond.PathContains))
        {
            if (!selection.Any(e => e.Path.FullPath.Contains(cond.PathContains, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Expands variable placeholders in a template string.
    /// </summary>
    public string ExpandVariables(string template, IReadOnlyList<FileEntry> selection, PathRef otherPaneLocation)
    {
        var first = selection.Count > 0 ? selection[0] : null;

        var result = VariablePattern().Replace(template, match =>
        {
            var variable = match.Groups[1].Value.ToLowerInvariant();
            return variable switch
            {
                "path" => first?.Path.FullPath ?? "",
                "filename" => first?.Name ?? "",
                "basename" => first is not null ? System.IO.Path.GetFileNameWithoutExtension(first.Name) : "",
                "ext" => first?.Extension.TrimStart('.') ?? "",
                "folder" => first?.Path.GetParent()?.FullPath ?? "",
                "other_pane" => otherPaneLocation.FullPath,
                "paths" => string.Join("\n", selection.Select(e => e.Path.FullPath)),
                "paths:quoted" => string.Join(" ", selection.Select(e => $"\"{e.Path.FullPath}\"")),
                _ => match.Value // Leave unknown variables as-is
            };
        });

        return result;
    }

    [GeneratedRegex(@"\{(\w+(?::\w+)?)\}")]
    private static partial Regex VariablePattern();
}
