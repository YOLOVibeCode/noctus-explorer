import Foundation

/// Represents a file or directory in the listing.
struct FileItem {
    let url: URL
    let name: String
    let isDirectory: Bool
    let size: Int64?
    let dateModified: Date?
    let kind: String

    init(url: URL) {
        self.url = url
        self.name = url.lastPathComponent

        let resourceValues = try? url.resourceValues(forKeys: [
            .isDirectoryKey, .fileSizeKey, .contentModificationDateKey,
            .localizedTypeDescriptionKey
        ])

        self.isDirectory = resourceValues?.isDirectory ?? false
        self.size = resourceValues?.fileSize.map { Int64($0) }
        self.dateModified = resourceValues?.contentModificationDate
        self.kind = resourceValues?.localizedTypeDescription ?? "Unknown"
    }

    var displaySize: String {
        guard let size = size, !isDirectory else { return "—" }
        return ByteCountFormatter.string(fromByteCount: size, countStyle: .file)
    }

    var displayDate: String {
        guard let date = dateModified else { return "—" }
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        formatter.timeStyle = .short
        return formatter.string(from: date)
    }
}
