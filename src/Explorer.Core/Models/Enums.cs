namespace NoctusExplorer.Core.Models;

public enum SplitMode
{
    Single,
    Vertical,
    Horizontal
}

public enum ViewMode
{
    Icons,
    SmallIcons,
    List,
    Details,
    Tiles,
    Content,
    Columns,
    Gallery
}

public enum SortField
{
    Name,
    Size,
    DateModified,
    DateCreated,
    Kind,
    Extension
}

public enum SortDirection
{
    Ascending,
    Descending
}

public enum PaneSide
{
    Left,
    Right
}

public enum ClipboardOperation
{
    Copy,
    Cut
}

public enum OperationStatus
{
    Queued,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public enum SpecialFolder
{
    Home,
    Desktop,
    Downloads,
    Documents,
    Trash,
    Root
}

public enum FileType
{
    Files,
    Folders,
    Both
}

public enum SelectionCount
{
    Any,
    Single,
    Multiple
}

public enum ActionType
{
    RunProgram,
    ShellCommand,
    OpenWith,
    CopyText,
    OpenUrl
}

public enum HashAlgorithmType
{
    MD5,
    SHA1,
    SHA256,
    SHA512
}

public enum ConflictResolution
{
    Overwrite,
    Skip,
    Rename,
    OverwriteIfNewer
}
