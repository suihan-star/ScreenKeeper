import AppKit
import ImageIO
import ScreenKeeperCore

/// Owns only our windows. It never changes hardware power, brightness or another app's windows.
@MainActor
final class OverlayController {
    private var privacy: [String: PrivacyPresentation] = [:]
    private var rest: [String: OverlayPanel] = [:]
    private var stopped = false

    func hasPrivacy(id: String) -> Bool { privacy[id] != nil }
    func hasRest(id: String) -> Bool { rest[id] != nil }

    @discardableResult
    func showPrivacy(on display: MacDisplay, language: AppLanguage, background: PrivacyBackground,
                     imagePaths: [String], slideshowSeconds: Int, onEnter: @escaping () -> Void) -> Bool {
        guard !stopped, display.canManageIndependently else { return false }
        if let existing = privacy[display.id] {
            existing.view.enterTitle = language == .chinese ? "进入此屏幕" : "Enter this screen"
            existing.view.onEnter = onEnter
            existing.configure(background: background, imagePaths: imagePaths, slideshowSeconds: slideshowSeconds)
            existing.panel.setFrame(display.screen.frame, display: true)
            existing.panel.orderFrontRegardless()
            return existing.panel.isVisible
        }
        let panel = makePanel(on: display, rest: false)
        let view = PrivacyOverlayView(frame: NSRect(origin: .zero, size: display.screen.frame.size))
        view.enterTitle = language == .chinese ? "进入此屏幕" : "Enter this screen"
        view.onEnter = onEnter
        panel.contentView = view
        let presentation = PrivacyPresentation(panel: panel, view: view)
        privacy[display.id] = presentation
        presentation.configure(background: background, imagePaths: imagePaths, slideshowSeconds: slideshowSeconds)
        panel.orderFrontRegardless()
        guard panel.isVisible else {
            dismissPrivacy(id: display.id)
            return false
        }
        return true
    }

    @discardableResult
    func showRest(on display: MacDisplay, onWake: @escaping () -> Void) -> Bool {
        guard !stopped, display.canManageIndependently else { return false }
        if let existing = rest[display.id] {
            existing.setFrame(display.screen.frame, display: true)
            existing.orderFrontRegardless()
            return existing.isVisible
        }
        let panel = makePanel(on: display, rest: true)
        let view = RestOverlayView(frame: NSRect(origin: .zero, size: display.screen.frame.size))
        view.onWake = onWake
        panel.contentView = view
        rest[display.id] = panel
        panel.orderFrontRegardless()
        guard panel.isVisible else {
            dismissRest(id: display.id)
            return false
        }
        return true
    }

    func dismissPrivacy(id: String) {
        guard let presentation = privacy.removeValue(forKey: id) else { return }
        presentation.stop()
        presentation.panel.orderOut(nil)
        presentation.panel.close()
    }

    func dismissRest(id: String) {
        guard let panel = rest.removeValue(forKey: id) else { return }
        panel.orderOut(nil)
        panel.close()
        // Removing software sleep must reveal an existing privacy mask, never dismiss it.
        privacy[id]?.panel.orderFrontRegardless()
    }

    func reconcile(displays: [MacDisplay]) {
        let current = Dictionary(uniqueKeysWithValues: displays.map { ($0.id, $0) })
        for id in Array(privacy.keys) {
            if let display = current[id] { privacy[id]?.panel.setFrame(display.screen.frame, display: true) }
            else { dismissPrivacy(id: id) }
        }
        for id in Array(rest.keys) {
            if let display = current[id] { rest[id]?.setFrame(display.screen.frame, display: true) }
            else { dismissRest(id: id) }
        }
    }

    /// Terminal lifecycle operation. A late timer must never recreate an overlay after exit begins.
    func closeAll() {
        stopped = true
        for id in Array(rest.keys) { dismissRest(id: id) }
        for id in Array(privacy.keys) { dismissPrivacy(id: id) }
    }

    private func makePanel(on display: MacDisplay, rest: Bool) -> OverlayPanel {
        let panel = OverlayPanel(contentRect: display.screen.frame,
            styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false, screen: display.screen)
        panel.setFrame(display.screen.frame, display: false)
        panel.isReleasedWhenClosed = false
        panel.isOpaque = true
        panel.backgroundColor = .black
        panel.hasShadow = false
        panel.hidesOnDeactivate = false
        panel.ignoresMouseEvents = false
        panel.isMovable = false
        panel.animationBehavior = .none
        panel.level = NSWindow.Level(rawValue: NSWindow.Level.screenSaver.rawValue + (rest ? 1 : 0))
        // These are requests to WindowServer, not proof of coverage of secure UI or every Space.
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .ignoresCycle]
        panel.setAccessibilityLabel(rest ? "ScreenKeeper screen rest" : "ScreenKeeper privacy screen")
        return panel
    }
}

@MainActor
private final class OverlayPanel: NSPanel {
    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { false }
}

@MainActor
private final class RestOverlayView: NSView {
    var onWake: (() -> Void)?
    override var isOpaque: Bool { true }
    override var acceptsFirstResponder: Bool { true }
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
    override func draw(_ dirtyRect: NSRect) { NSColor.black.setFill(); dirtyRect.fill() }
    // Consume the first click rather than forwarding it to the hidden application.
    override func mouseDown(with event: NSEvent) { onWake?() }
    override func rightMouseDown(with event: NSEvent) { onWake?() }
    override func otherMouseDown(with event: NSEvent) { onWake?() }
    override func scrollWheel(with event: NSEvent) { onWake?() }
    override func keyDown(with event: NSEvent) { onWake?() }
}

@MainActor
private final class PrivacyOverlayView: NSView {
    var image: NSImage? { didSet { needsDisplay = true } }
    var onEnter: (() -> Void)?
    var enterTitle: String = "进入此屏幕" { didSet { enterButton.title = enterTitle } }
    private lazy var enterButton = NSButton(title: enterTitle, target: self, action: #selector(enterScreen))
    override var isOpaque: Bool { true }
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        enterButton.bezelStyle = .rounded
        enterButton.controlSize = .large
        enterButton.font = .systemFont(ofSize: 16, weight: .medium)
        enterButton.contentTintColor = NSColor(calibratedRed: 0.12, green: 0.23, blue: 0.17, alpha: 1)
        enterButton.bezelColor = NSColor(calibratedRed: 0.76, green: 0.86, blue: 0.70, alpha: 1)
        enterButton.translatesAutoresizingMaskIntoConstraints = false
        addSubview(enterButton)
        NSLayoutConstraint.activate([
            enterButton.centerXAnchor.constraint(equalTo: centerXAnchor),
            enterButton.centerYAnchor.constraint(equalTo: centerYAnchor),
            enterButton.widthAnchor.constraint(greaterThanOrEqualToConstant: 184),
            enterButton.heightAnchor.constraint(greaterThanOrEqualToConstant: 44)
        ])
    }

    required init?(coder: NSCoder) { return nil }

    override func draw(_ dirtyRect: NSRect) {
        NSColor(calibratedRed: 0.055, green: 0.11, blue: 0.08, alpha: 1).setFill()
        dirtyRect.fill()
        guard let image, image.size.width > 0, image.size.height > 0 else { return }
        let scale = max(bounds.width / image.size.width, bounds.height / image.size.height)
        let width = image.size.width * scale
        let height = image.size.height * scale
        image.draw(in: NSRect(x: (bounds.width - width) / 2, y: (bounds.height - height) / 2,
            width: width, height: height), from: .zero, operation: .sourceOver, fraction: 1)
        NSColor(calibratedRed: 0.035, green: 0.08, blue: 0.05, alpha: 0.22).setFill()
        dirtyRect.fill()
    }

    @objc private func enterScreen(_ sender: NSButton) { onEnter?() }
}

@MainActor
private final class PrivacyPresentation: NSObject {
    let panel: OverlayPanel
    let view: PrivacyOverlayView
    private var timer: Timer?
    private var paths: [String] = []
    private var currentIndex = 0
    private var configuration: Configuration?

    private struct Configuration: Equatable {
        let background: PrivacyBackground
        let paths: [String]
        let interval: Int
    }

    init(panel: OverlayPanel, view: PrivacyOverlayView) {
        self.panel = panel
        self.view = view
        super.init()
    }

    func configure(background: PrivacyBackground, imagePaths: [String], slideshowSeconds: Int) {
        let next = Configuration(background: background, paths: Array(imagePaths.prefix(12)), interval: slideshowSeconds)
        guard next != configuration else { return }
        configuration = next
        timer?.invalidate()
        timer = nil
        switch background {
        case .builtIn:
            view.image = OverlayAssets.builtInImage
        case .solid:
            view.image = nil
        case .custom:
            paths = Array(imagePaths.prefix(12))
            currentIndex = 0
            loadCurrentImage()
            guard paths.count > 1 else { return }
            let interval = TimeInterval(max(5, min(86400, slideshowSeconds)))
            let slideshow = Timer(timeInterval: interval, target: self,
                selector: #selector(advanceImage), userInfo: nil, repeats: true)
            timer = slideshow
            RunLoop.main.add(slideshow, forMode: .common)
        }
    }

    func stop() { timer?.invalidate(); timer = nil }

    @objc private func advanceImage() {
        guard !paths.isEmpty else { return }
        currentIndex = (currentIndex + 1) % paths.count
        loadCurrentImage()
    }

    private func loadCurrentImage() {
        guard paths.indices.contains(currentIndex) else { view.image = nil; return }
        // Missing or unreadable custom images retain the opaque solid backdrop.
        view.image = OverlayAssets.loadLocalImage(url: URL(fileURLWithPath: paths[currentIndex]))
    }
}

@MainActor
enum OverlayAssets {
    static let builtInImage: NSImage? = {
        guard let url = AppResources.url("forest-mist", extension: "png") else { return nil }
        return loadLocalImage(url: url)
    }()

    static func loadLocalImage(url: URL) -> NSImage? {
        guard url.isFileURL,
              let attributes = try? FileManager.default.attributesOfItem(atPath: url.path),
              // Import limits the source to 10 MB. Its normalized PNG can be larger, so retain
              // the decoded pixel limit here rather than rejecting a valid converted photograph.
              let bytes = attributes[.size] as? NSNumber, bytes.int64Value <= 160 * 1024 * 1024,
              let source = CGImageSourceCreateWithURL(url as CFURL, nil),
              let properties = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [String: Any],
              let width = properties[kCGImagePropertyPixelWidth as String] as? NSNumber,
              let height = properties[kCGImagePropertyPixelHeight as String] as? NSNumber,
              width.doubleValue > 0, height.doubleValue > 0,
              width.doubleValue * height.doubleValue <= 40_000_000,
              let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else { return nil }
        return NSImage(cgImage: image, size: NSSize(width: image.width, height: image.height))
    }
}
