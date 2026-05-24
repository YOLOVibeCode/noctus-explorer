import Foundation
@testable import NoctusCore

final class MockShellService: ShellServiceProtocol {
    var enumerateResult: [FileEntry] = []
    var enumerateCalls: [PathRef] = []

    func enumerate(_ directory: PathRef) async throws -> [FileEntry] {
        enumerateCalls.append(directory)
        return enumerateResult
    }

    func resolve(_ path: String) async -> PathRef {
        PathRef(path)
    }

    func specialFolder(_ folder: SpecialFolder) -> PathRef {
        switch folder {
        case .home: return PathRef("/home", isDirectory: true)
        case .desktop: return PathRef("/home/Desktop", isDirectory: true)
        case .downloads: return PathRef("/home/Downloads", isDirectory: true)
        case .documents: return PathRef("/home/Documents", isDirectory: true)
        case .trash: return PathRef("/home/.Trash", isDirectory: true)
        case .root: return PathRef("/", isDirectory: true)
        }
    }

    func displayName(_ item: PathRef) -> String {
        item.displayName
    }

    func exists(_ item: PathRef) -> Bool {
        true
    }
}

final class MockFileWatcher: FileWatcherProtocol {
    var watchCalls: [PathRef] = []
    var unwatchCalls: [PathRef] = []
    var onChange: ((FileChangeType, PathRef, PathRef?) -> Void)?

    func watch(_ directory: PathRef) {
        watchCalls.append(directory)
    }

    func unwatch(_ directory: PathRef) {
        unwatchCalls.append(directory)
    }
}

final class MockFileOperations: FileOperationsProtocol {
    var copyCalls: [(sources: [PathRef], destination: PathRef)] = []
    var moveCalls: [(sources: [PathRef], destination: PathRef)] = []

    func copy(_ sources: [PathRef], to destination: PathRef) async throws {
        copyCalls.append((sources, destination))
    }

    func move(_ sources: [PathRef], to destination: PathRef) async throws {
        moveCalls.append((sources, destination))
    }

    func delete(_ items: [PathRef], permanently: Bool) async throws {}

    func createFolder(in parent: PathRef, name: String) async throws -> PathRef {
        PathRef("\(parent.fullPath)/\(name)", isDirectory: true)
    }

    func rename(_ item: PathRef, to newName: String) async throws -> PathRef {
        let parent = (item.fullPath as NSString).deletingLastPathComponent
        return PathRef("\(parent)/\(newName)", isDirectory: item.isDirectory)
    }
}
