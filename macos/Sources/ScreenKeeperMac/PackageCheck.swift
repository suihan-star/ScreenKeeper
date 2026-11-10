import Foundation
import ImageIO

enum PackageCheck {
    /// Runs before NSApplication, the instance lock or settings. Never touches a display.
    static func run() -> Int32 {
        let bundle = Bundle.main
        var errors: [String] = []
        if bundle.bundleURL.pathExtension != "app" { errors.append("Expected a packaged .app") }
        if bundle.bundleIdentifier != "com.skyrrats.screenkeeper.mac" { errors.append("Unexpected application identity") }
        if bundle.object(forInfoDictionaryKey: "LSMinimumSystemVersion") as? String != "13.0" {
            errors.append("Unexpected minimum macOS version")
        }
        let sourceHash = bundle.object(forInfoDictionaryKey: "ScreenKeeperSourceManifestSHA256") as? String ?? ""
        if sourceHash.count != 64 || !sourceHash.allSatisfy({ $0.isHexDigit }) { errors.append("Source manifest identity is missing") }
        for (name, ext) in [("forest-mist", "png"), ("icon", "svg"), ("AppIcon", "icns"),
                            ("LICENSE", ""), ("DISTRIBUTION", "txt"), ("SOURCE-SHA256SUMS", "txt")] {
            guard let url = bundle.url(forResource: name, withExtension: ext.isEmpty ? nil : ext),
                  let data = try? Data(contentsOf: url), !data.isEmpty else {
                errors.append("Missing or empty resource: \(name)"); continue
            }
            if ext == "png" {
                guard let source = CGImageSourceCreateWithData(data as CFData, nil),
                      let image = CGImageSourceCreateImageAtIndex(source, 0, nil),
                      image.width == 1672, image.height == 941 else {
                    errors.append("Built-in background could not be decoded"); continue
                }
            }
        }
        #if arch(arm64)
        let architecture = "arm64"
        #else
        let architecture = "other"
        errors.append("Application is not running as arm64")
        #endif
        let result: [String: Any] = ["passed": errors.isEmpty, "architecture": architecture,
            "sourceManifestSHA256": sourceHash, "errors": errors, "displayAccessed": false,
            "settingsAccessed": false, "hardwareValidated": false]
        if let data = try? JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys]),
           let text = String(data: data, encoding: .utf8) { print(text) }
        return errors.isEmpty ? 0 : 1
    }
}
