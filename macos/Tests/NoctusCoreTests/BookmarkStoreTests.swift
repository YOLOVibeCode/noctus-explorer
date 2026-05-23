import Testing
@testable import NoctusCore

@Suite struct BookmarkStoreTests {

    @Test func initiallyEmpty() {
        let store = BookmarkStore()
        #expect(store.items.isEmpty)
    }

    @Test func addAppearsInItems() {
        let store = BookmarkStore()
        store.add(Bookmark(name: "Home", target: PathRef("/home")))
        #expect(store.items.count == 1)
        #expect(store.items[0].name == "Home")
    }

    @Test func removeById() {
        let store = BookmarkStore()
        let bm = Bookmark(name: "Home", target: PathRef("/home"))
        store.add(bm)
        store.remove(bm.id)
        #expect(store.items.isEmpty)
    }

    @Test func reorderChangesPosition() {
        let store = BookmarkStore()
        let a = Bookmark(name: "A", target: PathRef("/a"), order: 0)
        let b = Bookmark(name: "B", target: PathRef("/b"), order: 1)
        store.add(a)
        store.add(b)
        store.reorder(b.id, to: 0)
        #expect(store.items[0].name == "B")
    }

    @Test func groupsReturnsDistinct() {
        let store = BookmarkStore()
        store.add(Bookmark(name: "A", target: PathRef("/a"), group: "Servers"))
        store.add(Bookmark(name: "B", target: PathRef("/b"), group: "Servers"))
        store.add(Bookmark(name: "C", target: PathRef("/c"), group: "Local"))
        #expect(store.groups().count == 2)
    }
}
