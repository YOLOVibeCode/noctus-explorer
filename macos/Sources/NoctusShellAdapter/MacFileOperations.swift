import Foundation
import NoctusCore

public final class MacFileOperations: FileOperationsProtocol {
    public init() {}

    public func copy(_ sources: [PathRef], to destination: PathRef) async throws {
        let fm = FileManager.default
        for source in sources {
            let srcURL = URL(fileURLWithPath: source.fullPath)
            let destURL = URL(fileURLWithPath: destination.fullPath)
                .appendingPathComponent(srcURL.lastPathComponent)
            try fm.copyItem(at: srcURL, to: destURL)
        }
    }

    public func move(_ sources: [PathRef], to destination: PathRef) async throws {
        let fm = FileManager.default
        for source in sources {
            let srcURL = URL(fileURLWithPath: source.fullPath)
            let destURL = URL(fileURLWithPath: destination.fullPath)
                .appendingPathComponent(srcURL.lastPathComponent)
            try fm.moveItem(at: srcURL, to: destURL)
        }
    }

    public func delete(_ items: [PathRef], permanently: Bool) async throws {
        let fm = FileManager.default
        for item in items {
            let url = URL(fileURLWithPath: item.fullPath)
            if permanently {
                try fm.removeItem(at: url)
            } else {
                try fm.trashItem(at: url, resultingItemURL: nil)
            }
        }
    }

    public func createFolder(in parent: PathRef, name: String) async throws -> PathRef {
        let url = URL(fileURLWithPath: parent.fullPath).appendingPathComponent(name)
        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: false)
        return PathRef(url.path, isDirectory: true)
    }

    public func rename(_ item: PathRef, to newName: String) async throws -> PathRef {
        let srcURL = URL(fileURLWithPath: item.fullPath)
        let destURL = srcURL.deletingLastPathComponent().appendingPathComponent(newName)
        try FileManager.default.moveItem(at: srcURL, to: destURL)
        return PathRef(destURL.path, isDirectory: item.isDirectory)
    }
}
