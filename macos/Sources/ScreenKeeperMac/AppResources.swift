import Foundation

enum AppResources {
    static func url(_ name: String, extension fileExtension: String) -> URL? {
        // Packaged .app resources live in Contents/Resources. SwiftPM's resource bundle
        // remains the fallback for `swift run` in the source checkout.
        if Bundle.main.bundleURL.pathExtension.lowercased() == "app" {
            // A missing packaged asset must reach the opaque fallback, never SwiftPM's
            // fatal accessor for a resource bundle that only exists in the source build.
            return Bundle.main.url(forResource: name, withExtension: fileExtension)
        }
        return Bundle.module.url(forResource: name, withExtension: fileExtension, subdirectory: "Resources")
    }
}
