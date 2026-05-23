import Testing
@testable import NoctusCore

@Suite struct PathRefTests {

    @Test func constructorSetsProperties() {
        let p = PathRef("/Users/me/file.txt")
        #expect(p.fullPath == "/Users/me/file.txt")
        #expect(p.displayName == "file.txt")
        #expect(p.isDirectory == false)
    }

    @Test func equalityCaseInsensitive() {
        let a = PathRef("/Users/ME/File.txt")
        let b = PathRef("/Users/me/file.txt")
        #expect(a == b)
        #expect(a.hashValue == b.hashValue)
    }

    @Test func differentPathsNotEqual() {
        #expect(PathRef("/a.txt") != PathRef("/b.txt"))
    }

    @Test func getParentReturnsParentDir() {
        let p = PathRef("/Users/me/docs/file.txt")
        let parent = p.getParent()
        #expect(parent?.fullPath == "/Users/me/docs")
        #expect(parent?.isDirectory == true)
    }

    @Test func normalizesBackslashes() {
        let p = PathRef("C:\\Users\\me\\file.txt")
        #expect(p.fullPath == "C:/Users/me/file.txt")
    }

    @Test func trailingSlashRemoved() {
        let p = PathRef("/Users/me/docs/")
        #expect(p.fullPath == "/Users/me/docs")
    }
}
