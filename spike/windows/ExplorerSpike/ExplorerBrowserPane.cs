using Vanara.PInvoke;
using Vanara.Windows.Shell;

namespace ExplorerSpike;

/// <summary>
/// A UserControl that hosts a single IExplorerBrowser COM instance.
/// This is the real Windows Explorer view — not a reimplementation.
///
/// The Vanara library's ExplorerBrowser class handles the COM lifecycle:
///   - CoCreateInstance of CLSID_ExplorerBrowser
///   - Initialize with the control's HWND
///   - BrowseToIDList / BrowseToObject for navigation
///   - IExplorerBrowserEvents for navigation/selection callbacks
///   - Proper COM release on disposal
/// </summary>
public class ExplorerBrowserPane : UserControl
{
    private ExplorerBrowser? _browser;

    public ExplorerBrowserPane()
    {
        // The ExplorerBrowser is created on first handle creation
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (_browser is not null) return;

        _browser = new ExplorerBrowser();

        // Configure before initializing
        _browser.ContentFlags =
            ExplorerBrowserContentSectionOptions.NoWebView |
            ExplorerBrowserContentSectionOptions.NoHeaderInAllViews;

        _browser.NavigationFlags =
            ExplorerBrowserNavigateOptions.ShowFrames;

        // Initialize the browser inside this control's window
        _browser.Initialize(Handle, ClientRectangle);
    }

    /// <summary>
    /// Navigate to a filesystem path.
    /// </summary>
    public void Navigate(string path)
    {
        if (_browser is null) return;

        try
        {
            using var item = ShellItem.Open(path);
            _browser.Navigate(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Navigate to a ShellItem (for special folders, virtual items).
    /// </summary>
    public void Navigate(ShellItem item)
    {
        _browser?.Navigate(item);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        // The Vanara ExplorerBrowser handles resize internally
        // if it was initialized with the control's handle
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _browser?.Dispose();
            _browser = null;
        }
        base.Dispose(disposing);
    }
}
