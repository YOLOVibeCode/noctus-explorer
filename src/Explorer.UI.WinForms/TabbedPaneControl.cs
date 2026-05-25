using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.UI.Contracts;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Combines a TabStripControl with multiple ExplorerBrowserPane instances.
/// Only the active tab's pane is visible. Manages tab lifecycle.
/// </summary>
public sealed class TabbedPaneControl : UserControl
{
    private readonly IShellService _shellService;
    private readonly TabStripControl _tabStrip;
    private readonly AddressBarControl _addressBar;
    private readonly Panel _contentPanel;
    private readonly Dictionary<int, ExplorerBrowserPane> _panes = [];
    private int _nextTabId;
    private int _activeTabId = -1;
    private bool _showNavigationTree;

    public event EventHandler<PathRef>? NavigationCompleted;
    public event EventHandler<IReadOnlyList<FileEntry>>? SelectionChanged;
    public event EventHandler? ItemsEnumerated;
    public event EventHandler? Activated;

    public TabbedPaneControl(IShellService shellService)
    {
        _shellService = shellService;
        Dock = DockStyle.Fill;

        _tabStrip = new TabStripControl();
        _addressBar = new AddressBarControl();
        _contentPanel = new Panel { Dock = DockStyle.Fill };

        _tabStrip.TabActivated += OnTabActivated;
        _tabStrip.TabClosed += OnTabClosed;
        _tabStrip.NewTabRequested += (_, _) => AddTab(null);

        _addressBar.NavigationRequested += (_, path) => NavigateActiveTab(path);
        _addressBar.GoBackRequested += (_, _) => ActivePaneOrNull?.GoBack();
        _addressBar.GoForwardRequested += (_, _) => ActivePaneOrNull?.GoForward();
        _addressBar.GoUpRequested += (_, _) => ActivePaneOrNull?.GoUp();

        // Dock order: LAST-added is closest to edge.
        // Visual top-to-bottom: tab strip, address bar, content panel.
        Controls.Add(_contentPanel);   // Fill — added first
        Controls.Add(_addressBar);     // Top — below tab strip
        Controls.Add(_tabStrip);       // Top — at the top
    }

    public PathRef CurrentLocation => ActivePaneOrNull?.CurrentLocation
        ?? _shellService.GetSpecialFolder(SpecialFolder.Home);

    public ExplorerBrowserPane? ActivePaneOrNull =>
        _activeTabId >= 0 && _panes.TryGetValue(_activeTabId, out var p) ? p : null;

    public TabStripControl TabStrip => _tabStrip;

    public IReadOnlyList<FileEntry> CurrentSelection =>
        ActivePaneOrNull?.CurrentSelection ?? [];

    public int CurrentItemCount => ActivePaneOrNull?.ItemCount ?? 0;

    /// <summary>Locations of all tabs in this pane, in tab-strip order.</summary>
    public IReadOnlyList<PathRef> TabLocations
        => _panes.Values.Select(p => p.CurrentLocation).ToList();

    /// <summary>Zero-based index of the active tab (or -1 if no tabs).</summary>
    public int ActiveTabIndex => FindTabIndex(_activeTabId);

    /// <summary>Restore a list of tab paths and activate one of them. Replaces existing tabs.</summary>
    public void RestoreTabs(IReadOnlyList<PathRef> paths, int activeIndex)
    {
        // Clear existing tabs first (CloseTab triggers auto-create when empty,
        // so we have to remove without re-adding by clearing _panes manually)
        foreach (var (id, pane) in _panes.ToList())
        {
            _contentPanel.Controls.Remove(pane);
            pane.Dispose();
        }
        _panes.Clear();
        while (_tabStrip.TabCount > 0) _tabStrip.RemoveTab(0);
        _activeTabId = -1;

        foreach (var p in paths) AddTab(p);

        if (activeIndex >= 0 && activeIndex < _tabStrip.TabCount)
            _tabStrip.ActivateTab(activeIndex);
    }

    /// <summary>Scale the tab strip height by a DPI factor (called from MainForm.OnHandleCreated).</summary>
    public void ScaleChrome(float scale)
    {
        _tabStrip.Height = (int)(30 * scale);
        _tabStrip.ApplyScale(scale);
        _addressBar.Height = (int)(34 * scale);
        _addressBar.ApplyScale(scale);
    }

    /// <summary>Focus the address bar in edit mode for typing a path.</summary>
    public void FocusAddressBar() => _addressBar.EnterEditMode();

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ShowNavigationTree
    {
        get => _showNavigationTree;
        set
        {
            if (_showNavigationTree == value) return;
            _showNavigationTree = value;
            foreach (var pane in _panes.Values)
                pane.SetNavigationTreeVisible(value);
        }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
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

        // Visible = true from the start so the hosted ExplorerBrowser COM control
        // creates its child window handle immediately and can populate items.
        // We hide non-active tabs via BringToFront ordering in OnTabActivated.
        var pane = new ExplorerBrowserPane(path, showNavigationTree: _showNavigationTree)
        {
            Dock = DockStyle.Fill,
            Visible = true,
        };
        pane.NavigationCompleted += (_, args) =>
        {
            if (_panes.ContainsKey(tabId) && _activeTabId == tabId)
            {
                var idx = FindTabIndex(tabId);
                _tabStrip.UpdateTabTitle(idx, args.Location.DisplayName);
                _addressBar.SetPath(args.Location);
                _addressBar.SetNavigationState(pane.CanGoBack, pane.CanGoForward);
                NavigationCompleted?.Invoke(this, args.Location);
            }
        };
        pane.HistoryChanged += (_, _) =>
        {
            if (_panes.ContainsKey(tabId) && _activeTabId == tabId)
                _addressBar.SetNavigationState(pane.CanGoBack, pane.CanGoForward);
        };
        pane.SelectionChanged += (_, args) =>
        {
            if (_panes.ContainsKey(tabId) && _activeTabId == tabId)
                SelectionChanged?.Invoke(this, args.SelectedItems);
        };
        pane.ItemsEnumerated += (_, _) =>
        {
            if (_panes.ContainsKey(tabId) && _activeTabId == tabId)
                ItemsEnumerated?.Invoke(this, EventArgs.Empty);
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
        _activeTabId = tabId;

        if (_panes.TryGetValue(tabId, out var newPane))
        {
            // All panes stay Visible (so their COM child windows stay alive);
            // we just bring the active tab to the front of the z-order.
            newPane.BringToFront();
            _addressBar.SetPath(newPane.CurrentLocation);
            _addressBar.SetNavigationState(newPane.CanGoBack, newPane.CanGoForward);
            SelectionChanged?.Invoke(this, newPane.CurrentSelection);
            NavigationCompleted?.Invoke(this, newPane.CurrentLocation);
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
