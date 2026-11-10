// swift-tools-version: 5.9
import PackageDescription

var products: [Product] = [.library(name: "ScreenKeeperCore", targets: ["ScreenKeeperCore"])]
var targets: [Target] = [
    .target(name: "ScreenKeeperCore"),
    .testTarget(name: "ScreenKeeperCoreTests", dependencies: ["ScreenKeeperCore"])
]
#if os(macOS)
products.append(.executable(name: "ScreenKeeper", targets: ["ScreenKeeperMac"]))
targets.append(.executableTarget(name: "ScreenKeeperMac", dependencies: ["ScreenKeeperCore"],
    resources: [.copy("Resources")]))
#endif

let package = Package(name: "ScreenKeeper", platforms: [.macOS(.v13)],
    products: products, targets: targets, swiftLanguageVersions: [.v5])
