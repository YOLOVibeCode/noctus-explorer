using System.Globalization;
using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

public enum CaseMode
{
    Upper,
    Lower,
    Title,
    Sentence
}

public sealed class ChangeCaseOperation : IRenameOperation
{
    public CaseMode Mode { get; }
    public bool PreserveExtension { get; }

    public ChangeCaseOperation(CaseMode mode, bool preserveExtension = true)
    {
        Mode = mode;
        PreserveExtension = preserveExtension;
    }

    public string Apply(string currentName, int index, FileEntry entry)
    {
        var (stem, ext) = SplitExtension(currentName);
        var transformed = ApplyToStem(stem);
        return PreserveExtension ? transformed + ext : ApplyToStem(currentName);
    }

    private string ApplyToStem(string stem) => Mode switch
    {
        CaseMode.Upper => stem.ToUpperInvariant(),
        CaseMode.Lower => stem.ToLowerInvariant(),
        CaseMode.Title => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(stem.ToLowerInvariant()),
        CaseMode.Sentence => ToSentenceCase(stem),
        _ => stem
    };

    private static string ToSentenceCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var lower = s.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    private static (string stem, string ext) SplitExtension(string name)
    {
        var dot = name.LastIndexOf('.');
        if (dot <= 0) return (name, "");
        return (name[..dot], name[dot..]);
    }
}
