using System.Text.RegularExpressions;
using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

public sealed class FindReplaceOperation : IRenameOperation
{
    public string Find { get; }
    public string Replace { get; }
    public bool UseRegex { get; }
    public bool CaseSensitive { get; }

    public FindReplaceOperation(string find, string replace, bool useRegex = false, bool caseSensitive = false)
    {
        Find = find ?? "";
        Replace = replace ?? "";
        UseRegex = useRegex;
        CaseSensitive = caseSensitive;
    }

    public string Apply(string currentName, int index, FileEntry entry)
    {
        if (string.IsNullOrEmpty(Find)) return currentName;

        if (UseRegex)
        {
            var opts = CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            try
            {
                return Regex.Replace(currentName, Find, Replace, opts);
            }
            catch (RegexParseException)
            {
                return currentName;
            }
        }

        var comparison = CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        return currentName.Replace(Find, Replace, comparison);
    }
}
