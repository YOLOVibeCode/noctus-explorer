# Noctus Explorer — Architecture & Implementation Plan

> **Version:** 0.1 Draft
> **Date:** 2026-05-23
> **Companion to:** SPEC.md (feature specification)

This document defines **how** the application is built — contracts, data flow, state management, threading, persistence, DI wiring, and a step-by-step implementation plan with acceptance criteria for each milestone.

---

## 1. Architectural Principles

1. **The spec is the cross-platform contract.** Windows (C#/.NET) and macOS (Swift/AppKit) are independent codebases. They share no code. They conform to the same spec.
2. **Core is pure.** No platform imports, no UI imports, no COM, no AppKit. Core is unit-testable in isolation.
3. **Dependencies point inward.** UI → Contracts → Core ← Shell Adapter. Nothing in Core references anything outside Core.
4. **The shell adapter is an implementation detail.** Core defines abstractions (interfaces/protocols). The shell adapter implements them. The UI never talks to the shell adapter directly — it goes through Core's view models and services.
5. **The UI is a thin shell.** It binds to view models, routes input to commands, and hosts native views. It does not contain business logic.
6. **State lives in view models.** The UI reads state from view models via reactive bindings. It never stores its own state (except transient layout state like splitter position during a drag).

---

## 2. Core Layer — Detailed Design

### 2.1 Models

All models are plain data objects. No behavior, no dependencies, no platform types.

```
PathRef
├── FullPath: string                    # Canonical absolute path
├── DisplayName: string                 # Filename or display name for virtual items
├── IsDirectory: bool
├── Platform-opaque handle: byte[]?     # Windows: serialized PIDL. Mac: bookmark data. Null for simple paths.
└── Equals/GetHashCode by FullPath

FileEntry
├── Path: PathRef
├── Name: string
├── Extension: string
├── Size: long?                         # Null for directories until calculated
├── DateModified: DateTimeOffset
├── DateCreated: DateTimeOffset
├── IsHidden: bool
├── IsSystem: bool                      # Windows-only concept; always false on Mac
└── Kind: string                        # "Folder", "Document", "Image", etc.

Bookmark
├── Id: Guid
├── Name: string
├── Target: PathRef
├── Group: string?                      # Null = top-level
└── Order: int

TabState
├── Id: int
├── Location: PathRef
├── ViewMode: ViewMode enum             # Icons, List, Details, Columns, Gallery, Tiles
├── ScrollPosition: double?             # Restore scroll on tab switch
└── SortField: SortField enum + SortDirection

PaneState
├── Tabs: List<TabState>
├── ActiveTabId: int
└── IsActive: bool                      # Is this the active pane?

WindowState
├── LeftPane: PaneState
├── RightPane: PaneState?               # Null in single-pane mode
├── SplitMode: SplitMode enum           # Single, Vertical, Horizontal
├── SplitRatio: double
├── WindowBounds: Rectangle
└── DropStackItems: List<PathRef>

CustomAction
├── Id: Guid
├── Label: string
├── Icon: string?                       # Path or system icon identifier
├── Group: string?                      # Submenu group name
├── Order: int
├── Conditions: ActionConditions
│   ├── AppliesTo: FileType enum        # Files, Folders, Both
│   ├── Extensions: string[]?           # Null = all
│   ├── SelectionCount: SelectionCount  # Any, Single, Multiple
│   └── PathContains: string?
├── ActionType: ActionType enum         # RunProgram, ShellCommand, OpenWith, CopyText, OpenUrl
├── ActionConfig: Dictionary<string,string>  # Type-specific config (program path, command, URL template, etc.)
├── RegisterWithOS: bool
└── Enabled: bool
```

### 2.2 Abstractions (interfaces the shell adapter implements)

```csharp
// C# — Explorer.Core/Abstractions/

interface IShellService
{
    Task<IReadOnlyList<FileEntry>> EnumerateAsync(PathRef directory, CancellationToken ct);
    Task<PathRef> ResolveAsync(string path);           // String path → PathRef with PIDL/bookmark
    PathRef GetSpecialFolder(SpecialFolder folder);    // Home, Desktop, Downloads, Trash, Root
    string GetDisplayName(PathRef item);
    bool Exists(PathRef item);
}

interface IFileOperations
{
    // All operations return an OperationHandle for the ops queue
    OperationHandle Copy(IReadOnlyList<PathRef> sources, PathRef destination);
    OperationHandle Move(IReadOnlyList<PathRef> sources, PathRef destination);
    OperationHandle Delete(IReadOnlyList<PathRef> items, bool permanent = false);
    OperationHandle CreateFolder(PathRef parent, string name);
    OperationHandle Rename(PathRef item, string newName);
}

interface IOperationHandle : IDisposable
{
    Guid Id { get; }
    string Description { get; }
    OperationStatus Status { get; }            // Queued, Running, Paused, Completed, Failed, Cancelled
    double Progress { get; }                   // 0.0–1.0
    long BytesTransferred { get; }
    long TotalBytes { get; }
    TimeSpan? EstimatedRemaining { get; }
    
    void Pause();
    void Resume();
    void Cancel();
    
    event EventHandler<OperationProgressEventArgs> ProgressChanged;
    event EventHandler<OperationConflictEventArgs> ConflictEncountered;  // UI handles this
    event EventHandler<OperationCompletedEventArgs> Completed;
}

interface IClipboardService
{
    Task SetFilesAsync(IReadOnlyList<PathRef> items, ClipboardOperation op);  // Copy or Cut
    Task<(IReadOnlyList<PathRef> Items, ClipboardOperation Op)?> GetFilesAsync();
    Task SetTextAsync(string text);
}

interface IPreviewService
{
    bool CanPreview(PathRef item);
    // Returns a platform-opaque preview handle that the UI layer knows how to render
    Task<object> GetPreviewAsync(PathRef item, Size maxSize, CancellationToken ct);
}

interface IFileWatcher : IDisposable
{
    void Watch(PathRef directory);
    void Unwatch(PathRef directory);
    event EventHandler<FileChangeEventArgs> Changed;  // Created, Modified, Deleted, Renamed
}

interface IContextMenuService
{
    // Populates and shows the native context menu, with Noctus items prepended
    void ShowContextMenu(IReadOnlyList<PathRef> items, Point screenPosition, IReadOnlyList<CustomAction> customActions);
}

interface IHashService
{
    Task<string> ComputeHashAsync(PathRef file, HashAlgorithm algo, IProgress<double> progress, CancellationToken ct);
}

interface IOSContextMenuRegistrar
{
    void Register(CustomAction action);
    void Unregister(CustomAction action);
    bool IsRegistered(CustomAction action);
}
```

### 2.3 Services

```
CommandRegistry
├── Register(CommandDefinition)
├── GetAll() → IReadOnlyList<CommandDefinition>
├── GetById(string id) → CommandDefinition?
├── Execute(string id)
└── CanExecute(string id) → bool

KeyBindingResolver
├── LoadBindings(Dictionary<string, KeyChord>)
├── Resolve(KeyChord) → string?              # Returns command ID or null
├── GetBinding(string commandId) → KeyChord?
└── SetBinding(string commandId, KeyChord)

SettingsStore
├── Load(string filePath)                    # Reads JSON, populates in-memory cache
├── Save()                                   # Writes current state to JSON
├── Get<T>(string key, T defaultValue) → T
├── Set<T>(string key, T value)
├── Subscribe(string keyPrefix, Action<string, object> onChange)  # Reactive notification
└── FilePath: string                         # Resolved on startup (portable vs installed)

BookmarkStore
├── Bookmarks: IReadOnlyList<Bookmark>       # Observable
├── Add(Bookmark)
├── Remove(Guid id)
├── Reorder(Guid id, int newIndex)
├── GetGroups() → IReadOnlyList<string>
└── Persistence: delegates to SettingsStore

CustomActionStore
├── Actions: IReadOnlyList<CustomAction>     # Observable
├── Add(CustomAction)
├── Update(CustomAction)
├── Remove(Guid id)
├── Reorder(Guid id, int newIndex)
├── Import(string jsonPath)
├── Export(string jsonPath, IReadOnlyList<Guid> actionIds)
└── Persistence: delegates to SettingsStore

CustomActionEngine
├── Evaluate(CustomAction, IReadOnlyList<FileEntry> selection, PathRef otherPaneLocation) → bool   # Should this action show?
├── Execute(CustomAction, IReadOnlyList<FileEntry> selection, PathRef otherPaneLocation)            # Run the action
└── ExpandVariables(string template, IReadOnlyList<FileEntry> selection, PathRef otherPaneLocation) → string

OperationsQueue
├── Operations: IReadOnlyList<IOperationHandle>  # Observable
├── Enqueue(IOperationHandle)
├── MaxConcurrent: int                           # Default 2
├── PauseAll() / ResumeAll() / CancelAll()
└── Event: OperationAdded, OperationCompleted

DropStackService
├── Items: IReadOnlyList<PathRef>            # Observable
├── Add(IReadOnlyList<PathRef>)
├── Remove(PathRef)
├── Clear()
└── IsStale(PathRef) → bool                  # Check if reference is still valid

NavigationHistory (per-tab)
├── Current: PathRef
├── CanGoBack / CanGoForward: bool
├── GoBack() → PathRef
├── GoForward() → PathRef
├── Push(PathRef)                            # Called on navigation
└── History: IReadOnlyList<PathRef>
```

### 2.4 ViewModels

```
MainViewModel
├── LeftPane: PaneViewModel
├── RightPane: PaneViewModel
├── ActivePane: PaneViewModel               # Points to left or right
├── InactivePane: PaneViewModel             # The other one
├── SplitMode: SplitMode                    # Observable, bound to UI
├── SplitRatio: double                      # Observable
├── DropStack: DropStackService
├── OperationsQueue: OperationsQueue
├── BookmarkStore: BookmarkStore
├── CommandRegistry: CommandRegistry
│
├── ToggleSplitMode()
├── SwitchActivePane()
├── CopyToOtherPane()
├── MoveToOtherPane()
├── SyncNavigation()
└── SaveSession() / RestoreSession()

PaneViewModel
├── Tabs: ObservableList<TabViewModel>
├── ActiveTab: TabViewModel                 # Observable
├── IsActive: bool                          # Observable — drives active pane indicator
│
├── AddTab(PathRef? initialLocation)
├── CloseTab(int tabId)
├── ActivateTab(int tabId)
├── MoveTabToOtherPane(int tabId)
└── RestoreTabs(List<TabState>)

TabViewModel
├── Id: int
├── Location: PathRef                       # Observable — current directory
├── DisplayName: string                     # Observable — derived from Location
├── Entries: ObservableList<FileEntry>       # Current directory contents (observable for UI binding)
├── Selection: ObservableList<FileEntry>     # Currently selected items
├── ViewMode: ViewMode                      # Observable
├── SortField: SortField                    # Observable
├── SortDirection: SortDirection             # Observable
├── FilterText: string                      # Observable — quick filter
├── IsFilterActive: bool
├── Navigation: NavigationHistory
├── IsPreviewVisible: bool                  # Observable
├── PreviewContent: object?                 # Observable — platform-opaque preview data
│
├── NavigateAsync(PathRef target)
├── RefreshAsync()
├── GoBack() / GoForward() / GoUp()
├── SetFilter(string text)
├── ClearFilter()
├── TogglePreview()
└── SelectionSummary: string               # "3 items selected (14.2 MB)" — observable, for status bar
```

### 2.5 Data Flow Diagram

```
User Input (keyboard/mouse)
        │
        ▼
┌─────────────┐    key chord     ┌──────────────────┐
│   UI Layer   │ ───────────────→│ KeyBindingResolver │
│  (WinForms/  │                 └────────┬───────────┘
│   AppKit)    │                    command ID
│              │                          │
│              │    ┌─────────────────────▼─────────────┐
│              │    │         CommandRegistry            │
│              │    │  Looks up command → calls execute  │
│              │    └─────────────────────┬─────────────┘
│              │                          │
│              │           ┌──────────────▼──────────────┐
│              │           │        ViewModel             │
│              │           │  Mutates state (Location,    │
│              │           │  Selection, SplitMode, etc.) │
│              │           └──────────────┬──────────────┘
│              │                          │
│              │              reactive binding / observation
│              │                          │
│              │◄─────────────────────────┘
│  UI updates  │
│  (data-bound │
│   controls)  │
└──────┬───────┘
       │ when navigation changes
       ▼
┌──────────────┐                 ┌────────────────┐
│  Shell Adapter│ ◄──────────────│  IFileWatcher   │
│  (enumerate,  │   file change  │  (OS notifies)  │
│   file ops)   │   events       └────────────────┘
└──────────────┘
```

---

## 3. Threading Model

### 3.1 Thread Roles

| Thread | Owns | Rules |
|--------|------|-------|
| **UI thread** (main) | All view model property changes, all UI control updates, all `IExplorerBrowser` / AppKit view calls | Must be STA on Windows. Never block. |
| **Shell STA worker(s)** | Long-running COM operations on Windows (deep enumeration, `IFileOperation` progress callbacks) | Created as STA threads. Not the UI thread. Marshal results back to UI thread. |
| **Thread pool** | Non-COM background work: hash computation, folder size calculation, settings I/O, fuzzy search in command palette, variable expansion for custom actions | Standard `Task.Run` / `DispatchQueue.global()`. No UI or COM calls. |

### 3.2 Rules

1. **ViewModel property setters that are observed by UI must execute on the UI thread.** On Windows, ReactiveUI's `ObserveOn(RxApp.MainThreadScheduler)` handles this. On Mac, `DispatchQueue.main.async`.
2. **Shell adapter methods are `async` and return on the caller's context.** The adapter internally marshals to the correct thread (STA for COM, main for AppKit).
3. **File operations are fire-and-forget from the UI's perspective.** The UI calls `IFileOperations.Copy(...)`, gets back an `IOperationHandle`, and binds to its `ProgressChanged` event. The operation runs on a background thread. Progress events are raised on the UI thread.
4. **The `IFileWatcher` raises `Changed` events on a background thread.** The receiving view model must marshal to the UI thread before updating `Entries`.

### 3.3 Cancellation

Every async operation accepts a `CancellationToken`. Tab closure cancels any pending enumeration or preview load for that tab. App shutdown cancels everything. The operations queue's cancel button triggers the token for that operation.

---

## 4. Persistence — Settings File Format

Single JSON file. Top-level keys are namespaced. The file is human-readable and hand-editable as a fallback.

```jsonc
{
  "general": {
    "restoreSession": true,
    "singleInstance": true,
    "newTabPath": "~",              // "~" = home, or absolute path
    "confirmDelete": true,
    "opsQueue.maxConcurrent": 2
  },
  "appearance": {
    "theme": "system",              // "system" | "light" | "dark"
    "showHiddenFiles": true,
    "showFileExtensions": true,
    "foldersFirst": true,
    "showStatusBar": true,
    "showBookmarkBar": true,
    "showDropStack": false,         // collapsed by default
    "showPreviewPane": false
  },
  "panes": {
    "defaultLayout": "dual",
    "splitDirection": "vertical",
    "splitRatio": 0.5
  },
  "win": {
    "classicContextMenu": true      // Windows 11 only
  },
  "shortcuts": {
    "file.newTab": "Ctrl+T",
    "file.closeTab": "Ctrl+W",
    "pane.switchActive": "Tab",
    "pane.copyToOther": "F5",
    "pane.moveToOther": "F6",
    // ... full map, only non-default overrides need to be present
  },
  "bookmarks": [
    { "id": "...", "name": "Home", "path": "~", "group": null, "order": 0 },
    { "id": "...", "name": "Projects", "path": "~/Dev", "group": null, "order": 1 },
    { "id": "...", "name": "prod-east", "path": "\\\\server\\share", "group": "Servers", "order": 2 }
  ],
  "toolbar": [
    "go.back", "go.forward", "go.parent", "|",
    "file.newTab", "view.viewMode", "view.toggleHidden",
    "view.toggleDualPane", "tools.filter", "tools.commandPalette"
  ],
  "customActions": [
    {
      "id": "a1b2c3...",
      "label": "Open in VS Code",
      "icon": null,
      "group": "Dev Tools",
      "order": 0,
      "conditions": {
        "appliesTo": "both",
        "extensions": null,
        "selectionCount": "any",
        "pathContains": null
      },
      "actionType": "runProgram",
      "actionConfig": {
        "program": "code",
        "arguments": "{path}",
        "workingDirectory": "{folder}",
        "runHidden": false,
        "runElevated": false
      },
      "registerWithOS": false,
      "enabled": true
    }
  ],
  "session": {
    "windowBounds": { "x": 100, "y": 100, "width": 1400, "height": 900 },
    "splitMode": "vertical",
    "splitRatio": 0.5,
    "leftPane": {
      "activeTabId": 0,
      "tabs": [
        { "id": 0, "path": "C:\\Users\\me\\Documents", "viewMode": "details", "sortField": "name", "sortDirection": "asc" },
        { "id": 1, "path": "D:\\Projects", "viewMode": "details", "sortField": "dateModified", "sortDirection": "desc" }
      ]
    },
    "rightPane": {
      "activeTabId": 0,
      "tabs": [
        { "id": 0, "path": "C:\\Users\\me\\Downloads", "viewMode": "list", "sortField": "name", "sortDirection": "asc" }
      ]
    },
    "dropStack": []
  }
}
```

### 4.1 Settings Load Order

1. Determine settings path: check for `noctus-explorer.json` next to executable (portable mode). If not found, use platform default path.
2. If the file doesn't exist, create it with defaults.
3. Load and deserialize. For missing keys, use hardcoded defaults (settings schema evolves; old files don't break).
4. The `session` section is updated on every app close (auto-save). Other sections are saved on explicit user action or on settings dialog close.

---

## 5. Dependency Injection Wiring

### 5.1 Windows — `Explorer.App/Program.cs`

```
Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
Application.EnableVisualStyles();

var services = new ServiceCollection();

// Core services
services.AddSingleton<SettingsStore>();
services.AddSingleton<CommandRegistry>();
services.AddSingleton<KeyBindingResolver>();
services.AddSingleton<BookmarkStore>();
services.AddSingleton<CustomActionStore>();
services.AddSingleton<CustomActionEngine>();
services.AddSingleton<DropStackService>();
services.AddSingleton<OperationsQueue>();

// Shell adapter (Windows implementations)
services.AddSingleton<IShellService, WinShellService>();
services.AddSingleton<IFileOperations, WinFileOperations>();
services.AddSingleton<IClipboardService, WinClipboardService>();
services.AddSingleton<IPreviewService, WinPreviewService>();
services.AddSingleton<IFileWatcher, WinFileWatcher>();
services.AddSingleton<IContextMenuService, WinContextMenuService>();
services.AddSingleton<IHashService, HashService>();           // Pure .NET, no platform dep
services.AddSingleton<IOSContextMenuRegistrar, WinContextMenuRegistrar>();

// ViewModels
services.AddSingleton<MainViewModel>();

// UI
services.AddTransient<MainForm>();

var provider = services.BuildServiceProvider();

// Load settings, register commands, restore session
var settings = provider.GetRequiredService<SettingsStore>();
settings.Load(SettingsPathResolver.Resolve());

var commands = provider.GetRequiredService<CommandRegistry>();
RegisterAllCommands(commands, provider);  // Registers all built-in commands

var vm = provider.GetRequiredService<MainViewModel>();
vm.RestoreSession();

// Run
var form = provider.GetRequiredService<MainForm>();
Application.Run(form);

// On exit
vm.SaveSession();
settings.Save();
```

### 5.2 macOS — `App/AppDelegate.swift`

Same logical flow using Swift's manual DI (no framework needed — constructor injection with a simple container struct):

```
struct AppContainer {
    let settings: SettingsStore
    let shellService: ShellServiceProtocol        // NSFileManager-based
    let fileOperations: FileOperationsProtocol    // NSFileManager + NSFileCoordinator
    let clipboardService: ClipboardServiceProtocol
    let previewService: PreviewServiceProtocol    // QLPreviewView
    let fileWatcher: FileWatcherProtocol          // FSEvents
    let contextMenuService: ContextMenuServiceProtocol
    let hashService: HashServiceProtocol
    let osMenuRegistrar: OSContextMenuRegistrarProtocol
    let commandRegistry: CommandRegistry
    let keyBindingResolver: KeyBindingResolver
    let bookmarkStore: BookmarkStore
    let customActionStore: CustomActionStore
    let customActionEngine: CustomActionEngine
    let dropStack: DropStackService
    let opsQueue: OperationsQueue
    let mainViewModel: MainViewModel
}
```

---

## 6. Key Component Interactions

### 6.1 Navigation Flow

```
User clicks breadcrumb segment "Documents"
  → UI calls TabViewModel.NavigateAsync(pathRef)
    → TabViewModel sets Location = pathRef
    → TabViewModel calls IShellService.EnumerateAsync(pathRef)
    → Results arrive → TabViewModel sets Entries = results
    → TabViewModel pushes pathRef onto NavigationHistory
    → UI binding updates:
        - Address bar shows new breadcrumb
        - File list shows new entries
        - Status bar shows item count
    → IFileWatcher.Watch(pathRef) for new location
    → IFileWatcher.Unwatch(previousPath)
    → If preview pane is open and selection exists, load preview
```

### 6.2 Cross-Pane Copy Flow

```
User presses F5
  → KeyBindingResolver maps F5 → "pane.copyToOther"
  → CommandRegistry executes "pane.copyToOther"
    → MainViewModel.CopyToOtherPane()
      → sources = ActivePane.ActiveTab.Selection
      → destination = InactivePane.ActiveTab.Location
      → handle = IFileOperations.Copy(sources, destination)
      → OperationsQueue.Enqueue(handle)
      → UI binds to handle.ProgressChanged → updates ops queue panel
      → On conflict: handle raises ConflictEncountered
        → UI shows conflict dialog (overwrite/skip/rename/apply-all)
        → User choice is passed back to handle via event args
      → On completion: handle raises Completed
        → Ops queue panel updates
        → If inactive pane is watching destination dir, IFileWatcher triggers refresh
```

### 6.3 Custom Action Execution Flow

```
User right-clicks a .jpg file
  → IContextMenuService.ShowContextMenu(selection, position, customActions)
    → Service builds native menu:
        1. Evaluate each CustomAction via CustomActionEngine.Evaluate() → show/hide
        2. Build Noctus section (built-in items + matching custom actions)
        3. Append native OS context menu items
    → User clicks "Convert to WebP" (custom action)
      → CustomActionEngine.Execute(action, selection, otherPaneLocation)
        → ExpandVariables("{path}" → "C:\Photos\sunset.jpg")
        → ActionType = ShellCommand → spawn process: "cwebp {path} -o {basename}.webp"
        → Process runs in background
```

### 6.4 Settings Change Flow

```
User changes split direction in Settings dialog
  → SettingsStore.Set("panes.splitDirection", "horizontal")
    → SettingsStore notifies subscribers of "panes.*" prefix
      → MainViewModel receives notification
        → MainViewModel.SplitMode = SplitMode.Horizontal
          → UI binding updates → SplitContainer orientation changes
    → SettingsStore marks itself dirty → saves on dialog close
```

---

## 7. Windows-Specific: IExplorerBrowser Hosting Details

### 7.1 Lifecycle per Pane

```
ExplorerBrowserPane : UserControl, IPaneView
│
├── OnHandleCreated()
│   ├── CoCreateInstance(CLSID_ExplorerBrowser) → IExplorerBrowser
│   ├── browser.SetOptions(EBO_SHOWFRAMES | EBO_NAVIGATEONCE disabled)
│   ├── browser.Initialize(this.Handle, clientRect, folderSettings)
│   ├── browser.Advise(this as IExplorerBrowserEvents) → adviseCookie
│   └── browser.BrowseToIDList(initialPidl, SBSP_ABSOLUTE)
│
├── Navigate(PathRef target)
│   └── browser.BrowseToIDList(target.Pidl, SBSP_ABSOLUTE)
│
├── IExplorerBrowserEvents.OnNavigationComplete(pidl)
│   ├── Update Location from pidl
│   ├── Get IFolderView2 → read selection, view mode
│   └── Raise NavigationCompleted event
│
├── IExplorerBrowserEvents.OnViewCreated(IShellView)
│   └── Optional: subclass the view's window to intercept messages
│
├── GetSelection()
│   ├── browser.GetCurrentView(IID_IFolderView2) → folderView
│   ├── folderView.GetSelection(false) → IShellItemArray
│   └── Convert each IShellItem → FileEntry
│
├── Refresh()
│   └── browser.GetCurrentView(IID_IShellView) → view.Refresh()
│
├── Dispose()
│   ├── browser.Unadvise(adviseCookie)
│   ├── browser.Destroy()
│   └── Marshal.ReleaseComObject(browser)
│
└── Win11 Classic Context Menu Enforcement
    ├── On pane creation, set registry key:
    │   HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32 = ""
    └── Or use IExplorerBrowser::SetPropertyBag to disable the modern menu
```

### 7.2 Focus Management

The hosted `IExplorerBrowser` owns a child HWND. When it has focus, it processes keystrokes (arrow keys for selection, F2 for rename, etc.). The app's chrome needs certain keys routed to the app instead:

- **Tab** → switch pane (must be intercepted before the hosted view gets it)
- **F5, F6, F7, F8** → cross-pane commands (intercept)
- **Ctrl+T, Ctrl+W, etc.** → tab management (intercept)
- **Arrow keys, Enter, Delete, F2** → let the hosted view handle these

Implementation: Install a message filter (`IMessageFilter.PreFilterMessage` on WinForms, or a low-level keyboard hook) that checks the key against the `KeyBindingResolver`. If the key maps to a registered command, suppress it from the hosted view and execute the command. Otherwise, pass it through.

---

## 8. macOS-Specific: AppKit View Hosting Details

### 8.1 Pane View Controller

```swift
class PaneViewController: NSViewController, IPaneView {
    
    // Three interchangeable view controllers for view modes:
    private var listVC: NSViewController      // NSOutlineView (list + details)
    private var columnVC: NSViewController     // NSBrowser (column view)
    private var iconVC: NSViewController       // NSCollectionView (icon + gallery)
    
    private var currentVC: NSViewController
    private var dataSource: FileSystemDataSource  // Shared data source
    
    func navigate(to target: PathRef) async {
        // 1. Update dataSource with new directory contents
        let entries = try await shellService.enumerate(target)
        dataSource.update(entries: entries)
        
        // 2. Reload current view
        currentVC.reloadData()
        
        // 3. Update file watcher
        fileWatcher.watch(target)
    }
    
    func switchViewMode(_ mode: ViewMode) {
        // Swap the child view controller
        removeChild(currentVC)
        currentVC = viewController(for: mode)
        addChild(currentVC)
        // Transfer selection state
    }
}
```

### 8.2 Context Menu Construction

```swift
override func menu(for event: NSEvent) -> NSMenu? {
    let menu = NSMenu()
    
    // 1. Noctus items
    menu.addItem(NSMenuItem(title: "Copy to Other Pane", action: #selector(copyToOther), ...))
    menu.addItem(NSMenuItem(title: "Move to Other Pane", action: #selector(moveToOther), ...))
    menu.addItem(NSMenuItem(title: "Add to Drop Stack", ...))
    menu.addItem(NSMenuItem(title: "Copy Path", ...))
    
    // 2. Custom actions (filtered by conditions)
    let matchingActions = customActionStore.actions.filter { engine.evaluate($0, selection, otherPane) }
    if !matchingActions.isEmpty {
        menu.addItem(.separator())
        for action in matchingActions { /* add menu items, grouped by submenu */ }
    }
    
    // 3. Separator + native items
    menu.addItem(.separator())
    
    // Populate from NSWorkspace: "Open With", Services, Share, Quick Actions
    // Use NSWorkspace.shared.urlForApplication(toOpen:) for Open With submenu
    // Use NSSharingServicePicker for Share submenu
    
    return menu
}
```

---

## 9. Implementation Plan — Detailed Milestones

Each milestone has: scope, deliverables, acceptance criteria, and known risks.

---

### Milestone W0 / M0 — Platform Spike (1–2 days each)

**Goal:** Prove the native view hosting works before writing any architecture.

**Windows (W0):**
- Throwaway WinForms project, one Form, one `IExplorerBrowser`
- Navigate to `C:\`
- **Accept if ALL pass:**
  - [ ] Rubber-band selection works
  - [ ] Double-click navigates into folder
  - [ ] Right-click shows full context menu (including 7-Zip, TortoiseGit if installed)
  - [ ] Drag-drop to/from a real Explorer window works
  - [ ] In-place rename (F2) works
  - [ ] Thumbnails render in medium/large icon view
  - [ ] Column headers sort in Details view
  - [ ] Two `IExplorerBrowser` instances in the same Form don't conflict
- **Risk:** Windows 11's modern context menu shows instead of classic → test mitigation via registry key or `IExplorerBrowser::SetPropertyBag`

**macOS (M0):**
- Throwaway AppKit project, one NSWindow, one `NSOutlineView` + one `NSBrowser`
- Navigate to `~/`
- **Accept if ALL pass:**
  - [ ] List view shows files with name, size, date columns
  - [ ] Column view navigates like Finder's column view
  - [ ] Right-click shows context menu with Open, Open With, Quick Actions
  - [ ] Drag-drop to/from a real Finder window works
  - [ ] Double-click opens files in default app
  - [ ] Rename via click-pause-click works
  - [ ] Two views in the same window work independently
- **Risk:** Context menu may not have full Finder parity (some items are Finder-internal) → document gaps

---

### Milestone W1 / M1 — Core + Shell Adapter (1 week each)

**Goal:** Build the pure core and platform adapter with no UI. Prove the abstractions work.

**Deliverables:**
- All Core models (PathRef, FileEntry, Bookmark, TabState, etc.)
- All Core abstractions (IShellService, IFileOperations, etc.)
- All Core services (CommandRegistry, SettingsStore, BookmarkStore, etc.)
- Shell adapter implementing all abstractions
- Console test harness exercising: enumerate directory, copy file, create folder, rename, delete to trash, listen for file changes, resolve special folders
- Settings store: load/save JSON, subscribe to changes
- Unit tests for all Core services (command registry, keybinding resolver, settings store, bookmark store, navigation history, custom action evaluation)

**Acceptance criteria:**
- [ ] `dotnet test` / `swift test` passes with >90% coverage on Core
- [ ] Console harness can enumerate `C:\Users` / `~/` and print entries
- [ ] Console harness can copy a file and observe progress callbacks
- [ ] Settings round-trip: save → load → values match
- [ ] Keybinding resolver correctly maps key chords to command IDs

---

### Milestone W2 / M2 — Single Pane UI (1 week each)

**Goal:** One pane, one tab, address bar, status bar. The app launches and is usable for basic navigation.

**Deliverables:**
- MainForm / MainWindowController with menu bar
- Single PaneView hosting the native file view
- Address bar with breadcrumb display and edit-mode toggle
- Status bar showing item count, selection count + size, free space
- MainViewModel, PaneViewModel, TabViewModel wired up
- Reactive bindings: navigation updates address bar and status bar
- Basic keybindings working (navigate back/forward/up, refresh)

**Acceptance criteria:**
- [ ] App launches in < 2 seconds
- [ ] Navigate by double-clicking folders in the native view
- [ ] Address bar breadcrumb updates on navigation
- [ ] Click breadcrumb segment → navigates to that ancestor
- [ ] Ctrl+L / Cmd+L → edit mode, type path, Enter → navigate
- [ ] Status bar shows "12 items" / "3 selected (4.5 MB)" / "128 GB free"
- [ ] Back/Forward/Up keyboard shortcuts work
- [ ] Ctrl+R / Cmd+R refreshes the view

---

### Milestone W3 / M3 — Dual Pane (3–4 days each)

**Deliverables:**
- SplitContainer / NSSplitView hosting two panes
- Active pane indicator (colored border)
- Tab key switches active pane
- F5 copies selection to inactive pane's location
- F6 moves selection to inactive pane's location
- View menu: Single / Vertical / Horizontal split toggle
- Split ratio adjustable by dragging divider

**Acceptance criteria:**
- [ ] Two independent panes showing different directories
- [ ] Active pane has visible indicator; inactive does not
- [ ] Tab key switches focus and indicator
- [ ] F5 on selected files → files appear in other pane's directory
- [ ] F6 on selected files → files moved to other pane's directory
- [ ] Toggle to single pane → one pane fills window; toggle back → previous state restored
- [ ] Drag divider → split ratio changes

---

### Milestone W4 / M4 — Tabs (3–4 days each)

**Deliverables:**
- Tab strip per pane (custom control on Windows, NSTabView or custom on Mac)
- New tab (Ctrl+T), close tab (Ctrl+W), switch (Ctrl+Tab, Ctrl+1..9)
- Drag to reorder tabs within a pane
- Drag tab to other pane → moves it
- "+" button on tab strip
- Session restore: last session's tabs reopen on launch

**Acceptance criteria:**
- [ ] New tab opens to default path
- [ ] Close tab works; closing last tab closes the pane (in dual mode) or creates a new tab (in single mode)
- [ ] Ctrl+Tab cycles through tabs
- [ ] Ctrl+1..9 jumps to nth tab
- [ ] Drag reorder works visually and persists
- [ ] Close app → reopen → same tabs at same paths

---

### Milestone W5 / M5 — Bookmarks + Command Palette (3–4 days each)

**Deliverables:**
- Bookmark bar below the toolbar
- Drag folder to bookmark bar → creates bookmark
- Right-click bookmark → rename, delete, assign to group
- Bookmark groups render as dropdown submenus
- Command palette dialog (Ctrl+Shift+P): fuzzy search over CommandRegistry
- Each palette entry shows command name, description, keybinding

**Acceptance criteria:**
- [ ] Bookmark bar shows saved bookmarks; click navigates active pane
- [ ] Drag-to-add works
- [ ] Groups appear as dropdown menus
- [ ] Command palette opens instantly, fuzzy-matches as user types
- [ ] Selecting a command executes it and closes the palette
- [ ] Palette shows all registered commands (including cross-pane ops, view toggles)

---

### Milestone W6 / M6 — Drop Stack + Batch Rename (3–4 days each)

**Deliverables:**
- Drop stack panel (collapsible, bottom of window)
- Drag files onto stack from either pane
- Drag files out of stack to a destination
- Remove individual items, clear all
- Stale reference detection (visual indicator)
- Batch rename dialog: find/replace, regex, insert text, change case, number sequence, date stamp
- Live preview in batch rename
- Chainable operations (add multiple steps)

**Acceptance criteria:**
- [ ] Drag 3 files from different directories → all appear in stack
- [ ] Drag from stack to pane → files copy/move to that pane's directory
- [ ] Delete original file → stack item shows stale indicator
- [ ] Batch rename: select 5 files → F&R "IMG" → "Photo" → preview shows all 5 before/after → confirm → files renamed

---

### Milestone W7 / M7 — Custom Context Menu Wizard (1 week each)

**Deliverables:**
- Settings → Context Menu management page
- "Add Custom Action" wizard (4-step flow per spec Section 4.10)
- Condition evaluation engine
- Variable substitution engine
- Action execution (run program, shell command, open with, copy text, open URL)
- Test button in wizard
- Drag to reorder, enable/disable toggle, edit, duplicate, delete
- Import/export to JSON
- OS-level registration: Windows registry (HKCU), macOS Quick Actions (~/Library/Services/)
- Predefined templates

**Acceptance criteria:**
- [ ] Create "Open in VS Code" action via wizard → right-click file → action appears → launches VS Code with correct path
- [ ] Condition: ".py files only" → action does not appear for .txt files
- [ ] Variable substitution: `{basename}`, `{ext}`, `{folder}`, `{other_pane}` all expand correctly
- [ ] Enable OS registration → action appears in Explorer/Finder context menu outside the app
- [ ] Disable OS registration → action disappears from OS context menu
- [ ] Export 3 actions → import on clean install → all 3 restored
- [ ] Test button executes the action immediately with a chosen file

---

### Milestone W8 / M8 — Preview Pane + Operations Queue (1 week each)

**Deliverables:**
- Preview pane (toggleable, right side of active pane)
- Image preview with scaling
- Text/code preview with syntax highlighting
- PDF preview (page rendering)
- Hex view for binary files
- Platform preview handler integration (IPreviewHandler / QLPreviewView)
- Operations queue panel (collapsible, bottom of window)
- Progress bars, speed, ETA per operation
- Pause/resume/cancel per operation
- Conflict resolution dialog (overwrite, skip, rename, overwrite-if-newer, apply-to-all)
- Queue concurrency management

**Acceptance criteria:**
- [ ] F3 toggles preview pane
- [ ] Select .jpg → preview shows image
- [ ] Select .py → preview shows syntax-highlighted code
- [ ] Select .pdf → preview shows first page
- [ ] Select unknown binary → hex view
- [ ] Copy 500 files → progress bar updates smoothly, speed and ETA shown
- [ ] Pause → progress freezes; resume → continues
- [ ] Cancel → remaining files not copied, completed files stay
- [ ] Conflict → dialog shows both files' info → user picks action → applies correctly
- [ ] "Apply to all" on overwrite → remaining conflicts auto-resolved

---

### Milestone W9 / M9 — Polish + Release (1–2 weeks each)

**Deliverables:**
- Quick filter (Ctrl+F): filter bar, glob support, case toggle
- Folder size calculation (background, cached, invalidated on change)
- File watcher integration: auto-refresh + change highlighting
- Checksum generation and verification (MD5, SHA-256, right-click menu)
- Action toolbar: customizable, drag-to-reorder, right-click → "Customize Toolbar…"
- Portable mode detection
- Single-instance behavior with new-tab forwarding
- Classic context menu enforcement on Windows 11
- Dark mode support (Win32 APIs / NSAppearance)
- Window position/size persistence
- Application icon and about dialog
- Distribution packaging:
  - Windows: portable ZIP + MSI/MSIX installer
  - macOS: DMG with drag-to-Applications, notarized + stapled
- CI pipeline: GitHub Actions (Windows runner + macOS runner), build + test on push

**Acceptance criteria:**
- [ ] Cold start < 1 second on commodity hardware
- [ ] Memory usage < 150 MB with 4 tabs across 2 panes
- [ ] All keyboard shortcuts from spec work correctly
- [ ] Settings file round-trips cleanly
- [ ] Dark mode: all chrome respects system theme
- [ ] Windows 11: right-click shows classic context menu, not "Show more options"
- [ ] Portable mode: drop `noctus-explorer.json` next to exe → settings read/written there
- [ ] Single instance: second launch sends path to first instance → opens as new tab
- [ ] Installer installs cleanly; uninstaller removes all (including OS-registered context menu items)
- [ ] macOS: app is signed, notarized, opens without Gatekeeper warning

---

## 10. Risk Register

| Risk | Severity | Mitigation | Milestone |
|------|----------|------------|-----------|
| `IExplorerBrowser` doesn't surface Win11 features cleanly | High | Spike in W0. If classic context menu can't be forced, document as known limitation | W0 |
| Multiple `IExplorerBrowser` instances are memory-heavy | Medium | Test with 8 tabs in W0 spike. If >500MB, implement tab virtualization (lazy init) | W0, W4 |
| Shell extensions crash the hosting process | Medium | Accept in v1. Document. Consider process isolation in v2 | Ongoing |
| WinForms High-DPI quirks on mixed-DPI setups | Medium | Test in W2, W3. Use `PerMonitorV2` DPI awareness. Known-good on .NET 8 | W2 |
| macOS context menu doesn't have full Finder parity | Medium | Spike in M0. Document which items are Finder-internal and can't be surfaced | M0 |
| ReactiveUI WinForms bindings have edge cases | Low | Keep bindings simple. Test property change propagation in unit tests | W2 |
| Settings file corruption | Low | Write to temp file, then atomic rename. Keep backup of previous settings | W1 |
| Custom action OS registration leaves orphans on crash | Low | On app startup, audit registered actions vs. stored actions, clean up orphans | W7 |

---

## 11. Dependencies & Libraries

### Windows

| Library | Purpose | NuGet Package |
|---------|---------|---------------|
| Vanara.Windows.Shell | COM interop (IExplorerBrowser, IShellFolder, IFileOperation, PIDL) | `Vanara.Windows.Shell` |
| Vanara.PInvoke.Shell32 | Shell32 P/Invoke declarations | `Vanara.PInvoke.Shell32` |
| ReactiveUI | MVVM bindings, reactive property change | `ReactiveUI` |
| ReactiveUI.WinForms | WinForms-specific binding adapters | `ReactiveUI.WinForms` |
| System.Reactive | Observable infrastructure for ReactiveUI | `System.Reactive` |
| Microsoft.Extensions.DependencyInjection | DI container | `Microsoft.Extensions.DependencyInjection` |
| Serilog | Structured logging | `Serilog`, `Serilog.Sinks.File` |
| AvalonEdit (optional) | Syntax-highlighted text preview in preview pane | `AvalonEdit` |

### macOS

| Library | Purpose | Source |
|---------|---------|--------|
| Foundation | File system, process management | System framework |
| AppKit | UI controls, window management | System framework |
| QuickLookUI | QLPreviewView for preview pane | System framework |
| Combine | Reactive bindings (Swift equivalent of ReactiveUI) | System framework |
| os.log | Structured logging | System framework |
| (No third-party dependencies for v1) | — | — |

---

## 12. Testing Strategy

| Layer | Framework | What's Tested | Coverage Target |
|-------|-----------|---------------|-----------------|
| Core Models | xUnit / XCTest | Model construction, equality, serialization | 95% |
| Core Services | xUnit / XCTest | CommandRegistry, KeyBindingResolver, SettingsStore, BookmarkStore, CustomActionStore, CustomActionEngine, NavigationHistory, DropStackService | 90% |
| Shell Adapter | Manual + integration tests | Enumerate, copy, move, delete, rename, file watcher, context menu registration | Manual on each target OS |
| UI | Manual smoke tests | Navigation, tabs, split, bookmarks, command palette, preview, ops queue, context menu wizard | Smoke test matrix per milestone |
| End-to-End | Manual | Full workflow: launch → navigate → copy between panes → rename → bookmark → close → reopen → session restored | Per-release checklist |

### Smoke Test Matrix (per release)

- [ ] Fresh install on Windows 10 22H2
- [ ] Fresh install on Windows 11 23H2
- [ ] Upgrade from previous version on Windows
- [ ] Fresh install on macOS 13 (Ventura)
- [ ] Fresh install on macOS 14 (Sonoma)
- [ ] Portable mode on Windows (USB drive)
- [ ] High-DPI display (200% scaling) on Windows
- [ ] Retina display on Mac
- [ ] Mixed-DPI dual-monitor on Windows
- [ ] Dark mode on both platforms
- [ ] Third-party shell extension test (7-Zip, TortoiseGit) on Windows
- [ ] Third-party Quick Action test on macOS
