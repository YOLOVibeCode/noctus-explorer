import Foundation

public final class PaneViewModel {
    private let shellService: ShellServiceProtocol
    private let fileWatcher: FileWatcherProtocol
    private var nextTabId: Int = 0

    public private(set) var tabs: [TabViewModel] = []
    public private(set) var activeTab: TabViewModel?
    public var isActive: Bool = false

    public init(shellService: ShellServiceProtocol, fileWatcher: FileWatcherProtocol) {
        self.shellService = shellService
        self.fileWatcher = fileWatcher
    }

    @discardableResult
    public func addTab(at location: PathRef? = nil) -> TabViewModel {
        let loc = location ?? shellService.specialFolder(.home)
        let tab = TabViewModel(id: nextTabId, location: loc, shellService: shellService, fileWatcher: fileWatcher)
        nextTabId += 1
        tabs.append(tab)
        activeTab = tab
        return tab
    }

    public func closeTab(_ tabId: Int) {
        guard let idx = tabs.firstIndex(where: { $0.id == tabId }) else { return }
        let tab = tabs[idx]
        tabs.remove(at: idx)

        if activeTab?.id == tab.id {
            if !tabs.isEmpty {
                activeTab = tabs[min(idx, tabs.count - 1)]
            } else {
                activeTab = nil
            }
        }
    }

    public func activateTab(_ tabId: Int) {
        if let tab = tabs.first(where: { $0.id == tabId }) {
            activeTab = tab
        }
    }

    public func restoreTabs(_ states: [TabState]) {
        tabs.removeAll()
        for state in states {
            let tab = TabViewModel(id: state.id, location: state.location, shellService: shellService, fileWatcher: fileWatcher)
            tab.viewMode = state.viewMode
            tab.sortField = state.sortField
            tab.sortDirection = state.sortDirection
            tabs.append(tab)
            if state.id >= nextTabId {
                nextTabId = state.id + 1
            }
        }
        if !tabs.isEmpty {
            activeTab = tabs[0]
        }
    }
}
