using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Abstractions;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Combines a TabStripControl with multiple ExplorerBrowserPane instances.
/// Only the active tab's pane is visible. Manages tab lifecycle.
/// </summary>
public sealed class TabbedPaneControl : UserControl
{
    private readonly IShellService _shellService;
    private readonly TabStripControl _tabStrip;
    private readonly Panel _contentPanel;
    private readonly Dictionary<int, ExplorerBrowserPane> _panes = [];
    private int _nextTabId;
    private int _activeTabId = -1;

    public event EventHandler<PathRef>? NavigationCompleted;
    public event EventHandler? Activated;

    public TabbedPaneControl(IShellService shellService)
    {
        _shellService = shellService;
        Dock = DockStyle.Fill;

        _tabStrip = new TabStripControl();
        _contentPanel = new Panel { Dock = DockStyle.Fill };

        _tabStrip.TabActivated += OnTabActivated;
        _tabStrip.TabClosed += OnTabClosed;
        _tabStrip.NewTabRequested += (_, _) => AddTab(null);

        Controls.Add(_contentPanel);
        Controls.Add(_tabStrip);
    }

    public PathRef CurrentLocation => ActivePaneOrNull?.CurrentLocation
        ?? _shellService.GetSpecialFolder(SpecialFolder.Home);

    public ExplorerBrowserPane? ActivePaneOrNull =>
        _activeTabId >= 0 && _panes.TryGetValue(_activeTabId, out var p) ? p : null;

    public TabStripControl TabStrip => _tabStrip;

    public bool IsActivePane
    {
        set
        {
            BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
        }
    }

    public int AddTab(PathRef? location)
    {
        var path = location ?? _shellService.GetSpecialFolder(SpecialFolder.Home);
        var tabId = _nextTabId++;

        var pane = new ExplorerBrowserPane(path) { Dock = DockStyle.Fill, Visible = false };
        pane.NavigationCompleted += (_, args) =>
        {
            if (_panes.ContainsKey(tabId) && _activeTabId == tabId)
            {
                var idx = FindTabIndex(tabId);
                _tabStrip.UpdateTabTitle(idx, args.Location.DisplayName);
                NavigationCompleted?.Invoke(this, args.Location);
            }
        };
        pane.GotFocus += (_, _) => Activated?.Invoke(this, EventArgs.Empty);
        pane.Enter += (_, _) => Activated?.Invoke(this, EventArgs.Empty);

        _panes[tabId] = pane;
        _contentPanel.Controls.Add(pane);

        _tabStrip.AddTab(path.DisplayName, tabId);
        return tabId;
    }

    public void CloseTab(int tabId)
    {
        if (!_panes.Remove(tabId, out var pane)) return;

        _contentPanel.Controls.Remove(pane);
        pane.Dispose();

        var idx = FindTabIndex(tabId);
        _tabStrip.RemoveTab(idx);

        // If no tabs left, create a new default one
        if (_panes.Count == 0)
            AddTab(null);
    }

    public void NavigateActiveTab(PathRef target)
    {
        ActivePaneOrNull?.NavigateAsync(target);
    }

    public void RefreshActiveTab()
    {
        ActivePaneOrNull?.Refresh();
    }

    public string StatusText => $"{_tabStrip.TabCount} tab{(_tabStrip.TabCount != 1 ? "s" : "")}";

    private void OnTabActivated(object? sender, int tabId)
    {
        // Hide current, show new
        if (_activeTabId >= 0 && _panes.TryGetValue(_activeTabId, out var oldPane))
            oldPane.Visible = false;

        _activeTabId = tabId;

        if (_panes.TryGetValue(tabId, out var newPane))
        {
            newPane.Visible = true;
            newPane.BringToFront();
        }
    }

    private void OnTabClosed(object? sender, int tabId)
    {
        CloseTab(tabId);
    }

    private int FindTabIndex(int tabId)
    {
        // Find index by iterating tabs — simple enough for <100 tabs
        var keys = _panes.Keys.ToList();
        return keys.IndexOf(tabId);
    }
}
