using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using NoctusExplorer.Core.ViewModels;
using NoctusExplorer.Core.Abstractions;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Main application window. Dual-pane layout with SplitContainer,
/// address bar, status bars, and menu bar. Driven by MainViewModel.
/// </summary>
public sealed class MainForm : Form
{
    private readonly MainViewModel _vm;
    private readonly IShellService _shellService;
    private readonly IFileOperations _fileOps;
    private readonly RecentLocationsService _recentLocations;
    private readonly RecentFilesService _recentFiles;
    private readonly SettingsStore _settings;
    private ToolStripMenuItem? _quickLinksMenuItem;
    private ToolStripMenuItem? _bookmarkBarMenuItem;
    private ToolStripMenuItem? _dropStackMenuItem;
    private ToolStripMenuItem? _navTreeMenuItem;

    private BookmarkBarControl _bookmarkBar = null!;
    private QuickLinksSidebar _quickLinks = null!;
    private TabbedPaneControl _leftPane = null!;
    private TabbedPaneControl _rightPane = null!;
    private SplitContainer _splitContainer = null!;
    private SplitContainer _sidebarSplit = null!;
    private StatusBarControl _leftStatusBar = null!;
    private StatusBarControl _rightStatusBar = null!;
    private Panel _statusPanel = null!;
    private DropStackPanel _dropStack = null!;
    private MenuStrip _menuStrip = null!;

    private TabbedPaneControl ActivePane => _vm.LeftPane.IsActive ? _leftPane : _rightPane;
    private TabbedPaneControl InactivePane => _vm.LeftPane.IsActive ? _rightPane : _leftPane;

    public MainForm(
        MainViewModel vm,
        IShellService shellService,
        IFileOperations fileOps,
        RecentLocationsService recentLocations,
        RecentFilesService recentFiles,
        SettingsStore settings)
    {
        _vm = vm;
        _shellService = shellService;
        _fileOps = fileOps;
        _recentLocations = recentLocations;
        _recentFiles = recentFiles;
        _settings = settings;

        // Restore persisted collections from settings BEFORE BuildLayout
        // so initial tabs/bookmarks reflect saved state.
        _vm.BookmarkStore.LoadFromJson(_settings.Get("bookmarks", ""));
        _recentLocations.LoadFromJson(_settings.Get("recent.visited", ""));
        _recentFiles.LoadFromJson(_settings.Get("recent.accessed", ""));

        Text = "Noctus Explorer";
        AutoScaleMode = AutoScaleMode.None;
        Font = new Font("Segoe UI", 9f);
        // Final Size + Location are set in OnHandleCreated where DeviceDpi is known
        // and any saved session state can be applied.
        StartPosition = FormStartPosition.Manual;

        BuildMenu();
        BuildLayout();
        WireEvents();
    }

    private void BuildMenu()
    {
        _menuStrip = new MenuStrip();

        // File
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add(Item("New &Tab", "Ctrl+T", (_, _) => ActivePane.AddTab(null)));
        fileMenu.DropDownItems.Add(Item("&Close Tab", "Ctrl+W", (_, _) => CloseActiveTab()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(Item("New &Folder", "F7", (_, _) => _vm.CommandRegistry.Execute("file.newFolder")));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(Item("E&xit", "Alt+F4", (_, _) => Close()));
        _menuStrip.Items.Add(fileMenu);

        // Edit
        var editMenu = new ToolStripMenuItem("&Edit");
        editMenu.DropDownItems.Add(Item("Cu&t", "Ctrl+X", (_, _) => _vm.CommandRegistry.Execute("edit.cut")));
        editMenu.DropDownItems.Add(Item("&Copy", "Ctrl+C", (_, _) => _vm.CommandRegistry.Execute("edit.copy")));
        editMenu.DropDownItems.Add(Item("&Paste", "Ctrl+V", (_, _) => _vm.CommandRegistry.Execute("edit.paste")));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add(Item("&Rename", "F2", (_, _) => _vm.CommandRegistry.Execute("edit.rename")));
        editMenu.DropDownItems.Add(Item("&Delete", "Del", (_, _) => _vm.CommandRegistry.Execute("edit.delete")));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add(Item("&Batch Rename…", "Ctrl+M", (_, _) => ShowBatchRename()));
        editMenu.DropDownItems.Add(Item("Add to Drop &Stack", "Ctrl+D", (_, _) => AddSelectionToDropStack()));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add(Item("Copy &Path", "Ctrl+Shift+C", (_, _) => _vm.CommandRegistry.Execute("edit.copyPath")));
        _menuStrip.Items.Add(editMenu);

        // View
        var viewMenu = new ToolStripMenuItem("&View");
        viewMenu.DropDownItems.Add(Item("&Single Pane", null, (_, _) => SetSplitMode(SplitMode.Single)));
        viewMenu.DropDownItems.Add(Item("Dual Pane — &Vertical", null, (_, _) => SetSplitMode(SplitMode.Vertical)));
        viewMenu.DropDownItems.Add(Item("Dual Pane — &Horizontal", null, (_, _) => SetSplitMode(SplitMode.Horizontal)));
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add(Item("&Refresh", "Ctrl+R", (_, _) => ActivePane.Refresh()));
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add(Item("Show &Hidden Files", "Ctrl+H", (_, _) => _vm.CommandRegistry.Execute("view.toggleHidden")));
        _navTreeMenuItem = Item("Show Pane &Navigation Tree", "Ctrl+Shift+N", null);
        _navTreeMenuItem.CheckOnClick = true;
        _navTreeMenuItem.CheckedChanged += (_, _) => SetNavigationTreeVisible(_navTreeMenuItem.Checked);
        viewMenu.DropDownItems.Add(_navTreeMenuItem);

        _quickLinksMenuItem = Item("Show &Quick Links Sidebar", "Ctrl+Shift+L", null);
        _quickLinksMenuItem.CheckOnClick = true;
        _quickLinksMenuItem.Checked = true;
        _quickLinksMenuItem.CheckedChanged += (_, _) => SetQuickLinksVisible(_quickLinksMenuItem.Checked);
        viewMenu.DropDownItems.Add(_quickLinksMenuItem);

        _bookmarkBarMenuItem = Item("Show &Bookmark Bar", null, null);
        _bookmarkBarMenuItem.CheckOnClick = true;
        _bookmarkBarMenuItem.Checked = true;
        _bookmarkBarMenuItem.CheckedChanged += (_, _) => _bookmarkBar.Visible = _bookmarkBarMenuItem.Checked;
        viewMenu.DropDownItems.Add(_bookmarkBarMenuItem);

        _dropStackMenuItem = Item("Show &Drop Stack", null, null);
        _dropStackMenuItem.CheckOnClick = true;
        _dropStackMenuItem.Checked = true;
        _dropStackMenuItem.CheckedChanged += (_, _) => _dropStack.Visible = _dropStackMenuItem.Checked;
        viewMenu.DropDownItems.Add(_dropStackMenuItem);
        _menuStrip.Items.Add(viewMenu);

        // Go
        var goMenu = new ToolStripMenuItem("&Go");
        goMenu.DropDownItems.Add(Item("&Back", "Alt+Left", (_, _) => GoBack()));
        goMenu.DropDownItems.Add(Item("&Forward", "Alt+Right", (_, _) => GoForward()));
        goMenu.DropDownItems.Add(Item("&Up", "Alt+Up", (_, _) => GoUp()));
        goMenu.DropDownItems.Add(new ToolStripSeparator());
        goMenu.DropDownItems.Add(Item("&Home", "Alt+Home", (_, _) => NavigateTo(SpecialFolder.Home)));
        goMenu.DropDownItems.Add(Item("&Desktop", null, (_, _) => NavigateTo(SpecialFolder.Desktop)));
        goMenu.DropDownItems.Add(Item("Do&wnloads", null, (_, _) => NavigateTo(SpecialFolder.Downloads)));
        goMenu.DropDownItems.Add(Item("D&ocuments", null, (_, _) => NavigateTo(SpecialFolder.Documents)));
        goMenu.DropDownItems.Add(new ToolStripSeparator());
        goMenu.DropDownItems.Add(Item("Go to &Folder…", "Ctrl+L", (_, _) => ActivePane.FocusAddressBar()));
        _menuStrip.Items.Add(goMenu);

        // Tools
        var toolsMenu = new ToolStripMenuItem("&Tools");
        toolsMenu.DropDownItems.Add(Item("&Copy to Other Pane", "F5", (_, _) => CopyToOtherPane()));
        toolsMenu.DropDownItems.Add(Item("&Move to Other Pane", "F6", (_, _) => MoveToOtherPane()));
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add(Item("S&witch Pane", "Tab", (_, _) => SwitchPane()));
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add(Item("Command &Palette…", "Ctrl+Shift+P", (_, _) => _vm.CommandRegistry.Execute("tools.commandPalette")));
        _menuStrip.Items.Add(toolsMenu);

        // Help
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add("&About Noctus Explorer", null, (_, _) =>
            MessageBox.Show("Noctus Explorer v0.3.0\nDual-pane file manager hosting native Windows Explorer views.",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information));
        _menuStrip.Items.Add(helpMenu);

        MainMenuStrip = _menuStrip;
        // Do NOT add to Controls here — BuildLayout adds it last so it docks at the very top.
    }

    /// <summary>
    /// Builds a ToolStripMenuItem with proper shortcut-key display.
    /// ToolStripMenuItem does not split text on \t for shortcuts; use
    /// ShortcutKeyDisplayString instead (right-aligned in the menu).
    /// </summary>
    private static ToolStripMenuItem Item(string text, string? shortcut, EventHandler? onClick)
    {
        var item = new ToolStripMenuItem(text);
        if (!string.IsNullOrEmpty(shortcut))
            item.ShortcutKeyDisplayString = shortcut;
        if (onClick is not null)
            item.Click += onClick;
        return item;
    }

    private void BuildLayout()
    {
        var homePath = _shellService.GetSpecialFolder(SpecialFolder.Home);
        var desktopPath = _shellService.GetSpecialFolder(SpecialFolder.Desktop);

        // Split container with two explorer panes (inner)
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4,
            SplitterDistance = 600,
        };

        _leftPane = new TabbedPaneControl(_shellService) { Dock = DockStyle.Fill };
        _rightPane = new TabbedPaneControl(_shellService) { Dock = DockStyle.Fill };

        RestoreOrSeedTabs(_leftPane, "session.tabs.left.paths", "session.tabs.left.active", homePath);
        RestoreOrSeedTabs(_rightPane, "session.tabs.right.paths", "session.tabs.right.active", desktopPath);

        _splitContainer.Panel1.Controls.Add(_leftPane);
        _splitContainer.Panel2.Controls.Add(_rightPane);

        // Quick links sidebar (left side, shared across both panes)
        _quickLinks = new QuickLinksSidebar(_vm.BookmarkStore, _shellService, _recentLocations, _recentFiles);
        _quickLinks.LinkClicked += (_, path) => ActivePane.NavigateActiveTab(path);

        // Outer split: [sidebar] | [dual-pane split]
        _sidebarSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 220,
        };
        _sidebarSplit.Panel1.Controls.Add(_quickLinks);
        _sidebarSplit.Panel2.Controls.Add(_splitContainer);

        // Status bars (bottom) — one for each pane, side by side
        _statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 24,
        };

        _leftStatusBar = new StatusBarControl { Dock = DockStyle.Left, Width = 0 };
        _rightStatusBar = new StatusBarControl { Dock = DockStyle.Right, Width = 0 };

        var statusSplitter = new Label
        {
            Dock = DockStyle.None,
            Width = 1,
            BackColor = SystemColors.ControlDark,
            Height = 20,
        };

        _statusPanel.Controls.Add(_leftStatusBar);
        _statusPanel.Controls.Add(_rightStatusBar);

        // Resize status bars to match split proportions
        _splitContainer.SplitterMoved += (_, _) => ResizeStatusBars();
        _statusPanel.Resize += (_, _) => ResizeStatusBars();

        // Bookmark bar
        _bookmarkBar = new BookmarkBarControl(_vm.BookmarkStore);
        _bookmarkBar.BookmarkClicked += (_, path) => ActivePane.NavigateActiveTab(path);

        // Drop stack
        _dropStack = new DropStackPanel(_vm.DropStack);

        // WinForms Dock ordering: LAST-added child is CLOSEST to the docked edge.
        // Desired visual (top → bottom): menu, address bar, bookmarks, panes (fill),
        // status bar, drop stack. So we add in the reverse of the "edge-first" order.
        Controls.Add(_sidebarSplit);     // Fill — wraps sidebar + dual-pane split
        Controls.Add(_bookmarkBar);      // Top — below the menu
        Controls.Add(_menuStrip);        // Top — at the very top
        Controls.Add(_statusPanel);      // Bottom — above the drop stack
        Controls.Add(_dropStack);        // Bottom — at the very bottom

        UpdateActivePaneVisual();
    }

    private void WireEvents()
    {
        _leftPane.NavigationCompleted += (_, _) => RefreshStatusBar(_leftPane, _leftStatusBar);
        _rightPane.NavigationCompleted += (_, _) => RefreshStatusBar(_rightPane, _rightStatusBar);

        _leftPane.SelectionChanged += (_, items) =>
        {
            RefreshStatusBar(_leftPane, _leftStatusBar);
            RecordSelection(_leftPane.CurrentLocation, items);
        };
        _rightPane.SelectionChanged += (_, items) =>
        {
            RefreshStatusBar(_rightPane, _rightStatusBar);
            RecordSelection(_rightPane.CurrentLocation, items);
        };

        _leftPane.ItemsEnumerated += (_, _) => RefreshStatusBar(_leftPane, _leftStatusBar);
        _rightPane.ItemsEnumerated += (_, _) => RefreshStatusBar(_rightPane, _rightStatusBar);

        // Click in a pane to activate it
        _leftPane.Activated += (_, _) => SetActive(PaneSide.Left);
        _rightPane.Activated += (_, _) => SetActive(PaneSide.Right);
    }

    private static void RefreshStatusBar(TabbedPaneControl pane, StatusBarControl status)
    {
        var selection = pane.CurrentSelection;
        long selectedSize = 0;
        foreach (var e in selection) selectedSize += e.Size ?? 0;
        status.Update(pane.CurrentItemCount, selection.Count, selectedSize, pane.CurrentLocation.FullPath);
    }

    /// <summary>
    /// On non-empty selection, treat as an active engagement:
    /// the pane's folder becomes "recently visited" and the selected files
    /// (not folders) become "recently accessed". Re-selecting promotes both.
    /// </summary>
    private void RecordSelection(PathRef paneLocation, IReadOnlyList<FileEntry> selection)
    {
        if (selection.Count == 0) return;
        _recentLocations.Visit(paneLocation);
        foreach (var entry in selection)
            if (!entry.IsDirectory)
                _recentFiles.Access(entry.Path);
    }

    // MARK: - Pane management

    private void SetActive(PaneSide side)
    {
        _vm.LeftPane.IsActive = (side == PaneSide.Left);
        _vm.RightPane.IsActive = (side == PaneSide.Right);
        UpdateActivePaneVisual();
    }

    private void SwitchPane()
    {
        _vm.SwitchActivePane();
        ActivePane.Focus();
        UpdateActivePaneVisual();
    }

    private void UpdateActivePaneVisual()
    {
        _leftPane.IsActivePane = _vm.LeftPane.IsActive;
        _rightPane.IsActivePane = _vm.RightPane.IsActive;
    }

    private void SetSplitMode(SplitMode mode)
    {
        _vm.SplitMode = mode;
        switch (mode)
        {
            case SplitMode.Single:
                _splitContainer.Panel2Collapsed = true;
                _rightStatusBar.Visible = false;
                break;
            case SplitMode.Vertical:
                _splitContainer.Panel2Collapsed = false;
                _splitContainer.Orientation = Orientation.Vertical;
                _rightStatusBar.Visible = true;
                break;
            case SplitMode.Horizontal:
                _splitContainer.Panel2Collapsed = false;
                _splitContainer.Orientation = Orientation.Horizontal;
                _rightStatusBar.Visible = true;
                break;
        }
        ResizeStatusBars();
    }

    private void ResizeStatusBars()
    {
        if (_splitContainer.Panel2Collapsed)
        {
            _leftStatusBar.Width = _statusPanel.Width;
        }
        else
        {
            var ratio = (double)_splitContainer.SplitterDistance / Math.Max(_splitContainer.Width, 1);
            _leftStatusBar.Width = (int)(_statusPanel.Width * ratio);
            _rightStatusBar.Width = _statusPanel.Width - _leftStatusBar.Width;
        }
    }

    // MARK: - Command Palette

    private void ShowCommandPalette()
    {
        using var dialog = new CommandPaletteDialog(_vm.CommandRegistry, _vm.KeyBindingResolver);
        dialog.ShowDialog(this);
    }

    // MARK: - Tab management

    private void CloseActiveTab()
    {
        var strip = ActivePane.TabStrip;
        if (strip.ActiveIndex >= 0)
        {
            // TabClosed event will handle cleanup
            strip.RemoveTab(strip.ActiveIndex);
        }
    }

    // MARK: - Cross-pane operations

    private void CopyToOtherPane()
    {
        var sources = ActivePane.CurrentSelection.Select(e => e.Path).ToList();
        if (sources.Count == 0) return;
        var destination = InactivePane.CurrentLocation;
        _vm.CopyToOtherPane(sources, destination);
        _recentLocations.Visit(destination);   // destination is now actively engaged
        InactivePane.RefreshActiveTab();
    }

    private void MoveToOtherPane()
    {
        var sources = ActivePane.CurrentSelection.Select(e => e.Path).ToList();
        if (sources.Count == 0) return;
        var destination = InactivePane.CurrentLocation;
        _vm.MoveToOtherPane(sources, destination);
        _recentLocations.Visit(destination);
        ActivePane.RefreshActiveTab();
        InactivePane.RefreshActiveTab();
    }

    // MARK: - View options

    private void SetNavigationTreeVisible(bool visible)
    {
        _leftPane.ShowNavigationTree = visible;
        _rightPane.ShowNavigationTree = visible;
    }

    private void SetQuickLinksVisible(bool visible)
    {
        _sidebarSplit.Panel1Collapsed = !visible;
    }

    private void SyncCheckedMenuItem(string textPrefix, bool newState)
    {
        foreach (ToolStripMenuItem item in _menuStrip.Items)
            foreach (var sub in item.DropDownItems)
                if (sub is ToolStripMenuItem tsmi && (tsmi.Text?.StartsWith(textPrefix, StringComparison.Ordinal) == true))
                    tsmi.Checked = newState;
    }

    // MARK: - Drop Stack / Batch Rename

    private void AddSelectionToDropStack()
    {
        var selection = ActivePane.CurrentSelection;
        if (selection.Count == 0) return;
        _vm.DropStack.Add(selection.Select(e => e.Path).ToList());
    }

    private void ShowBatchRename()
    {
        var selection = ActivePane.CurrentSelection;
        if (selection.Count == 0)
        {
            MessageBox.Show(this, "Select one or more items first.", "Batch Rename",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new BatchRenameDialog(selection, _fileOps);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            ActivePane.RefreshActiveTab();
    }

    // MARK: - Navigation

    private void GoBack() => ActivePane.ActivePaneOrNull?.GoBack();
    private void GoForward() => ActivePane.ActivePaneOrNull?.GoForward();
    private void GoUp() => ActivePane.ActivePaneOrNull?.GoUp();

    private void NavigateTo(SpecialFolder folder)
    {
        ActivePane.NavigateActiveTab(_shellService.GetSpecialFolder(folder));
    }

    // MARK: - Keyboard

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Tab:
                SwitchPane();
                return true;
            case Keys.F5:
                CopyToOtherPane();
                return true;
            case Keys.F6:
                MoveToOtherPane();
                return true;
            case Keys.Control | Keys.T:
                ActivePane.AddTab(null);
                return true;
            case Keys.Control | Keys.W:
                CloseActiveTab();
                return true;
            case Keys.Control | Keys.Tab:
                ActivePane.TabStrip.ActivateNext();
                return true;
            case Keys.Control | Keys.Shift | Keys.Tab:
                ActivePane.TabStrip.ActivatePrevious();
                return true;
            case Keys.Control | Keys.D1: ActivePane.TabStrip.ActivateByNumber(1); return true;
            case Keys.Control | Keys.D2: ActivePane.TabStrip.ActivateByNumber(2); return true;
            case Keys.Control | Keys.D3: ActivePane.TabStrip.ActivateByNumber(3); return true;
            case Keys.Control | Keys.D4: ActivePane.TabStrip.ActivateByNumber(4); return true;
            case Keys.Control | Keys.D5: ActivePane.TabStrip.ActivateByNumber(5); return true;
            case Keys.Control | Keys.D6: ActivePane.TabStrip.ActivateByNumber(6); return true;
            case Keys.Control | Keys.D7: ActivePane.TabStrip.ActivateByNumber(7); return true;
            case Keys.Control | Keys.D8: ActivePane.TabStrip.ActivateByNumber(8); return true;
            case Keys.Control | Keys.D9: ActivePane.TabStrip.ActivateByNumber(9); return true;
            case Keys.Alt | Keys.Left:
                GoBack();
                return true;
            case Keys.Alt | Keys.Right:
                GoForward();
                return true;
            case Keys.Alt | Keys.Up:
                GoUp();
                return true;
            case Keys.Alt | Keys.Home:
                NavigateTo(SpecialFolder.Home);
                return true;
            case Keys.Control | Keys.L:
            case Keys.F4:
                ActivePane.FocusAddressBar();
                return true;
            case Keys.Control | Keys.R:
                ActivePane.RefreshActiveTab();
                return true;
            case Keys.Control | Keys.Shift | Keys.P:
                ShowCommandPalette();
                return true;
            case Keys.Control | Keys.M:
                ShowBatchRename();
                return true;
            case Keys.Control | Keys.D:
                AddSelectionToDropStack();
                return true;
            case Keys.Control | Keys.Shift | Keys.N:
                {
                    var newState = !_leftPane.ShowNavigationTree;
                    SetNavigationTreeVisible(newState);
                    SyncCheckedMenuItem("Show Pane &Navigation Tree", newState);
                    return true;
                }
            case Keys.Control | Keys.Shift | Keys.L:
                {
                    var newState = _sidebarSplit.Panel1Collapsed;  // toggle: if collapsed, will show
                    SetQuickLinksVisible(newState);
                    SyncCheckedMenuItem("Show &Quick Links Sidebar", newState);
                    return true;
                }
        }

        // Configurable shortcuts via KeyBindingResolver
        var chord = KeysToChord(keyData);
        if (chord is not null)
        {
            var commandId = _vm.KeyBindingResolver.Resolve(chord);
            if (commandId is not null && _vm.CommandRegistry.CanExecute(commandId))
            {
                _vm.CommandRegistry.Execute(commandId);
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private static KeyChord? KeysToChord(Keys keyData)
    {
        var modifiers = keyData & Keys.Modifiers;
        var key = keyData & Keys.KeyCode;
        if (key == Keys.None) return null;
        return new KeyChord(
            key.ToString(),
            ctrl: modifiers.HasFlag(Keys.Control),
            shift: modifiers.HasFlag(Keys.Shift),
            alt: modifiers.HasFlag(Keys.Alt)
        );
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        var scale = DeviceDpi / 96f;
        var area = Screen.PrimaryScreen?.WorkingArea ?? Screen.FromHandle(Handle).WorkingArea;
        MinimumSize = new Size((int)(720 * scale), (int)(480 * scale));

        // Scale chrome heights/widths so they aren't clipped at high DPI.
        _bookmarkBar.Height = (int)(30 * scale);
        _statusPanel.Height = (int)(30 * scale);
        _leftStatusBar.ApplyScale(scale);
        _rightStatusBar.ApplyScale(scale);
        _dropStack.ApplyScale(scale);
        _leftPane.ScaleChrome(scale);
        _rightPane.ScaleChrome(scale);
        _splitContainer.SplitterWidth = Math.Max(4, (int)(6 * scale));
        if (_quickLinks is not null)
            _quickLinks.ApplyScale(scale);

        RestoreWindowState(scale, area);
    }

    /// <summary>
    /// Apply saved window bounds, splitter positions, and visibility states.
    /// On first launch (no saved settings), fall back to centered default size.
    /// </summary>
    private void RestoreWindowState(float scale, Rectangle area)
    {
        // Window bounds
        var savedW = _settings.Get("session.window.width", -1);
        var savedH = _settings.Get("session.window.height", -1);
        var savedX = _settings.Get("session.window.x", int.MinValue);
        var savedY = _settings.Get("session.window.y", int.MinValue);
        var maximized = _settings.Get("session.window.maximized", false);

        if (savedW > 0 && savedH > 0)
        {
            // Clamp to the working area in case the saved bounds are off-screen
            Size = new Size(
                Math.Min(savedW, area.Width),
                Math.Min(savedH, area.Height));
        }
        else
        {
            var preferredW = (int)(1200 * scale);
            var preferredH = (int)(760 * scale);
            Size = new Size(
                Math.Min(preferredW, area.Width - 40),
                Math.Min(preferredH, area.Height - 40));
        }

        if (savedX != int.MinValue && savedY != int.MinValue
            && IsLocationOnAnyScreen(new Point(savedX, savedY)))
        {
            Location = new Point(savedX, savedY);
        }
        else
        {
            Location = new Point(area.X + (area.Width - Width) / 2,
                                 area.Y + (area.Height - Height) / 2);
        }

        if (maximized) WindowState = FormWindowState.Maximized;

        // Splitter distances stored as RATIOS so they scale to the current screen.
        // Defer to BeginInvoke so the SplitContainers have their final size after layout.
        BeginInvoke(new Action(() =>
        {
            var sidebarRatio = _settings.Get("session.split.sidebarRatio", 0.18);
            if (_sidebarSplit.Width > 0)
            {
                var d = (int)Math.Round(sidebarRatio * _sidebarSplit.Width);
                _sidebarSplit.SplitterDistance = Math.Clamp(d, 80, _sidebarSplit.Width - 100);
            }
            var paneRatio = _settings.Get("session.split.panesRatio", 0.5);
            if (_splitContainer.Width > 0)
            {
                var d = (int)Math.Round(paneRatio * _splitContainer.Width);
                _splitContainer.SplitterDistance = Math.Clamp(d, 100, _splitContainer.Width - 100);
            }
        }));

        // Visibility toggles
        var sidebarVisible = _settings.Get("session.visibility.sidebar", true);
        var bookmarkBarVisible = _settings.Get("session.visibility.bookmarkBar", true);
        var dropStackVisible = _settings.Get("session.visibility.dropStack", true);
        var navTreeVisible = _settings.Get("session.visibility.navTree", false);

        if (_quickLinksMenuItem is not null) _quickLinksMenuItem.Checked = sidebarVisible;
        if (_bookmarkBarMenuItem is not null) _bookmarkBarMenuItem.Checked = bookmarkBarVisible;
        if (_dropStackMenuItem is not null) _dropStackMenuItem.Checked = dropStackVisible;
        if (_navTreeMenuItem is not null) _navTreeMenuItem.Checked = navTreeVisible;
    }

    private static bool IsLocationOnAnyScreen(Point pt)
    {
        foreach (var screen in Screen.AllScreens)
            if (screen.WorkingArea.Contains(pt)) return true;
        return false;
    }

    private void SaveWindowState()
    {
        // Use the restored bounds (not maximized bounds) if currently maximized
        var saveBounds = WindowState == FormWindowState.Maximized ? RestoreBounds : Bounds;
        _settings.Set("session.window.x", saveBounds.X);
        _settings.Set("session.window.y", saveBounds.Y);
        _settings.Set("session.window.width", saveBounds.Width);
        _settings.Set("session.window.height", saveBounds.Height);
        _settings.Set("session.window.maximized", WindowState == FormWindowState.Maximized);

        // Save splitter positions as RATIOS (0.0–1.0) of their container so they
        // scale correctly when restored on a different screen size or DPI.
        if (_splitContainer.Width > 0)
            _settings.Set("session.split.panesRatio",
                Math.Clamp(_splitContainer.SplitterDistance / (double)_splitContainer.Width, 0.05, 0.95));
        if (_sidebarSplit.Width > 0)
            _settings.Set("session.split.sidebarRatio",
                Math.Clamp(_sidebarSplit.SplitterDistance / (double)_sidebarSplit.Width, 0.05, 0.5));

        _settings.Set("session.visibility.sidebar", !_sidebarSplit.Panel1Collapsed);
        _settings.Set("session.visibility.bookmarkBar", _bookmarkBar.Visible);
        _settings.Set("session.visibility.dropStack", _dropStack.Visible);
        _settings.Set("session.visibility.navTree", _leftPane.ShowNavigationTree);

        // Tabs per pane (paths in order + active index)
        SaveTabs(_leftPane, "session.tabs.left.paths", "session.tabs.left.active");
        SaveTabs(_rightPane, "session.tabs.right.paths", "session.tabs.right.active");

        // Bookmarks + Recent lists
        _settings.Set("bookmarks", _vm.BookmarkStore.ToJson());
        _settings.Set("recent.visited", _recentLocations.ToJson());
        _settings.Set("recent.accessed", _recentFiles.ToJson());
    }

    private void SaveTabs(TabbedPaneControl pane, string pathsKey, string activeKey)
    {
        var paths = pane.TabLocations.Select(p => p.FullPath).ToList();
        _settings.Set(pathsKey, System.Text.Json.JsonSerializer.Serialize(paths));
        _settings.Set(activeKey, pane.ActiveTabIndex);
    }

    /// <summary>Restore tabs from settings, or seed with a default location.</summary>
    private void RestoreOrSeedTabs(TabbedPaneControl pane, string pathsKey, string activeKey, PathRef fallback)
    {
        var savedJson = _settings.Get(pathsKey, "");
        if (string.IsNullOrWhiteSpace(savedJson))
        {
            pane.AddTab(fallback);
            return;
        }

        try
        {
            var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(savedJson) ?? new();
            var refs = paths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => new PathRef(p, isDirectory: true))
                .ToList();

            if (refs.Count == 0) { pane.AddTab(fallback); return; }

            var activeIndex = _settings.Get(activeKey, 0);
            pane.RestoreTabs(refs, activeIndex);
        }
        catch
        {
            pane.AddTab(fallback);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveWindowState();
        _vm.SaveSession();
        base.OnFormClosing(e);
    }
}
