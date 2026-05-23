using NoctusExplorer.Core.Models;

namespace NoctusExplorer.UI.Contracts;

public interface ITabHost
{
    int AddTab(PathRef initialLocation);
    void CloseTab(int tabId);
    void ActivateTab(int tabId);
    int ActiveTabId { get; }
    event EventHandler<TabEventArgs> ActiveTabChanged;
}

public class TabEventArgs : EventArgs
{
    public required int TabId { get; init; }
}
