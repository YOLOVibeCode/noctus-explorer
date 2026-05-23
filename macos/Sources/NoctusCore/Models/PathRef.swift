import Foundation

/// Immutable reference to a filesystem location.
public struct PathRef: Equatable, Hashable, CustomStringConvertible {
    public let fullPath: String
    public let displayName: String
    public let isDirectory: Bool
    public let platformHandle: Data?

    public init(_ fullPath: String, displayName: String? = nil, isDirectory: Bool = false, platformHandle: Data? = nil) {
        precondition(!fullPath.isEmpty, "Path cannot be empty")
        let normalized = PathRef.normalize(fullPath)
        self.fullPath = normalized
        self.displayName = displayName ?? (normalized as NSString).lastPathComponent
        self.isDirectory = isDirectory
        self.platformHandle = platformHandle
    }

    public var description: String { fullPath }

    public func getParent() -> PathRef? {
        let parent = (fullPath as NSString).deletingLastPathComponent
        guard !parent.isEmpty, parent != fullPath else { return nil }
        return PathRef(parent, isDirectory: true)
    }

    public static func == (lhs: PathRef, rhs: PathRef) -> Bool {
        lhs.fullPath.caseInsensitiveCompare(rhs.fullPath) == .orderedSame
    }

    public func hash(into hasher: inout Hasher) {
        hasher.combine(fullPath.lowercased())
    }

    private static func normalize(_ path: String) -> String {
        var p = path.replacingOccurrences(of: "\\", with: "/")
        while p.hasSuffix("/") && p.count > 1 {
            p.removeLast()
        }
        return p
    }
}
