import AppKit
import Combine
import CoreGraphics

@MainActor
final class SessionMonitor: NSObject, ObservableObject {
    @Published private(set) var isAvailable = false
    var onChange: (() -> Void)?
    private var systemSleeping = false
    private var screensSleeping = false
    private var sessionInactive = false
    private var sessionConfirmed = false

    override init() {
        super.init()
        let workspace = NSWorkspace.shared.notificationCenter
        workspace.addObserver(self, selector: #selector(willSleep), name: NSWorkspace.willSleepNotification, object: nil)
        workspace.addObserver(self, selector: #selector(didWake), name: NSWorkspace.didWakeNotification, object: nil)
        workspace.addObserver(self, selector: #selector(screensDidSleep), name: NSWorkspace.screensDidSleepNotification, object: nil)
        workspace.addObserver(self, selector: #selector(screensDidWake), name: NSWorkspace.screensDidWakeNotification, object: nil)
        workspace.addObserver(self, selector: #selector(sessionResigned), name: NSWorkspace.sessionDidResignActiveNotification, object: nil)
        workspace.addObserver(self, selector: #selector(sessionBecameActive), name: NSWorkspace.sessionDidBecomeActiveNotification, object: nil)
        NotificationCenter.default.addObserver(self, selector: #selector(applicationBecameActive),
            name: NSApplication.didBecomeActiveNotification, object: nil)
        // Public session metadata does not provide a documented screen-lock flag. At startup wait
        // for an interactive activation rather than treating an existing console session as unlocked.
        update()
    }

    deinit {
        NSWorkspace.shared.notificationCenter.removeObserver(self)
        NotificationCenter.default.removeObserver(self)
    }

    @objc private func willSleep(_ notification: Notification) {
        systemSleeping = true
        sessionConfirmed = false
        update()
    }
    @objc private func didWake(_ notification: Notification) { systemSleeping = false; update() }
    @objc private func screensDidSleep(_ notification: Notification) {
        screensSleeping = true
        sessionConfirmed = false
        update()
    }
    @objc private func screensDidWake(_ notification: Notification) { screensSleeping = false; update() }
    @objc private func sessionResigned(_ notification: Notification) {
        sessionInactive = true
        sessionConfirmed = false
        update()
    }
    @objc private func sessionBecameActive(_ notification: Notification) {
        sessionInactive = false
        sessionConfirmed = true
        update()
    }
    @objc private func applicationBecameActive(_ notification: Notification) {
        sessionConfirmed = true
        update()
    }

    private func update() {
        let dictionary = CGSessionCopyCurrentDictionary() as? [String: Any]
        let console = dictionary?[kCGSessionOnConsoleKey as String] as? Bool ?? false
        let loggedIn = dictionary?[kCGSessionLoginDoneKey as String] as? Bool ?? false
        let available = console && loggedIn && sessionConfirmed && !sessionInactive && !systemSleeping && !screensSleeping
        guard available != isAvailable else { return }
        isAvailable = available
        onChange?()
    }
}
