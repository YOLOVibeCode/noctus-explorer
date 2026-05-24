import Foundation

public final class MainViewModel {
    private let fileOps: FileOperationsProtocol
    private let settings: SettingsStore

    public let leftPane: PaneViewModel
    public let rightPane: PaneViewModel
    public let commandRegistry: CommandRegistry
    public let keyBindingResolver: KeyBindingResolver
    public let bookmarkStore: BookmarkStore
    public let dropStack: DropStackService

    public var splitMode: SplitMode = .vertical
    public var splitRatio: Double = 0.5

    public var activePane: PaneViewModel { leftPane.isActive ? leftPane : rightPane }
    public var inactivePane: PaneViewModel { leftPane.isActive ? rightPane : leftPane }

    public init(
        shellService: ShellServiceProtocol,
        fileOps: FileOperationsProtocol,
        fileWatcher: FileWatcherProtocol,
        settings: SettingsStore,
        commandRegistry: CommandRegistry,
        keyBindingResolver: KeyBindingResolver,
        bookmarkStore: BookmarkStore,
        dropStack: DropStackService
    ) {
        self.fileOps = fileOps
        self.settings = settings
        self.commandRegistry = commandRegistry
        self.keyBindingResolver = keyBindingResolver
        self.bookmarkStore = bookmarkStore
        self.dropStack = dropStack

        self.leftPane = PaneViewModel(shellService: shellService, fileWatcher: fileWatcher)
        self.rightPane = PaneViewModel(shellService: shellService, fileWatcher: fileWatcher)
        leftPane.isActive = true
    }

    public func switchActivePane() {
        leftPane.isActive = !leftPane.isActive
        rightPane.isActive = !rightPane.isActive
    }

    public func toggleSplitMode() {
        switch splitMode {
        case .vertical: splitMode = .horizontal
        case .horizontal: splitMode = .single
        case .single: splitMode = .vertical
        }
    }

    public func copyToOtherPane() async throws {
        guard let selection = activePane.activeTab?.selection,
              !selection.isEmpty,
              let destination = inactivePane.activeTab?.location else { return }
        let sources = selection.map(\.path)
        try await fileOps.copy(sources, to: destination)
    }

    public func moveToOtherPane() async throws {
        guard let selection = activePane.activeTab?.selection,
              !selection.isEmpty,
              let destination = inactivePane.activeTab?.location else { return }
        let sources = selection.map(\.path)
        try await fileOps.move(sources, to: destination)
    }

    public func syncNavigation() async throws {
        guard let activeLocation = activePane.activeTab?.location,
              inactivePane.activeTab != nil else { return }
        try await inactivePane.activeTab?.navigate(to: activeLocation)
    }

    public func saveSession() {
        settings.set("session.splitMode", value: splitMode.rawValue)
        settings.set("session.splitRatio", value: splitRatio)
    }

    public func restoreSession() {
        let modeStr: String = settings.get("session.splitMode", default: "vertical")
        if let mode = SplitMode(rawValue: modeStr) {
            splitMode = mode
        }
        splitRatio = settings.get("session.splitRatio", default: 0.5)
    }
}
