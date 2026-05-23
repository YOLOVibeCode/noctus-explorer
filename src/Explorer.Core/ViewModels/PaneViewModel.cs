using System.Collections.ObjectModel;
using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using ReactiveUI;

namespace NoctusExplorer.Core.ViewModels;

public class PaneViewModel : ReactiveObject
{
    private readonly IShellService _shellService;
    private readonly IFileWatcher _fileWatcher;
    private TabViewModel? _activeTab;
    private bool _isActive;
    private int _nextTabId;

    public PaneViewModel(IShellService shellService, IFileWatcher fileWatcher)
    {
        _shellService = shellService;
        _fileWatcher = fileWatcher;
    }

    public ObservableCollection<TabViewModel> Tabs { get; } = [];

    public TabViewModel? ActiveTab
    {
        get => _activeTab;
        private set => this.RaiseAndSetIfChanged(ref _activeTab, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public TabViewModel AddTab(PathRef? initialLocation = null)
    {
        var location = initialLocation ?? _shellService.GetSpecialFolder(SpecialFolder.Home);
        var tab = new TabViewModel(_nextTabId++, location, _shellService, _fileWatcher);
        Tabs.Add(tab);
        ActiveTab = tab;
        return tab;
    }

    public void CloseTab(int tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return;

        var idx = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (ActiveTab == tab)
        {
            ActiveTab = Tabs.Count > 0
                ? Tabs[Math.Min(idx, Tabs.Count - 1)]
                : null;
        }
    }

    public void ActivateTab(int tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is not null)
            ActiveTab = tab;
    }

    public void RestoreTabs(IReadOnlyList<TabState> tabStates)
    {
        Tabs.Clear();
        foreach (var state in tabStates)
        {
            var tab = new TabViewModel(state.Id, state.Location, _shellService, _fileWatcher)
            {
                ViewMode = state.ViewMode,
                SortField = state.SortField,
                SortDirection = state.SortDirection,
            };
            Tabs.Add(tab);
            if (state.Id >= _nextTabId)
                _nextTabId = state.Id + 1;
        }

        if (Tabs.Count > 0)
            ActiveTab = Tabs[0];
    }

    public TabViewModel? RemoveAndReturnTab(int tabId)
    {
        var tab = Tabs.FirstOrDefault(t => t.Id == tabId);
        if (tab is null) return null;
        CloseTab(tabId);
        return tab;
    }
}
