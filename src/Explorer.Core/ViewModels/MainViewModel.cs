using NoctusExplorer.Core.Abstractions;
using NoctusExplorer.Core.Models;
using NoctusExplorer.Core.Services;
using ReactiveUI;

namespace NoctusExplorer.Core.ViewModels;

public class MainViewModel : ReactiveObject
{
    private readonly IFileOperations _fileOps;
    private readonly SettingsStore _settings;
    private SplitMode _splitMode = SplitMode.Vertical;
    private double _splitRatio = 0.5;

    public MainViewModel(
        IShellService shellService,
        IFileOperations fileOps,
        IFileWatcher fileWatcher,
        SettingsStore settings,
        CommandRegistry commandRegistry,
        KeyBindingResolver keyBindingResolver,
        BookmarkStore bookmarkStore,
        CustomActionStore customActionStore,
        DropStackService dropStack,
        OperationsQueue operationsQueue)
    {
        _fileOps = fileOps;
        _settings = settings;

        LeftPane = new PaneViewModel(shellService, fileWatcher);
        RightPane = new PaneViewModel(shellService, fileWatcher);

        CommandRegistry = commandRegistry;
        KeyBindingResolver = keyBindingResolver;
        BookmarkStore = bookmarkStore;
        CustomActionStore = customActionStore;
        DropStack = dropStack;
        OperationsQueue = operationsQueue;

        // Left pane starts active
        LeftPane.IsActive = true;
    }

    public PaneViewModel LeftPane { get; }
    public PaneViewModel RightPane { get; }

    public PaneViewModel ActivePane => LeftPane.IsActive ? LeftPane : RightPane;
    public PaneViewModel InactivePane => LeftPane.IsActive ? RightPane : LeftPane;

    public SplitMode SplitMode
    {
        get => _splitMode;
        set => this.RaiseAndSetIfChanged(ref _splitMode, value);
    }

    public double SplitRatio
    {
        get => _splitRatio;
        set => this.RaiseAndSetIfChanged(ref _splitRatio, value);
    }

    public CommandRegistry CommandRegistry { get; }
    public KeyBindingResolver KeyBindingResolver { get; }
    public BookmarkStore BookmarkStore { get; }
    public CustomActionStore CustomActionStore { get; }
    public DropStackService DropStack { get; }
    public OperationsQueue OperationsQueue { get; }

    public void SwitchActivePane()
    {
        LeftPane.IsActive = !LeftPane.IsActive;
        RightPane.IsActive = !RightPane.IsActive;
        this.RaisePropertyChanged(nameof(ActivePane));
        this.RaisePropertyChanged(nameof(InactivePane));
    }

    public void ToggleSplitMode()
    {
        SplitMode = SplitMode switch
        {
            SplitMode.Single => SplitMode.Vertical,
            SplitMode.Vertical => SplitMode.Horizontal,
            SplitMode.Horizontal => SplitMode.Single,
            _ => SplitMode.Vertical
        };
    }

    public void CopyToOtherPane()
    {
        var selection = ActivePane.ActiveTab?.Selection;
        var destination = InactivePane.ActiveTab?.Location;
        if (selection is null || selection.Count == 0 || destination is null) return;

        CopyToOtherPane(selection.Select(e => e.Path).ToList(), destination);
    }

    public void CopyToOtherPane(IReadOnlyList<PathRef> sources, PathRef destination)
    {
        if (sources.Count == 0) return;
        var handle = _fileOps.Copy(sources, destination);
        OperationsQueue.Enqueue(handle);
    }

    public void MoveToOtherPane()
    {
        var selection = ActivePane.ActiveTab?.Selection;
        var destination = InactivePane.ActiveTab?.Location;
        if (selection is null || selection.Count == 0 || destination is null) return;

        MoveToOtherPane(selection.Select(e => e.Path).ToList(), destination);
    }

    public void MoveToOtherPane(IReadOnlyList<PathRef> sources, PathRef destination)
    {
        if (sources.Count == 0) return;
        var handle = _fileOps.Move(sources, destination);
        OperationsQueue.Enqueue(handle);
    }

    public async Task SyncNavigationAsync(CancellationToken ct = default)
    {
        var activeLocation = ActivePane.ActiveTab?.Location;
        if (activeLocation is null || InactivePane.ActiveTab is null) return;
        await InactivePane.ActiveTab.NavigateAsync(activeLocation, ct);
    }

    public void SaveSession()
    {
        _settings.Set("session.splitMode", SplitMode.ToString());
        _settings.Set("session.splitRatio", SplitRatio);
        // Tab state serialization would be expanded in full implementation
    }

    public void RestoreSession()
    {
        var modeStr = _settings.Get("session.splitMode", "Vertical");
        if (Enum.TryParse<SplitMode>(modeStr, out var mode))
            SplitMode = mode;
        SplitRatio = _settings.Get("session.splitRatio", 0.5);
    }
}
