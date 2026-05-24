import Testing
@testable import NoctusCore

@Suite struct MainViewModelTests {

    private func createMain() -> MainViewModel {
        let shell = MockShellService()
        let fileOps = MockFileOperations()
        let watcher = MockFileWatcher()
        let settings = SettingsStore()
        let commands = CommandRegistry()
        let keys = KeyBindingResolver()
        let bookmarks = BookmarkStore()
        let dropStack = DropStackService()

        return MainViewModel(
            shellService: shell,
            fileOps: fileOps,
            fileWatcher: watcher,
            settings: settings,
            commandRegistry: commands,
            keyBindingResolver: keys,
            bookmarkStore: bookmarks,
            dropStack: dropStack
        )
    }

    @Test func leftPaneIsInitiallyActive() {
        let vm = createMain()
        #expect(vm.leftPane.isActive == true)
        #expect(vm.rightPane.isActive == false)
        #expect(vm.activePane === vm.leftPane)
    }

    @Test func switchActivePaneTogglesActivePane() {
        let vm = createMain()
        vm.switchActivePane()
        #expect(vm.leftPane.isActive == false)
        #expect(vm.rightPane.isActive == true)
        #expect(vm.activePane === vm.rightPane)
        #expect(vm.inactivePane === vm.leftPane)
    }

    @Test func switchActivePaneTwiceReturnToOriginal() {
        let vm = createMain()
        vm.switchActivePane()
        vm.switchActivePane()
        #expect(vm.activePane === vm.leftPane)
    }

    @Test func toggleSplitModeCyclesThroughModes() {
        let vm = createMain()
        #expect(vm.splitMode == .vertical)
        vm.toggleSplitMode()
        #expect(vm.splitMode == .horizontal)
        vm.toggleSplitMode()
        #expect(vm.splitMode == .single)
        vm.toggleSplitMode()
        #expect(vm.splitMode == .vertical)
    }

    @Test func defaultSplitRatioIsHalf() {
        let vm = createMain()
        #expect(vm.splitRatio == 0.5)
    }

    @Test func saveAndRestoreSessionPersistsSplitMode() {
        let vm = createMain()
        vm.splitMode = .horizontal  // Need to make this settable
        vm.splitRatio = 0.7
        vm.saveSession()
        vm.restoreSession()
        #expect(vm.splitMode == .horizontal)
        #expect(vm.splitRatio == 0.7)
    }
}
