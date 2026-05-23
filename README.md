# Noctus Explorer

> A native dual-pane file manager that hosts the real OS file view instead of reimplementing it.

- **Windows:** Embeds `IExplorerBrowser` COM — the actual Windows Explorer view with full shell extension support
- **macOS:** Native AppKit views (`NSOutlineView`, `NSBrowser`) — not a port, a native Mac app

---

## Downloads

Grab the latest release from the [Releases page](https://github.com/YOLOVibeCode/noctus-explorer/releases).

| Platform | Architecture | File |
|----------|-------------|------|
| Windows | x64 (Intel/AMD) | `NoctusExplorer-x.y.z-win-x64.zip` |
| Windows | x86 (32-bit) | `NoctusExplorer-x.y.z-win-x86.zip` |
| Windows | ARM64 (Snapdragon/Copilot+) | `NoctusExplorer-x.y.z-win-arm64.zip` |
| macOS | ARM64 (Apple Silicon) | `NoctusExplorer-x.y.z-macos-arm64.zip` |

All Windows builds are **self-contained single-file executables** — no .NET runtime required. Unzip and run.

---

## Features

- **Dual pane** — two independent file views side by side (vertical or horizontal split)
- **Tabs** — per-pane tab bar with Ctrl+T/W, Ctrl+Tab, Ctrl+1-9
- **Native shell** — context menus, drag-drop, thumbnails, third-party extensions all work because the view IS the real Explorer/Finder
- **Bookmark bar** — click to navigate, drag to add, right-click to manage
- **Command palette** — Ctrl+Shift+P, fuzzy search over every command
- **Cross-pane operations** — F5 copy to other pane, F6 move to other pane
- **Address bar** — breadcrumb display with clickable segments, Ctrl+L to type a path
- **Status bar** — item count, selection size, free disk space
- **Keyboard-driven** — every action has a shortcut, all configurable
- **Classic aesthetic** — pre-Windows 11 look on Windows, native AppKit on Mac
- **Portable** — drop `noctus-explorer.json` next to the exe for portable settings

### Keyboard Shortcuts

| Action | Windows | macOS |
|--------|---------|-------|
| Switch pane | Tab | Tab |
| Copy to other pane | F5 | F5 |
| Move to other pane | F6 | F6 |
| New tab | Ctrl+T | Cmd+T |
| Close tab | Ctrl+W | Cmd+W |
| Next/prev tab | Ctrl+Tab / Ctrl+Shift+Tab | Ctrl+Tab |
| Tab by number | Ctrl+1..9 | Cmd+1..9 |
| Back / Forward | Alt+Left / Alt+Right | Cmd+[ / Cmd+] |
| Parent folder | Alt+Up | Cmd+Up |
| Home | Alt+Home | Cmd+Shift+H |
| Address bar | Ctrl+L or F4 | Cmd+L |
| Refresh | Ctrl+R | Cmd+R |
| Command palette | Ctrl+Shift+P | Cmd+Shift+P |

---

## Building from Source

### Windows

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
# Run locally
dotnet run --project src/Explorer.App

# Run tests
dotnet test tests/Explorer.Core.Tests

# Publish for a specific architecture
dotnet publish src/Explorer.App/Explorer.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64

# Or use the publish script (all three architectures)
.\publish.ps1
.\publish.ps1 -Runtime win-arm64
.\publish.ps1 -Runtime win-x86
.\publish.ps1 -Version 1.0.0
```

### macOS

Requires Xcode 16+ (for Swift Testing framework).

```bash
cd macos

# Run locally
swift run

# Run tests
swift test

# Build release
swift build -c release
# Binary at: macos/.build/release/NoctusExplorer
```

---

## Releasing

Releases are fully automated. Push a semver tag and CI handles everything:

```bash
# Stable release
git tag v1.0.0
git push origin v1.0.0

# Pre-release (beta, rc, etc.)
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1
```

**What happens automatically:**

1. Runs all tests (137 .NET + 45 Swift)
2. Builds all 4 platform binaries (win-x64, win-x86, win-arm64, macos-arm64)
3. Stamps the assembly version from the tag
4. Creates ZIP archives per platform
5. Generates a changelog from commits since the previous tag
6. Creates a [GitHub Release](https://github.com/YOLOVibeCode/noctus-explorer/releases) with all ZIPs attached

Tags with hyphens (`v1.0.0-beta.1`, `v1.0.0-rc.1`) are marked as **pre-release**.

### Version Convention

| Tag | Type | GitHub Release |
|-----|------|---------------|
| `v1.0.0` | Stable | Latest release |
| `v1.0.0-beta.1` | Beta | Pre-release |
| `v1.0.0-rc.1` | Release candidate | Pre-release |
| `v0.4.0` | Development | Latest release |

---

## Architecture

```
Windows (.NET 10)                    macOS (Swift)
─────────────────                    ─────────────
Explorer.App                         NoctusApp
  └─ Explorer.UI.WinForms             └─ NoctusUI
       └─ Explorer.UI.Contracts            └─ NoctusShellAdapter
       └─ Explorer.Shell.Windows           └─ NoctusCore
       └─ Explorer.Core
```

Both platforms follow the same layered architecture:

| Layer | Purpose | Platform deps |
|-------|---------|---------------|
| **Core** | Models, ViewModels, Services, Commands | None (pure logic) |
| **Shell Adapter** | File operations, shell queries, file watching | OS-specific |
| **UI** | Window, controls, native view hosting | Framework-specific |
| **App** | Entry point, DI wiring, command registration | All |

The two codebases are independent — the spec keeps them in sync, not shared code.

### Design Documents

- [`SPEC.md`](SPEC.md) — Feature specification (18 features, both platforms)
- [`ARCHITECTURE.md`](ARCHITECTURE.md) — Implementation plan (interfaces, data flow, threading, milestones)

---

## Project Status

### Windows Track

| Milestone | Status |
|-----------|--------|
| W0 — IExplorerBrowser Spike | Done |
| W1 — Core + Shell Adapter | Done (137 tests) |
| W2 — Single Pane UI | Done |
| W3 — Dual Pane | Done |
| W4 — Tabs | Done |
| W5 — Bookmarks + Command Palette | Done |
| W6 — Drop Stack + Batch Rename | Planned |
| W7 — Custom Context Menu Wizard | Planned |
| W8 — Preview Pane + Ops Queue | Planned |
| W9 — Polish + Release | Planned |

### macOS Track

| Milestone | Status |
|-----------|--------|
| M0 — AppKit Spike | Done |
| M2 — Single Pane UI | Done (45 tests) |
| M3 — Dual Pane | Done |
| M4 — Tabs | Planned |
| M5 — Bookmarks + Command Palette | Planned |

---

## Why This Exists

Every third-party file manager reimplements the shell view in its own widget toolkit, and the result is always uncanny: rubber-band selection lags, drag-drop breaks, shell extensions don't appear, thumbnails behave differently. The native shell isn't just a widget — it's a deep COM ecosystem (Windows) or AppKit component hierarchy (macOS) that third-party tools register against.

**Noctus Explorer does not reimplement the shell view. It hosts it.**

What it adds is the chrome that the OS refuses to ship: dual panes, real tabs, bookmarks, a command palette, cross-pane operations, and a keyboard-driven power-user experience.

---

## License

Source-available. License TBD.
