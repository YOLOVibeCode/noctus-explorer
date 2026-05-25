using NoctusExplorer.Core.Models;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Address bar with breadcrumb display and text-edit mode.
/// Ctrl+L or F4 switches to edit mode. Enter navigates. Escape cancels.
/// </summary>
public sealed class AddressBarControl : UserControl
{
    private readonly Panel _navButtonPanel;
    private readonly Button _backButton;
    private readonly Button _forwardButton;
    private readonly Button _upButton;
    private readonly Panel _breadcrumbPanel;
    private readonly TextBox _editBox;
    private readonly ToolTip _tip = new() { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 6000 };
    private bool _isEditing;

    public event EventHandler<PathRef>? NavigationRequested;
    public event EventHandler? GoBackRequested;
    public event EventHandler? GoForwardRequested;
    public event EventHandler? GoUpRequested;

    public AddressBarControl()
    {
        Height = 32;
        Dock = DockStyle.Top;
        BackColor = SystemColors.Window;
        Padding = new Padding(4, 2, 4, 2);

        _navButtonPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 90,
            BackColor = SystemColors.Window,
        };

        // Segoe MDL2 Assets — the icon font Windows Explorer uses for its own nav buttons.
        // Back = U+E72B, Forward = U+E72A, Up = U+E70E.
        _backButton = MakeNavButton("", "Back — go to the previous folder in this pane's history (Alt+Left)");
        _backButton.Click += (_, _) => GoBackRequested?.Invoke(this, EventArgs.Empty);
        _forwardButton = MakeNavButton("", "Forward — go to the next folder in this pane's history (Alt+Right)");
        _forwardButton.Click += (_, _) => GoForwardRequested?.Invoke(this, EventArgs.Empty);
        _upButton = MakeNavButton("", "Up — go to the parent folder (Alt+Up)");
        _upButton.Click += (_, _) => GoUpRequested?.Invoke(this, EventArgs.Empty);

        // FlowLayout left-to-right
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
        };
        _backButton.Enabled = false;
        _forwardButton.Enabled = false;
        flow.Controls.Add(_backButton);
        flow.Controls.Add(_forwardButton);
        flow.Controls.Add(_upButton);
        _navButtonPanel.Controls.Add(flow);

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
        Controls.Add(_navButtonPanel);
    }

    private Button MakeNavButton(string glyph, string tip)
    {
        var b = new Button
        {
            Text = glyph,
            Width = 24,
            Height = 22,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe MDL2 Assets", 9f),
            ForeColor = SystemColors.GrayText,
            BackColor = SystemColors.Window,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 3, 1, 3),
            Padding = new Padding(0),
            TabStop = false,
            UseVisualStyleBackColor = false,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = SystemColors.ControlLight;
        b.FlatAppearance.MouseDownBackColor = SystemColors.ControlLightLight;
        b.EnabledChanged += (_, _) =>
            b.ForeColor = b.Enabled ? SystemColors.ControlText : SystemColors.GrayText;
        _tip.SetToolTip(b, tip);
        return b;
    }

    /// <summary>Update the enabled/disabled state of the Back/Forward buttons.</summary>
    public void SetNavigationState(bool canGoBack, bool canGoForward)
    {
        _backButton.Enabled = canGoBack;
        _forwardButton.Enabled = canGoForward;
    }

    public void ApplyScale(float scale)
    {
        var bh = (int)(22 * scale);
        var bw = (int)(24 * scale);
        foreach (Control c in new[] { _backButton, _forwardButton, _upButton })
        {
            c.Width = bw;
            c.Height = bh;
        }
        _navButtonPanel.Width = (int)(82 * scale);
    }

    public void SetPath(PathRef path)
    {
        RebuildBreadcrumbs(path);
        _editBox.Text = path.FullPath.Replace('/', System.IO.Path.DirectorySeparatorChar);
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
