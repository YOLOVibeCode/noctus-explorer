# W0 — IExplorerBrowser Spike

Throwaway project to prove that hosting the real Windows Explorer view via `IExplorerBrowser` COM works.

## Build & Run

Requires Windows 10+ with [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.

```
cd spike/windows/ExplorerSpike
dotnet run
```

## What to Test

Open the app and verify each of these manually:

- [ ] **Rubber-band selection** — click and drag to select multiple files
- [ ] **Double-click navigation** — double-click a folder to enter it
- [ ] **Right-click context menu** — full shell menu including third-party extensions (7-Zip, TortoiseGit, etc.)
- [ ] **Drag-drop** — drag files to/from a real Explorer window
- [ ] **In-place rename** — press F2 on a selected file
- [ ] **Thumbnails** — switch to Large Icons view, verify thumbnails render
- [ ] **Column sort** — in Details view, click column headers to sort
- [ ] **Two instances** — both panes work independently, no conflicts
- [ ] **Tab key** — switches active pane (border indicator moves)
- [ ] **Window resize** — both panes resize cleanly

## If the spike fails

If any of the above don't work natively, document what's broken. The entire project premise depends on this working. Possible issues:

- **Windows 11 modern context menu** — may need registry tweak or `IExplorerBrowser::SetPropertyBag` to force classic menu
- **Vanara API surface** — if `ExplorerBrowser` wrapper doesn't expose enough control, may need to drop to raw COM interop
- **Memory** — open Task Manager and check memory usage with 2 panes. Note the number for future reference

## Architecture Note

This spike uses `Vanara.Windows.Forms.ExplorerBrowser` which wraps the COM `IExplorerBrowser` interface. In the real app, this is behind our `IPaneView` contract in `Explorer.UI.WinForms/ExplorerBrowserPane.cs`.
