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

    private AddressBarControl _addressBar = null!;
    private BookmarkBarControl _bookmarkBar = null!;
    private TabbedPaneControl _leftPane = null!;
    private TabbedPaneControl _rightPane = null!;
    private SplitContainer _splitContainer = null!;
    private StatusBarControl _leftStatusBar = null!;
    private StatusBarControl _rightStatusBar = null!;
    private Panel _statusPanel = null!;
    private MenuStrip _menuStrip = null!;

    private TabbedPaneControl ActivePane => _vm.LeftPane.IsActive ? _leftPane : _rightPane;
    private TabbedPaneControl InactivePane => _vm.LeftPane.IsActive ? _rightPane : _leftPane;

    public MainForm(MainViewModel vm, IShellService shellService, IFileOperations fileOps)
    {
        _vm = vm;
        _shellService = shellService;
        _fileOps = fileOps;

        Text = "Noctus Explorer";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        BuildMenu();
        BuildLayout();
        WireEvents();
    }

    private void BuildMenu()
    {
        _menuStrip = new MenuStrip();

        // File
        var fileMenu = new ToolStripMenuItem("&File");
        fileMenu.DropDownItems.Add("New &Tab\tCtrl+T", null, (_, _) => ActivePane.AddTab(null));
        fileMenu.DropDownItems.Add("&Close Tab\tCtrl+W", null, (_, _) => CloseActiveTab());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("New &Folder\tF7", null, (_, _) => _vm.CommandRegistry.Execute("file.newFolder"));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("E&xit\tAlt+F4", null, (_, _) => Close());
        _menuStrip.Items.Add(fileMenu);

        // Edit
        var editMenu = new ToolStripMenuItem("&Edit");
        editMenu.DropDownItems.Add("Cu&t\tCtrl+X", null, (_, _) => _vm.CommandRegistry.Execute("edit.cut"));
        editMenu.DropDownItems.Add("&Copy\tCtrl+C", null, (_, _) => _vm.CommandRegistry.Execute("edit.copy"));
        editMenu.DropDownItems.Add("&Paste\tCtrl+V", null, (_, _) => _vm.CommandRegistry.Execute("edit.paste"));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("&Rename\tF2", null, (_, _) => _vm.CommandRegistry.Execute("edit.rename"));
        editMenu.DropDownItems.Add("&Delete\tDel", null, (_, _) => _vm.CommandRegistry.Execute("edit.delete"));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("Copy &Path\tCtrl+Shift+C", null, (_, _) => _vm.CommandRegistry.Execute("edit.copyPath"));
        _menuStrip.Items.Add(editMenu);

        // View
        var viewMenu = new ToolStripMenuItem("&View");
        viewMenu.DropDownItems.Add("&Single Pane", null, (_, _) => SetSplitMode(SplitMode.Single));
        viewMenu.DropDownItems.Add("Dual Pane — &Vertical", null, (_, _) => SetSplitMode(SplitMode.Vertical));
        viewMenu.DropDownItems.Add("Dual Pane — &Horizontal", null, (_, _) => SetSplitMode(SplitMode.Horizontal));
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add("&Refresh\tCtrl+R", null, (_, _) => ActivePane.Refresh());
        viewMenu.DropDownItems.Add(new ToolStripSeparator());
        viewMenu.DropDownItems.Add("Show &Hidden Files\tCtrl+H", null, (_, _) => _vm.CommandRegistry.Execute("view.toggleHidden"));
        _menuStrip.Items.Add(viewMenu);

        // Go
        var goMenu = new ToolStripMenuItem("&Go");
        goMenu.DropDownItems.Add("&Back\tAlt+Left", null, (_, _) => GoBack());
        goMenu.DropDownItems.Add("&Forward\tAlt+Right", null, (_, _) => GoForward());
        goMenu.DropDownItems.Add("&Up\tAlt+Up", null, (_, _) => GoUp());
        goMenu.DropDownItems.Add(new ToolStripSeparator());
        goMenu.DropDownItems.Add("&Home\tAlt+Home", null, (_, _) => NavigateTo(SpecialFolder.Home));
        goMenu.DropDownItems.Add("&Desktop", null, (_, _) => NavigateTo(SpecialFolder.Desktop));
        goMenu.DropDownItems.Add("Do&wnloads", null, (_, _) => NavigateTo(SpecialFolder.Downloads));
        goMenu.DropDownItems.Add("D&ocuments", null, (_, _) => NavigateTo(SpecialFolder.Documents));
        goMenu.DropDownItems.Add(new ToolStripSeparator());
        goMenu.DropDownItems.Add("Go to &Folder…\tCtrl+L", null, (_, _) => _addressBar.EnterEditMode());
        _menuStrip.Items.Add(goMenu);

        // Tools
        var toolsMenu = new ToolStripMenuItem("&Tools");
        toolsMenu.DropDownItems.Add("&Copy to Other Pane\tF5", null, (_, _) => CopyToOtherPane());
        toolsMenu.DropDownItems.Add("&Move to Other Pane\tF6", null, (_, _) => MoveToOtherPane());
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("S&witch Pane\tTab", null, (_, _) => SwitchPane());
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("Command &Palette…\tCtrl+Shift+P", null, (_, _) => _vm.CommandRegistry.Execute("tools.commandPalette"));
        _menuStrip.Items.Add(toolsMenu);

        // Help
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add("&About Noctus Explorer", null, (_, _) =>
            MessageBox.Show("Noctus Explorer v0.3.0\nDual-pane file manager hosting native Windows Explorer views.",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information));
        _menuStrip.Items.Add(helpMenu);

        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);
    }

    private void BuildLayout()
    {
        var homePath = _shellService.GetSpecialFolder(SpecialFolder.Home);
        var desktopPath = _shellService.GetSpecialFolder(SpecialFolder.Desktop);

        // Address bar (top)
        _addressBar = new AddressBarControl();

        // Split container with two explorer panes
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4,
            SplitterDistance = 600,
        };

        _leftPane = new TabbedPaneControl(_shellService) { Dock = DockStyle.Fill };
        _rightPane = new TabbedPaneControl(_shellService) { Dock = DockStyle.Fill };

        _leftPane.AddTab(homePath);
        _rightPane.AddTab(desktopPath);

        _splitContainer.Panel1.Controls.Add(_leftPane);
        _splitContainer.Panel2.Controls.Add(_rightPane);

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

        // Order: status panel (bottom), address bar (top), bookmark bar, split container (fill)
        Controls.Add(_splitContainer);
        Controls.Add(_bookmarkBar);
        Controls.Add(_addressBar);
        Controls.Add(_statusPanel);

        _addressBar.SetPath(homePath);
        UpdateActivePaneVisual();
    }

    private void WireEvents()
    {
        _addressBar.NavigationRequested += (_, path) => ActivePane.NavigateActiveTab(path);

        _leftPane.NavigationCompleted += (_, path) =>
        {
            if (_vm.LeftPane.IsActive)
                _addressBar.SetPath(path);
            _leftStatusBar.Update(0, 0, 0, path.FullPath);
        };

        _rightPane.NavigationCompleted += (_, path) =>
        {
            if (_vm.RightPane.IsActive)
                _addressBar.SetPath(path);
            _rightStatusBar.Update(0, 0, 0, path.FullPath);
        };

        // Click in a pane to activate it
        _leftPane.Activated += (_, _) => SetActive(PaneSide.Left);
        _rightPane.Activated += (_, _) => SetActive(PaneSide.Right);
    }

    // MARK: - Pane management

    private void SetActive(PaneSide side)
    {
        _vm.LeftPane.IsActive = (side == PaneSide.Left);
        _vm.RightPane.IsActive = (side == PaneSide.Right);
        _addressBar.SetPath(ActivePane.CurrentLocation);
        UpdateActivePaneVisual();
    }

    private void SwitchPane()
    {
        _vm.SwitchActivePane();
        _addressBar.SetPath(ActivePane.CurrentLocation);
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
        _vm.CopyToOtherPane();
        InactivePane.RefreshActiveTab();
    }

    private void MoveToOtherPane()
    {
        _vm.MoveToOtherPane();
        ActivePane.RefreshActiveTab();
        InactivePane.RefreshActiveTab();
    }

    // MARK: - Navigation

    private void GoBack()
    {
        var tab = _vm.ActivePane.ActiveTab;
        if (tab?.Navigation.CanGoBack == true)
        {
            var target = tab.Navigation.GoBack();
            ActivePane.NavigateActiveTab(target);
        }
    }

    private void GoForward()
    {
        var tab = _vm.ActivePane.ActiveTab;
        if (tab?.Navigation.CanGoForward == true)
        {
            var target = tab.Navigation.GoForward();
            ActivePane.NavigateActiveTab(target);
        }
    }

    private void GoUp()
    {
        var parent = ActivePane.CurrentLocation.GetParent();
        if (parent is not null)
            ActivePane.NavigateActiveTab(parent);
    }

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
                _addressBar.EnterEditMode();
                return true;
            case Keys.Control | Keys.R:
                ActivePane.RefreshActiveTab();
                return true;
            case Keys.Control | Keys.Shift | Keys.P:
                ShowCommandPalette();
                return true;
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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _vm.SaveSession();
        base.OnFormClosing(e);
    }
}
