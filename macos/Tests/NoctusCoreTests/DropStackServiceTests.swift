import Testing
@testable import NoctusCore

@Suite struct DropStackServiceTests {

    @Test func initiallyEmpty() {
        let stack = DropStackService()
        #expect(stack.items.isEmpty)
    }

    @Test func addAppearsInItems() {
        let stack = DropStackService()
        stack.add([PathRef("/test.txt")])
        #expect(stack.items.count == 1)
    }

    @Test func addDuplicateNotAdded() {
        let stack = DropStackService()
        stack.add([PathRef("/test.txt")])
        stack.add([PathRef("/test.txt")])
        #expect(stack.items.count == 1)
    }

    @Test func removeExistingItem() {
        let stack = DropStackService()
        let p = PathRef("/test.txt")
        stack.add([p])
        stack.remove(p)
        #expect(stack.items.isEmpty)
    }

    @Test func clearRemovesAll() {
        let stack = DropStackService()
        stack.add([PathRef("/a"), PathRef("/b")])
        stack.clear()
        #expect(stack.items.isEmpty)
    }

    @Test func onChangeCalledOnAdd() {
        let stack = DropStackService()
        var called = false
        stack.onChange = { called = true }
        stack.add([PathRef("/a")])
        #expect(called == true)
    }
}
