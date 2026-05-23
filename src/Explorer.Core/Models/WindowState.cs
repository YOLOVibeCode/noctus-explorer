namespace NoctusExplorer.Core.Models;

public sealed record WindowState(
    PaneState LeftPane,
    PaneState? RightPane,
    SplitMode SplitMode,
    double SplitRatio,
    WindowBounds Bounds,
    IReadOnlyList<PathRef> DropStackItems);

public sealed record WindowBounds(int X, int Y, int Width, int Height);
