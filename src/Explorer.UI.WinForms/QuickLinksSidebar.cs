using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Left-side sidebar with a vertical list of quick-link destinations:
/// special folders (Home, Desktop, Downloads, Documents) + user bookmarks.
/// Click an item to navigate the active pane there. Drag a folder onto the
/// sidebar to add it as a bookmark.
/// </summary>
public sealed class QuickLinksSidebar : UserControl
{
    private readonly BookmarkStore _bookmarks;
    private readonly IShellService _shell;
    private readonly RecentLocationsService _recent;
    private readonly RecentFilesService _recentFiles;
    private readonly Panel _headerPanel;
    private readonly Label _headerLabel;
    private readonly Button _addButton;
    private readonly TreeView _tree;
    private readonly TreeNode _specialNode;
    private readonly TreeNode _recentNode;
    private readonly TreeNode _recentFilesNode;
    private readonly TreeNode _bookmarksNode;

    public event EventHandler<PathRef>? LinkClicked;

    public QuickLinksSidebar(
        BookmarkStore bookmarks,
        IShellService shell,
        RecentLocationsService recent,
        RecentFilesService recentFiles)
    {
        _bookmarks = bookmarks;
        _shell = shell;
        _recent = recent;
        _recentFiles = recentFiles;
        Dock = DockStyle.Fill;
        BackColor = SystemColors.Window;

        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = SystemColors.ControlLight,
        };
        _headerLabel = new Label
        {
            Text = "  Quick Links",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        };
        _addButton = new Button
        {
            Text = "+",
            Dock = DockStyle.Right,
            Width = 28,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        _addButton.FlatAppearance.BorderSize = 0;
        _addButton.Click += (_, _) => AddCurrentFolderPrompt();
        _headerPanel.Controls.Add(_headerLabel);
        _headerPanel.Controls.Add(_addButton);

        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            FullRowSelect = true,
            ShowLines = false,
            ShowRootLines = false,
            ShowPlusMinus = true,
            ShowNodeToolTips = true,
            HideSelection = false,
            Font = new Font("Segoe UI", 9f),
            ItemHeight = 24,
            AllowDrop = true,
        };
        _tree.NodeMouseDoubleClick += (_, e) => HandleNodeActivate(e.Node);
        _tree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                HandleNodeActivate(e.Node);
            else if (e.Button == MouseButtons.Right && e.Node?.Tag is Bookmark bm)
                ShowBookmarkContextMenu(_tree, bm, e.Location);
        };
        _tree.DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Link;
        };
        _tree.DragDrop += OnDragDrop;

        _specialNode = new TreeNode("This PC") { ForeColor = SystemColors.GrayText };
        _recentNode = new TreeNode("Recently Visited") { ForeColor = SystemColors.GrayText };
        _recentFilesNode = new TreeNode("Recently Accessed") { ForeColor = SystemColors.GrayText };
        _bookmarksNode = new TreeNode("Bookmarks") { ForeColor = SystemColors.GrayText };
        _tree.Nodes.Add(_specialNode);
        _tree.Nodes.Add(_recentNode);
        _tree.Nodes.Add(_recentFilesNode);
        _tree.Nodes.Add(_bookmarksNode);

        Controls.Add(_tree);
        Controls.Add(_headerPanel);

        _bookmarks.BookmarksChanged += (_, _) => Rebuild();
        _recent.Changed += (_, _) => RebuildRecent();
        _recentFiles.Changed += (_, _) => RebuildRecentFiles();
        Rebuild();
    }

    public void ApplyScale(float scale)
    {
        _headerPanel.Height = (int)(30 * scale);
        _addButton.Width = (int)(32 * scale);
        _tree.ItemHeight = (int)(26 * scale);
    }

    private void Rebuild()
    {
        _tree.BeginUpdate();
        _specialNode.Nodes.Clear();
        _bookmarksNode.Nodes.Clear();

        foreach (var sf in new[]
        {
            (Label: "Home", Folder: SpecialFolder.Home),
            (Label: "Desktop", Folder: SpecialFolder.Desktop),
            (Label: "Downloads", Folder: SpecialFolder.Downloads),
            (Label: "Documents", Folder: SpecialFolder.Documents),
        })
        {
            var path = _shell.GetSpecialFolder(sf.Folder);
            _specialNode.Nodes.Add(new TreeNode(sf.Label) { Tag = path });
        }

        foreach (var bm in _bookmarks.Bookmarks)
            _bookmarksNode.Nodes.Add(new TreeNode("★ " + bm.Name) { Tag = bm });

        _specialNode.Expand();
        _bookmarksNode.Expand();
        _tree.EndUpdate();

        RebuildRecent();
        RebuildRecentFiles();
    }

    private void RebuildRecent()
    {
        _tree.BeginUpdate();
        _recentNode.Nodes.Clear();
        foreach (var p in _recent.Recent)
        {
            var name = string.IsNullOrEmpty(p.DisplayName) ? Path.GetFileName(p.FullPath) : p.DisplayName;
            if (string.IsNullOrEmpty(name)) name = p.FullPath;
            var node = new TreeNode(name) { Tag = p, ToolTipText = p.FullPath.Replace('/', '\\') };
            _recentNode.Nodes.Add(node);
        }
        _recentNode.Expand();
        _tree.EndUpdate();
    }

    private void RebuildRecentFiles()
    {
        _tree.BeginUpdate();
        _recentFilesNode.Nodes.Clear();
        foreach (var p in _recentFiles.Recent)
        {
            var name = string.IsNullOrEmpty(p.DisplayName) ? Path.GetFileName(p.FullPath) : p.DisplayName;
            if (string.IsNullOrEmpty(name)) name = p.FullPath;
            // Tag the file with its parent dir so clicking navigates the pane there
            // (and lets the user re-select the file). Tooltip shows full file path.
            var parent = p.GetParent();
            var node = new TreeNode(name)
            {
                Tag = parent ?? p,
                ToolTipText = p.FullPath.Replace('/', '\\'),
            };
            _recentFilesNode.Nodes.Add(node);
        }
        _recentFilesNode.Expand();
        _tree.EndUpdate();
    }

    private void HandleNodeActivate(TreeNode? node)
    {
        if (node?.Tag is PathRef path)
            LinkClicked?.Invoke(this, path);
        else if (node?.Tag is Bookmark bm)
            LinkClicked?.Invoke(this, bm.Target);
    }

    private void ShowBookmarkContextMenu(Control anchor, Bookmark bm, Point pos)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Remove", null, (_, _) => _bookmarks.Remove(bm.Id));
        menu.Items.Add("Rename…", null, (_, _) =>
        {
            var name = Microsoft.VisualBasic.Interaction.InputBox("Bookmark name:", "Rename", bm.Name);
            if (!string.IsNullOrEmpty(name))
            {
                _bookmarks.Remove(bm.Id);
                _bookmarks.Add(bm with { Name = name });
            }
        });
        menu.Show(anchor, pos);
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths) return;
        foreach (var p in paths)
        {
            if (!Directory.Exists(p)) continue;
            var name = Path.GetFileName(p);
            if (string.IsNullOrEmpty(name)) name = p;
            _bookmarks.Add(new Bookmark(Guid.NewGuid(), name,
                new PathRef(p, isDirectory: true), null, _bookmarks.Bookmarks.Count));
        }
    }

    private void AddCurrentFolderPrompt()
    {
        var input = Microsoft.VisualBasic.Interaction.InputBox(
            "Folder path:", "Add Quick Link", "");
        if (string.IsNullOrWhiteSpace(input)) return;
        var expanded = Environment.ExpandEnvironmentVariables(input);
        if (!Directory.Exists(expanded))
        {
            MessageBox.Show(this, $"Folder not found:\n{expanded}", "Add Quick Link",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var name = Path.GetFileName(expanded.TrimEnd('\\', '/'));
        if (string.IsNullOrEmpty(name)) name = expanded;
        _bookmarks.Add(new Bookmark(Guid.NewGuid(), name,
            new PathRef(expanded, isDirectory: true), null, _bookmarks.Bookmarks.Count));
    }
}
