# Noctus Explorer — Functional Specification

> **Version:** 0.1 Draft
> **Date:** 2026-05-23
> **Status:** Design Phase

---

## 1. Product Definition

**Noctus Explorer** is a dual-pane file manager for Windows and macOS that hosts each platform's native file browsing view rather than reimplementing it.

- On **Windows**, it embeds the real Explorer view via the `IExplorerBrowser` COM interface.
- On **macOS**, it embeds the real Finder column/list/icon views via `NSBrowser` / `NSOutlineView` backed by `NSFileManager` and `NSWorkspace`.

The user gets the native shell's full fidelity — context menus, drag-drop, thumbnails, third-party extensions — wrapped in power-user chrome: dual panes, tabs, bookmarks, cross-pane operations, and a command palette.

### 1.1 What This Is Not

- Not a cross-platform toolkit app that looks the same everywhere. Each platform gets native chrome.
- Not a feature-kitchen-sink. If the OS already does it well, we host it; we don't rebuild it.
- Not a cloud file manager, archive tool, or terminal emulator with a file browser bolted on.

---

## 2. Target Platforms

| Platform | Min Version | UI Framework | Shell Hosting Strategy |
|----------|-------------|--------------|----------------------|
| Windows | Windows 10 1903+ | WinForms | `IExplorerBrowser` COM |
| macOS | macOS 13 Ventura+ | AppKit (Cocoa) | `NSOutlineView` / `NSCollectionView` with `FileManager` + `NSWorkspace` |

### 2.1 Visual Identity Per Platform

**Windows:** Pre-Windows 11 aesthetic. No Fluent/Mica/acrylic. Classic menu bar, sharp corners, dense layout. Visual target is Windows 10 with the ribbon collapsed. On Windows 11, force the classic context menu by default.

**macOS:** Native AppKit chrome. Follows the system appearance (light/dark). Standard macOS title bar, toolbar style, and spacing. Should feel like it belongs next to other well-made Mac apps (like Pathfinder, Transmit, or Forklift) — not like a Windows port.

---

## 3. Core Concepts

### 3.1 Panes

The window contains one or two **panes**. Each pane is an independent file browsing surface showing a single directory. One pane is always the **active pane** (indicated visually). The other is the **inactive pane**.

- **Single-pane mode:** One pane fills the window. The app behaves like a tabbed file browser.
- **Dual-pane mode:** Two panes side-by-side (vertical split) or stacked (horizontal split). Split ratio is adjustable by dragging the divider.

The user can toggle between single and dual-pane mode at any time. The inactive pane's state is preserved when hidden.

### 3.2 Tabs

Each pane has its own independent **tab bar**. Each tab represents a separate directory location within that pane. Tabs can be:

- Created (`Ctrl+T` / `Cmd+T`)
- Closed (`Ctrl+W` / `Cmd+W`)
- Reordered by drag
- Switched via `Ctrl+Tab` / `Ctrl+1..9` (or `Cmd` equivalents)
- Dragged between panes (moves the tab to the other pane's tab bar)

On launch, the previous session's tabs are restored (configurable).

### 3.3 Active Pane

Exactly one pane is active at any time. The active pane:

- Receives keyboard input
- Is the source for cross-pane operations (copy-to-other, move-to-other)
- Is visually indicated (subtle border highlight or title accent)

Switching active pane: `Tab` key, clicking in the other pane, or `Ctrl+Left`/`Ctrl+Right` (`Cmd` on Mac).

### 3.4 Navigation

Each pane has an **address bar** at the top with two modes:

- **Breadcrumb mode** (default): Clickable path segments. Click any segment to navigate to that ancestor.
- **Edit mode**: Click the breadcrumb bar (or press `Ctrl+L` / `Cmd+L`) to type a path directly. Supports auto-completion.

Standard back/forward navigation with history. `Alt+Up` / `Cmd+Up` to go to parent.

---

## 4. Feature Specification

The following features apply to **both platforms** unless explicitly noted.

### 4.1 Cross-Pane Operations

The primary reason dual-pane exists. Selected files in the active pane can be operated on with the inactive pane as the target.

| Action | Windows | macOS | Notes |
|--------|---------|-------|-------|
| Copy to other pane | `F5` | `F5` | Total Commander convention |
| Move to other pane | `F6` | `F6` | Total Commander convention |
| Sync navigation | `Ctrl+Shift+S` | `Cmd+Shift+S` | Navigate inactive pane to same path as active |
| Open same folder | `Ctrl+Shift+O` | `Cmd+Shift+O` | Open active pane's folder in a new tab in the other pane |

Cross-pane copy/move uses the platform's native file operation API:
- Windows: `IFileOperation` (recycle bin support, undo, conflict resolution, progress)
- macOS: `NSFileCoordinator` + `NSFileManager` (Trash support, conflict resolution)

### 4.2 Bookmarks Bar

A horizontal bar of saved folder shortcuts below the toolbar. Shared across both panes.

- Drag a folder onto the bar to add a bookmark
- Right-click a bookmark to rename, remove, or organize into groups
- Click a bookmark to navigate the active pane
- Bookmarks are stored in the settings file, not in the OS bookmark system

Bookmark groups appear as dropdown menus on the bar (one level deep, no nesting).

### 4.3 Command Palette

`Ctrl+Shift+P` / `Cmd+Shift+P` opens a searchable list of every registered command. Fuzzy matching on command name. Each entry shows the current keybinding if one exists.

Commands include all menu actions, view toggles, pane operations, and navigation actions. The palette is the canonical way to discover functionality.

### 4.4 Status Bar

A bar at the bottom of each pane showing:

- Number of items in the current folder
- Number of selected items and their total size
- Free space on the current volume

When no items are selected, shows the folder's total item count and size (computed lazily in the background).

### 4.5 Keyboard Shortcuts

All shortcuts are user-configurable via a JSON file. Defaults follow a hybrid of platform convention and Total Commander convention:

#### Navigation
| Action | Windows Default | macOS Default |
|--------|----------------|---------------|
| Go back | `Alt+Left` | `Cmd+[` |
| Go forward | `Alt+Right` | `Cmd+]` |
| Go to parent | `Alt+Up` | `Cmd+Up` |
| Go to home | `Alt+Home` | `Cmd+Shift+H` |
| Address bar (edit) | `Ctrl+L` | `Cmd+L` |
| Address bar (edit) | `F4` | `F4` |

#### Pane & Tab
| Action | Windows Default | macOS Default |
|--------|----------------|---------------|
| Switch active pane | `Tab` | `Tab` |
| New tab | `Ctrl+T` | `Cmd+T` |
| Close tab | `Ctrl+W` | `Cmd+W` |
| Next tab | `Ctrl+Tab` | `Ctrl+Tab` |
| Previous tab | `Ctrl+Shift+Tab` | `Ctrl+Shift+Tab` |
| Tab 1–9 | `Ctrl+1..9` | `Cmd+1..9` |
| Toggle dual pane | `Ctrl+F2` | `Cmd+F2` |
| Toggle split direction | `Ctrl+Shift+F2` | `Cmd+Shift+F2` |

#### File Operations
| Action | Windows Default | macOS Default |
|--------|----------------|---------------|
| Copy to other pane | `F5` | `F5` |
| Move to other pane | `F6` | `F6` |
| New folder | `F7` | `F7` |
| Delete (to trash) | `F8` or `Delete` | `F8` or `Cmd+Backspace` |
| Rename | `F2` | `Enter` (macOS convention) |
| Refresh | `Ctrl+R` | `Cmd+R` |
| Select all | `Ctrl+A` | `Cmd+A` |
| Copy (clipboard) | `Ctrl+C` | `Cmd+C` |
| Cut (clipboard) | `Ctrl+X` | `Cmd+X` |
| Paste (clipboard) | `Ctrl+V` | `Cmd+V` |

#### Utility
| Action | Windows Default | macOS Default |
|--------|----------------|---------------|
| Command palette | `Ctrl+Shift+P` | `Cmd+Shift+P` |
| Toggle hidden files | `Ctrl+H` | `Cmd+Shift+.` |
| Search / filter | `Ctrl+F` | `Cmd+F` |
| Copy path to clipboard | `Ctrl+Shift+C` | `Cmd+Option+C` |

### 4.6 Quick Filter

`Ctrl+F` / `Cmd+F` activates a filter bar at the top of the active pane. As the user types, the current directory's contents are filtered in real-time by filename. Supports:

- Glob patterns (`*.jpg`, `report*`)
- Case-insensitive by default, case-sensitive with a toggle

This is **not** a recursive search — it filters the current view only. Press `Escape` to clear the filter and show all items.

### 4.7 Folder Size Calculation

Folders in the view can optionally show their total size. This is computed lazily in a background thread when the user requests it:

- Right-click a folder → "Calculate Size"
- Or toggle "Show Folder Sizes" for the current view (calculates all visible folders)

Results are cached for the session and invalidated on file system change notifications.

### 4.8 Batch Rename

`Ctrl+M` / `Cmd+M` on a multi-file selection opens a batch rename dialog. Operations (chainable, applied in order):

- **Find & Replace** — literal text or regex
- **Insert Text** — at position, before/after pattern
- **Change Case** — UPPER, lower, Title, Sentence
- **Number Sequence** — append/prepend incrementing number (configurable start, step, padding)
- **Date Stamp** — insert file's created/modified date in a chosen format

A live preview list shows "before → after" for every selected file. Nothing executes until the user confirms.

### 4.9 Drop Stack

A small persistent panel (collapsible, docked to the bottom or side of the window) that acts as a staging area for files.

- Drag files from any pane/tab onto the drop stack to collect them
- Files from multiple different directories can accumulate in the stack
- Drag files out of the stack to a destination to copy/move them
- The stack holds **references**, not copies — if the original file is moved/deleted, the reference becomes stale (shown visually)
- Clear all, or remove individual items
- Persists across tab/pane navigation but **not** across app sessions

### 4.10 Context Menu System

The context menu is a first-class, user-configurable feature. It has three layers that compose together into a single right-click menu.

#### Layer 1 — OS Native Context Menu

The platform's native shell context menu is always included as the base. This gives the user everything they'd get in Explorer or Finder, including third-party shell extensions.

- **Windows:** Full shell context menu (7-Zip, TortoiseGit, "Open with Code", etc.). On Windows 11, the classic (full) menu is shown by default — not the truncated "Show more options" version.
- **macOS:** Standard context menu via `NSWorkspace` + Services + Quick Actions. Third-party extensions appear as they would in Finder.

#### Layer 2 — Noctus Built-in Items

Noctus prepends its own section at the top of the context menu, visually separated:

- Copy to Other Pane
- Move to Other Pane
- Add to Drop Stack
- Copy Path
- (Any user-created custom actions — see Layer 3)

These built-in items can be hidden individually via Settings → Context Menu if the user doesn't want them.

#### Layer 3 — User-Defined Custom Context Menu Items (Wizard-Driven)

Users can create their own context menu entries through a **visual wizard** in Settings → Context Menu → "Add Custom Action…". No registry editing, no plist hacking, no config files to hand-edit.

##### The Custom Action Wizard

The wizard walks the user through creating a context menu item in a series of clearly labeled steps, all within the app's settings UI.

**Step 1 — Name & Placement**
- **Menu label:** What the user sees in the context menu (e.g., "Open in VS Code", "Convert to PDF", "Upload to Server")
- **Icon:** Optional. Pick from system icons, browse for a custom `.ico`/`.icns`, or leave blank
- **Position:** Where in the Noctus section of the menu this item appears (drag to reorder, or place inside a submenu group)
- **Submenu group:** Optionally nest under a named submenu (e.g., "Dev Tools →" containing "Open in VS Code", "Open in Terminal", "Git Bash Here")

**Step 2 — When to Show (Conditions)**

Checkboxes and dropdowns — no expressions to write:

| Condition | Options |
|-----------|---------|
| Applies to | Files only / Folders only / Both |
| File extension filter | Blank (all) or comma-separated list (e.g., `.jpg, .png, .gif`) |
| Selection count | Any / Single item only / Multiple items only |
| Show only if path contains | Optional substring match on the full path (e.g., only show in project folders containing `.git`) |

If no conditions are set, the item appears on every right-click.

**Step 3 — What to Do (Action)**

A dropdown selects the action type, and the form below it adapts:

| Action Type | Configuration |
|-------------|---------------|
| **Run a program** | Program path (browse button), arguments (with variable substitution — see below), working directory, run hidden (checkbox), run as admin/elevated (checkbox, Windows only) |
| **Run a shell command** | Command text (single line or multi-line script), shell choice (cmd/PowerShell/bash on Windows; zsh/bash on Mac), run in background (checkbox) |
| **Open with application** | Pick an application (browse or select from installed apps list) |
| **Copy text to clipboard** | Template string with variables (e.g., copy a formatted path, a markdown link, etc.) |
| **Open URL** | URL template with variables (e.g., `https://github.com/search?q={filename}`) |

**Variable substitution** — available in all text fields in Step 3:

| Variable | Expands To |
|----------|------------|
| `{path}` | Full path of the selected item (`C:\Users\me\file.txt` or `/Users/me/file.txt`) |
| `{folder}` | Parent folder path |
| `{filename}` | Filename with extension |
| `{basename}` | Filename without extension |
| `{ext}` | Extension without dot |
| `{paths}` | All selected items, one per line (for multi-select) |
| `{paths:quoted}` | All selected items, each quoted, space-separated |
| `{other_pane}` | Path of the inactive pane's current directory |

A **"Test"** button at the bottom of Step 3 runs the action immediately on a user-chosen file so they can verify it works before saving.

**Step 4 — Review & Save**

Shows a summary card:
```
┌─────────────────────────────────────────────┐
│  "Open in VS Code"                    [Edit]│
│  Show: All files and folders                │
│  Action: Run program                        │
│  Command: code "{path}"                     │
│  Shortcut: (none)                           │
└─────────────────────────────────────────────┘
```
Save adds it to the context menu immediately. No restart required.

##### Managing Custom Actions

Settings → Context Menu shows all custom actions in a list:

- **Drag to reorder** — changes the order in the context menu
- **Enable/disable toggle** — per item, without deleting
- **Edit** — reopens the wizard pre-filled
- **Duplicate** — clone an existing action as a starting point
- **Delete** — remove with confirmation
- **Import/Export** — custom actions can be exported to a JSON file and imported on another machine or shared with others

##### Submenu Groups

Custom actions can be organized into named submenus. In the Settings list, groups appear as collapsible headers. The user creates a group by typing a group name in Step 1, or by dragging an action onto another action (which creates a group containing both).

In the context menu, a group renders as a submenu:
```
  Dev Tools          →  Open in VS Code
                        Open in Terminal
                        Git Bash Here
  ───────────────────
  Copy to Other Pane
  Move to Other Pane
```

##### OS-Level Registration (Optional)

By default, custom actions only appear when right-clicking inside Noctus Explorer. However, the wizard includes an **advanced option**:

> ☐ **Also register this action in the OS context menu**
> (Makes this action appear when right-clicking files in Windows Explorer / macOS Finder too)

When checked, Noctus registers the action using the platform's native mechanism:

- **Windows:** Writes a registry key under `HKCU\Software\Classes\*\shell\NoctusExplorer.{action-id}\` (for files) and/or `HKCU\Software\Classes\Directory\shell\` (for folders). Uses `HKCU` so no admin rights are needed. The registry entry points back to the Noctus executable with a command-line flag that executes the action directly (e.g., `noctus-explorer.exe --run-action {action-id} "%1"`).
- **macOS:** Creates a Quick Action (Automator workflow or Shortcuts action, depending on macOS version) in `~/Library/Services/` that invokes the Noctus action. The action appears in Finder's context menu under Quick Actions / Services.

Unregistration is automatic: disabling or deleting the action in Noctus removes the OS-level registration. Uninstalling Noctus cleans up all registered entries.

A warning is shown when enabling this option:
> "This will modify your system's context menu outside of Noctus Explorer. The registration uses per-user settings (no admin required) and will be cleaned up if you disable or delete this action."

##### Predefined Action Templates

The wizard offers a "Start from template" option with common actions pre-configured:

- Open in VS Code
- Open in Terminal / PowerShell / Command Prompt
- Open in Sublime Text
- Copy as Markdown Link
- Copy folder tree to clipboard
- Set as wallpaper (images)
- Create ZIP of selected items

Templates are starting points — the user can modify anything before saving.

### 4.11 File View Modes

The hosted native view handles view modes. The app provides toolbar/menu controls to switch between them:

**Windows** (via `IExplorerBrowser` / `IFolderView2`):
- Extra Large / Large / Medium / Small Icons
- List
- Details
- Tiles
- Content

**macOS** (native AppKit views):
- Icon view
- List view
- Column view
- Gallery view (macOS 10.14+)

View mode preference is stored per-tab and restored on session reload.

### 4.12 Hidden Files Toggle

A toolbar button and keyboard shortcut (`Ctrl+H` / `Cmd+Shift+.`) toggle visibility of hidden/system files. Default: **visible** (power-user default). The setting is global, not per-pane.

### 4.13 Sorting

Sort controls are provided via the view header (in list/details view) and via a menu. Available sort fields:

- Name, Size, Date Modified, Date Created, Kind/Type, Extension

Folders-before-files sorting is the default (configurable).

### 4.14 Preview Pane

`F3` or `Space` (tap) toggles a resizable preview panel on the right side or bottom of the active pane. The preview renders the selected file inline without opening an external application.

**Supported content types:**

| Type | Rendering |
|------|-----------|
| Images (PNG, JPG, GIF, BMP, SVG, WEBP) | Scaled preview with zoom on click |
| PDF | Page-by-page rendering |
| Plain text, code files | Syntax-highlighted with line numbers (read-only) |
| Markdown | Rendered HTML |
| Audio (MP3, WAV, FLAC, AAC) | Waveform + playback controls |
| Video (MP4, MKV, MOV) | Thumbnail + playback controls |
| Binary / unknown | Hex view (first 4 KB, scrollable) |

**Platform implementation:**
- **Windows:** Use the shell's `IPreviewHandler` COM interface where a handler is registered (leverages third-party preview handlers like those from Office, Adobe, etc.). Fall back to built-in renderers for common types.
- **macOS:** Use `QLPreviewView` (Quick Look framework) which already handles most file types via system and third-party Quick Look plugins. Fall back to built-in renderers where Quick Look doesn't cover.

The preview pane remembers its visibility and size per session. It does not auto-play video/audio — the user must click play.

### 4.15 Operations Queue

All file operations (copy, move, delete) go through a centralized **operations queue** displayed as a collapsible panel at the bottom of the window.

Each operation shows:
- Source → destination
- Progress bar with percentage
- Transfer speed and ETA
- Pause / resume / cancel buttons

Multiple operations can run simultaneously (configurable max concurrency, default 2). When more operations are queued than can run, they wait in a visible queue that can be reordered by drag.

Conflict resolution (file already exists) pauses the operation and shows a dialog with options: Overwrite, Skip, Rename (auto-append number), Overwrite if Newer, Apply to All. The dialog shows both files' sizes and dates side-by-side.

The queue persists across pane/tab navigation. When the last operation completes, the panel auto-collapses after 5 seconds (configurable). A brief notification (system toast on both platforms) is shown when a background operation completes.

### 4.16 File Watcher & Change Highlighting

Both panes automatically refresh when the underlying directory changes (file created, modified, renamed, or deleted by an external process).

- **Windows:** `SHChangeNotifyRegister` (already in the shell adapter)
- **macOS:** `FSEvents` / `DispatchSource`

When a change is detected:
- The view refreshes automatically (no manual F5 needed)
- Newly created or modified files are briefly highlighted (subtle background flash, ~2 seconds) so the user notices what changed
- Deleted files disappear from the view

Auto-refresh can be disabled per-pane for performance on directories with high churn (e.g., build output folders). A manual refresh (`Ctrl+R` / `Cmd+R`) is always available.

### 4.17 Checksum / Hash

Right-click one or more files → "Checksums" submenu:

- **Generate:** Compute MD5, SHA-1, SHA-256, or SHA-512. Results shown in a dialog with copy-to-clipboard buttons. Optionally save as a `.sha256` (or similar) sidecar file alongside the original.
- **Verify:** Select a checksum file (`.md5`, `.sha256`, `.sfv`) and verify all files it references. Results shown as a pass/fail list with color coding.

For multi-file selection, all hashes are computed in parallel on background threads with a progress indicator. This is also available as a command: `tools.checksum` in the command palette.

### 4.18 Action Toolbar

A configurable row of buttons below the menu bar (or merged with the bookmark bar, user's choice). Each button triggers a registered command — either a built-in command or a user-created custom action from the context menu wizard (Section 4.10).

- Right-click the toolbar → "Customize Toolbar…"
- Add buttons by dragging from a command list (same registry as the command palette)
- Remove buttons by dragging off the toolbar
- Separator items can be inserted between groups
- Each button shows an icon + optional text label (configurable: icon only, text only, or both)
- Custom actions created in the wizard (Section 4.10) are automatically available as toolbar candidates

Default toolbar buttons: Back, Forward, Up, New Tab, View Mode dropdown, Toggle Hidden, Toggle Dual Pane, Filter, Command Palette.

---

## 5. Settings & Configuration

### 5.1 Storage

Settings are stored as a single JSON file:

- **Portable mode:** If a file named `noctus-explorer.json` exists next to the executable, it is used. This makes the app USB-portable.
- **Installed mode (Windows):** `%APPDATA%\NoctusExplorer\settings.json`
- **Installed mode (macOS):** `~/Library/Application Support/NoctusExplorer/settings.json`

### 5.2 Settings Categories

| Category | Examples |
|----------|---------|
| General | Restore session on launch, single-instance mode, default new-tab path |
| Appearance | Dark mode override (system/light/dark), show status bar, show bookmarks bar |
| Panes | Default layout (single/dual), default split direction, split ratio |
| Shortcuts | Full keybinding map (action ID → key chord) |
| Context Menu | Custom action definitions, built-in item visibility, OS registration state |
| Bookmarks | Ordered list of bookmarks with name, path, and optional group |
| Session | Last window position/size, open tabs per pane with paths and view modes |

### 5.3 Configurable Behaviors

| Setting | Default | Notes |
|---------|---------|-------|
| `general.restoreSession` | `true` | Reopen last session's tabs on launch |
| `general.singleInstance` | `true` | New invocation opens a tab in existing window |
| `general.newTabPath` | Home directory | Where new tabs open |
| `general.confirmDelete` | `true` | Ask before deleting (even to trash) |
| `appearance.theme` | `"system"` | `"system"`, `"light"`, or `"dark"` |
| `appearance.showHiddenFiles` | `true` | Power-user default |
| `appearance.showFileExtensions` | `true` | Always show extensions |
| `appearance.foldersFirst` | `true` | Sort folders before files |
| `panes.defaultLayout` | `"dual"` | `"single"` or `"dual"` |
| `panes.splitDirection` | `"vertical"` | `"vertical"` or `"horizontal"` |
| `panes.splitRatio` | `0.5` | 0.0–1.0 |
| `win.classicContextMenu` | `true` | Windows 11 only: force classic menu |

---

## 6. Architecture

### 6.1 Layer Diagram

```
┌─────────────────────────────────────────────────┐
│                   App Shell                      │
│         (composition root, DI, entry point)      │
├────────────────────┬────────────────────────────┤
│   UI: WinForms     │    UI: AppKit (Cocoa)       │
│   (Windows only)   │    (macOS only)             │
├────────────────────┴────────────────────────────┤
│              UI Contracts (interfaces)           │
│   IPaneView, ITabHost, ISplitLayout, IUIPack     │
├─────────────────────────────────────────────────┤
│              Platform Shell Adapter              │
│   Windows: IExplorerBrowser, IFileOperation      │
│   macOS: NSFileManager, NSWorkspace              │
├─────────────────────────────────────────────────┤
│                    Core                          │
│   Models, ViewModels, Commands, Services         │
│   (pure C# / pure Swift — no platform deps)     │
└─────────────────────────────────────────────────┘
```

### 6.2 Language Decision

| Layer | Windows | macOS |
|-------|---------|-------|
| Core | C# (.NET 8) | Swift |
| Shell Adapter | C# + Vanara (COM interop) | Swift + AppKit/Foundation |
| UI | C# WinForms + ReactiveUI | Swift + AppKit |
| App Shell | C# | Swift |

The core layer on each platform is **native to that platform's ecosystem**. There is no shared cross-platform code. The spec is the contract — both implementations conform to the same feature set and UX patterns, but the codebases are independent.

**Rationale:** A shared C# core via .NET on Mac would mean Avalonia or Mac Catalyst, both of which feel non-native. A shared Swift core on Windows would mean… nothing good. The value of this app is native feel; that requires native code. The spec document is what keeps them in sync.

### 6.3 Project Structure — Windows

```
NoctusExplorer.sln
├── Explorer.Core/                 # Pure C#. No platform refs.
│   ├── Models/                    # FileEntry, PathRef, Bookmark, Tab, PaneState
│   ├── ViewModels/                # MainViewModel, PaneViewModel, TabViewModel
│   ├── Commands/                  # ICommand implementations
│   ├── Services/                  # CommandRegistry, KeyBindingResolver,
│   │                              # BookmarkStore, SettingsStore, DropStack,
│   │                              # CustomActionStore, CustomActionEngine
│   └── Abstractions/              # IShellService, IFileOperations, IClipboardService
│
├── Explorer.Shell.Windows/        # COM interop via Vanara
│   ├── ShellService.cs            # IShellFolder, PIDL operations
│   ├── FileOperations.cs          # IFileOperation wrapper
│   ├── ClipboardService.cs        # Shell clipboard formats
│   ├── ShellNotifications.cs      # SHChangeNotifyRegister
│   └── ContextMenuRegistrar.cs    # HKCU registry write/remove for OS-level actions
│
├── Explorer.UI.Contracts/         # IPaneView, ITabHost, ISplitLayout, IUIPack
│
├── Explorer.UI.WinForms/          # WinForms UI Pack
│   ├── MainForm.cs
│   ├── ExplorerBrowserPane.cs     # Hosts IExplorerBrowser
│   ├── TabStripControl.cs
│   ├── BookmarkBarControl.cs
│   ├── AddressBarControl.cs
│   ├── StatusBarControl.cs
│   ├── DropStackPanel.cs
│   ├── CommandPaletteDialog.cs
│   ├── BatchRenameDialog.cs
│   ├── CustomActionWizard.cs      # Multi-step wizard for creating context menu actions
│   ├── ContextMenuSettingsPage.cs  # Settings page: list, reorder, enable/disable actions
│   └── ClassicTheme.cs
│
├── Explorer.App/                  # Entry point, DI wiring
│   ├── Program.cs
│   └── appsettings.json
│
└── Explorer.Tests/                # xUnit tests for Core
```

### 6.4 Project Structure — macOS

```
NoctusExplorer.xcodeproj
├── Core/                          # Pure Swift. No AppKit imports.
│   ├── Models/                    # FileEntry, PathRef, Bookmark, Tab, PaneState
│   ├── ViewModels/                # MainViewModel, PaneViewModel, TabViewModel
│   ├── Commands/                  # Command protocol implementations
│   └── Services/                  # CommandRegistry, KeyBindingResolver,
│                                  # BookmarkStore, SettingsStore, DropStack,
│                                  # CustomActionStore, CustomActionEngine
│
├── ShellAdapter/                  # Foundation + AppKit (file ops only)
│   ├── FileOperations.swift       # NSFileManager + NSFileCoordinator
│   ├── WorkspaceService.swift     # NSWorkspace (open, reveal, app lookup)
│   ├── FSEventStream.swift        # File system change monitoring
│   └── QuickActionRegistrar.swift # Create/remove ~/Library/Services/ workflows
│
├── UI/                            # AppKit UI
│   ├── MainWindowController.swift
│   ├── PaneViewController.swift   # Hosts NSOutlineView / NSCollectionView / NSBrowser
│   ├── TabBarView.swift
│   ├── BookmarkBarView.swift
│   ├── AddressBarView.swift
│   ├── StatusBarView.swift
│   ├── DropStackView.swift
│   ├── CommandPalettePanel.swift
│   ├── BatchRenameSheet.swift
│   ├── CustomActionWizardController.swift
│   └── ContextMenuPreferencesViewController.swift
│
├── App/
│   ├── AppDelegate.swift
│   └── main.swift
│
└── Tests/                         # XCTest for Core
```

---

## 7. Platform-Specific Behaviors

Some behaviors differ by platform because the OS handles them differently. These are not feature gaps — they are correct platform adaptation.

| Behavior | Windows | macOS |
|----------|---------|-------|
| Shell view hosting | `IExplorerBrowser` COM (real Explorer) | `NSOutlineView` / `NSCollectionView` (native AppKit views, not literally Finder) |
| Context menu | Shell context menu with third-party extensions | `NSMenu` from `NSWorkspace` + Services |
| File operations | `IFileOperation` (undo, recycle bin, progress) | `NSFileManager` + `NSFileCoordinator` (Trash, progress) |
| File change monitoring | `SHChangeNotifyRegister` | `FSEvents` / `DispatchSource.makeFileSystemObjectSource` |
| Hidden files | File attribute flag | Dot-prefix convention + `chflags hidden` |
| Dark mode | Win32 dark mode APIs | `NSAppearance` (automatic) |
| Single instance | Named mutex + WM_COPYDATA | `NSDistributedNotificationCenter` or `NSRunningApplication` |
| Window chrome | Classic Win32 frame, no custom title bar | Standard AppKit title bar + toolbar |
| System tray / menu bar | Not present | Not present |

### 7.1 macOS Shell View — Clarification

macOS does not expose Finder as an embeddable component the way Windows exposes `IExplorerBrowser`. The macOS pane implementation uses **native AppKit file browsing views** (`NSOutlineView` for list, `NSBrowser` for column, `NSCollectionView` for icon/gallery) backed by `FileManager`. This is not "hosting Finder" — it is building a native file browser from the same AppKit components Finder itself uses.

The result feels native because it **is** native AppKit — not because it embeds Finder. Third-party Finder extensions (Quick Actions, Share menu items) are surfaced via standard `NSWorkspace` and `NSSharingServicePicker` APIs.

---

## 8. Menu Bar Structure

Both platforms have a menu bar. The structure is identical; only accelerator keys differ.

```
File
  New Tab                          Ctrl+T / Cmd+T
  Close Tab                        Ctrl+W / Cmd+W
  New Folder                       F7
  New File                         Ctrl+Shift+N / Cmd+Shift+N
  ─────
  Close Window                     Alt+F4 / Cmd+Q

Edit
  Cut                              Ctrl+X / Cmd+X
  Copy                             Ctrl+C / Cmd+C
  Paste                            Ctrl+V / Cmd+V
  ─────
  Select All                       Ctrl+A / Cmd+A
  Invert Selection                 Ctrl+Shift+A / Cmd+Shift+A
  ─────
  Rename                           F2 / Enter
  Delete                           F8 / Cmd+Backspace
  ─────
  Batch Rename…                    Ctrl+M / Cmd+M
  Copy Path                        Ctrl+Shift+C / Cmd+Option+C

View
  Single Pane                      Ctrl+1 / Cmd+1  (when not tab-switching)
  Dual Pane – Vertical             Ctrl+2 / Cmd+2
  Dual Pane – Horizontal           Ctrl+3 / Cmd+3
  ─────
  Icons / List / Details / …       (submenu, platform-appropriate modes)
  ─────
  Show Hidden Files                Ctrl+H / Cmd+Shift+.
  Show File Extensions             (always on by default)
  Folders First                    (toggle)
  ─────
  Sort By →                        (submenu: Name, Size, Date, Kind, Extension)
  ─────
  Refresh                          Ctrl+R / Cmd+R

Go
  Back                             Alt+Left / Cmd+[
  Forward                          Alt+Right / Cmd+]
  Parent Folder                    Alt+Up / Cmd+Up
  Home                             Alt+Home / Cmd+Shift+H
  ─────
  (Bookmark list appears here)

Tools
  Command Palette…                 Ctrl+Shift+P / Cmd+Shift+P
  Filter…                          Ctrl+F / Cmd+F
  Calculate Folder Size            (selected folders)
  ─────
  Copy to Other Pane               F5
  Move to Other Pane               F6
  ─────
  Custom Actions…                  (opens context menu settings page)

Help
  About Noctus Explorer
  Keyboard Shortcuts Reference
```

---

## 9. Window Layout Specification

### 9.1 Wireframe — Dual Pane Vertical Split

```
┌──────────────────────────────────────────────────────────────┐
│  File  Edit  View  Go  Tools  Help                           │  ← Menu bar
├──────────────────────────────────────────────────────────────┤
│  [◀][▶][▲] [≡][⊞][👁] [⊟] [.*] [⌘P]  ★ Home  ★ Downloads   │  ← Action toolbar + bookmarks
├─────────────────────────────┬────────────────────────────────┤
│  [Tab 1] [Tab 2] [+]       │  [Tab 1] [Tab 2] [Tab 3] [+]  │  ← Tab bars
├─────────────────────────────┼────────────────────────────────┤
│  ◀ ▶ ▲  C:\Users\me\Docs   │  ◀ ▶ ▲  D:\Backup\2026        │  ← Address bars
├─────────────────────────────┼────────────────────────────────┤
│                             │                         ┌──────┤
│   [Native file view]       │   [Native file view]    │Preview│
│                             │                         │ Pane  │
│   Documents/                │   2026-05-backup/       │      │
│   Photos/                   │   archive.zip           │[img] │
│  ►report.docx               │   notes.txt             │      │
│   budget.xlsx               │                         │830 KB│
│                             │                         │.docx │
├─────────────────────────────┼─────────────────────────┴──────┤
│  4 items | 2 sel (14.3 MB)  │  3 items | 128.5 GB free       │  ← Status bars
├──────────────────────────────────────────────────────────────┤
│  Drop Stack: report.docx, Photos/vacation.jpg        [Clear] │  ← Drop stack
├──────────────────────────────────────────────────────────────┤
│  ▼ Operations: Copying 3 files → D:\Backup  ████░░ 62% 4.2s │  ← Ops queue
└──────────────────────────────────────────────────────────────┘
```

### 9.2 Active Pane Indicator

The active pane has a 2px colored border on its inner edge (e.g., system accent color on both platforms). The address bar text may also be slightly bolder or the background subtly tinted. The indicator must be visible but not distracting.

---

## 10. Command Registry

Every action in the app is a registered command with a stable string ID. This is the backbone for keybindings, the command palette, menus, and toolbar buttons.

### 10.1 Command ID Convention

```
<namespace>.<action>

Examples:
  file.newTab
  file.closeTab
  file.newFolder
  edit.copy
  edit.paste
  edit.rename
  edit.delete
  edit.batchRename
  edit.copyPath
  view.toggleDualPane
  view.splitVertical
  view.splitHorizontal
  view.toggleHidden
  view.refresh
  view.sortByName
  view.sortBySize
  go.back
  go.forward
  go.parent
  go.home
  pane.switchActive
  pane.copyToOther
  pane.moveToOther
  pane.syncNavigation
  tools.commandPalette
  tools.filter
  tools.calculateSize
  dropstack.addSelection
  dropstack.clear
```

### 10.2 Command Properties

Each registered command has:

| Property | Type | Description |
|----------|------|-------------|
| `id` | string | Stable identifier (e.g. `"pane.copyToOther"`) |
| `name` | string | Human-readable name (e.g. `"Copy to Other Pane"`) |
| `description` | string | One-line description for the command palette |
| `defaultBinding` | KeyChord? | Platform-specific default keybinding |
| `icon` | Icon? | Optional icon for toolbar/menu |
| `canExecute` | () → bool | Whether the command is currently available |
| `execute` | () → void | The action |

---

## 11. Build & Distribution

### 11.1 Windows

- **Language:** C# 12, .NET 8, `net8.0-windows10.0.19041.0`
- **Build:** `dotnet publish` → single-file self-contained executable
- **Target size:** < 20 MB
- **Distribution:** Portable ZIP (no installer required) + optional MSI/MSIX installer
- **CI:** GitHub Actions, Windows runner

### 11.2 macOS

- **Language:** Swift 5.9+, targeting macOS 13+
- **Build:** Xcode / `xcodebuild` → `.app` bundle
- **Target size:** < 15 MB
- **Distribution:** DMG with drag-to-Applications, notarized and stapled
- **CI:** GitHub Actions, macOS runner
- **Signing:** Developer ID for direct distribution; optionally Mac App Store (sandboxing implications TBD)

---

## 12. Milestones — Build Order

Each milestone is independently demoable. Risk-first: prove the hardest unknowns early.

### Windows Track

| # | Milestone | Description | Est. |
|---|-----------|-------------|------|
| W0 | Shell Spike | Throwaway WinForms app hosting one `IExplorerBrowser`. Confirm context menus, drag-drop, thumbnails, third-party extensions all work. **If this fails, re-evaluate the entire approach.** | 1–2 days |
| W1 | Core + Shell Adapter | `Explorer.Core` and `Explorer.Shell.Windows` with console test harness. Enumerate, copy, listen for changes. xUnit tests. | 1 week |
| W2 | Single Pane UI | One pane, one tab, address bar, status bar. Full native view hosting. | 1 week |
| W3 | Dual Pane | Split layout, active pane tracking, F5/F6 cross-pane ops. | 3–4 days |
| W4 | Tabs | Tab strip per pane. New/close/reorder/switch/restore. | 3–4 days |
| W5 | Bookmarks + Command Palette | Bookmark bar, command palette with fuzzy search. | 3–4 days |
| W6 | Drop Stack + Batch Rename | Drop stack panel, batch rename dialog. | 3–4 days |
| W7 | Custom Context Menu Wizard | Settings UI for custom actions: wizard flow, condition editor, variable substitution, action execution engine, import/export. OS-level registration (HKCU registry). | 1 week |
| W8 | Preview Pane + Ops Queue | Preview pane (IPreviewHandler + built-in renderers), operations queue with pause/resume/cancel, conflict resolution dialog. | 1 week |
| W9 | Polish + Release | Quick filter, folder size calc, file watcher with change highlighting, checksums, action toolbar, portable mode, single-instance, classic context menu on Win11, dark mode, settings persistence, installer. v1.0. | 1–2 weeks |

### macOS Track

| # | Milestone | Description | Est. |
|---|-----------|-------------|------|
| M0 | AppKit Spike | Bare AppKit window with `NSOutlineView` list + `NSBrowser` column view. Confirm native feel, context menus, drag-drop, Quick Look. | 1–2 days |
| M1 | Core + Shell Adapter | Swift Core models/services + FileManager/NSWorkspace adapter. Unit tests. | 1 week |
| M2 | Single Pane UI | One pane, one tab, address bar, status bar, view mode switching. | 1 week |
| M3 | Dual Pane | NSSplitView, active pane tracking, cross-pane ops. | 3–4 days |
| M4 | Tabs | Tab bar per pane. Standard Mac tab behavior. | 3–4 days |
| M5 | Bookmarks + Command Palette | Bookmark bar, command palette (NSPanel with search field). | 3–4 days |
| M6 | Drop Stack + Batch Rename | Drop stack view, batch rename sheet. | 3–4 days |
| M7 | Custom Context Menu Wizard | Settings UI for custom actions: wizard sheet, condition editor, variable substitution, action execution engine, import/export. OS-level registration (Quick Actions in ~/Library/Services/). | 1 week |
| M8 | Preview Pane + Ops Queue | Preview pane (QLPreviewView + built-in renderers), operations queue with pause/resume/cancel, conflict resolution sheet. | 1 week |
| M9 | Polish + Release | Quick filter, folder size calc, file watcher with change highlighting, checksums, action toolbar, dark mode, settings persistence, DMG packaging, notarization. v1.0. | 1–2 weeks |

The two tracks can run in parallel or sequentially. The spec keeps them in sync.

---

## 13. Out of Scope for v1

These are explicitly not in v1. Listing them prevents scope creep.

- Cloud storage integrations (rely on OS-level integrations like OneDrive, iCloud)
- Built-in archive browsing (rely on 7-Zip / macOS Archive Utility)
- Built-in terminal emulator
- Plugin/extension marketplace
- Theming engine
- Git/SVN status integration
- Process manager
- Scripting/macro system
- Multi-window support (v1 is single-window; new invocations open tabs)

### Considered for v2+

These are strong features that were researched and validated but deferred to keep v1 focused:

| Feature | Description | Why Deferred |
|---------|-------------|--------------|
| **Folder Sync / Dir Diff** | Compare two directories visually (identical / modified / missing), then sync one-way or both-ways. Core reason users pick Total Commander and ForkLift. | Significant complexity — needs its own diff engine, conflict UI, and robust error handling for large trees. |
| **Remote Connections (SFTP/FTP/WebDAV/S3)** | Browse remote servers as if they were local panes. Copy between local and remote, or remote-to-remote. Replaces FileZilla/Cyberduck. | Requires a virtual filesystem layer, credential management, connection pooling, and timeout handling. |
| **Embedded Terminal Pane** | A real shell embedded at the bottom of the window, bidirectionally synced with the active pane's directory. Top developer request. | Requires embedding a terminal emulator (conpty on Windows, pseudo-tty on Mac), bidirectional path sync, and focus management. |
| **Side-by-Side File Diff** | Select two files → compare line-by-line with color-coded diff highlighting. Essential for code and config files. | Needs a diff algorithm, syntax highlighting, and a scrollable split view. Could integrate with external diff tools as a simpler alternative. |
| **Duplicate File Finder** | Scan selected folders for duplicate files by content hash. Show groups, allow selective deletion. | Needs efficient hashing of potentially millions of files, a results UI with grouping, and safe batch deletion. |
| **Disk Usage Treemap** | Visual breakdown of disk usage as a treemap or sunburst chart. Few file managers have this — genuine differentiator. | Requires a treemap rendering component and deep recursive size computation. Standalone tools (WinDirStat, GrandPerspective) do this well already. |
| **Virtual Folders / Collections** | Named containers holding shortcuts to files from anywhere on disk. Browse and operate on them as if co-located. Project-centric workflow. | Needs a persistent collection store, a virtual filesystem adapter, and UI for managing collections. |
| **Custom Metadata Columns** | Add sortable columns for EXIF data, audio tags, video resolution, PDF page count, git status. Directory Opus's killer feature. | Requires metadata extraction for dozens of file types, column definition UI, and performance optimization for large directories. |
| **File/Folder Notes** | Attach plain-text comments to any file/folder, stored in a local database. | Needs a sidecar database, indexing, and a notes editor panel. |
| **Color Labels / Tags** | Assign persistent color labels and text tags for workflow status. On Mac, honor native Finder tags. | Needs a cross-platform tag storage strategy (sidecar DB on Windows, xattrs on Mac), filter/sort integration. |

---

## 14. Glossary

| Term | Definition |
|------|------------|
| **Pane** | One of the two file browsing surfaces in the window |
| **Active pane** | The pane currently receiving input and serving as source for operations |
| **Inactive pane** | The other pane; target for cross-pane operations |
| **Tab** | A navigation context within a pane; each tab has its own path and view state |
| **Drop stack** | Staging area for collecting files from multiple locations before acting on them |
| **Command palette** | Searchable overlay listing all available commands |
| **Bookmark** | A saved folder shortcut displayed in the bookmark bar |
| **Quick filter** | Live filename filter on the current directory view |
| **PIDL** | (Windows) Pointer to Item ID List — the shell's universal item reference |
| **IExplorerBrowser** | (Windows) COM interface that hosts the real Explorer view |
| **UI Pack** | (Windows architecture) A swappable UI implementation conforming to the UI Contracts |
| **PathRef** | Internal model representing a filesystem path (abstraction over string path / PIDL / URL) |
