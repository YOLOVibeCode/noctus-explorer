using NoctusExplorer.Core.Models;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Address bar with breadcrumb display and text-edit mode.
/// Ctrl+L or F4 switches to edit mode. Enter navigates. Escape cancels.
/// </summary>
public sealed class AddressBarControl : UserControl
{
    private readonly Panel _breadcrumbPanel;
    private readonly TextBox _editBox;
    private bool _isEditing;

    public event EventHandler<PathRef>? NavigationRequested;

    public AddressBarControl()
    {
        Height = 32;
        Dock = DockStyle.Top;
        BackColor = SystemColors.Window;
        Padding = new Padding(4, 2, 4, 2);

        _breadcrumbPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Cursor = Cursors.Hand,
        };
        _breadcrumbPanel.Click += (_, _) => EnterEditMode();

        _editBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.None,
            Visible = false,
        };
        _editBox.KeyDown += EditBox_KeyDown;
        _editBox.LostFocus += (_, _) => ExitEditMode();

        Controls.Add(_editBox);
        Controls.Add(_breadcrumbPanel);
    }

    public void SetPath(PathRef path)
    {
        RebuildBreadcrumbs(path);
        _editBox.Text = path.FullPath;
    }

    public void EnterEditMode()
    {
        _isEditing = true;
        _breadcrumbPanel.Visible = false;
        _editBox.Visible = true;
        _editBox.SelectAll();
        _editBox.Focus();
    }

    public void ExitEditMode()
    {
        _isEditing = false;
        _editBox.Visible = false;
        _breadcrumbPanel.Visible = true;
    }

    private void EditBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            var text = _editBox.Text.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                var expanded = Environment.ExpandEnvironmentVariables(text);
                if (Directory.Exists(expanded))
                {
                    NavigationRequested?.Invoke(this, new PathRef(expanded, isDirectory: true));
                    ExitEditMode();
                }
            }
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            ExitEditMode();
        }
    }

    private void RebuildBreadcrumbs(PathRef path)
    {
        _breadcrumbPanel.Controls.Clear();

        var segments = path.FullPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        int x = 4;

        for (int i = 0; i < segments.Length; i++)
        {
            var segmentIndex = i;
            var fullSegmentPath = string.Join(Path.DirectorySeparatorChar.ToString(),
                segments.Take(segmentIndex + 1));

            // Add drive letter colon back for Windows paths
            if (segmentIndex == 0 && fullSegmentPath.Length == 1 && char.IsLetter(fullSegmentPath[0]))
                fullSegmentPath += ":";

            if (!fullSegmentPath.StartsWith('/') && !fullSegmentPath.Contains(':'))
                fullSegmentPath = "/" + fullSegmentPath;

            var btn = new LinkLabel
            {
                Text = segments[i],
                AutoSize = true,
                Location = new Point(x, 6),
                Font = new Font("Segoe UI", 9f),
                LinkBehavior = LinkBehavior.HoverUnderline,
                Tag = fullSegmentPath,
            };
            btn.LinkClicked += (s, _) =>
            {
                if (s is LinkLabel lbl && lbl.Tag is string p)
                    NavigationRequested?.Invoke(this, new PathRef(p, isDirectory: true));
            };

            _breadcrumbPanel.Controls.Add(btn);
            x += btn.Width + 4;

            // Separator
            if (i < segments.Length - 1)
            {
                var sep = new Label
                {
                    Text = "›",
                    AutoSize = true,
                    Location = new Point(x, 6),
                    Font = new Font("Segoe UI", 9f),
                    ForeColor = SystemColors.GrayText,
                };
                _breadcrumbPanel.Controls.Add(sep);
                x += sep.Width + 4;
            }
        }
    }
}
