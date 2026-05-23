import Foundation
import NoctusCore

public final class MacShellService: ShellServiceProtocol {
    public init() {}

    public func enumerate(_ directory: PathRef) async throws -> [FileEntry] {
        let url = URL(fileURLWithPath: directory.fullPath)
        let keys: [URLResourceKey] = [
            .isDirectoryKey, .fileSizeKey, .contentModificationDateKey,
            .creationDateKey, .isHiddenKey, .localizedTypeDescriptionKey
        ]

        let contents = try FileManager.default.contentsOfDirectory(
            at: url, includingPropertiesForKeys: keys, options: []
        )

        return contents.map { fileURL in
            let vals = try? fileURL.resourceValues(forKeys: Set(keys))
            let isDir = vals?.isDirectory ?? false
            return FileEntry(
                path: PathRef(fileURL.path, isDirectory: isDir),
                name: fileURL.lastPathComponent,
                ext: fileURL.pathExtension,
                size: vals?.fileSize.map { Int64($0) },
                dateModified: vals?.contentModificationDate ?? Date(),
                dateCreated: vals?.creationDate ?? Date(),
                isHidden: vals?.isHidden ?? false,
                kind: vals?.localizedTypeDescription ?? "Unknown"
            )
        }
    }

    public func resolve(_ path: String) async -> PathRef {
        let expanded = NSString(string: path).expandingTildeInPath
        var isDir: ObjCBool = false
        FileManager.default.fileExists(atPath: expanded, isDirectory: &isDir)
        return PathRef(expanded, isDirectory: isDir.boolValue)
    }

    public func specialFolder(_ folder: SpecialFolder) -> PathRef {
        switch folder {
        case .home:
            return PathRef(FileManager.default.homeDirectoryForCurrentUser.path, isDirectory: true)
        case .desktop:
            return pathFor(.desktopDirectory)
        case .downloads:
            return pathFor(.downloadsDirectory)
        case .documents:
            return pathFor(.documentDirectory)
        case .trash:
            return PathRef(FileManager.default.homeDirectoryForCurrentUser
                .appendingPathComponent(".Trash").path, isDirectory: true)
        case .root:
            return PathRef("/", isDirectory: true)
        }
    }

    public func displayName(_ item: PathRef) -> String {
        FileManager.default.displayName(atPath: item.fullPath)
    }

    public func exists(_ item: PathRef) -> Bool {
        FileManager.default.fileExists(atPath: item.fullPath)
    }

    private func pathFor(_ dir: FileManager.SearchPathDirectory) -> PathRef {
        let url = FileManager.default.urls(for: dir, in: .userDomainMask).first
            ?? FileManager.default.homeDirectoryForCurrentUser
        return PathRef(url.path, isDirectory: true)
    }
}
