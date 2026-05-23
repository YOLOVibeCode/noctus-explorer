using NoctusExplorer.Core.Models;

namespace NoctusExplorer.UI.Contracts;

public interface IPaneView : IDisposable
{
    Task NavigateAsync(PathRef target);
    PathRef CurrentLocation { get; }
    IReadOnlyList<FileEntry> CurrentSelection { get; }
    event EventHandler<NavigationEventArgs> NavigationCompleted;
    event EventHandler<SelectionEventArgs> SelectionChanged;
    void Refresh();
    void Focus();
}

public class NavigationEventArgs : EventArgs
{
    public required PathRef Location { get; init; }
}

public class SelectionEventArgs : EventArgs
{
    public required IReadOnlyList<FileEntry> SelectedItems { get; init; }
}
