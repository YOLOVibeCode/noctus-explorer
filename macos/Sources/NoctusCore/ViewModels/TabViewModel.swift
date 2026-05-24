import Foundation

public final class TabViewModel {
    private let shellService: ShellServiceProtocol
    private let fileWatcher: FileWatcherProtocol

    public let id: Int
    public private(set) var location: PathRef
    public private(set) var displayName: String
    public var entries: [FileEntry] = []
    public var selection: [FileEntry] = []
    public var viewMode: ViewMode = .details
    public var sortField: SortField = .name
    public var sortDirection: SortDirection = .ascending
    public private(set) var filterText: String = ""
    public private(set) var isFilterActive: Bool = false
    public private(set) var isPreviewVisible: Bool = false
    public private(set) var selectionSummary: String = ""
    public let navigation: NavigationHistory

    public init(id: Int, location: PathRef, shellService: ShellServiceProtocol, fileWatcher: FileWatcherProtocol) {
        self.id = id
        self.location = location
        self.shellService = shellService
        self.fileWatcher = fileWatcher
        self.navigation = NavigationHistory(location)
        self.displayName = shellService.displayName(location)
    }

    public func navigate(to target: PathRef) async throws {
        let previousLocation = location
        let newEntries = try await shellService.enumerate(target)
        location = target
        displayName = shellService.displayName(target)
        navigation.push(target)
        entries = newEntries
        selection = []
        updateSelectionSummary()
        fileWatcher.unwatch(previousLocation)
        fileWatcher.watch(target)
    }

    public func refresh() async throws {
        let newEntries = try await shellService.enumerate(location)
        entries = newEntries
        selection = []
        updateSelectionSummary()
    }

    public func goBack() async throws {
        guard navigation.canGoBack else { return }
        let target = navigation.goBack()
        try await navigateWithoutHistory(to: target)
    }

    public func goForward() async throws {
        guard navigation.canGoForward else { return }
        let target = navigation.goForward()
        try await navigateWithoutHistory(to: target)
    }

    public func goUp() async throws {
        guard let parent = location.getParent() else { return }
        try await navigate(to: parent)
    }

    public func setFilter(_ text: String) {
        filterText = text
        isFilterActive = !text.isEmpty
    }

    public func clearFilter() {
        filterText = ""
        isFilterActive = false
    }

    public func togglePreview() {
        isPreviewVisible = !isPreviewVisible
    }

    public func updateSelectionSummary() {
        if selection.isEmpty {
            selectionSummary = "\(entries.count) items"
        } else {
            let totalSize = selection.reduce(Int64(0)) { $0 + ($1.size ?? 0) }
            selectionSummary = "\(selection.count) selected (\(formatSize(totalSize)))"
        }
    }

    private func navigateWithoutHistory(to target: PathRef) async throws {
        let previousLocation = location
        let newEntries = try await shellService.enumerate(target)
        location = target
        displayName = shellService.displayName(target)
        entries = newEntries
        selection = []
        updateSelectionSummary()
        fileWatcher.unwatch(previousLocation)
        fileWatcher.watch(target)
    }

    private func formatSize(_ bytes: Int64) -> String {
        switch bytes {
        case ..<1024:
            return "\(bytes) B"
        case ..<(1024 * 1024):
            return String(format: "%.1f KB", Double(bytes) / 1024.0)
        case ..<(1024 * 1024 * 1024):
            return String(format: "%.1f MB", Double(bytes) / (1024.0 * 1024.0))
        default:
            return String(format: "%.1f GB", Double(bytes) / (1024.0 * 1024.0 * 1024.0))
        }
    }
}
