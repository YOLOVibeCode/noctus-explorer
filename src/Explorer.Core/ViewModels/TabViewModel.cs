using System.Collections.ObjectModel;
using System.Reactive.Linq;
using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using ReactiveUI;

namespace NoctusExplorer.Core.ViewModels;

public class TabViewModel : ReactiveObject
{
    private readonly IShellService _shellService;
    private readonly IFileWatcher _fileWatcher;
    private PathRef _location;
    private string _displayName = "";
    private ViewMode _viewMode = ViewMode.Details;
    private SortField _sortField = SortField.Name;
    private SortDirection _sortDirection = SortDirection.Ascending;
    private string _filterText = "";
    private bool _isFilterActive;
    private bool _isPreviewVisible;
    private object? _previewContent;
    private string _selectionSummary = "";

    public TabViewModel(int id, PathRef initialLocation, IShellService shellService, IFileWatcher fileWatcher)
    {
        Id = id;
        _location = initialLocation;
        _shellService = shellService;
        _fileWatcher = fileWatcher;
        Navigation = new NavigationHistory(initialLocation);
        DisplayName = shellService.GetDisplayName(initialLocation);
    }

    public int Id { get; }

    public PathRef Location
    {
        get => _location;
        private set => this.RaiseAndSetIfChanged(ref _location, value);
    }

    public string DisplayName
    {
        get => _displayName;
        private set => this.RaiseAndSetIfChanged(ref _displayName, value);
    }

    public ObservableCollection<FileEntry> Entries { get; } = [];
    public ObservableCollection<FileEntry> Selection { get; } = [];

    public ViewMode ViewMode
    {
        get => _viewMode;
        set => this.RaiseAndSetIfChanged(ref _viewMode, value);
    }

    public SortField SortField
    {
        get => _sortField;
        set => this.RaiseAndSetIfChanged(ref _sortField, value);
    }

    public SortDirection SortDirection
    {
        get => _sortDirection;
        set => this.RaiseAndSetIfChanged(ref _sortDirection, value);
    }

    public string FilterText
    {
        get => _filterText;
        set => this.RaiseAndSetIfChanged(ref _filterText, value);
    }

    public bool IsFilterActive
    {
        get => _isFilterActive;
        private set => this.RaiseAndSetIfChanged(ref _isFilterActive, value);
    }

    public bool IsPreviewVisible
    {
        get => _isPreviewVisible;
        set => this.RaiseAndSetIfChanged(ref _isPreviewVisible, value);
    }

    public object? PreviewContent
    {
        get => _previewContent;
        private set => this.RaiseAndSetIfChanged(ref _previewContent, value);
    }

    public string SelectionSummary
    {
        get => _selectionSummary;
        private set => this.RaiseAndSetIfChanged(ref _selectionSummary, value);
    }

    public NavigationHistory Navigation { get; }

    public async Task NavigateAsync(PathRef target, CancellationToken ct = default)
    {
        var previousLocation = Location;

        var entries = await _shellService.EnumerateAsync(target, ct);
        Location = target;
        DisplayName = _shellService.GetDisplayName(target);
        Navigation.Push(target);

        Entries.Clear();
        foreach (var entry in entries)
            Entries.Add(entry);

        Selection.Clear();
        UpdateSelectionSummary();

        _fileWatcher.Unwatch(previousLocation);
        _fileWatcher.Watch(target);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var entries = await _shellService.EnumerateAsync(Location, ct);
        Entries.Clear();
        foreach (var entry in entries)
            Entries.Add(entry);

        Selection.Clear();
        UpdateSelectionSummary();
    }

    public async Task GoBackAsync(CancellationToken ct = default)
    {
        if (!Navigation.CanGoBack) return;
        var target = Navigation.GoBack();
        await NavigateToWithoutHistory(target, ct);
    }

    public async Task GoForwardAsync(CancellationToken ct = default)
    {
        if (!Navigation.CanGoForward) return;
        var target = Navigation.GoForward();
        await NavigateToWithoutHistory(target, ct);
    }

    public async Task GoUpAsync(CancellationToken ct = default)
    {
        var parent = Location.GetParent();
        if (parent is not null)
            await NavigateAsync(parent, ct);
    }

    public void SetFilter(string text)
    {
        FilterText = text;
        IsFilterActive = !string.IsNullOrEmpty(text);
    }

    public void ClearFilter()
    {
        FilterText = "";
        IsFilterActive = false;
    }

    public void TogglePreview()
    {
        IsPreviewVisible = !IsPreviewVisible;
    }

    public void UpdateSelectionSummary()
    {
        if (Selection.Count == 0)
        {
            SelectionSummary = $"{Entries.Count} items";
        }
        else
        {
            var totalSize = Selection.Sum(e => e.Size ?? 0);
            SelectionSummary = $"{Selection.Count} selected ({FormatSize(totalSize)})";
        }
    }

    private async Task NavigateToWithoutHistory(PathRef target, CancellationToken ct)
    {
        var previousLocation = Location;
        var entries = await _shellService.EnumerateAsync(target, ct);
        Location = target;
        DisplayName = _shellService.GetDisplayName(target);

        Entries.Clear();
        foreach (var entry in entries)
            Entries.Add(entry);

        Selection.Clear();
        UpdateSelectionSummary();

        _fileWatcher.Unwatch(previousLocation);
        _fileWatcher.Watch(target);
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F1} GB"
    };
}
