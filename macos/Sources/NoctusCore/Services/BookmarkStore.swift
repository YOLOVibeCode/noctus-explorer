import Foundation

public final class BookmarkStore {
    private var bookmarks: [Bookmark] = []

    public var items: [Bookmark] { bookmarks }
    public var onChange: (() -> Void)?

    public init() {}

    public func add(_ bookmark: Bookmark) {
        bookmarks.append(bookmark)
        bookmarks.sort { $0.order < $1.order }
        onChange?()
    }

    public func remove(_ id: UUID) {
        bookmarks.removeAll { $0.id == id }
        onChange?()
    }

    public func reorder(_ id: UUID, to newIndex: Int) {
        guard let idx = bookmarks.firstIndex(where: { $0.id == id }) else { return }
        let item = bookmarks.remove(at: idx)
        let clampedIndex = min(max(newIndex, 0), bookmarks.count)
        bookmarks.insert(item, at: clampedIndex)
        for i in bookmarks.indices {
            bookmarks[i].order = i
        }
        onChange?()
    }

    public func groups() -> [String] {
        Array(Set(bookmarks.compactMap(\.group)))
    }
}
