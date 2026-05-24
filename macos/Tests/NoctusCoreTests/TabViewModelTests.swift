import Testing
import Foundation
@testable import NoctusCore

@Suite struct TabViewModelTests {

    private func createTab(path: String = "/home") -> (vm: TabViewModel, shell: MockShellService, watcher: MockFileWatcher) {
        let shell = MockShellService()
        let watcher = MockFileWatcher()
        let location = PathRef(path, isDirectory: true)
        let vm = TabViewModel(id: 0, location: location, shellService: shell, fileWatcher: watcher)
        return (vm, shell, watcher)
    }

    private func makeEntry(path: String, name: String, ext: String, size: Int64?) -> FileEntry {
        FileEntry(
            path: PathRef(path),
            name: name,
            ext: ext,
            size: size,
            dateModified: Date(),
            dateCreated: Date(),
            isHidden: false,
            kind: "Document"
        )
    }

    @Test func constructorSetsInitialLocation() {
        let (vm, _, _) = createTab(path: "/home")
        #expect(vm.location.fullPath == "/home")
    }

    @Test func navigateUpdatesLocation() async throws {
        let (vm, _, _) = createTab()
        let target = PathRef("/docs", isDirectory: true)
        try await vm.navigate(to: target)
        #expect(vm.location == target)
    }

    @Test func navigatePopulatesEntries() async throws {
        let (vm, shell, _) = createTab()
        let target = PathRef("/docs", isDirectory: true)
        shell.enumerateResult = [makeEntry(path: "/docs/file.txt", name: "file.txt", ext: ".txt", size: 100)]
        try await vm.navigate(to: target)
        #expect(vm.entries.count == 1)
        #expect(vm.entries[0].name == "file.txt")
    }

    @Test func navigateWatchesNewDirectory() async throws {
        let (vm, _, watcher) = createTab()
        let target = PathRef("/docs", isDirectory: true)
        try await vm.navigate(to: target)
        #expect(watcher.watchCalls.contains(target))
    }

    @Test func navigateUnwatchesPreviousDirectory() async throws {
        let (vm, _, watcher) = createTab(path: "/home")
        let original = vm.location
        let target = PathRef("/docs", isDirectory: true)
        try await vm.navigate(to: target)
        #expect(watcher.unwatchCalls.contains(original))
    }

    @Test func goBackNavigatesToPreviousLocation() async throws {
        let (vm, _, _) = createTab(path: "/a")
        let b = PathRef("/b", isDirectory: true)
        try await vm.navigate(to: b)
        try await vm.goBack()
        #expect(vm.location.fullPath == "/a")
    }

    @Test func goForwardNavigatesToNextLocation() async throws {
        let (vm, _, _) = createTab(path: "/a")
        let b = PathRef("/b", isDirectory: true)
        try await vm.navigate(to: b)
        try await vm.goBack()
        try await vm.goForward()
        #expect(vm.location.fullPath == "/b")
    }

    @Test func goUpNavigatesToParent() async throws {
        let (vm, _, _) = createTab(path: "/home/user/docs")
        try await vm.goUp()
        #expect(vm.location.fullPath == "/home/user")
    }

    @Test func updateSelectionSummaryNoSelectionShowsItemCount() {
        let (vm, _, _) = createTab()
        vm.entries.append(makeEntry(path: "/a.txt", name: "a.txt", ext: ".txt", size: 100))
        vm.updateSelectionSummary()
        #expect(vm.selectionSummary == "1 items")
    }

    @Test func updateSelectionSummaryWithSelectionShowsCountAndSize() {
        let (vm, _, _) = createTab()
        let entry = makeEntry(path: "/a.txt", name: "a.txt", ext: ".txt", size: 2048)
        vm.entries.append(entry)
        vm.selection.append(entry)
        vm.updateSelectionSummary()
        #expect(vm.selectionSummary == "1 selected (2.0 KB)")
    }

    @Test func setFilterActivatesFilter() {
        let (vm, _, _) = createTab()
        vm.setFilter("*.txt")
        #expect(vm.filterText == "*.txt")
        #expect(vm.isFilterActive == true)
    }

    @Test func clearFilterDeactivatesFilter() {
        let (vm, _, _) = createTab()
        vm.setFilter("*.txt")
        vm.clearFilter()
        #expect(vm.filterText == "")
        #expect(vm.isFilterActive == false)
    }

    @Test func togglePreviewFlipsVisibility() {
        let (vm, _, _) = createTab()
        #expect(vm.isPreviewVisible == false)
        vm.togglePreview()
        #expect(vm.isPreviewVisible == true)
        vm.togglePreview()
        #expect(vm.isPreviewVisible == false)
    }
}
