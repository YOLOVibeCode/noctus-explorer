import Testing
@testable import NoctusCore

@Suite struct NavigationHistoryTests {

    @Test func initialCurrentIsStartPath() {
        let h = NavigationHistory(PathRef("/home", isDirectory: true))
        #expect(h.current.fullPath == "/home")
    }

    @Test func initiallyCannotGoBackOrForward() {
        let h = NavigationHistory(PathRef("/start", isDirectory: true))
        #expect(h.canGoBack == false)
        #expect(h.canGoForward == false)
    }

    @Test func pushUpdatesCurrent() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/b", isDirectory: true))
        #expect(h.current.fullPath == "/b")
    }

    @Test func pushEnablesGoBack() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/b", isDirectory: true))
        #expect(h.canGoBack == true)
    }

    @Test func goBackReturnsPrevious() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/b", isDirectory: true))
        let result = h.goBack()
        #expect(result.fullPath == "/a")
    }

    @Test func goBackEnablesGoForward() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/b", isDirectory: true))
        _ = h.goBack()
        #expect(h.canGoForward == true)
    }

    @Test func goForwardReturnsNext() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/b", isDirectory: true))
        _ = h.goBack()
        let result = h.goForward()
        #expect(result.fullPath == "/b")
    }

    @Test func pushAfterGoBackClearsForward() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/b", isDirectory: true))
        h.push(PathRef("/c", isDirectory: true))
        _ = h.goBack()
        h.push(PathRef("/d", isDirectory: true))
        #expect(h.canGoForward == false)
    }

    @Test func pushSameAsCurrentDoesNotDuplicate() {
        let h = NavigationHistory(PathRef("/a", isDirectory: true))
        h.push(PathRef("/a", isDirectory: true))
        #expect(h.canGoBack == false)
    }
}
