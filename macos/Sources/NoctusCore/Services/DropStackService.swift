import Foundation

public final class DropStackService {
    private var stack: [PathRef] = []

    public var items: [PathRef] { stack }
    public var onChange: (() -> Void)?

    public init() {}

    public func add(_ items: [PathRef]) {
        var added = false
        for item in items where !stack.contains(item) {
            stack.append(item)
            added = true
        }
        if added { onChange?() }
    }

    public func remove(_ item: PathRef) {
        if stack.removeAll(where: { $0 == item }) > 0 {
            onChange?()
        }
    }

    public func clear() {
        guard !stack.isEmpty else { return }
        stack.removeAll()
        onChange?()
    }
}

private extension Array {
    @discardableResult
    mutating func removeAll(where predicate: (Element) -> Bool) -> Int {
        let before = count
        self = filter { !predicate($0) }
        return before - count
    }
}
