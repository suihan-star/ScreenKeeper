import AppKit
import ApplicationServices
import Combine
import CoreGraphics
import Darwin
import ScreenKeeperCore
import SwiftUI

@main
@MainActor
enum ScreenKeeperApp {
    static func main() {
        if CommandLine.arguments.contains("--package-check") { exit(PackageCheck.run()) }
        let application = NSApplication.shared
        if CommandLine.arguments.contains("--diagnostics") {
            Diagnostics.printReport()
            return
        }
        if let identifier = Bundle.main.bundleIdentifier,
           let existing = NSRunningApplication.runningApplications(withBundleIdentifier: identifier)
            .first(where: { $0.processIdentifier != ProcessInfo.processInfo.processIdentifier }) {
            existing.activate(options: [.activateAllWindows, .activateIgnoringOtherApps])
            return
        }
        application.setActivationPolicy(.regular)
        let instanceLock: InstanceLock
        do { instanceLock = try InstanceLock() }
        catch {
            let alert = NSAlert()
            alert.messageText = "屏幕良伴无法启动 / ScreenKeeper could not start"
            alert.informativeText = "请确认软件未在运行，且当前用户可以访问配置目录。\nCheck that another instance is not running and your settings directory is accessible."
            alert.runModal()
            return
        }
        let delegate = AppDelegate()
        application.delegate = delegate
        withExtendedLifetime((delegate, instanceLock)) { application.run() }
    }
}

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate, NSWindowDelegate, NSMenuDelegate {
    private var model: AppModel!
    private var window: NSWindow?
    private var statusItem: NSStatusItem?
    private var preferenceObserver: AnyCancellable?

    func applicationDidFinishLaunching(_ notification: Notification) {
        model = AppModel()
        let view = RootSettingsView().environmentObject(model)
        let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 1020, height: 760),
            styleMask: [.titled, .closable, .miniaturizable, .resizable], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false
        window.contentView = NSHostingView(rootView: view)
        window.minSize = NSSize(width: 700, height: 500)
        window.delegate = self; window.center()
        self.window = window
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.squareLength)
        statusItem?.button?.image = makeMenuIcon()
        let statusMenu = NSMenu(); statusMenu.delegate = self; statusItem?.menu = statusMenu
        preferenceObserver = model.$saved.sink { [weak self] _ in
            DispatchQueue.main.async { self?.refreshAppMenu() }
        }
        refreshAppMenu(); showSettings()
    }

    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool { false }
    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool { showSettings(); return true }
    func applicationDidBecomeActive(_ notification: Notification) {
        if window?.isVisible == false { showSettings() }
    }
    func windowShouldClose(_ sender: NSWindow) -> Bool { model.cancelHealthPreview(); sender.orderOut(nil); return false }
    func windowWillMiniaturize(_ notification: Notification) { model.cancelHealthPreview() }

    func applicationShouldTerminate(_ sender: NSApplication) -> NSApplication.TerminateReply {
        if model.hasAnyDraft {
            let alert = NSAlert()
            alert.messageText = model.text("还有未保存的修改", "You have unsaved changes")
            alert.informativeText = model.text("退出将放弃这些修改；已经保存的设置会保留。", "Quitting discards unsaved changes. Saved settings are kept.")
            alert.addButton(withTitle: model.text("继续编辑", "Keep editing"))
            alert.addButton(withTitle: model.text("放弃修改并退出", "Discard and quit"))
            showSettings()
            alert.window.level = NSWindow.Level(rawValue: NSWindow.Level.screenSaver.rawValue + 2)
            alert.window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]
            if alert.runModal() != .alertSecondButtonReturn { return .terminateCancel }
        }
        model.shutdown()
        return .terminateNow
    }

    func applicationWillTerminate(_ notification: Notification) { model?.shutdown() }
    func menuWillOpen(_ menu: NSMenu) { populate(menu) }

    @objc private func showSettings() {
        window?.deminiaturize(nil); window?.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }
    @objc private func protectSelected() { model.protectNow() }
    @objc private func toggleHealthPause() { model.toggleHealthPause() }
    @objc private func website() { model.openWebsite() }
    @objc private func quit() { NSApp.terminate(nil) }

    private func populate(_ menu: NSMenu) {
        menu.removeAllItems()
        add(menu, model.text("打开屏幕良伴", "Open ScreenKeeper"), #selector(showSettings))
        menu.addItem(.separator())
        add(menu, model.text("立即保护所选屏幕", "Protect selected display"), #selector(protectSelected))
        add(menu, model.text("暂停／恢复健康提醒", "Pause / resume health reminders"), #selector(toggleHealthPause))
        menu.addItem(.separator())
        add(menu, model.text("访问官网", "Visit website"), #selector(website))
        add(menu, model.text("退出屏幕良伴", "Quit ScreenKeeper"), #selector(quit), key: "q")
    }

    private func add(_ menu: NSMenu, _ title: String, _ action: Selector, key: String = "") {
        let item = NSMenuItem(title: title, action: action, keyEquivalent: key); item.target = self; menu.addItem(item)
    }

    private func refreshAppMenu() {
        guard model != nil else { return }
        window?.title = model.text("屏幕良伴", "ScreenKeeper")
        statusItem?.button?.toolTip = window?.title
        let main = NSMenu()
        let product = NSMenuItem(); let productMenu = NSMenu(); populate(productMenu)
        product.submenu = productMenu; main.addItem(product)
        let editing = NSMenuItem(title: model.text("编辑", "Edit"), action: nil, keyEquivalent: "")
        let editMenu = NSMenu()
        for (title, selector, key) in [(model.text("撤销", "Undo"), "undo:", "z"),
            (model.text("剪切", "Cut"), "cut:", "x"), (model.text("复制", "Copy"), "copy:", "c"),
            (model.text("粘贴", "Paste"), "paste:", "v"), (model.text("全选", "Select All"), "selectAll:", "a")] {
            editMenu.addItem(NSMenuItem(title: title, action: Selector(selector), keyEquivalent: key))
        }
        editing.submenu = editMenu; main.addItem(editing); NSApp.mainMenu = main
    }

    private func makeMenuIcon() -> NSImage {
        if let url = AppResources.url("icon", extension: "svg"), let image = NSImage(contentsOf: url) {
            image.size = NSSize(width: 20, height: 20)
            image.isTemplate = true
            return image
        }
        // Template rendering follows macOS menu-bar contrast independently of the app theme.
        let image = NSImage(size: NSSize(width: 22, height: 22), flipped: false) { _ in
            NSColor.black.setStroke()
            let rear = NSBezierPath(roundedRect: NSRect(x: 2, y: 7, width: 14, height: 12), xRadius: 2, yRadius: 2)
            rear.lineWidth = 1.6; rear.stroke()
            let frontRect = NSRect(x: 6, y: 3, width: 14, height: 12)
            NSColor.clear.setFill()
            let front = NSBezierPath(roundedRect: frontRect, xRadius: 2, yRadius: 2)
            front.lineWidth = 1.7; front.stroke()
            let moon = NSBezierPath()
            moon.appendArc(withCenter: NSPoint(x: 13, y: 9), radius: 3.3, startAngle: 85, endAngle: 290)
            moon.curve(to: NSPoint(x: 13.3, y: 12.3), controlPoint1: NSPoint(x: 11.8, y: 8.8), controlPoint2: NSPoint(x: 11.5, y: 10.8))
            moon.close(); NSColor.black.setFill(); moon.fill()
            return true
        }
        image.isTemplate = true
        return image
    }
}

@MainActor
enum Diagnostics {
    static func printReport() {
        let catalog = DisplayCatalog()
        // No serial numbers, persistent IDs, window titles, images, or color-table values.
        let screens: [[String: Any]] = catalog.displays.enumerated().map { index, display in
            let probe = ReadOnlyDisplayProbe.capture(display)
            var row: [String: Any] = [:]
            if let data = try? JSONEncoder().encode(probe),
               let object = try? JSONSerialization.jsonObject(with: data) as? [String: Any] { row = object }
            row["number"] = index + 1
            return row
        }
        let report: [String: Any] = ["app": "ScreenKeeper macOS development preview", "schema": 1,
            "system": ProcessInfo.processInfo.operatingSystemVersionString,
            "accessibilityGranted": AXIsProcessTrusted(), "displays": screens,
            "note": "Read-only. No display colors, brightness, topology, or settings were changed."]
        if let data = try? JSONSerialization.data(withJSONObject: report, options: [.prettyPrinted, .sortedKeys]),
           let string = String(data: data, encoding: .utf8) { print(string) }
    }
}
