import AppKit
import Combine
import ScreenKeeperCore
import SwiftUI

@MainActor
final class HealthPresentation {
    private(set) var hostID: String?
    private var panel: ReminderPanel?
    private var scene: NSWindow?
    private var language: AppLanguage = .chinese
    private var onStart: () -> Void = {}
    private var onSnooze: () -> Void = {}
    private var onSkip: () -> Void = {}
    private let cardState = HealthCardState()

    func show(on display: MacDisplay, language: AppLanguage, remaining: Int, breaking: Bool, preview: Bool,
              onStart: @escaping () -> Void, onSnooze: @escaping () -> Void, onSkip: @escaping () -> Void) {
        close()
        self.language = language; self.onStart = onStart; self.onSnooze = onSnooze; self.onSkip = onSkip
        hostID = display.id
        if breaking {
            let background = NSWindow(contentRect: display.screen.frame, styleMask: [.borderless], backing: .buffered, defer: false)
            background.isReleasedWhenClosed = false
            background.level = .floating
            background.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary]
            background.backgroundColor = NSColor(red: 0.055, green: 0.13, blue: 0.10, alpha: 1)
            background.contentView = NSHostingView(rootView: RestScene())
            background.setFrame(display.screen.frame, display: true)
            background.orderFrontRegardless()
            scene = background
        }
        let window = ReminderPanel(contentRect: NSRect(x: 0, y: 0, width: 470, height: 370),
            styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        window.isReleasedWhenClosed = false; window.hidesOnDeactivate = false
        window.isFloatingPanel = true; window.level = NSWindow.Level(rawValue: NSWindow.Level.floating.rawValue + 1)
        window.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary]
        window.isOpaque = false; window.backgroundColor = .clear; window.hasShadow = true
        window.onEscape = onSkip
        panel = window
        cardState.language = language
        cardState.preview = preview
        window.contentView = NSHostingView(rootView: HostedHealthCard(state: cardState,
            onStart: onStart, onSnooze: onSnooze, onSkip: onSkip))
        update(remaining: remaining, breaking: breaking)
        let size = window.contentView?.fittingSize ?? NSSize(width: 470, height: 370)
        let width = min(max(430, size.width), display.screen.frame.width)
        let height = min(max(300, size.height), display.screen.frame.height)
        window.setFrame(NSRect(x: display.screen.frame.midX - width / 2,
            y: display.screen.frame.midY - height / 2, width: width, height: height), display: true)
        window.orderFrontRegardless()
        if breaking { window.makeKey() }
    }

    func update(remaining: Int, breaking: Bool) {
        if cardState.remaining != max(0, remaining) { cardState.remaining = max(0, remaining) }
        if cardState.breaking != breaking { cardState.breaking = breaking }
    }

    func close() {
        panel?.orderOut(nil); panel?.close(); panel = nil
        scene?.orderOut(nil); scene?.close(); scene = nil
        hostID = nil
    }
}

@MainActor
private final class HealthCardState: ObservableObject {
    @Published var language: AppLanguage = .chinese
    @Published var remaining = 0
    @Published var breaking = false
    @Published var preview = false
}

@MainActor
private struct HostedHealthCard: View {
    @ObservedObject var state: HealthCardState
    let onStart: () -> Void
    let onSnooze: () -> Void
    let onSkip: () -> Void
    var body: some View {
        VStack(spacing: 8) {
            if state.preview {
                Text(state.language == .chinese ? "效果预览 · 操作后关闭，不改变正式周期" : "Preview · Actions close this preview without changing your cycle")
                    .font(.caption).foregroundStyle(.white).padding(8).background(.black.opacity(0.7), in: Capsule())
            }
            HealthCardView(language: state.language, remainingSeconds: state.remaining, isBreaking: state.breaking,
                onStart: onStart, onSnooze: onSnooze, onSkip: onSkip)
        }
    }
}

private final class ReminderPanel: NSPanel {
    var onEscape: (() -> Void)?
    override var canBecomeKey: Bool { true }
    override var canBecomeMain: Bool { false }
    override func cancelOperation(_ sender: Any?) { onEscape?() }
}

private struct RestScene: View {
    private let image = AppResources.url("forest-mist", extension: "png")
        .flatMap { NSImage(contentsOf: $0) }
    var body: some View {
        GeometryReader { proxy in
            ZStack {
                Color(red: 0.055, green: 0.13, blue: 0.10)
                if let image { Image(nsImage: image).resizable().scaledToFill().frame(width: proxy.size.width, height: proxy.size.height).clipped() }
                Color.black.opacity(0.22)
            }
        }.ignoresSafeArea()
    }
}
