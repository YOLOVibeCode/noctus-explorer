using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using NoctusExplorer.Core.ViewModels;
using NoctusExplorer.Core.Abstractions;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Main application window. Hosts address bar, single explorer pane, and status bar.
/// Driven by MainViewModel. Menu bar wired to CommandRegistry.
/// </summary>
public sealed class MainForm : Form
{
    private readonly MainViewModel _vm;
    private readonly IShellService _shellService;

    private AddressBarControl _addressBar = null!;
    private ExplorerBrowserPane _pane = null!;
    private StatusBarControl _statusBar = null!;
    private MenuStrip _menuStrip = null!;

    public MainForm(MainViewModel vm, IShellService shellService)
    {
        _vm = vm;
        _shellService = shellService;

        Text = "Noctus Explorer";
        Width = 1000;
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
        viewMenu.DropDownItems.Add("&Refresh\tCtrl+R", null, (_, _) => _pane.Refresh());
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
        toolsMenu.DropDownItems.Add("&Command Palette…\tCtrl+Shift+P", null, (_, _) => _vm.CommandRegistry.Execute("tools.commandPalette"));
        _menuStrip.Items.Add(toolsMenu);

        // Help
        var helpMenu = new ToolStripMenuItem("&Help");
        helpMenu.DropDownItems.Add("&About Noctus Explorer", null, (_, _) =>
            MessageBox.Show("Noctus Explorer v0.2.0\nDual-pane file manager hosting native Windows Explorer views.",
                "About", MessageBoxButtons.OK, MessageBoxIcon.Information));
        _menuStrip.Items.Add(helpMenu);

        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);
    }

    private void BuildLayout()
    {
        var initialPath = _shellService.GetSpecialFolder(SpecialFolder.Home);

        _addressBar = new AddressBarControl();
        _pane = new ExplorerBrowserPane(initialPath);
        _statusBar = new StatusBarControl();

        // Order matters: status bar first (bottom), then address bar (top), then pane (fill)
        Controls.Add(_pane);
        Controls.Add(_addressBar);
        Controls.Add(_statusBar);

        _addressBar.SetPath(initialPath);
        _statusBar.Update(0, 0, 0, initialPath.FullPath);
    }

    private void WireEvents()
    {
        // Address bar navigation
        _addressBar.NavigationRequested += (_, path) =>
        {
            _pane.NavigateAsync(path);
        };

        // Explorer browser navigation completed → update address bar and status
        _pane.NavigationCompleted += (_, args) =>
        {
            _addressBar.SetPath(args.Location);
            _statusBar.Update(0, 0, 0, args.Location.FullPath);
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
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
                _pane.Refresh();
                return true;
        }

        // Route through KeyBindingResolver for configurable shortcuts
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

    private void GoBack()
    {
        var tab = _vm.ActivePane.ActiveTab;
        if (tab?.Navigation.CanGoBack == true)
        {
            var target = tab.Navigation.GoBack();
            _pane.NavigateAsync(target);
        }
    }

    private void GoForward()
    {
        var tab = _vm.ActivePane.ActiveTab;
        if (tab?.Navigation.CanGoForward == true)
        {
            var target = tab.Navigation.GoForward();
            _pane.NavigateAsync(target);
        }
    }

    private void GoUp()
    {
        var parent = _pane.CurrentLocation.GetParent();
        if (parent is not null)
            _pane.NavigateAsync(parent);
    }

    private void NavigateTo(SpecialFolder folder)
    {
        var path = _shellService.GetSpecialFolder(folder);
        _pane.NavigateAsync(path);
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
