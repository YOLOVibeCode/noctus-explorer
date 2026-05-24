import Testing
@testable import NoctusCore

@Suite struct PaneViewModelTests {

    private func createPane() -> PaneViewModel {
        let shell = MockShellService()
        let watcher = MockFileWatcher()
        return PaneViewModel(shellService: shell, fileWatcher: watcher)
    }

    @Test func initiallyNoTabs() {
        let pane = createPane()
        #expect(pane.tabs.isEmpty)
        #expect(pane.activeTab == nil)
    }

    @Test func addTabCreatesTabAndActivatesIt() {
        let pane = createPane()
        let tab = pane.addTab()
        #expect(pane.tabs.count == 1)
        #expect(pane.activeTab?.id == tab.id)
    }

    @Test func addTabWithLocationSetsTabLocation() {
        let pane = createPane()
        let loc = PathRef("/docs", isDirectory: true)
        let tab = pane.addTab(at: loc)
        #expect(tab.location == loc)
    }

    @Test func closeTabRemovesTab() {
        let pane = createPane()
        let tab = pane.addTab()
        pane.closeTab(tab.id)
        #expect(pane.tabs.isEmpty)
    }

    @Test func closeActiveTabActivatesNeighbor() {
        let pane = createPane()
        let tab1 = pane.addTab()
        let tab2 = pane.addTab()
        pane.closeTab(tab2.id)
        #expect(pane.activeTab?.id == tab1.id)
    }

    @Test func activateTabSwitchesActiveTab() {
        let pane = createPane()
        let tab1 = pane.addTab()
        _ = pane.addTab()
        pane.activateTab(tab1.id)
        #expect(pane.activeTab?.id == tab1.id)
    }

    @Test func restoreTabsPopulatesFromState() {
        let pane = createPane()
        let states = [
            TabState(id: 0, location: PathRef("/a", isDirectory: true)),
            TabState(id: 1, location: PathRef("/b", isDirectory: true), viewMode: .list)
        ]
        pane.restoreTabs(states)
        #expect(pane.tabs.count == 2)
        #expect(pane.tabs[1].viewMode == .list)
    }
}
