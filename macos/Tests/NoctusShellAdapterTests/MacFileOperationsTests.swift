import Testing
import Foundation
@testable import NoctusCore
@testable import NoctusShellAdapter

@Suite(.serialized) struct MacFileOperationsTests {

    private let ops = MacFileOperations()
    private let fm = FileManager.default

    private func makeTempDir() throws -> (URL, () -> Void) {
        let dir = fm.temporaryDirectory
            .appendingPathComponent("noctus-test-\(UUID().uuidString)")
        try fm.createDirectory(at: dir, withIntermediateDirectories: true)
        let cleanup: () -> Void = { _ = try? FileManager.default.removeItem(at: dir) }
        return (dir, cleanup)
    }

    @Test func copySingleFile() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let srcDir = dir.appendingPathComponent("src")
        let dstDir = dir.appendingPathComponent("dst")
        try fm.createDirectory(at: srcDir, withIntermediateDirectories: true)
        try fm.createDirectory(at: dstDir, withIntermediateDirectories: true)

        let file = srcDir.appendingPathComponent("hello.txt")
        try "hello world".write(to: file, atomically: true, encoding: .utf8)

        try await ops.copy([PathRef(file.path)], to: PathRef(dstDir.path, isDirectory: true))

        let copied = dstDir.appendingPathComponent("hello.txt")
        #expect(fm.fileExists(atPath: copied.path))
        let content = try String(contentsOf: copied, encoding: .utf8)
        #expect(content == "hello world")
        // Source still exists
        #expect(fm.fileExists(atPath: file.path))
    }

    @Test func copyMultipleFiles() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let srcDir = dir.appendingPathComponent("src")
        let dstDir = dir.appendingPathComponent("dst")
        try fm.createDirectory(at: srcDir, withIntermediateDirectories: true)
        try fm.createDirectory(at: dstDir, withIntermediateDirectories: true)

        let f1 = srcDir.appendingPathComponent("a.txt")
        let f2 = srcDir.appendingPathComponent("b.txt")
        try "a".write(to: f1, atomically: true, encoding: .utf8)
        try "b".write(to: f2, atomically: true, encoding: .utf8)

        try await ops.copy([PathRef(f1.path), PathRef(f2.path)], to: PathRef(dstDir.path, isDirectory: true))

        #expect(fm.fileExists(atPath: dstDir.appendingPathComponent("a.txt").path))
        #expect(fm.fileExists(atPath: dstDir.appendingPathComponent("b.txt").path))
    }

    @Test func moveSingleFile() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let srcDir = dir.appendingPathComponent("src")
        let dstDir = dir.appendingPathComponent("dst")
        try fm.createDirectory(at: srcDir, withIntermediateDirectories: true)
        try fm.createDirectory(at: dstDir, withIntermediateDirectories: true)

        let file = srcDir.appendingPathComponent("move-me.txt")
        try "data".write(to: file, atomically: true, encoding: .utf8)

        try await ops.move([PathRef(file.path)], to: PathRef(dstDir.path, isDirectory: true))

        #expect(fm.fileExists(atPath: dstDir.appendingPathComponent("move-me.txt").path))
        // Source removed
        #expect(!fm.fileExists(atPath: file.path))
    }

    @Test func deletePermanently() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let file = dir.appendingPathComponent("delete-me.txt")
        try "gone".write(to: file, atomically: true, encoding: .utf8)

        try await ops.delete([PathRef(file.path)], permanently: true)
        #expect(!fm.fileExists(atPath: file.path))
    }

    @Test func createFolderCreatesDirectory() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let result = try await ops.createFolder(in: PathRef(dir.path, isDirectory: true), name: "newfolder")
        #expect(fm.fileExists(atPath: result.fullPath))
        #expect(result.isDirectory)
    }

    @Test func renameChangesName() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let file = dir.appendingPathComponent("old.txt")
        try "content".write(to: file, atomically: true, encoding: .utf8)

        let result = try await ops.rename(PathRef(file.path), to: "new.txt")
        #expect(fm.fileExists(atPath: result.fullPath))
        #expect(!fm.fileExists(atPath: file.path))
        #expect(result.fullPath.hasSuffix("new.txt"))
    }

    @Test func copyDirectory() async throws {
        let (dir, cleanup) = try makeTempDir()
        defer { cleanup() }

        let srcDir = dir.appendingPathComponent("src")
        let subDir = srcDir.appendingPathComponent("folder")
        let dstDir = dir.appendingPathComponent("dst")
        try fm.createDirectory(at: subDir, withIntermediateDirectories: true)
        try fm.createDirectory(at: dstDir, withIntermediateDirectories: true)
        try "nested".write(to: subDir.appendingPathComponent("inside.txt"), atomically: true, encoding: .utf8)

        try await ops.copy([PathRef(subDir.path, isDirectory: true)], to: PathRef(dstDir.path, isDirectory: true))

        let copiedInside = dstDir.appendingPathComponent("folder/inside.txt")
        #expect(fm.fileExists(atPath: copiedInside.path))
    }
}
