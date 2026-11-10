import AppKit
import ApplicationServices
import CoreGraphics

struct ActivitySample {
    let idleSeconds: Double?
    let keyboardIdleSeconds: Double?
    let pointerIdleSeconds: Double?
    let mouseDisplayID: String?
    let foregroundDisplayID: String?
    let foregroundKnown: Bool
}

@MainActor
final class ActivityMonitor {
    var accessibilityGranted: Bool { AXIsProcessTrusted() }

    /// Explicit UI action only. Sampling never triggers a permission prompt.
    func requestAccessibility() {
        let key = kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String
        _ = AXIsProcessTrustedWithOptions([key: true] as CFDictionary)
    }

    func sample(displays: [MacDisplay]) -> ActivitySample {
        let pointerTypes: [CGEventType] = [.mouseMoved, .leftMouseDown, .rightMouseDown, .otherMouseDown,
            .leftMouseUp, .rightMouseUp, .otherMouseUp, .leftMouseDragged, .rightMouseDragged,
            .otherMouseDragged, .scrollWheel, .tabletPointer, .tabletProximity]
        let pointerTimes = pointerTypes.compactMap(secondsSince)
        let keyboardTypes: [CGEventType] = [.keyDown, .keyUp, .flagsChanged]
        let keyboard = keyboardTypes.compactMap(secondsSince).min()
        // kCGAnyInputEventType is deliberately passed as its public sentinel, not a keyboard hook.
        let anyEvent = CGEventType(rawValue: UInt32.max)
        // Some SDK imports expose only named enum cases. Falling back to the minimum of the
        // documented input types keeps failure explicit without force-unwrapping an unknown case.
        let allInput = anyEvent.flatMap(secondsSince) ?? (pointerTypes + keyboardTypes).compactMap(secondsSince).min()
        let mouse = displays.first { NSMouseInRect(NSEvent.mouseLocation, $0.screen.frame, false) }?.id
        let foreground = foregroundDisplay(displays: displays)
        return ActivitySample(idleSeconds: allInput, keyboardIdleSeconds: keyboard,
            pointerIdleSeconds: pointerTimes.min(), mouseDisplayID: mouse,
            foregroundDisplayID: foreground.id, foregroundKnown: foreground.known)
    }

    private func secondsSince(_ type: CGEventType) -> Double? {
        let value = CGEventSource.secondsSinceLastEventType(.combinedSessionState, eventType: type)
        return value.isFinite && value >= 0 ? value : nil
    }

    private func foregroundDisplay(displays: [MacDisplay]) -> (id: String?, known: Bool) {
        guard accessibilityGranted, let application = NSWorkspace.shared.frontmostApplication else {
            return (nil, false)
        }
        let appElement = AXUIElementCreateApplication(application.processIdentifier)
        // A stalled third-party accessibility server must not freeze the main run loop indefinitely.
        AXUIElementSetMessagingTimeout(appElement, 0.15)
        var focused: CFTypeRef?
        guard AXUIElementCopyAttributeValue(appElement, kAXFocusedWindowAttribute as CFString, &focused) == .success,
              let focused, CFGetTypeID(focused) == AXUIElementGetTypeID() else { return (nil, false) }
        let window = focused as! AXUIElement
        var positionValue: CFTypeRef?
        var sizeValue: CFTypeRef?
        guard AXUIElementCopyAttributeValue(window, kAXPositionAttribute as CFString, &positionValue) == .success,
              AXUIElementCopyAttributeValue(window, kAXSizeAttribute as CFString, &sizeValue) == .success,
              let positionValue, let sizeValue,
              CFGetTypeID(positionValue) == AXValueGetTypeID(), CFGetTypeID(sizeValue) == AXValueGetTypeID()
        else { return (nil, false) }
        var origin = CGPoint.zero
        var size = CGSize.zero
        guard AXValueGetValue(positionValue as! AXValue, .cgPoint, &origin),
              AXValueGetValue(sizeValue as! AXValue, .cgSize, &size),
              origin.x.isFinite, origin.y.isFinite, size.width.isFinite, size.height.isFinite,
              size.width > 0, size.height > 0 else { return (nil, false) }
        // Accessibility and CGDisplayBounds both use top-left global coordinates; NSScreen does not.
        let bounds = CGRect(origin: origin, size: size)
        let intersections = displays.compactMap { display -> (String, CGFloat)? in
            let overlap = CGDisplayBounds(display.displayID).intersection(bounds)
            guard !overlap.isNull, overlap.width > 0, overlap.height > 0 else { return nil }
            return (display.id, overlap.width * overlap.height)
        }.sorted { $0.1 > $1.1 }
        guard let largest = intersections.first else { return (nil, false) }
        // Ambiguous cross-screen windows are unknown so automatic protection can defer conservatively.
        guard intersections.count == 1 else { return (nil, false) }
        return (largest.0, true)
    }
}
