using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace ExplorerSpike;

/// <summary>
/// W0 Spike: Hosts two IExplorerBrowser instances side-by-side in a SplitContainer.
///
/// Acceptance criteria (from ARCHITECTURE.md):
///   [ ] Rubber-band selection works
///   [ ] Double-click navigates into folder
///   [ ] Right-click shows full context menu (including 7-Zip, TortoiseGit if installed)
///   [ ] Drag-drop to/from a real Explorer window works
///   [ ] In-place rename (F2) works
///   [ ] Thumbnails render in medium/large icon view
///   [ ] Column headers sort in Details view
///   [ ] Two IExplorerBrowser instances in the same Form don't conflict
/// </summary>
public class SpikeForm : Form
{
    private ExplorerBrowserPane _leftPane;
    private ExplorerBrowserPane _rightPane;
    private SplitContainer _splitContainer;
    private Label _statusLabel;

    public SpikeForm()
    {
        Text = "Noctus Explorer — W0 Spike (IExplorerBrowser)";
        Width = 1200;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        // Split container for dual pane
        _splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 4,
        };

        // Status bar
        _statusLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = SystemColors.Control,
            Text = "Ready — Tab to switch panes, F2 rename, right-click for context menu",
            Padding = new Padding(8, 0, 0, 0),
        };

        Controls.Add(_splitContainer);
        Controls.Add(_statusLabel);

        // Panes are created after the handle exists (COM needs a window handle)
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Create the two explorer browser panes
        _leftPane = new ExplorerBrowserPane();
        _rightPane = new ExplorerBrowserPane();

        _splitContainer.Panel1.Controls.Add(_leftPane);
        _splitContainer.Panel2.Controls.Add(_rightPane);

        _leftPane.Dock = DockStyle.Fill;
        _rightPane.Dock = DockStyle.Fill;

        // Navigate to different starting locations
        _leftPane.Navigate(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        _rightPane.Navigate(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));

        // Active pane tracking
        _leftPane.GotFocus += (_, _) => SetActivePane(_leftPane);
        _rightPane.GotFocus += (_, _) => SetActivePane(_rightPane);

        _leftPane.BorderStyle = BorderStyle.FixedSingle;
        _rightPane.BorderStyle = BorderStyle.None;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Tab)
        {
            // Switch active pane
            if (_leftPane.BorderStyle == BorderStyle.FixedSingle)
            {
                SetActivePane(_rightPane);
                _rightPane.Focus();
            }
            else
            {
                SetActivePane(_leftPane);
                _leftPane.Focus();
            }
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void SetActivePane(ExplorerBrowserPane pane)
    {
        _leftPane.BorderStyle = pane == _leftPane ? BorderStyle.FixedSingle : BorderStyle.None;
        _rightPane.BorderStyle = pane == _rightPane ? BorderStyle.FixedSingle : BorderStyle.None;
        _statusLabel.Text = $"Active: {(pane == _leftPane ? "Left" : "Right")} pane";
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _leftPane?.Dispose();
        _rightPane?.Dispose();
        base.OnFormClosed(e);
    }
}
