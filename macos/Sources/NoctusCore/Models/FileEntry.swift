import Foundation

public struct FileEntry {
    public let path: PathRef
    public let name: String
    public let ext: String
    public let size: Int64?
    public let dateModified: Date
    public let dateCreated: Date
    public let isHidden: Bool
    public let kind: String

    public var isDirectory: Bool { path.isDirectory }

    public init(path: PathRef, name: String, ext: String, size: Int64?,
                dateModified: Date, dateCreated: Date, isHidden: Bool, kind: String) {
        self.path = path
        self.name = name
        self.ext = ext
        self.size = size
        self.dateModified = dateModified
        self.dateCreated = dateCreated
        self.isHidden = isHidden
        self.kind = kind
    }
}
