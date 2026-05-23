import Foundation

public final class NavigationHistory {
    private var history: [PathRef]
    private var position: Int

    public init(_ initialLocation: PathRef) {
        self.history = [initialLocation]
        self.position = 0
    }

    public var current: PathRef { history[position] }
    public var canGoBack: Bool { position > 0 }
    public var canGoForward: Bool { position < history.count - 1 }

    public func push(_ location: PathRef) {
        guard location != current else { return }
        if position < history.count - 1 {
            history.removeSubrange((position + 1)...)
        }
        history.append(location)
        position = history.count - 1
    }

    public func goBack() -> PathRef {
        precondition(canGoBack, "No back history")
        position -= 1
        return current
    }

    public func goForward() -> PathRef {
        precondition(canGoForward, "No forward history")
        position += 1
        return current
    }
}
