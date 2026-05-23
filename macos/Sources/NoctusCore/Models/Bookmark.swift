import Foundation

public struct Bookmark: Identifiable {
    public let id: UUID
    public let name: String
    public let target: PathRef
    public let group: String?
    public var order: Int

    public init(id: UUID = UUID(), name: String, target: PathRef, group: String? = nil, order: Int = 0) {
        self.id = id
        self.name = name
        self.target = target
        self.group = group
        self.order = order
    }
}
