// swift-tools-version:5.9
import PackageDescription

let package = Package(
    name: "NoctusExplorer",
    platforms: [.macOS(.v13)],
    products: [
        .executable(name: "NoctusExplorer", targets: ["NoctusApp"]),
    ],
    targets: [
        // Pure Swift core — no platform imports
        .target(
            name: "NoctusCore",
            path: "Sources/NoctusCore"
        ),
        // Shell adapter — Foundation + AppKit for file ops
        .target(
            name: "NoctusShellAdapter",
            dependencies: ["NoctusCore"],
            path: "Sources/NoctusShellAdapter"
        ),
        // AppKit UI
        .target(
            name: "NoctusUI",
            dependencies: ["NoctusCore", "NoctusShellAdapter"],
            path: "Sources/NoctusUI"
        ),
        // App entry point
        .executableTarget(
            name: "NoctusApp",
            dependencies: ["NoctusCore", "NoctusShellAdapter", "NoctusUI"],
            path: "Sources/NoctusApp"
        ),
        // Tests
        .testTarget(
            name: "NoctusCoreTests",
            dependencies: ["NoctusCore"],
            path: "Tests/NoctusCoreTests"
        ),
    ]
)
