import AppKit
import CoreGraphics

struct DisplayProbeResult: Codable {
    let widthPoints: Double
    let heightPoints: Double
    let backingScaleFactor: Double
    let builtIn: Bool
    let mirrored: Bool
    let identityReliable: Bool
    let gammaCapacity: Int
    let gammaReadSucceeded: Bool
    let gammaEntriesRead: Int
    let extendedDynamicRangeHeadroom: Double
    let colorAdjustmentVerified: Bool
}

@MainActor
enum ReadOnlyDisplayProbe {
    /// Read-only diagnostic. Success is not an SDR verdict, write permission, or visual validation.
    static func capture(_ display: MacDisplay) -> DisplayProbeResult {
        let capacity = Int(CGDisplayGammaTableCapacity(display.displayID))
        var readSucceeded = false
        var count: UInt32 = 0
        if capacity > 0 && capacity <= 16_384 {
            var red = [CGGammaValue](repeating: 0, count: capacity)
            var green = red
            var blue = red
            let result = CGGetDisplayTransferByTable(display.displayID, UInt32(capacity), &red, &green, &blue, &count)
            readSucceeded = result == .success && count > 0 && count <= UInt32(capacity)
        }
        return DisplayProbeResult(widthPoints: Double(display.screen.frame.width),
            heightPoints: Double(display.screen.frame.height), backingScaleFactor: Double(display.screen.backingScaleFactor),
            builtIn: display.isBuiltIn, mirrored: display.isMirrored, identityReliable: display.isIdentityReliable,
            gammaCapacity: capacity, gammaReadSucceeded: readSucceeded, gammaEntriesRead: readSucceeded ? Int(count) : 0,
            extendedDynamicRangeHeadroom: Double(display.screen.maximumExtendedDynamicRangeColorComponentValue),
            colorAdjustmentVerified: false)
    }
}
