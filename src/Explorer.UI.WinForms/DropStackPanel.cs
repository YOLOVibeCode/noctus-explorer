using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Bottom-docked drop stack panel: a horizontal strip of file references collected from any pane.
/// Drag files in to add; drag chips out to copy them somewhere; right-click a chip to remove.
/// Stale references (file deleted/moved since added) render with a struck-through label.
/// </summary>
public sealed class DropStackPanel : UserControl
{
    private readonly DropStackService _stack;
    private readonly Button _toggleButton;
    private readonly Button _clearButton;
    private readonly Label _countLabel;
    private readonly Panel _headerPanel;
    private readonly FlowLayoutPanel _itemsPanel;
    private bool _collapsed;

    private float _scale = 1f;

    public DropStackPanel(DropStackService stack)
    {
        _stack = stack;
        Dock = DockStyle.Bottom;
        Height = 80;
        BackColor = SystemColors.Control;
        AllowDrop = true;

        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 26,
            BackColor = SystemColors.ControlLight,
        };

        _toggleButton = new Button
        {
            Text = "▾ Drop Stack",
            Dock = DockStyle.Left,
            Width = 110,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        _toggleButton.FlatAppearance.BorderSize = 0;
        _toggleButton.Click += (_, _) => ToggleCollapsed();

        _countLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SystemColors.GrayText,
            Padding = new Padding(0, 0, 8, 0),
        };

        _clearButton = new Button
        {
            Text = "Clear",
            Dock = DockStyle.Right,
            Width = 60,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
        };
        _clearButton.FlatAppearance.BorderSize = 0;
        _clearButton.Click += (_, _) => _stack.Clear();

        _headerPanel.Controls.Add(_countLabel);
        _headerPanel.Controls.Add(_clearButton);
        _headerPanel.Controls.Add(_toggleButton);

        _itemsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(4),
            AllowDrop = true,
        };
        _itemsPanel.DragEnter += OnDragEnter;
        _itemsPanel.DragDrop += OnDragDrop;

        Controls.Add(_itemsPanel);
        Controls.Add(_headerPanel);

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;

        _stack.ItemsChanged += (_, _) => Rebuild();
        Rebuild();
    }

    public bool IsCollapsed => _collapsed;

    public void ToggleCollapsed()
    {
        _collapsed = !_collapsed;
        _itemsPanel.Visible = !_collapsed;
        Height = _collapsed ? (int)(26 * _scale) : (int)(80 * _scale);
        _toggleButton.Text = _collapsed ? "▸ Drop Stack" : "▾ Drop Stack";
    }

    public void ApplyScale(float scale)
    {
        _scale = scale;
        Height = _collapsed ? (int)(26 * scale) : (int)(80 * scale);
        _headerPanel.Height = (int)(28 * scale);
        _toggleButton.Width = (int)(120 * scale);
        _clearButton.Width = (int)(70 * scale);
        _itemsPanel.Padding = new Padding((int)(6 * scale));
    }

    private void Rebuild()
    {
        foreach (Control c in _itemsPanel.Controls)
            c.Dispose();
        _itemsPanel.Controls.Clear();

        foreach (var item in _stack.Items)
            _itemsPanel.Controls.Add(MakeChip(item));

        var n = _stack.Items.Count;
        _countLabel.Text = $"  ({n} item{(n == 1 ? "" : "s")})";
        _clearButton.Enabled = n > 0;
    }

    private Control MakeChip(PathRef item)
    {
        var stale = _stack.IsStale(item);
        var name = string.IsNullOrEmpty(item.DisplayName) ? Path.GetFileName(item.FullPath) : item.DisplayName;

        var chip = new Button
        {
            Text = stale ? name + " (missing)" : name,
            AutoSize = true,
            Height = 36,
            MinimumSize = new Size(100, 36),
            MaximumSize = new Size(220, 36),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f, stale ? FontStyle.Strikeout : FontStyle.Regular),
            ForeColor = stale ? Color.Firebrick : SystemColors.ControlText,
            BackColor = stale ? Color.MistyRose : SystemColors.Window,
            Padding = new Padding(8, 4, 8, 4),
            Margin = new Padding(2),
            Cursor = Cursors.Hand,
            Tag = item,
        };
        chip.FlatAppearance.BorderSize = 1;
        chip.FlatAppearance.BorderColor = SystemColors.ControlDark;

        // Tooltip with full path
        var tip = new ToolTip();
        tip.SetToolTip(chip, item.FullPath + (stale ? "\n(file no longer exists)" : ""));

        chip.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                ShowChipContextMenu(chip, item);
            }
            else if (e.Button == MouseButtons.Left && !stale)
            {
                // Start drag-out
                var data = new DataObject(DataFormats.FileDrop, new[] { item.FullPath });
                chip.DoDragDrop(data, DragDropEffects.Copy | DragDropEffects.Move);
            }
        };

        return chip;
    }

    private void ShowChipContextMenu(Control anchor, PathRef item)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Remove from stack", null, (_, _) => _stack.Remove(item));
        menu.Items.Add("Copy path", null, (_, _) =>
        {
            try { Clipboard.SetText(item.FullPath); } catch { /* clipboard may be busy */ }
        });
        menu.Show(anchor, new Point(0, anchor.Height));
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths || paths.Length == 0)
            return;

        var refs = new List<PathRef>(paths.Length);
        foreach (var p in paths)
        {
            var isDir = Directory.Exists(p);
            refs.Add(new PathRef(p, isDirectory: isDir));
        }
        _stack.Add(refs);
    }
}
