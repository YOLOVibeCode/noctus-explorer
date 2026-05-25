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
    private IReadOnlyList<FileEntry> _currentSelection = [];

    // Per-pane navigation history (browser-style Back/Forward).
    private readonly List<PathRef> _history = [];
    private int _historyPos = -1;
    private bool _navigatingFromHistory;

    public ExplorerBrowserPane(PathRef initialLocation, bool showNavigationTree = false)
    {
        _currentLocation = initialLocation;
        Dock = DockStyle.Fill;

        _browser = new ExplorerBrowser
        {
            Dock = DockStyle.Fill,
        };

        // Hide the navigation tree by default — dual-pane already IS the navigation,
        // and the tree duplicates space inside each pane. User can re-enable via View menu.
        _browser.PaneVisibility.Navigation = showNavigationTree
            ? PaneVisibilityState.Show
            : PaneVisibilityState.Hide;

        _browser.Navigated += OnBrowserNavigated;
        _browser.SelectionChanged += OnBrowserSelectionChanged;
        _browser.ItemsEnumerated += OnBrowserItemsEnumerated;

        Controls.Add(_browser);
    }

    /// <summary>Show or hide the navigation tree inside this pane.</summary>
    public void SetNavigationTreeVisible(bool visible)
    {
        _browser.PaneVisibility.Navigation = visible
            ? PaneVisibilityState.Show
            : PaneVisibilityState.Hide;
        // Re-navigate so the layout picks up the change
        NavigateToPath(_currentLocation);
    }

    public bool IsNavigationTreeVisible
        => _browser.PaneVisibility.Navigation != PaneVisibilityState.Hide;

    public PathRef CurrentLocation => _currentLocation;
    public IReadOnlyList<FileEntry> CurrentSelection => _currentSelection;
    public int ItemCount => _browser.Items?.Count ?? 0;

    public event EventHandler<NavigationEventArgs>? NavigationCompleted;
    public event EventHandler<SelectionEventArgs>? SelectionChanged;
    public event EventHandler? ItemsEnumerated;
    public event EventHandler? HistoryChanged;

    public bool CanGoBack => _historyPos > 0;
    public bool CanGoForward => _historyPos < _history.Count - 1;

    public void GoBack()
    {
        if (!CanGoBack) return;
        _historyPos--;
        _navigatingFromHistory = true;
        NavigateToPath(_history[_historyPos]);
    }

    public void GoForward()
    {
        if (!CanGoForward) return;
        _historyPos++;
        _navigatingFromHistory = true;
        NavigateToPath(_history[_historyPos]);
    }

    public void GoUp()
    {
        var parent = _currentLocation.GetParent();
        if (parent is not null) NavigateAsync(parent);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
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
            // Vanara's ShellItem.Open passes the path to IShellItem via SHParseDisplayName,
            // which requires Windows-native backslashes. PathRef stores forward slashes
            // for cross-platform consistency, so convert at this boundary.
            var winPath = target.FullPath.Replace('/', '\\');
            var item = ShellItem.Open(winPath);
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
            if (location is null) return;

            var path = location.FileSystemPath
                ?? location.GetDisplayName(ShellItemDisplayString.DesktopAbsoluteParsing);

            if (path is not null)
            {
                _currentLocation = new PathRef(path, isDirectory: true);
                _currentSelection = [];

                // Update history: if this navigation was user-initiated (not a Back/Forward),
                // truncate any forward entries and push the new location.
                if (_navigatingFromHistory)
                {
                    _navigatingFromHistory = false;
                }
                else
                {
                    // Skip duplicates (e.g. Refresh of the current location)
                    if (_historyPos < 0 || _history[_historyPos] != _currentLocation)
                    {
                        if (_historyPos < _history.Count - 1)
                            _history.RemoveRange(_historyPos + 1, _history.Count - _historyPos - 1);
                        _history.Add(_currentLocation);
                        _historyPos = _history.Count - 1;
                    }
                }

                NavigationCompleted?.Invoke(this, new NavigationEventArgs { Location = _currentLocation });
                SelectionChanged?.Invoke(this, new SelectionEventArgs { SelectedItems = _currentSelection });
                HistoryChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        catch
        {
            // Virtual shell locations may not have a parsable path
        }
    }

    private void OnBrowserSelectionChanged(object? sender, EventArgs e)
    {
        try
        {
            _currentSelection = ConvertSelection(_browser.SelectedItems);
            SelectionChanged?.Invoke(this, new SelectionEventArgs { SelectedItems = _currentSelection });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Selection read failed: {ex.Message}");
        }
    }

    private void OnBrowserItemsEnumerated(object? sender, EventArgs e)
    {
        ItemsEnumerated?.Invoke(this, EventArgs.Empty);
    }

    private static IReadOnlyList<FileEntry> ConvertSelection(IReadOnlyList<ShellItem>? items)
    {
        if (items is null || items.Count == 0) return [];

        var result = new List<FileEntry>(items.Count);
        foreach (var shellItem in items)
        {
            FileEntry? entry = TryConvert(shellItem);
            if (entry is not null) result.Add(entry);
        }
        return result;
    }

    private static FileEntry? TryConvert(ShellItem shellItem)
    {
        try
        {
            var path = shellItem.FileSystemPath;
            if (string.IsNullOrEmpty(path)) return null;

            var isFolder = shellItem.IsFolder;
            var name = shellItem.Name ?? Path.GetFileName(path);
            var ext = isFolder ? "" : Path.GetExtension(name);

            long? size = null;
            DateTimeOffset modified = default, created = default;
            bool hidden = false, system = false;

            if (isFolder)
            {
                var dirInfo = new DirectoryInfo(path);
                if (dirInfo.Exists)
                {
                    modified = dirInfo.LastWriteTimeUtc;
                    created = dirInfo.CreationTimeUtc;
                    hidden = dirInfo.Attributes.HasFlag(FileAttributes.Hidden);
                    system = dirInfo.Attributes.HasFlag(FileAttributes.System);
                }
            }
            else
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    size = fileInfo.Length;
                    modified = fileInfo.LastWriteTimeUtc;
                    created = fileInfo.CreationTimeUtc;
                    hidden = fileInfo.Attributes.HasFlag(FileAttributes.Hidden);
                    system = fileInfo.Attributes.HasFlag(FileAttributes.System);
                }
            }

            return new FileEntry(
                Path: new PathRef(path, isDirectory: isFolder),
                Name: name,
                Extension: ext,
                Size: size,
                DateModified: modified,
                DateCreated: created,
                IsHidden: hidden,
                IsSystem: system,
                Kind: isFolder ? "Folder" : (string.IsNullOrEmpty(ext) ? "File" : $"{ext.TrimStart('.').ToUpperInvariant()} File"));
        }
        catch
        {
            return null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _browser.Navigated -= OnBrowserNavigated;
            _browser.SelectionChanged -= OnBrowserSelectionChanged;
            _browser.ItemsEnumerated -= OnBrowserItemsEnumerated;
            _browser.Dispose();
        }
        base.Dispose(disposing);
    }
}
