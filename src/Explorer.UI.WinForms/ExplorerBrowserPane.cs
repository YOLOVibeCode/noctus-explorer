using NoctusExplorer.Core.Models;
using NoctusExplorer.UI.Contracts;
using Vanara.Windows.Forms;
using Vanara.Windows.Shell;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Hosts a single IExplorerBrowser COM instance via Vanara.
/// Implements IPaneView so the rest of the app can drive it through the contract.
/// </summary>
public sealed class ExplorerBrowserPane : UserControl, IPaneView
{
    private ExplorerBrowser? _browser;
    private PathRef _currentLocation;

    public ExplorerBrowserPane(PathRef initialLocation)
    {
        _currentLocation = initialLocation;
        Dock = DockStyle.Fill;
    }

    // IPaneView
    public PathRef CurrentLocation => _currentLocation;
    public IReadOnlyList<FileEntry> CurrentSelection => []; // TODO: read from IFolderView2

    public event EventHandler<NavigationEventArgs>? NavigationCompleted;
    public event EventHandler<SelectionEventArgs>? SelectionChanged;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (_browser is not null) return;

        _browser = new ExplorerBrowser();
        _browser.ContentFlags =
            ExplorerBrowserContentSectionOptions.NoWebView |
            ExplorerBrowserContentSectionOptions.NoHeaderInAllViews;
        _browser.NavigationFlags = ExplorerBrowserNavigateOptions.ShowFrames;

        _browser.Initialize(Handle, ClientRectangle);

        // Wire navigation events
        _browser.Navigated += (_, args) =>
        {
            if (args.NewLocation is ShellItem item)
            {
                var path = item.FileSystemPath ?? item.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);
                if (path is not null)
                {
                    _currentLocation = new PathRef(path, isDirectory: true);
                    NavigationCompleted?.Invoke(this, new NavigationEventArgs { Location = _currentLocation });
                }
            }
        };

        // Navigate to initial location
        NavigateToPath(_currentLocation);
    }

    public Task NavigateAsync(PathRef target)
    {
        NavigateToPath(target);
        return Task.CompletedTask;
    }

    public void Refresh()
    {
        // Re-navigate to current location to refresh
        NavigateToPath(_currentLocation);
    }

    void IPaneView.Focus()
    {
        base.Focus();
    }

    private void NavigateToPath(PathRef target)
    {
        if (_browser is null) return;

        try
        {
            using var item = ShellItem.Open(target.FullPath);
            _browser.Navigate(item);
            _currentLocation = target;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
        }
    }

    public new void Dispose()
    {
        _browser?.Dispose();
        _browser = null;
        base.Dispose();
    }

    void IDisposable.Dispose() => Dispose();
}
