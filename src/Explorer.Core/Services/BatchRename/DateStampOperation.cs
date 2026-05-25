using NoctusExplorer.Core.Models;

namespace NoctusExplorer.Core.Services.BatchRename;

public enum DateField
{
    Modified,
    Created
}

public sealed class DateStampOperation : IRenameOperation
{
    public string Format { get; }
    public DateField Field { get; }
    public InsertPosition Position { get; }
    public string Separator { get; }

    public DateStampOperation(string format = "yyyy-MM-dd", DateField field = DateField.Modified, InsertPosition position = InsertPosition.AtStart, string separator = "_")
    {
        Format = format ?? "yyyy-MM-dd";
        Field = field;
        Position = position;
        Separator = separator ?? "";
    }

    public string Apply(string currentName, int index, FileEntry entry)
    {
        var date = Field == DateField.Created ? entry.DateCreated : entry.DateModified;
        string stamp;
        try
        {
            stamp = date.ToString(Format);
        }
        catch (FormatException)
        {
            stamp = date.ToString("yyyy-MM-dd");
        }

        var dot = currentName.LastIndexOf('.');
        var hasExt = dot > 0;
        var stem = hasExt ? currentName[..dot] : currentName;
        var ext = hasExt ? currentName[dot..] : "";

        return Position switch
        {
            InsertPosition.AtStart => stamp + Separator + currentName,
            InsertPosition.AtEnd => currentName + Separator + stamp,
            InsertPosition.BeforeExtension => stem + Separator + stamp + ext,
            _ => stem + Separator + stamp + ext
        };
    }
}
