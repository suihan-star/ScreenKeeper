import AppKit
import ColorSync
import Combine
import CoreGraphics

struct MacDisplay: Identifiable {
    let id: String
    let displayID: CGDirectDisplayID
    let screen: NSScreen
    let title: String
    let isBuiltIn: Bool
    let isMirrored: Bool
    let isIdentityReliable: Bool

    var canManageIndependently: Bool { isIdentityReliable && !isMirrored }
}

@MainActor
final class DisplayCatalog: NSObject, ObservableObject {
    @Published private(set) var displays: [MacDisplay] = []
    var onChange: (() -> Void)?

    override init() {
        super.init()
        NotificationCenter.default.addObserver(self, selector: #selector(screenParametersChanged),
            name: NSApplication.didChangeScreenParametersNotification, object: nil)
        refresh()
    }

    deinit { NotificationCenter.default.removeObserver(self) }

    @objc private func screenParametersChanged(_ notification: Notification) { refresh() }

    func refresh() {
        // CGDirectDisplayID addresses this connection; only the UUID associates saved preferences.
        let candidates: [(screen: NSScreen, displayID: CGDirectDisplayID, uuid: String?, name: String)] =
            NSScreen.screens.compactMap { screen in
                guard let number = screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber else {
                    return nil
                }
                let displayID = number.uint32Value
                let uuid = CGDisplayCreateUUIDFromDisplayID(displayID).map {
                    CFUUIDCreateString(nil, $0.takeRetainedValue()) as String
                }
                let name = screen.localizedName.trimmingCharacters(in: .whitespacesAndNewlines)
                return (screen, displayID, uuid, name)
            }
        let identityCounts = Dictionary(grouping: candidates.compactMap(\.uuid), by: { $0 }).mapValues(\.count)
        let nameCounts = Dictionary(grouping: candidates, by: { $0.name.lowercased() }).mapValues(\.count)
        displays = candidates.enumerated().map { index, candidate in
            let reliable = candidate.uuid.map { identityCounts[$0] == 1 } ?? false
            let id = reliable ? candidate.uuid! : "unresolved-\(candidate.displayID)"
            let number = index + 1
            let title: String
            if candidate.name.isEmpty {
                title = "Display \(number)"
            } else if nameCounts[candidate.name.lowercased(), default: 0] > 1 {
                title = "\(candidate.name) · \(number)"
            } else {
                title = candidate.name
            }
            return MacDisplay(id: id, displayID: candidate.displayID, screen: candidate.screen, title: title,
                isBuiltIn: CGDisplayIsBuiltin(candidate.displayID) != 0,
                isMirrored: CGDisplayIsInMirrorSet(candidate.displayID) != 0,
                isIdentityReliable: reliable)
        }
        onChange?()
    }
}
