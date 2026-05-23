# Noctus Explorer

> A native Windows file manager built on the principle that **Windows Explorer is right, and everyone else is wrong**.

---

## Vision

There is no replacement for the native, 132-fluid feel of Windows Explorer. Every third-party "Explorer alternative" — Files, Explorer++, Q-Dir, Directory Opus, Total Commander, FreeCommander, Double Commander — feels *off*. They reimplement the shell view in their own widget toolkits, and the result is always uncanny: rubber-band selection lags, drag-drop handshakes break with certain apps, third-party shell extensions don't appear in context menus, thumbnail providers behave differently, in-place rename doesn't quite match, column virtualization stutters, breadcrumb navigation feels foreign. Pick any one and you'll find something that doesn't quite work the way thirty years of muscle memory expects.

The Windows shell isn't just a widget — it's a deep COM ecosystem with `IShellFolder`, `IContextMenu`, `IDataObject`, `IFileOperation`, `IThumbnailProvider`, `IPreviewHandler`, namespace extensions, and a thousand other surfaces that third-party tools register against. Reimplement the view layer and you lose the ecosystem. The fluidity isn't a styling problem; it's an integration problem.

**Noctus Explorer does not reimplement the shell view. It hosts it.**

Using the `IExplorerBrowser` COM interface (available since Vista), the application embeds the *real* Windows Explorer view inside its own chrome. Each pane in Noctus Explorer is, literally, an instance of Explorer. Selection, sort, rename, drag-drop, context menus, thumbnail providers, third-party shell extensions, namespace extensions — all of it works exactly as it does in `explorer.exe`, because it *is* `explorer.exe`'s view component.

What Noctus Explorer adds is the chrome that Microsoft refuses to ship: **dual-pane layout, real tabs, classic styling, scriptable commands, power-user keybindings, bookmarks, and a clean Windows 7/10-era aesthetic that does not subject the user to Windows 11's Fluent redesign.**

---

## Guiding Principles

1. **Native first, always.** When Windows already provides a capability, host it — don't reimplement it.
2. **Classic aesthetic.** No Fluent. No Mica. No acrylic. No animated card transitions. Crisp, dense, fast, monochrome iconography where appropriate. The visual target is Windows 7 / Windows 10 (pre-Sun Valley) Explorer.
3. **Lightweight.** Cold start under one second on commodity hardware. Memory footprint comparable to Explorer++. No Electron, no WebView2, no embedded Chromium, no .NET 4.8 bloat.
4. **Power-user defaults.** Keyboard-driven. Every action discoverable through a command palette. Hidden files, file extensions, full paths — all visible by default.
5. **UI-pluggable architecture.** The application core has zero direct UI dependencies. The default presentation is native WinForms, but the architecture supports alternative UI packs (Qt, WPF, Avalonia) implementing a stable contract.
6. **Honest about platform coupling.** Windows-only is not a bug. The native shell hosting *is* the value proposition. Cross-platform is a non-goal for v1.
7. **No telemetry. No update servers calling home. No accounts. No cloud.** Configuration is local. The application is portable by default.

---

## Architecture Overview

Noctus Explorer follows a strict **Hexagonal / Ports-and-Adapters** architecture. The core knows nothing about Windows, COM, WinForms, or any specific UI framework. Adapters bridge the core to the outside world.

```
NoctusExplorer.sln
├── Explorer.Core/                # Pure C#. No platform, no UI references.
│   ├── Models/                   # FileEntry, PathRef, NavigationState,
│   │                             # Bookmark, Tab, PaneState, Selection
│   ├── Commands/                 # ICommand pattern: Copy, Cut, Paste,
│   │                             # Rename, Delete, NewFolder, Refresh,
│   │                             # GoUp, GoBack, GoForward, ToggleHidden,
│   │                             # CopyToOtherPane, MoveToOtherPane, etc.
│   ├── ViewModels/               # MainViewModel, PaneViewModel,
│   │                             # TabViewModel, BookmarkBarViewModel
│   ├── Services/                 # NavigationHistory, BookmarkStore,
│   │                             # SettingsStore, CommandRegistry,
│   │                             # KeyBindingResolver
│   └── Abstractions/             # IShellService, IFileOperations,
│                                 # IClipboardService, IDialogService,
│                                 # IThumbnailService, INotificationSink
│
├── Explorer.Shell.Windows/       # Vanara + Win32. Implements Core abstractions.
│   ├── ShellService.cs           # Wraps IShellFolder, PIDL operations
│   ├── FileOperations.cs         # Wraps IFileOperation (proper recycle bin,
│   │                             # progress, undo, conflict resolution)
│   ├── ClipboardService.cs       # Shell clipboard format handling
│   ├── ThumbnailService.cs       # IThumbnailProvider integration
│   └── ShellNotifications.cs     # SHChangeNotifyRegister for file events
│
├── Explorer.UI.Contracts/        # The UI Pack contract. UI-framework-agnostic.
│   ├── IPaneView.cs              # Navigate, Selection, Activate, Refresh
│   ├── ITabHost.cs               # AddTab, RemoveTab, ActivateTab
│   ├── ISplitLayout.cs           # Vertical/horizontal/single, ratio
│   ├── IChromeHost.cs            # MenuBar, Toolbar, StatusBar, AddressBar
│   ├── IDialogSurface.cs         # File dialogs, confirm, prompt, progress
│   └── IUIPack.cs                # Composition root for a UI implementation
│
├── Explorer.UI.WinForms/         # Reference UI Pack. Native WinForms.
│   ├── MainForm.cs               # Hosts split layout, tab strip, chrome
│   ├── ExplorerBrowserPane.cs    # Embeds IExplorerBrowser via Vanara
│   ├── ClassicTheme.cs           # Win7/Win10 visual styles
│   └── Bindings/                 # ReactiveUI WinForms binding glue
│
├── Explorer.UI.Qt/               # (Future) Alternative UI Pack using Qt
├── Explorer.UI.Wpf/              # (Future) Alternative UI Pack using WPF
│
└── Explorer.App/                 # Composition root. DI wiring. Entry point.
    ├── Program.cs                # Selects UI pack, wires services, runs
    └── appsettings.json          # Default config (overridable per-user)
```

### The IExplorerBrowser Hosting Strategy

Each pane in Noctus Explorer wraps a single `IExplorerBrowser` COM instance, created via `CLSID_ExplorerBrowser`. The pane:

- Owns the COM lifetime (initialize on creation, release on disposal)
- Navigates by handing the browser a PIDL via `BrowseToIDList` or a path via `BrowseToObject`
- Listens to `IExplorerBrowserEvents` for selection changes, navigation completion, and view mode changes
- Exposes a `IPaneView` contract to the rest of the app — `Navigate(path)`, `SelectionChanged` event, `GetSelection()`, `Refresh()`, etc.
- Forwards focus correctly so keyboard shortcuts route either to the hosted view or to the app chrome depending on context

The hosted view handles everything that makes Explorer feel like Explorer: column virtualization, rubber-band select, in-place rename, right-click context menus (including third-party extensions like 7-Zip, TortoiseGit, WinMerge), drag-and-drop with any other Explorer window or third-party app, thumbnail rendering, namespace extensions (Recycle Bin, Libraries, network shares, FTP sites, mounted ZIPs). None of this is reimplemented.

What Noctus Explorer's chrome adds:

- **Two panes side-by-side** in a splitter, each independently navigable
- **Tab strip per pane**, or a unified tab strip with split-tab semantics
- **Active pane indicator** with cheap visual hint (border color, title bar accent)
- **Cross-pane commands**: F5 to copy selection to inactive pane, F6 to move, Tab to switch active pane (Total Commander muscle memory)
- **Address bar with breadcrumb + text edit toggle**
- **Bookmark bar** across the top, configurable per-pane or shared
- **Status bar** showing selection count, total size, free space on current drive
- **Command palette** (Ctrl+Shift+P) listing every registered command, fuzzy-searchable

---

## The UI Pack Abstraction

The most important architectural decision in Noctus Explorer is **what the UI Pack boundary describes**.

**Wrong abstraction:** "A widget toolkit. Swap WinForms for Qt and everything else stays the same."
This fails because the entire native-fluidity premise depends on `IExplorerBrowser`, which is Windows-only. A Qt UI pack on Linux cannot host Explorer. So the value of the abstraction isn't "run anywhere."

**Right abstraction:** "A presentation contract. Different UI packs can implement different *experiences* of the same application core."

A WinForms UI Pack on Windows hosts `IExplorerBrowser` and gets native shell fidelity. A hypothetical Qt UI Pack on Windows could *also* host `IExplorerBrowser` (Qt has Win32 native window embedding via `QWindow::fromWinId`), with a different visual chrome. A Qt UI Pack on Linux would implement `IPaneView` against `QFileSystemModel` + `QListView`, knowing it's not Explorer-on-Linux but a port of the *app*, not the *experience*. A WPF UI Pack on Windows would also host `IExplorerBrowser` but with a more modern chrome. An Avalonia UI Pack would be cross-platform-capable but accept the same caveat as Qt-on-Linux.

In all cases, the UI Pack receives the same `MainViewModel`, the same `CommandRegistry`, the same `BookmarkStore` — and is responsible for rendering them and routing user input back into command invocations.

### UI Pack Contract — Minimal Interface Sketch

```csharp
public interface IUIPack
{
    string Name { get; }
    string PlatformRequirement { get; }   // e.g. "Windows >= 10.0.17763"
    void Run(MainViewModel rootVm, IServiceProvider services);
}

public interface IPaneView : IDisposable
{
    Task NavigateAsync(PathRef target);
    PathRef CurrentLocation { get; }
    IReadOnlyList<FileEntry> CurrentSelection { get; }
    event EventHandler<NavigationEventArgs> NavigationCompleted;
    event EventHandler<SelectionEventArgs> SelectionChanged;
    void Refresh();
    void Focus();
}

public interface ITabHost
{
    int AddTab(PathRef initialLocation);
    void CloseTab(int tabId);
    void ActivateTab(int tabId);
    int ActiveTabId { get; }
    event EventHandler<TabEventArgs> ActiveTabChanged;
}

public interface ISplitLayout
{
    SplitMode Mode { get; set; }          // Single | Vertical | Horizontal
    double SplitRatio { get; set; }       // 0.0 .. 1.0
    IPaneView LeftPane { get; }
    IPaneView RightPane { get; }
    PaneSide ActiveSide { get; }
    void TogglePane();                    // switch active pane
}
```

The point is that nothing in these contracts says "ListView" or "TreeView" or "WinForms." A UI Pack is free to implement panes however it wants, as long as the contract holds.

---

## Cross-Cutting Concerns — Who Owns What

This section pins down ownership decisions that don't surface from the project layout alone, but matter for clean evolution.

### Commands

**Core owns the command registry.** Every command (Copy, Paste, ToggleSplit, NewTab, OpenCommandPalette, FocusBookmarkBar, etc.) is registered in `Explorer.Core.Services.CommandRegistry` at startup. Commands are identified by stable string IDs (`"file.copy"`, `"view.toggleSplit"`) so keybindings, command palette entries, and external scripts can reference them.

The UI Pack does *not* invent commands. It surfaces them — populating menus, toolbars, and the command palette by enumerating the registry. It also forwards keystrokes to the `KeyBindingResolver`, which maps key chord → command ID → execution.

The one exception: a UI Pack may register **presentation-only commands** that genuinely don't exist outside its world (e.g. `"winforms.dockSidebar"`). These are namespaced under the UI Pack's name and are not expected to appear in other packs.

### Settings

Settings live in `Explorer.Core.Services.SettingsStore`, persisted as JSON. The store distinguishes three scopes:

- **Core settings** — orientation, last-session tabs, bookmark list, keybindings. Always loaded.
- **Shell adapter settings** — confirm-on-delete, recycle-bin behavior, classic context menu preference. Loaded by `Explorer.Shell.Windows`.
- **UI Pack settings** — split ratio, font size for chrome, dark mode override, classic theme variant. Each UI Pack reads/writes its own settings under a namespaced key (e.g. `"ui.winforms.splitRatio"`).

The settings store is dumb. It does not know what the keys mean. It just persists and notifies on change. Each consumer subscribes to the keys it cares about.

### Shell Notifications and File System Changes

`Explorer.Shell.Windows.ShellNotifications` subscribes to `SHChangeNotifyRegister` for the entire app. When a file system change arrives, it raises a Core-level event on `INotificationSink`. Both panes' view models subscribe; only the pane whose current location matches the change refreshes. This avoids each pane individually registering shell notifications and duplicating work.

### Threading Model

Three threads matter:

1. **UI thread** — owned by the UI Pack. All `IPaneView` interactions, all `IExplorerBrowser` calls, all view model property changes that bind to UI must happen here.
2. **Shell COM thread** — by default the UI thread is the STA that hosts COM. For long-running shell operations (large recursive copy, deep directory enumeration), we marshal off the UI thread but back onto an STA worker.
3. **Background pool** — for non-COM work: parsing settings, fuzzy-matching the command palette, computing folder sizes for the status bar.

ReactiveUI's `RxApp.MainThreadScheduler` and `RxApp.TaskpoolScheduler` are the primary handles. The UI Pack registers the correct main-thread scheduler at startup.

---

## MVVM and Binding Strategy

WinForms has no native MVVM. The chosen binding library is **ReactiveUI**, specifically because:

1. It has first-class adapters for WinForms, WPF, Avalonia, and (via QtSharp) Qt.
2. Its `ReactiveObject` and `WhenAnyValue` are framework-agnostic — view models can live in `Explorer.Core` with zero UI references.
3. Command pattern (`ReactiveCommand<TIn, TOut>`) maps cleanly onto the keyboard-driven philosophy and the command palette.
4. It survives a UI framework change. If Noctus Explorer ever ships a WPF or Qt UI Pack, the view models do not change. Only the bindings do.

`CommunityToolkit.Mvvm` was considered and rejected because its source generators leak attributes into view model code, which subtly couples the view models to a specific binding strategy.

---

## Visual Design

The reference aesthetic is **Windows 10 Explorer with the ribbon collapsed**, leaning slightly toward Windows 7's denser presentation. Specifically:

- **No ribbon.** A classic menu bar (File, Edit, View, Tools, Help) plus a compact toolbar.
- **System chrome.** No custom title bar redraw. The window frame is the OS's window frame, period.
- **Sharp corners on Windows 10.** Honor Windows 11's rounded corners only on Windows 11.
- **Iconography:** prefer system shell icons where available. For chrome icons not provided by the shell, use a tight, monochrome 16x16 / 32x32 set in the style of Tango or Fugue. No emoji. No multi-color illustration glyphs.
- **Typography:** Segoe UI 9pt for chrome. Hosted listview uses whatever the shell uses (also Segoe UI by default).
- **Dark mode:** supported via the standard Win32 dark mode APIs (`AllowDarkModeForWindow` etc.). Hosted Explorer view picks up the system theme automatically.

---

## Core Feature Set — v1 Scope

### Must Ship in v1
- Hosted `IExplorerBrowser` in single, dual-vertical, and dual-horizontal layouts
- Tab strip with new tab, close tab, drag-reorder, restore-last-session
- Address bar (breadcrumb + edit mode toggle)
- Bookmark bar with folder grouping
- Status bar (selection count, total size, free space)
- Cross-pane commands: copy / move / sync-to-other-pane
- F-key bindings (F2 rename, F3 search, F5 copy, F6 move, F7 newfolder, F8 delete) — Total Commander style, configurable
- Tab switching via `Ctrl+Tab` / `Ctrl+Shift+Tab` / `Ctrl+1..9`
- Pane switching via `Tab` / `Ctrl+Left` / `Ctrl+Right`
- Command palette (`Ctrl+Shift+P`)
- Portable mode: settings written to `noctus-explorer.json` next to the executable if present, otherwise `%APPDATA%\NoctusExplorer\`
- Single-instance behavior with new-tab forwarding (configurable)
- Classic context menu shown by default (Windows 11's "Show more options" subordinated)

### Explicitly Out of Scope for v1
- Cloud storage integrations (rely on the shell's existing namespace extensions)
- Built-in archive handling (rely on 7-Zip / WinRAR / Explorer's built-in ZIP handler)
- File previewers beyond what the shell provides
- Theming engine for the hosted view (the hosted view *is* the system)
- Cross-platform (Qt/Linux is a v2 conversation at earliest)
- Plugin marketplace (a command-registration API exists, but no marketplace)

### Considered for v2+
- Scriptable commands (PowerShell / C# Scripting)
- FTP/SFTP namespace extension (already partially "free" via Windows' built-in support, but a better one is desirable)
- A Qt UI Pack as a proof-of-concept for the UI Pack abstraction
- Multi-monitor session persistence
- Per-pane "filter as you type" overlay

---

## Milestones — Build Order

Each milestone is independently demoable. The order is risk-first: prove the riskiest unknown earliest so the whole house of cards doesn't get built on a bad foundation.

### M0 — IExplorerBrowser Spike (1-2 days)
A throwaway WinForms project. One Form. Reference Vanara. Host a single `IExplorerBrowser`. Navigate to `C:\`. Confirm rubber-band selection, double-click navigation, right-click context menu (including a third-party extension like 7-Zip), drag-drop to another Explorer window, in-place rename, and thumbnail rendering all work natively. **If this milestone fails or feels off, the entire premise of the project is invalidated.** No further work proceeds until this passes.

### M1 — Core + Shell Adapter Headless (1 week)
Create `Explorer.Core` and `Explorer.Shell.Windows`. Build a console test harness in `Explorer.App` that exercises: enumerate a directory, copy a file via `IFileOperation`, listen for shell change notifications, resolve a path to a PIDL and back. No UI. xUnit tests for everything testable. This forces honest abstractions before any pixel is drawn.

### M2 — Single-Pane WinForms UI (1 week)
Stand up `Explorer.UI.Contracts` and `Explorer.UI.WinForms`. One pane, one tab, address bar, status bar. Hosted `IExplorerBrowser` driven via the `IPaneView` contract. ReactiveUI binding between the pane view model and the host control. End-of-milestone test: can navigate, can see selection in status bar, can rename, can copy/paste to another Explorer window.

### M3 — Dual Pane + Active Pane Tracking (3-4 days)
Add `ISplitLayout`. Two `IExplorerBrowser` instances side-by-side. Active pane indicator. Tab key switches focus. F5/F6 copy/move selection to the inactive pane. End-of-milestone test: feel test against Total Commander muscle memory.

### M4 — Tabs (3-4 days)
Tab strip per pane. New tab, close tab, drag-reorder, restore-last-session via the settings store. Ctrl+T, Ctrl+W, Ctrl+1..9.

### M5 — Bookmarks + Command Palette (3-4 days)
Bookmark bar across the top, drag-to-add-bookmark. Command palette (Ctrl+Shift+P) with fuzzy search over the command registry.

### M6 — Polish + Portable Mode + First Release (1 week)
Single-instance handling. Portable-mode detection. Classic context menu enforcement on Win11. Dark mode wiring. Icons. About box. Installer/portable zip. Publish v1.0.

**Total realistic part-time effort with Claude Code assistance: 6-8 weeks.**

---

## Glossary

Useful terms for anyone (or future-Claude) jumping into this codebase cold.

- **PIDL** — *Pointer to an Item ID List.* The shell's universal pointer to an item in its namespace. Works for files, virtual folders, FTP sites, Recycle Bin contents — anything Explorer can navigate to. Files have paths; the shell uses PIDLs.
- **Shell namespace** — The unified tree the shell exposes: Desktop at the root, with This PC, Network, Libraries, Recycle Bin, mounted drives, and any registered namespace extension as branches. Not the same as the filesystem.
- **Namespace extension** — A COM component that adds a virtual branch to the shell namespace. Examples: Dropbox's old shell extension, mounted-FTP folders, archive-as-folder extensions.
- **IShellFolder** — The COM interface representing a folder in the shell namespace. Lets you enumerate children, resolve names to PIDLs, get display names, etc.
- **IExplorerBrowser** — The COM interface that *is* the Explorer view. The whole point of this project.
- **IFileOperation** — The shell's transactional file-ops API. Handles recycle bin, conflict prompts, undo, progress UI. Always prefer this over `System.IO.File.*` for user-initiated operations.
- **STA / MTA** — *Single-/Multi-Threaded Apartment.* COM threading models. Shell COM is STA. The UI thread must be STA. Worker threads doing shell COM must also be STA.
- **Shell extension** — Generic term for any COM component registered to participate in Explorer: context menu handlers, thumbnail providers, preview handlers, icon handlers, property handlers, namespace extensions.
- **PIDL absolute vs relative** — A PIDL can be absolute (from desktop root) or relative (from a known parent). The distinction matters for serialization (bookmarks) and for cross-process passing.

---

## Build, Tooling, and Development

- **Language:** C# 12, targeting .NET 8 (LTS) with Windows-only TFM `net8.0-windows10.0.19041.0`
- **UI:** WinForms (default UI Pack)
- **Shell interop:** [Vanara.PInvoke.Shell32](https://github.com/dahall/Vanara) (`Vanara.Windows.Shell`, `Vanara.PInvoke.Shell32`, `Vanara.Windows.Forms`)
- **MVVM:** ReactiveUI + ReactiveUI.WinForms
- **DI:** Microsoft.Extensions.DependencyInjection
- **Logging:** Microsoft.Extensions.Logging with Serilog sink (file + debug output)
- **Testing:** xUnit for `Explorer.Core` (UI-free, easily testable). Manual smoke matrix for shell hosting.
- **Build:** `dotnet build` + `dotnet publish` to a single trimmed self-contained executable. Target a sub-20MB published size.
- **CI:** GitHub Actions, Windows runner, build + xUnit run on every push to main.
- **License:** Source-available. License model TBD. Likely GPLv3 to mirror Explorer++.

### Dev Setup

1. Install **.NET 8 SDK** (Windows). Verify `dotnet --version` shows 8.x.
2. Install **Visual Studio 2022 17.8+** with the *.NET desktop development* workload, or **JetBrains Rider 2024.1+**. VS Code with C# Dev Kit also works but WinForms designer support is weaker.
3. Clone the repo. `dotnet restore` from the solution root.
4. Set `Explorer.App` as startup project. F5 to run.
5. For shell extension debugging, run Visual Studio elevated; some extensions refuse to load in low-integrity processes.
6. Recommended test machines:
   - Windows 10 22H2 (baseline target)
   - Windows 11 23H2 (compatibility surface)
   - Windows 11 with classic-shell tweaks disabled (worst-case context-menu testing)

---

## Honest Risks and Open Questions

1. **`IExplorerBrowser` is an old API.** Microsoft hasn't deprecated it, but it has also not invested in it. Some Win11 shell features may not surface cleanly through it. Concrete known issue: the new Windows 11 simplified context menu sometimes shows when hosting; we'll need to test whether `IExplorerBrowserCommandTarget` or message-pumping tweaks restore the classic menu in hosted scenarios.
2. **Hosting multiple `IExplorerBrowser` instances per process** can be memory-heavy and may have COM apartment threading complications. Need to confirm this is tolerable with 4-8 panes/tabs open.
3. **Some shell extensions assume they're loaded into `explorer.exe`.** Most behave well in any host, but a poorly written extension can crash the hosting process. We may need a process-isolation strategy for v2 (each pane in a worker process, COM-marshalled), but v1 will accept the in-process risk.
4. **High-DPI / per-monitor DPI.** WinForms support here is real but quirky. Need to test all the dual-pane resizing scenarios across mixed-DPI setups.
5. **WinForms is "legacy" by Microsoft's marketing but not by capability.** It is the right choice here, but be prepared for occasional sneering from people who haven't thought about why it works.

---

## Why This Project Exists

Because every existing alternative reimplements what doesn't need reimplementing, and ignores what does. Because Windows 11's Explorer is a regression. Because tabs took twenty years to arrive and dual-pane still hasn't. Because the right answer has been sitting in `shobjidl.h` since 2006 and almost nobody has used it correctly. Because the file manager is the most-used application on the operating system and it deserves to be exactly right.

Noctus Explorer is the file manager that hosts Explorer instead of impersonating it.
