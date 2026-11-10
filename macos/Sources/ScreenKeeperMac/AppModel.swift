import AppKit
import ApplicationServices
import Combine
import ImageIO
import ScreenKeeperCore
import SwiftUI
import UniformTypeIdentifiers

enum SettingsModule: String, CaseIterable, Identifiable {
    case rest, privacy, health, color, general
    var id: String { rawValue }
}

@MainActor
final class AppModel: ObservableObject {
    @Published var saved = AppSettings()
    @Published var draft = AppSettings()
    @Published var displays: [MacDisplay] = []
    @Published var selectedScreenID: String?
    @Published var selectedModule: SettingsModule = .rest
    @Published var statusText = ""
    @Published var errorMessage: String?
    @Published var healthSnapshot = HealthSnapshot(state: .disabled, remainingSeconds: 0, shouldPresent: false)
    @Published var privacyProtectedIDs: Set<String> = []
    @Published var restingIDs: Set<String> = []
    @Published var sessionAvailable = false
    @Published var accessibilityGranted = false

    private let store: SettingsStore
    private let dataDirectory: URL
    private let catalog = DisplayCatalog()
    private let activity = ActivityMonitor()
    private let session = SessionMonitor()
    private let overlays = OverlayController()
    private let health = HealthTimer()
    private let healthPresentation = HealthPresentation()
    private var timer: Timer?
    private var runtime: [String: ScreenRuntime] = [:]
    private var previousInput: ActivitySample?
    private var stopped = false
    private var previewingHealth = false
    private var previewDeadline: Double = 0
    private var previousHealthState: HealthState = .disabled
    private var previousTick: Double?
    private var healthBlockedUntil: Double = 0
    private var failedOverlayIDs: Set<String> = []

    private struct ScreenRuntime {
        var restSince: Double
        var privacySince: Double
        var restSnoozeUntil: Double = 0
        var privacySnoozeUntil: Double = 0
        var restWasDeferred = false
        var privacyWasDeferred = false
    }

    private var now: Double { ProcessInfo.processInfo.systemUptime }

    init() {
        let directory = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("ScreenKeeper", isDirectory: true)
        dataDirectory = directory
        store = SettingsStore(directory: directory)
        healthSnapshot = health.tick(now: ProcessInfo.processInfo.systemUptime,
            sessionAvailable: false, inputIdleSeconds: nil, screenAvailable: false)
        do {
            saved = try store.load()
            draft = saved
        } catch {
            errorMessage = settingsErrorMessage(error)
        }
        health.apply(settings: saved.health, now: now)
        catalog.onChange = { [weak self] in self?.syncDisplays() }
        session.onChange = { [weak self] in self?.sessionChanged() }
        syncDisplays()
        sessionAvailable = session.isAvailable
        timer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
            Task { @MainActor [weak self] in self?.tick() }
        }
    }

    func text(_ chinese: String, _ english: String) -> String {
        draft.language == .chinese ? chinese : english
    }

    var isCurrentModuleDirty: Bool {
        switch selectedModule {
        case .rest:
            return allScreenIDs.contains { (saved.screens[$0] ?? ScreenSettings()).rest != (draft.screens[$0] ?? ScreenSettings()).rest }
        case .privacy:
            return allScreenIDs.contains { (saved.screens[$0] ?? ScreenSettings()).privacy != (draft.screens[$0] ?? ScreenSettings()).privacy }
        case .health: return saved.health != draft.health
        case .color, .general: return false
        }
    }

    var hasAnyDraft: Bool {
        saved.health != draft.health || allScreenIDs.contains {
            (saved.screens[$0] ?? ScreenSettings()) != (draft.screens[$0] ?? ScreenSettings())
        }
    }
    private var allScreenIDs: Set<String> { Set(saved.screens.keys).union(draft.screens.keys) }

    func refreshDisplays() {
        guard !hasAnyDraft else {
            errorMessage = text("请先保存或放弃当前模块的修改。", "Save or discard changes in this module first.")
            return
        }
        catalog.refresh()
        syncDisplays()
    }

    private func syncDisplays() {
        guard !stopped else { return }
        displays = catalog.displays
        let connected = Set(displays.map(\.id))
        overlays.reconcile(displays: displays)
        privacyProtectedIDs.formIntersection(connected)
        restingIDs.formIntersection(connected)
        failedOverlayIDs.formIntersection(connected)
        runtime = runtime.filter { connected.contains($0.key) }
        for display in displays {
            if runtime[display.id] == nil { runtime[display.id] = ScreenRuntime(restSince: now, privacySince: now) }
        }
        if !displays.contains(where: { $0.id == selectedScreenID }) { selectedScreenID = displays.first?.id }
        // A reminder must not migrate to another display after its host disappears.
        if let host = healthPresentation.hostID, !connected.contains(host) { dismissHealth() }
    }

    private func sessionChanged() {
        guard !stopped else { return }
        sessionAvailable = session.isAvailable
        previousInput = nil
        for id in runtime.keys {
            runtime[id]?.restSince = now
            runtime[id]?.privacySince = now
        }
        dismissHealth()
        if !sessionAvailable {
            for display in displays {
                overlays.dismissRest(id: display.id)
            }
            restingIDs.removeAll()
        } else {
            for display in displays where privacyProtectedIDs.contains(display.id) {
                if !presentPrivacy(display) { failedOverlayIDs.insert(display.id) }
            }
        }
    }

    func saveCurrentModule() {
        var candidate = saved
        for id in allScreenIDs {
            var screen = candidate.screens[id] ?? ScreenSettings()
            let edited = draft.screens[id] ?? ScreenSettings()
            if selectedModule == .rest { screen.rest = edited.rest }
            if selectedModule == .privacy { screen.privacy = edited.privacy }
            candidate.screens[id] = screen
        }
        if selectedModule == .health { candidate.health = draft.health }
        guard persist(candidate) else { return }
        // Only replace this module's draft, never another module's pending edits.
        discardCurrentModule()
        failedOverlayIDs.removeAll()
        for display in displays {
            let settings = saved.screens[display.id] ?? ScreenSettings()
            if selectedModule == .rest {
                runtime[display.id]?.restSince = now
                if !settings.rest.enabled { wake(display.id) }
            }
            if selectedModule == .privacy {
                runtime[display.id]?.privacySince = now
                if !settings.privacy.enabled { enter(display.id) }
                else if privacyProtectedIDs.contains(display.id) { _ = presentPrivacy(display) }
            }
        }
        if selectedModule == .health { dismissHealth(); health.apply(settings: saved.health, now: now) }
        statusText = text("已保存当前模块的设置", "Settings saved for this module")
    }

    func discardCurrentModule() {
        for id in allScreenIDs {
            var edited = draft.screens[id] ?? ScreenSettings()
            let baseline = saved.screens[id] ?? ScreenSettings()
            if selectedModule == .rest { edited.rest = baseline.rest }
            if selectedModule == .privacy { edited.privacy = baseline.privacy }
            draft.screens[id] = edited
        }
        if selectedModule == .health { draft.health = saved.health; dismissHealth() }
    }

    @discardableResult private func persist(_ settings: AppSettings) -> Bool {
        do { try store.save(settings); saved = settings; return true }
        catch {
            errorMessage = settingsErrorMessage(error)
            return false
        }
    }

    private func settingsErrorMessage(_ error: Error) -> String {
        guard let settingsError = error as? SettingsError else {
            return text("无法保存设置，修改和原文件均已保留。", "Settings could not be saved. Changes and the original file are preserved.")
        }
        switch settingsError {
        case .unsupportedSchema:
            return text("设置来自不兼容的版本，已停止读写并保留原文件。", "These settings use an incompatible version. The original file has been preserved.")
        case .invalidValue:
            return text("请检查时长、免打扰条件和图片选择；更改尚未保存。", "Check durations, quiet conditions, and selected images. Changes have not been saved.")
        case .unreadableSettings:
            return text("无法读取设置，已保留原文件并停止覆盖。请检查文件权限或使用兼容版本。", "Settings could not be read and will not be overwritten. Check file access or use a compatible version.")
        case .writeFailed:
            return text("保存失败，修改仍保留。请检查磁盘空间和配置目录权限。", "Save failed. Changes are preserved. Check disk space and access to the settings directory.")
        }
    }

    func updateLanguage(_ language: AppLanguage) {
        var candidate = saved; candidate.language = language
        if persist(candidate) { draft.language = language; dismissHealth(); refreshPrivacyAppearance() }
    }

    func updateTheme(_ theme: AppTheme) {
        var candidate = saved; candidate.theme = theme
        if persist(candidate) { draft.theme = theme }
    }

    private func refreshPrivacyAppearance() {
        for display in displays where privacyProtectedIDs.contains(display.id) && sessionAvailable { _ = presentPrivacy(display) }
    }

    func copyCurrentScreen(to targets: Set<String>) {
        guard let sourceID = selectedScreenID else { return }
        let source = draft.screens[sourceID] ?? ScreenSettings()
        for id in targets where id != sourceID && displays.contains(where: { $0.id == id && $0.isIdentityReliable && !$0.isMirrored }) {
            var target = draft.screens[id] ?? ScreenSettings()
            if selectedModule == .rest {
                let enabled = target.rest.enabled; target.rest = source.rest; target.rest.enabled = enabled
            } else if selectedModule == .privacy {
                let enabled = target.privacy.enabled; target.privacy = source.privacy; target.privacy.enabled = enabled
            }
            draft.screens[id] = target
        }
        statusText = text("已应用到所选屏幕，保存后生效；各屏开关保持不变。", "Copied to selected screens. Save to apply; their enabled states are unchanged.")
    }

    func protectNow() {
        guard let display = displays.first(where: { $0.id == selectedScreenID }), eligible(display), sessionAvailable else { return }
        let settings = saved.screens[display.id] ?? ScreenSettings()
        guard settings.privacy.enabled else {
            errorMessage = text("请先启用并保存本屏隐私保护。", "Enable and save privacy protection for this display first.")
            return
        }
        failedOverlayIDs.remove(display.id)
        _ = presentPrivacy(display)
    }

    private func eligible(_ display: MacDisplay) -> Bool { display.isIdentityReliable && !display.isMirrored }

    @discardableResult private func presentPrivacy(_ display: MacDisplay) -> Bool {
        guard !stopped, sessionAvailable, eligible(display) else { return false }
        dismissHealth()
        let preferences = (saved.screens[display.id] ?? ScreenSettings()).privacy
        let shown = overlays.showPrivacy(on: display, language: saved.language, background: preferences.background,
            imagePaths: preferences.imagePaths, slideshowSeconds: preferences.slideshowSeconds,
            onEnter: { [weak self] in self?.enter(display.id) })
        if shown { privacyProtectedIDs.insert(display.id) }
        else {
            failedOverlayIDs.insert(display.id)
            errorMessage = text("无法显示隐私幕，本屏尚未受到保护。", "The privacy curtain could not be displayed. This screen is not protected.")
        }
        return shown
    }

    private func enter(_ id: String) {
        overlays.dismissPrivacy(id: id)
        privacyProtectedIDs.remove(id)
        runtime[id]?.privacySince = now
    }

    private func wake(_ id: String) {
        overlays.dismissRest(id: id)
        restingIDs.remove(id)
        runtime[id]?.restSince = now
    }

    func wakeSelected() { if let id = selectedScreenID { wake(id) } }

    func snoozeSelected(minutes: Int) {
        guard let id = selectedScreenID, (1...1440).contains(minutes) else { return }
        let screen = saved.screens[id] ?? ScreenSettings()
        if selectedModule == .rest && screen.rest.enabled { runtime[id]?.restSnoozeUntil = now + Double(minutes * 60) }
        if selectedModule == .privacy && screen.privacy.enabled { runtime[id]?.privacySnoozeUntil = now + Double(minutes * 60) }
        statusText = text("已暂缓 \(minutes) 分钟；现有保护保持不变。", "Snoozed for \(minutes) minutes; existing protection remains.")
    }

    func resumeSelected() {
        guard let id = selectedScreenID else { return }
        if selectedModule == .rest { runtime[id]?.restSnoozeUntil = 0; runtime[id]?.restSince = now }
        if selectedModule == .privacy { runtime[id]?.privacySnoozeUntil = 0; runtime[id]?.privacySince = now }
        statusText = text("已恢复自动计时", "Automatic timing resumed")
    }

    private func conditionHolds(_ conditions: QuietConditions, displayID: String, sample: ActivitySample) -> Bool {
        (conditions.mouseOnScreen && sample.mouseDisplayID == displayID) ||
        (conditions.foregroundOnScreen && (!sample.foregroundKnown || sample.foregroundDisplayID == displayID)) ||
        (conditions.computerInput && (sample.idleSeconds == nil || sample.idleSeconds! < 2))
    }

    private func tick() {
        guard !stopped else { return }
        sessionAvailable = session.isAvailable
        accessibilityGranted = AXIsProcessTrusted()
        let sample = activity.sample(displays: displays)
        let time = now
        if let previousTick, time - previousTick > 5 || time < previousTick {
            for id in runtime.keys {
                runtime[id]?.restSince = time
                runtime[id]?.privacySince = time
            }
            previousInput = nil
            dismissHealth()
        }
        previousTick = time
        let keyboardActive = newInput(sample.keyboardIdleSeconds, previous: previousInput?.keyboardIdleSeconds)
        let pointerActive = newInput(sample.pointerIdleSeconds, previous: previousInput?.pointerIdleSeconds)
        previousInput = sample
        for display in displays {
            guard var state = runtime[display.id] else { continue }
            let settings = saved.screens[display.id] ?? ScreenSettings()
            let inputHere = (pointerActive && sample.mouseDisplayID == display.id) ||
                (keyboardActive && (!sample.foregroundKnown || sample.foregroundDisplayID == display.id))
            if inputHere {
                state.restSince = time; state.privacySince = time
                wake(display.id)
            }
            let restDeferred = conditionHolds(settings.rest.conditions, displayID: display.id, sample: sample) || time < state.restSnoozeUntil
            let privacyDeferred = (settings.privacy.quietWhileInUse && conditionHolds(settings.privacy.conditions, displayID: display.id, sample: sample)) || time < state.privacySnoozeUntil
            if restDeferred || state.restWasDeferred { state.restSince = time }
            if privacyDeferred || state.privacyWasDeferred { state.privacySince = time }
            state.restWasDeferred = restDeferred; state.privacyWasDeferred = privacyDeferred
            if !sessionAvailable || sample.idleSeconds == nil {
                state.restSince = time; state.privacySince = time
                runtime[display.id] = state
                continue
            }
            runtime[display.id] = state
            guard eligible(display), !failedOverlayIDs.contains(display.id) else { continue }
            if settings.privacy.enabled && !privacyDeferred && !privacyProtectedIDs.contains(display.id) && time - state.privacySince >= Double(settings.privacy.idleSeconds) {
                _ = presentPrivacy(display)
            }
            if settings.rest.enabled && !restDeferred && !restingIDs.contains(display.id) && time - state.restSince >= Double(settings.rest.idleSeconds) {
                if settings.privacy.enabled && !privacyProtectedIDs.contains(display.id) {
                    if privacyDeferred || !presentPrivacy(display) { continue }
                }
                dismissHealth()
                if overlays.showRest(on: display, onWake: { [weak self] in self?.wake(display.id) }) { restingIDs.insert(display.id) }
                else { failedOverlayIDs.insert(display.id); errorMessage = text("无法显示休息画面。", "The rest overlay could not be displayed.") }
            }
        }
        let host = availableHealthDisplay(sample)
        healthSnapshot = health.tick(now: time, sessionAvailable: sessionAvailable,
            inputIdleSeconds: sample.idleSeconds, screenAvailable: host != nil && !previewingHealth && time >= healthBlockedUntil)
        if previewingHealth {
            if time >= previewDeadline || !sessionAvailable || host == nil { dismissHealth() }
        } else if healthSnapshot.shouldPresent, let display = host {
            if healthPresentation.hostID == nil || previousHealthState != healthSnapshot.state {
                presentHealth(on: display, breaking: healthSnapshot.state == .breaking)
            }
            healthPresentation.update(remaining: Int(healthSnapshot.remainingSeconds.rounded(.up)), breaking: healthSnapshot.state == .breaking)
        } else { healthPresentation.close() }
        previousHealthState = healthSnapshot.state
        if !sessionAvailable { statusText = text("会话暂停中，等待解锁或打开设置窗口。", "Session paused. Unlock or open the settings window to resume.") }
        else if !accessibilityGranted && displays.count > 1 {
            statusText = text("未授权辅助功能：无法确定输入窗口所在屏幕，自动保护将保守延后。", "Accessibility permission is needed to locate the active window. Automatic protection is conservatively deferred.")
        }
    }

    private func newInput(_ current: Double?, previous: Double?) -> Bool {
        guard let current else { return false }
        guard let previous else { return current < 0.6 }
        return current < 0.6 || current < previous
    }

    private func availableHealthDisplay(_ sample: ActivitySample) -> MacDisplay? {
        if let existing = healthPresentation.hostID {
            return displays.first { $0.id == existing && !privacyProtectedIDs.contains($0.id) && !restingIDs.contains($0.id) }
        }
        let target = displays.first { $0.id == sample.mouseDisplayID } ?? displays.first
        guard let target, !privacyProtectedIDs.contains(target.id), !restingIDs.contains(target.id) else { return nil }
        return target
    }

    func previewHealth() {
        guard sessionAvailable, let display = availableHealthDisplay(activity.sample(displays: displays)) else {
            errorMessage = text("当前没有可展示提醒的屏幕。", "There is no available display for a reminder."); return
        }
        previewingHealth = true; previewDeadline = now + 20
        presentHealth(on: display, breaking: false)
    }

    private func presentHealth(on display: MacDisplay, breaking: Bool) {
        healthPresentation.show(on: display, language: saved.language,
            remaining: previewingHealth ? 20 : Int(healthSnapshot.remainingSeconds.rounded(.up)), breaking: breaking,
            preview: previewingHealth,
            onStart: { [weak self] in self?.startBreak() }, onSnooze: { [weak self] in self?.snoozeHealth() },
            onSkip: { [weak self] in self?.skipHealth() })
    }

    func dismissHealth() { previewingHealth = false; healthPresentation.close(); healthBlockedUntil = now + 30 }
    func cancelHealthPreview() { if previewingHealth { dismissHealth() } }
    func startBreak() {
        if previewingHealth { dismissHealth(); return }
        health.startBreak(now: now); healthBlockedUntil = 0; healthPresentation.close(); tick()
    }
    func snoozeHealth() {
        if previewingHealth { dismissHealth(); return }
        health.snooze(now: now); dismissHealth()
    }
    func skipHealth() {
        if previewingHealth { dismissHealth(); return }
        health.skip(now: now); dismissHealth()
    }
    private var healthPaused = false
    func toggleHealthPause() { healthPaused.toggle(); health.setPaused(healthPaused, now: now); dismissHealth() }

    func openWebsite() {
        guard let url = URL(string: "https://screenkeeper.skyrrats.com/"), NSWorkspace.shared.open(url) else {
            errorMessage = text("无法打开官网：https://screenkeeper.skyrrats.com/", "Could not open https://screenkeeper.skyrrats.com/"); return
        }
    }

    func requestAccessibility() {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        accessibilityGranted = AXIsProcessTrustedWithOptions(options)
    }

    func importPrivacyImages() {
        guard let id = selectedScreenID else { return }
        let panel = NSOpenPanel(); panel.allowedContentTypes = [.png, .jpeg, .heic, .tiff]
        panel.allowsMultipleSelection = true; panel.canChooseDirectories = false
        guard panel.runModal() == .OK else { return }
        var settings = draft.screens[id] ?? ScreenSettings()
        guard settings.privacy.imagePaths.count + panel.urls.count <= 12 else {
            errorMessage = text("最多选择 12 张图片。", "Choose up to 12 images."); return
        }
        do {
            let directory = dataDirectory.appendingPathComponent("PrivacyImages", isDirectory: true)
            try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
            var imported: [String] = []
            for url in panel.urls {
                let bytes = try url.resourceValues(forKeys: [.fileSizeKey]).fileSize ?? Int.max
                guard bytes <= 10 * 1024 * 1024,
                    let source = CGImageSourceCreateWithURL(url as CFURL, nil),
                    let properties = CGImageSourceCopyPropertiesAtIndex(source, 0, nil) as? [CFString: Any],
                    let width = properties[kCGImagePropertyPixelWidth] as? Int,
                    let height = properties[kCGImagePropertyPixelHeight] as? Int,
                    width > 0, height > 0, Double(width) * Double(height) <= 40_000_000,
                    let image = CGImageSourceCreateImageAtIndex(source, 0, nil),
                    let data = NSBitmapImageRep(cgImage: image).representation(using: .png, properties: [:]) else {
                    throw NSError(domain: "ScreenKeeper", code: 1, userInfo: [NSLocalizedDescriptionKey: text("图片无法读取或超过大小限制。", "An image is unreadable or exceeds the size limit.")])
                }
                let target = directory.appendingPathComponent(UUID().uuidString + ".png")
                try data.write(to: target, options: .atomic)
                imported.append(target.path)
            }
            settings.privacy.imagePaths += imported; settings.privacy.background = .custom
            draft.screens[id] = settings
        } catch { errorMessage = error.localizedDescription }
    }

    func removePrivacyImage(path: String) {
        guard let id = selectedScreenID else { return }
        var settings = draft.screens[id] ?? ScreenSettings()
        settings.privacy.imagePaths.removeAll { $0 == path }; draft.screens[id] = settings
    }

    func shutdown() {
        guard !stopped else { return }
        stopped = true; timer?.invalidate(); timer = nil
        overlays.closeAll(); healthPresentation.close()
        privacyProtectedIDs.removeAll(); restingIDs.removeAll()
    }
}
