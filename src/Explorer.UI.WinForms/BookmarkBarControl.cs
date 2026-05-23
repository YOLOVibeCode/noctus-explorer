using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Horizontal bookmark bar below the address bar. Click to navigate.
/// Supports drag-to-add and right-click to remove/rename.
/// </summary>
public sealed class BookmarkBarControl : UserControl
{
    private readonly BookmarkStore _store;
    private readonly FlowLayoutPanel _flow;

    public event EventHandler<PathRef>? BookmarkClicked;

    public BookmarkBarControl(BookmarkStore store)
    {
        _store = store;
        Height = 28;
        Dock = DockStyle.Top;
        BackColor = SystemColors.Control;
        AllowDrop = true;

        _flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(4, 2, 4, 2),
        };

        Controls.Add(_flow);

        _store.BookmarksChanged += (_, _) => Rebuild();
        Rebuild();

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.Text) == true ||
                e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Link;
        };

        DragDrop += (_, e) =>
        {
            string? path = null;
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                path = files[0];
            else if (e.Data?.GetData(DataFormats.Text) is string text)
                path = text;

            if (path is not null && Directory.Exists(path))
            {
                var name = Path.GetFileName(path);
                if (string.IsNullOrEmpty(name)) name = path;
                _store.Add(new Bookmark(Guid.NewGuid(), name, new PathRef(path, isDirectory: true), null, _store.Bookmarks.Count));
            }
        };
    }

    private void Rebuild()
    {
        _flow.Controls.Clear();

        // Group bookmarks
        var ungrouped = _store.Bookmarks.Where(b => b.Group is null).ToList();
        var groups = _store.Bookmarks.Where(b => b.Group is not null)
            .GroupBy(b => b.Group!)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var bm in ungrouped)
            _flow.Controls.Add(MakeButton(bm));

        foreach (var (groupName, bookmarks) in groups)
        {
            var dropDown = new ToolStripDropDownButton(groupName + " ▾");
            foreach (var bm in bookmarks)
            {
                var item = new ToolStripMenuItem(bm.Name);
                var target = bm.Target;
                item.Click += (_, _) => BookmarkClicked?.Invoke(this, target);
                dropDown.DropDownItems.Add(item);
            }
            var host = new ToolStripControlHost(new Label { Text = groupName + " ▾", AutoSize = true, Cursor = Cursors.Hand });
            _flow.Controls.Add(MakeGroupButton(groupName, bookmarks));
        }
    }

    private Button MakeButton(Bookmark bm)
    {
        var btn = new Button
        {
            Text = "★ " + bm.Name,
            AutoSize = true,
            Height = 22,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 0, 2, 0),
            Tag = bm,
        };
        btn.FlatAppearance.BorderSize = 0;

        btn.Click += (_, _) => BookmarkClicked?.Invoke(this, bm.Target);

        btn.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
                ShowContextMenu(btn, bm, e.Location);
        };

        return btn;
    }

    private Button MakeGroupButton(string groupName, List<Bookmark> bookmarks)
    {
        var btn = new Button
        {
            Text = "▾ " + groupName,
            AutoSize = true,
            Height = 22,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Margin = new Padding(2, 0, 2, 0),
        };
        btn.FlatAppearance.BorderSize = 0;

        btn.Click += (_, _) =>
        {
            var menu = new ContextMenuStrip();
            foreach (var bm in bookmarks)
            {
                var target = bm.Target;
                menu.Items.Add(bm.Name, null, (_, _) => BookmarkClicked?.Invoke(this, target));
            }
            menu.Show(btn, new Point(0, btn.Height));
        };

        return btn;
    }

    private void ShowContextMenu(Control anchor, Bookmark bm, Point location)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Rename…", null, (_, _) =>
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox("Bookmark name:", "Rename", bm.Name);
            if (!string.IsNullOrEmpty(input))
            {
                _store.Remove(bm.Id);
                _store.Add(bm with { Name = input });
            }
        });
        menu.Items.Add("Remove", null, (_, _) => _store.Remove(bm.Id));
        menu.Show(anchor, location);
    }
}
