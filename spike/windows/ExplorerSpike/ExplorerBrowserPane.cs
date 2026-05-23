using Vanara.Windows.Forms;
using Vanara.Windows.Shell;

namespace ExplorerSpike;

/// <summary>
/// A UserControl hosting the Vanara ExplorerBrowser control
/// (wraps IExplorerBrowser COM — the real Windows Explorer view).
/// </summary>
public class ExplorerBrowserPane : UserControl
{
    private readonly ExplorerBrowser _browser;
    private string? _pendingPath;

    public ExplorerBrowserPane()
    {
        _browser = new ExplorerBrowser { Dock = DockStyle.Fill };
        Controls.Add(_browser);
    }

    public void Navigate(string path)
    {
        if (!IsHandleCreated)
        {
            _pendingPath = path;
            return;
        }

        try
        {
            var item = ShellItem.Open(path);
            _browser.Navigate(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_pendingPath is not null)
        {
            Navigate(_pendingPath);
            _pendingPath = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _browser.Dispose();
        base.Dispose(disposing);
    }
}
