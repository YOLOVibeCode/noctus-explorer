using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

public sealed class NumberSequenceOperation : IRenameOperation
{
    public int Start { get; }
    public int Step { get; }
    public int Padding { get; }
    public bool Prepend { get; }
    public string Separator { get; }

    public NumberSequenceOperation(int start = 1, int step = 1, int padding = 0, bool prepend = false, string separator = "_")
    {
        Start = start;
        Step = step;
        Padding = Math.Max(0, padding);
        Prepend = prepend;
        Separator = separator ?? "";
    }

    public string Apply(string currentName, int index, FileEntry entry)
    {
        var number = Start + (index * Step);
        var formatted = Padding > 0 ? number.ToString().PadLeft(Padding, '0') : number.ToString();

        var dot = currentName.LastIndexOf('.');
        if (dot <= 0)
        {
            return Prepend ? formatted + Separator + currentName : currentName + Separator + formatted;
        }

        var stem = currentName[..dot];
        var ext = currentName[dot..];
        return Prepend
            ? formatted + Separator + stem + ext
            : stem + Separator + formatted + ext;
    }
}
