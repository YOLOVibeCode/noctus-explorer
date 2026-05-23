import Foundation

public enum SplitMode: String, Codable {
    case single, vertical, horizontal
}

public enum ViewMode: String, Codable {
    case icons, smallIcons, list, details, tiles, content, columns, gallery
}

public enum SortField: String, Codable {
    case name, size, dateModified, dateCreated, kind, ext
}

public enum SortDirection: String, Codable {
    case ascending, descending
}

public enum PaneSide {
    case left, right
}

public enum ClipboardOperation {
    case copy, cut
}

public enum OperationStatus {
    case queued, running, paused, completed, failed, cancelled
}

public enum SpecialFolder {
    case home, desktop, downloads, documents, trash, root
}

public enum FileType: String, Codable {
    case files, folders, both
}

public enum SelectionCount: String, Codable {
    case any, single, multiple
}

public enum ActionType: String, Codable {
    case runProgram, shellCommand, openWith, copyText, openUrl
}

public enum HashAlgorithmType {
    case md5, sha1, sha256, sha512
}

public enum ConflictResolution {
    case overwrite, skip, rename, overwriteIfNewer
}

public enum FileChangeType {
    case created, modified, deleted, renamed
}
