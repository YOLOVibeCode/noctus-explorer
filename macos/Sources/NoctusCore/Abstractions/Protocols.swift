import Foundation

public protocol ShellServiceProtocol {
    func enumerate(_ directory: PathRef) async throws -> [FileEntry]
    func resolve(_ path: String) async -> PathRef
    func specialFolder(_ folder: SpecialFolder) -> PathRef
    func displayName(_ item: PathRef) -> String
    func exists(_ item: PathRef) -> Bool
}

public protocol FileOperationsProtocol {
    func copy(_ sources: [PathRef], to destination: PathRef) async throws
    func move(_ sources: [PathRef], to destination: PathRef) async throws
    func delete(_ items: [PathRef], permanently: Bool) async throws
    func createFolder(in parent: PathRef, name: String) async throws -> PathRef
    func rename(_ item: PathRef, to newName: String) async throws -> PathRef
}

public protocol FileWatcherProtocol {
    func watch(_ directory: PathRef)
    func unwatch(_ directory: PathRef)
    var onChange: ((FileChangeType, PathRef, PathRef?) -> Void)? { get set }
}

public protocol ClipboardServiceProtocol {
    func setFiles(_ items: [PathRef], operation: ClipboardOperation)
    func getFiles() -> (items: [PathRef], operation: ClipboardOperation)?
    func setText(_ text: String)
}
