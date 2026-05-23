using NoctusExplorer.Core.Models;
using NoctusExplorer.UI.Contracts;
using Vanara.Windows.Forms;
using Vanara.Windows.Shell;

namespace NoctusExplorer.UI.WinForms;

/// <summary>
/// Hosts a single Vanara ExplorerBrowser control (which wraps IExplorerBrowser COM).
/// The ExplorerBrowser is a WinForms Control — we embed it as a child.
/// Implements IPaneView so the rest of the app can drive it through the contract.
/// </summary>
public sealed class ExplorerBrowserPane : UserControl, IPaneView
{
    private readonly ExplorerBrowser _browser;
    private PathRef _currentLocation;

    public ExplorerBrowserPane(PathRef initialLocation)
    {
        _currentLocation = initialLocation;
        Dock = DockStyle.Fill;

        // ExplorerBrowser is a WinForms Control — add it as a child
        _browser = new ExplorerBrowser
        {
            Dock = DockStyle.Fill,
        };

        // Wire navigation events
        _browser.Navigated += OnBrowserNavigated;

        Controls.Add(_browser);
    }

    // IPaneView
    public PathRef CurrentLocation => _currentLocation;
    public IReadOnlyList<FileEntry> CurrentSelection => []; // TODO: read from IFolderView2

    public event EventHandler<NavigationEventArgs>? NavigationCompleted;
    public event EventHandler<SelectionEventArgs>? SelectionChanged;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Navigate to initial location once the handle exists
        NavigateToPath(_currentLocation);
    }

    public Task NavigateAsync(PathRef target)
    {
        NavigateToPath(target);
        return Task.CompletedTask;
    }

    public new void Refresh()
    {
        NavigateToPath(_currentLocation);
    }

    void IPaneView.Focus()
    {
        _browser.Focus();
    }

    private void NavigateToPath(PathRef target)
    {
        if (!IsHandleCreated) return;

        try
        {
            var item = ShellItem.Open(target.FullPath);
            _browser.Navigate(item);
            _currentLocation = target;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Navigation failed: {ex.Message}");
        }
    }

    private void OnBrowserNavigated(object? sender, ExplorerBrowser.NavigatedEventArgs args)
    {
        try
        {
            var location = args.NewLocation;
            var path = location.FileSystemPath
                ?? location.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);

            if (path is not null)
            {
                _currentLocation = new PathRef(path, isDirectory: true);
                NavigationCompleted?.Invoke(this, new NavigationEventArgs { Location = _currentLocation });
            }
        }
        catch
        {
            // Ignore navigation event errors for virtual shell locations
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _browser.Navigated -= OnBrowserNavigated;
            _browser.Dispose();
        }
        base.Dispose(disposing);
    }
}
