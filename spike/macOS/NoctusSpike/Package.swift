// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "NoctusSpike",
    platforms: [.macOS(.v13)],
    targets: [
        .executableTarget(
            name: "NoctusSpike",
            path: "Sources"
        )
    ]
)
