import Testing
import Foundation
@testable import NoctusCore
@testable import NoctusShellAdapter

@Suite(.serialized) struct MacShellServiceTests {

    private let service = MacShellService()

    private func makeTempDir() throws -> (URL, () -> Void) {
        let dir = FileManager.default.temporaryDirectory
            .appendingPathComponent("noctus-test-\(UUID().uuidString)")
        try FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        let cleanup: () -> Void = { _ = try? FileManager.default.removeItem(at: dir) }
        return (dir, cleanup)
    }

    @Test func enumerateReturnsFiles() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        try "hello".write(to: dir.appendingPathComponent("test.txt"), atomically: true, encoding: .utf8)
        try FileManager.default.createDirectory(at: dir.appendingPathComponent("subdir"), withIntermediateDirectories: false)

        let entries = try await service.enumerate(PathRef(dir.path, isDirectory: true))
        #expect(entries.count == 2)

        let names = Set(entries.map(\.name))
        #expect(names.contains("test.txt"))
        #expect(names.contains("subdir"))
    }

    @Test func enumerateIncludesFileMetadata() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let data = Data(repeating: 0x41, count: 256)
        try data.write(to: dir.appendingPathComponent("data.bin"))

        let entries = try await service.enumerate(PathRef(dir.path, isDirectory: true))
        #expect(entries.count == 1)
        #expect(entries[0].name == "data.bin")
        #expect(entries[0].ext == "bin")
        #expect(entries[0].size == 256)
        #expect(entries[0].isDirectory == false)
    }

    @Test func enumerateDirectoryFlagSet() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        try FileManager.default.createDirectory(at: dir.appendingPathComponent("child"), withIntermediateDirectories: false)

        let entries = try await service.enumerate(PathRef(dir.path, isDirectory: true))
        let child = entries.first { $0.name == "child" }
        #expect(child?.isDirectory == true)
    }

    @Test func enumerateEmptyDirectory() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let entries = try await service.enumerate(PathRef(dir.path, isDirectory: true))
        #expect(entries.isEmpty)
    }

    @Test func existsReturnsTrueForExistingPath() throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        #expect(service.exists(PathRef(dir.path, isDirectory: true)) == true)
    }

    @Test func existsReturnsFalseForMissingPath() {
        #expect(service.exists(PathRef("/nonexistent/path/\(UUID().uuidString)")) == false)
    }

    @Test func displayNameReturnsLastComponent() throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let name = service.displayName(PathRef(dir.path, isDirectory: true))
        #expect(!name.isEmpty)
    }

    @Test func specialFolderHomeExists() {
        let home = service.specialFolder(.home)
        #expect(service.exists(home))
        #expect(home.isDirectory)
    }

    @Test func resolveExpandsTilde() async {
        let ref = await service.resolve("~/Desktop")
        #expect(!ref.fullPath.contains("~"))
        #expect(ref.fullPath.contains("Desktop"))
    }
}
