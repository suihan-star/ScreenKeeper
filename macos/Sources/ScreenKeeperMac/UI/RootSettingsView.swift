import AppKit
import SwiftUI
import ScreenKeeperCore

@MainActor
struct RootSettingsView: View {
    @EnvironmentObject private var model: AppModel
    @State private var colorTab = 0
    @State private var showCopy = false
    @State private var copyTargets: Set<String> = []
    @State private var showSnooze = false
    @State private var snoozeSeconds = 900

    private var palette: CalmPalette { CalmPalette(model.draft.theme) }
    private var language: AppLanguage { model.draft.language }
    private var selectedDisplay: MacDisplay? { model.displays.first { $0.id == model.selectedScreenID } }
    private static let brandImage: NSImage? = AppResources.url("icon", extension: "svg")
        .flatMap { NSImage(contentsOf: $0) }
    private var isScreenModule: Bool { model.selectedModule == .rest || model.selectedModule == .privacy }
    private var selectedScreenCanOperate: Bool {
        guard let display = selectedDisplay else { return false }
        return display.isIdentityReliable && !display.isMirrored && model.sessionAvailable
    }
    private func t(_ chinese: String, _ english: String) -> String { model.text(chinese, english) }

    var body: some View {
        VStack(spacing: 0) {
            titleBar
            Rectangle().fill(palette.line).frame(height: 1)
            HStack(spacing: 0) {
                sidebar
                VStack(alignment: .leading, spacing: 0) {
                    moduleHeader
                    if isScreenModule { screenPicker }
                    if let error = model.errorMessage { errorBanner(error) }
                    if isScreenModule, selectedDisplay != nil { runtimeCard }
                    ScrollView {
                        VStack(alignment: .leading, spacing: 22) { moduleContent }
                            .frame(maxWidth: 820, alignment: .leading)
                            .frame(maxWidth: .infinity, alignment: .leading).padding(24)
                    }
                    footer
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
        }
        .foregroundStyle(palette.foreground).background(palette.background)
        .tint(palette.accent).preferredColorScheme(palette.isDark ? .dark : .light)
        .frame(minWidth: 700, minHeight: 500)
        .sheet(isPresented: $showCopy) { copySheet }
        .sheet(isPresented: $showSnooze) { snoozeSheet }
        .onChange(of: model.selectedModule) { _ in model.cancelHealthPreview() }
        .onChange(of: model.selectedScreenID) { _ in model.cancelHealthPreview() }
    }

    private var titleBar: some View {
        HStack(spacing: 13) {
            Group {
                if let image = Self.brandImage {
                    Image(nsImage: image).renderingMode(.template).resizable().scaledToFit()
                } else {
                    Image(systemName: "display").resizable().scaledToFit()
                }
            }.frame(width: 24, height: 24).foregroundStyle(palette.accent).accessibilityHidden(true)
            ViewThatFits(in: .horizontal) {
                HStack(alignment: .firstTextBaseline, spacing: 14) {
                    Text(t("屏幕良伴", "ScreenKeeper")).font(.system(size: 20, weight: .semibold))
                    Text(t("照顾屏幕，也照顾自己", "Care for your screen. Care for yourself."))
                        .font(.system(size: 11)).foregroundStyle(palette.muted)
                }
                Text(t("屏幕良伴", "ScreenKeeper")).font(.system(size: 20, weight: .semibold))
            }
            Spacer(minLength: 8)
            Menu {
                Button(t("静绿深色", "Forest")) { model.updateTheme(.forest) }
                Button(t("雾白绿", "Fog")) { model.updateTheme(.fog) }
            } label: { Image(systemName: palette.isDark ? "moon" : "sun.max") }
                .menuStyle(.borderlessButton).frame(width: 30)
                .help(t("外观主题", "Appearance"))
                .accessibilityLabel(t("外观主题", "Appearance"))
            Picker(t("语言", "Language"), selection: Binding(get: { language }, set: { model.updateLanguage($0) })) {
                Text("中文").tag(AppLanguage.chinese)
                Text("English").tag(AppLanguage.english)
            }.labelsHidden().frame(width: 92)
            Button(action: model.openWebsite) { Image(systemName: "arrow.up.right.square") }
                .buttonStyle(.plain).help(t("打开官网", "Open website"))
                .accessibilityLabel(t("打开官网", "Open website"))
        }.padding(.horizontal, 22).padding(.vertical, 17)
    }

    private var sidebar: some View {
        VStack(alignment: .leading, spacing: 8) {
            navigationButton(.rest, "display", "屏幕休息", "Screen rest")
            navigationButton(.privacy, "lock.shield", "隐私保护", "Privacy")
            navigationButton(.health, "leaf", "健康提醒", "Wellbeing")
            navigationButton(.color, "sun.max", "色温调节", "Color temperature")
            Spacer()
            navigationButton(.general, "slider.horizontal.3", "通用设置", "General")
            Text(t("Mac 开发预览", "Mac development preview"))
                .font(.system(size: 10)).foregroundStyle(palette.muted)
                .padding(.horizontal, 10).padding(.top, 12)
        }.padding(12).frame(width: 160).frame(maxHeight: .infinity).background(palette.sidebar)
    }

    private func navigationButton(_ module: SettingsModule, _ icon: String, _ chinese: String, _ english: String) -> some View {
        Button { model.selectedModule = module } label: {
            HStack(spacing: 10) {
                Image(systemName: icon).frame(width: 18)
                Text(t(chinese, english)).font(.system(size: 12, weight: model.selectedModule == module ? .semibold : .regular))
                    .fixedSize(horizontal: false, vertical: true)
                Spacer(minLength: 0)
            }
            .padding(.horizontal, 11).padding(.vertical, 12).frame(maxWidth: .infinity)
            .foregroundStyle(model.selectedModule == module ? palette.onAccent : palette.foreground)
            .background(model.selectedModule == module ? palette.accent : .clear, in: RoundedRectangle(cornerRadius: 10))
            .contentShape(Rectangle())
        }.buttonStyle(.plain)
        .accessibilityAddTraits(model.selectedModule == module ? [.isSelected] : [])
    }

    private var moduleHeader: some View {
        HStack {
            Text(moduleTitle).font(.system(size: 24, weight: .semibold))
            Spacer()
            if isScreenModule || model.selectedModule == .color {
                Button(action: model.refreshDisplays) { Label(t("刷新", "Refresh"), systemImage: "arrow.clockwise") }
                    .buttonStyle(.borderless).disabled(model.hasAnyDraft)
                    .help(t("请先保存或放弃当前更改", "Save or discard current changes before refreshing"))
            }
        }.padding(.horizontal, 24).padding(.top, 23).padding(.bottom, 14)
    }

    private var moduleTitle: String {
        switch model.selectedModule {
        case .rest: return t("屏幕休息", "Screen rest")
        case .privacy: return t("隐私保护", "Privacy")
        case .health: return t("健康提醒", "Wellbeing")
        case .color: return t("色温调节", "Color temperature")
        case .general: return t("通用设置", "General")
        }
    }

    private var screenPicker: some View {
        VStack(alignment: .leading, spacing: 8) {
            Picker(t("选择屏幕", "Display"), selection: $model.selectedScreenID) {
                if model.displays.isEmpty { Text(t("未发现可用屏幕", "No displays available")).tag(String?.none) }
                ForEach(model.displays) { display in
                    Text(display.title).tag(Optional(display.id))
                }
            }.pickerStyle(.menu).frame(maxWidth: 400, alignment: .leading)
            if let display = selectedDisplay, display.isMirrored || !display.isIdentityReliable {
                Text(t("此屏处于镜像模式或身份尚不明确，暂不执行独立屏幕操作。",
                       "This display is mirrored or cannot be identified reliably. Independent screen actions are unavailable."))
                    .font(.caption).foregroundStyle(.orange).fixedSize(horizontal: false, vertical: true)
            }
        }.padding(.horizontal, 24).padding(.bottom, 16)
    }

    private func errorBanner(_ message: String) -> some View {
        Label(message, systemImage: "exclamationmark.circle")
            .font(.system(size: 12)).foregroundStyle(.orange)
            .fixedSize(horizontal: false, vertical: true)
            .padding(12).frame(maxWidth: .infinity, alignment: .leading)
            .background(Color.orange.opacity(0.08), in: RoundedRectangle(cornerRadius: 8))
            .padding(.horizontal, 24).padding(.bottom, 12)
            .accessibilityLabel(message)
    }

    private var runtimeCard: some View {
        VStack(alignment: .leading, spacing: 10) {
            Label(runtimeTitle, systemImage: model.selectedModule == .privacy ? "shield" : "display")
                .font(.system(size: 13, weight: .semibold))
            if !model.statusText.isEmpty {
                Text(model.statusText).font(.system(size: 12)).foregroundStyle(palette.muted)
                    .fixedSize(horizontal: false, vertical: true)
            }
            HStack(spacing: 14) {
                Button(t("暂缓…", "Snooze…")) {
                    let profile = model.saved.screens[model.selectedScreenID ?? ""] ?? ScreenSettings()
                    snoozeSeconds = (model.selectedModule == .privacy ? profile.privacy.snoozeMinutes : profile.rest.snoozeMinutes) * 60
                    showSnooze = true
                }.disabled(!savedScreenFeatureEnabled)
                Button(t("恢复计时", "Resume timer"), action: model.resumeSelected)
                Spacer(minLength: 0)
                if model.selectedModule == .privacy {
                    Button(t("本屏立即保护", "Protect now"), action: model.protectNow)
                        .disabled(!selectedScreenCanOperate || !savedScreenFeatureEnabled)
                } else {
                    Button(t("唤醒此屏幕", "Wake screen"), action: model.wakeSelected)
                        .disabled(!model.restingIDs.contains(model.selectedScreenID ?? ""))
                }
            }.buttonStyle(.borderless).font(.system(size: 12))
        }.padding(15).background(palette.surface, in: RoundedRectangle(cornerRadius: 12))
            .padding(.horizontal, 24).padding(.bottom, 2)
    }

    private var savedScreenFeatureEnabled: Bool {
        let profile = model.saved.screens[model.selectedScreenID ?? ""] ?? ScreenSettings()
        return model.selectedModule == .privacy ? profile.privacy.enabled : profile.rest.enabled
    }

    private var runtimeTitle: String {
        guard model.sessionAvailable else { return t("会话暂停中", "Session paused") }
        let id = model.selectedScreenID ?? ""
        if model.selectedModule == .privacy, model.privacyProtectedIDs.contains(id) { return t("此屏已进入隐私保护", "Privacy curtain is active") }
        if model.selectedModule == .rest, model.restingIDs.contains(id) { return t("此屏正在休息", "Screen rest is active") }
        return savedScreenFeatureEnabled ? t("按已保存设置运行", "Using saved settings") : t("此功能尚未开启", "This feature is off")
    }

    @ViewBuilder private var moduleContent: some View {
        switch model.selectedModule {
        case .rest:
            if selectedDisplay != nil { restContent.id(model.selectedScreenID) } else { noScreens }
        case .privacy:
            if selectedDisplay != nil { privacyContent.id(model.selectedScreenID) } else { noScreens }
        case .health: healthContent
        case .color: colorContent
        case .general: generalContent
        }
    }

    private var noScreens: some View {
        VStack(alignment: .leading, spacing: 12) {
            Image(systemName: "display.trianglebadge.exclamationmark").font(.largeTitle)
            SettingHeading(title: t("等待显示器连接", "Waiting for a display"),
                           subtitle: t("连接后会自动出现在这里，原有设置会保留。", "Connected displays appear automatically. Saved settings are retained."))
        }.padding(.vertical, 30).foregroundStyle(palette.muted)
    }

    private var restContent: some View {
        VStack(alignment: .leading, spacing: 22) {
            Toggle(t("开启此屏幕休息", "Enable screen rest"), isOn: restBinding(\.enabled)).toggleStyle(.switch)
            if restBinding(\.enabled).wrappedValue {
                HStack(alignment: .top) {
                    SettingHeading(title: t("空闲多久后休息", "Rest after inactivity"))
                    Spacer(minLength: 12)
                    DurationEditor(seconds: restBinding(\.idleSeconds), language: language)
                }
                CalmDivider(palette: palette)
                SettingHeading(title: t("保持唤醒", "Keep awake"), subtitle: t("满足以下任一条件时，暂不进入屏幕休息。", "Keep this screen awake while any selected condition is met."))
                conditionsEditor(restBinding(\.conditions))
                CalmDivider(palette: palette)
                SettingHeading(title: t("休息方式", "Rest appearance"), subtitle: t("软件遮黑。普通输入可唤醒，不会关闭已启用的隐私幕。", "Software blackout. Input wakes this screen without dismissing its privacy curtain."))
                HStack {
                    Text(t("默认暂缓时长", "Default snooze"))
                    Spacer()
                    DurationEditor(seconds: minutesAsSeconds(restBinding(\.snoozeMinutes)), language: language, units: [.minutes, .hours])
                }
            }
            CalmDivider(palette: palette)
            copyButton
        }
    }

    private var privacyContent: some View {
        VStack(alignment: .leading, spacing: 22) {
            Toggle(t("开启此屏幕隐私保护", "Enable privacy protection"), isOn: privacyBinding(\.enabled)).toggleStyle(.switch)
            if privacyBinding(\.enabled).wrappedValue {
                HStack(alignment: .top) {
                    SettingHeading(title: t("何时保护", "When to protect"), subtitle: t("本屏无操作多久后保护", "Protect after this screen is inactive"))
                    Spacer(minLength: 12)
                    DurationEditor(seconds: privacyBinding(\.idleSeconds), language: language)
                }
                Toggle(t("使用时不打扰", "Do not interrupt while in use"), isOn: privacyBinding(\.quietWhileInUse)).toggleStyle(.switch)
                if privacyBinding(\.quietWhileInUse).wrappedValue {
                    Text(t("满足以下任一条件时，暂不进入隐私保护。", "Defer privacy protection while any selected condition is met."))
                        .font(.caption).foregroundStyle(palette.muted)
                    conditionsEditor(privacyBinding(\.conditions)).padding(.leading, 12)
                }
                CalmDivider(palette: palette)
                SettingHeading(title: t("如何保护", "How to protect"), subtitle: t("隐私幕 · 通过“进入此屏幕”明确解除。", "Privacy curtain · Select “Enter this screen” to dismiss it."))
                privacyAppearance
                Text(t("回到桌面与指定应用将在 Mac 窗口控制验证完成后开放。", "Desktop and selected-app actions will follow Mac window-control validation."))
                    .font(.caption).foregroundStyle(palette.muted).fixedSize(horizontal: false, vertical: true)
                CalmDivider(palette: palette)
                HStack {
                    Text(t("默认暂缓时长", "Default snooze"))
                    Spacer()
                    DurationEditor(seconds: minutesAsSeconds(privacyBinding(\.snoozeMinutes)), language: language, units: [.minutes, .hours])
                }
            }
            CalmDivider(palette: palette)
            copyButton
        }
    }

    private var privacyAppearance: some View {
        VStack(alignment: .leading, spacing: 12) {
            Picker(t("隐私幕外观", "Curtain appearance"), selection: privacyBinding(\.background)) {
                Text(t("山林薄雾", "Forest mist")).tag(PrivacyBackground.builtIn)
                Text(t("纯色", "Solid color")).tag(PrivacyBackground.solid)
                Text(t("我的图片", "My pictures")).tag(PrivacyBackground.custom)
            }.pickerStyle(.segmented)
            if privacyBinding(\.background).wrappedValue == .custom {
                let paths = privacyBinding(\.imagePaths).wrappedValue
                ForEach(paths, id: \.self) { path in
                    HStack {
                        Image(systemName: "photo")
                        Text(URL(fileURLWithPath: path).lastPathComponent).lineLimit(1).truncationMode(.middle)
                        Spacer()
                        Button(t("移除", "Remove")) { model.removePrivacyImage(path: path) }.buttonStyle(.borderless)
                    }.font(.system(size: 12))
                }
                Button(action: model.importPrivacyImages) { Label(t("添加图片…", "Add pictures…"), systemImage: "plus") }
                if paths.count > 1 {
                    HStack {
                        Text(t("轮播间隔", "Slideshow interval"))
                        Spacer()
                        DurationEditor(seconds: privacyBinding(\.slideshowSeconds), language: language, units: [.seconds, .minutes])
                    }
                } else if paths.isEmpty {
                    Text(t("请选择图片；读取失败时保持不透明纯色保护。", "Choose a picture. An unreadable image falls back to an opaque solid curtain."))
                        .font(.caption).foregroundStyle(palette.muted)
                }
            } else {
                HStack(spacing: 12) {
                    Image(systemName: privacyBinding(\.background).wrappedValue == .builtIn ? "mountain.2" : "rectangle.fill")
                        .font(.system(size: 28)).foregroundStyle(palette.accent).frame(width: 64, height: 48)
                    Text(t("幕布仅展示“进入此屏幕”按钮。", "The curtain shows only the “Enter this screen” button."))
                        .font(.system(size: 12)).foregroundStyle(palette.muted)
                }.padding(12).frame(maxWidth: .infinity, alignment: .leading)
                    .background(palette.surface, in: RoundedRectangle(cornerRadius: 10))
            }
        }
    }

    private func conditionsEditor(_ conditions: Binding<QuietConditions>) -> some View {
        VStack(alignment: .leading, spacing: 11) {
            Toggle(t("鼠标停留在本屏", "Pointer is on this screen"), isOn: conditions.mouseOnScreen)
            Toggle(t("前台窗口在本屏", "Frontmost window is on this screen"), isOn: conditions.foregroundOnScreen)
            Toggle(t("电脑有键盘或鼠标输入", "The computer has keyboard or mouse input"), isOn: conditions.computerInput)
            if conditions.foregroundOnScreen.wrappedValue && !model.accessibilityGranted {
                HStack(alignment: .top) {
                    Text(t("判断前台窗口需要辅助功能权限。", "Frontmost-window detection requires Accessibility permission."))
                        .font(.caption).fixedSize(horizontal: false, vertical: true)
                    Button(t("去授权", "Grant access"), action: model.requestAccessibility).buttonStyle(.borderless)
                }.foregroundStyle(palette.muted)
            }
        }.toggleStyle(.checkbox).font(.system(size: 13))
    }

    private var healthContent: some View {
        VStack(alignment: .leading, spacing: 22) {
            Toggle(t("开启健康提醒", "Enable wellbeing reminders"), isOn: healthBinding(\.enabled)).toggleStyle(.switch)
            if healthBinding(\.enabled).wrappedValue {
                HStack {
                    Text(t("工作多久后提醒", "Remind me after working for"))
                    Spacer()
                    DurationEditor(seconds: minutesAsSeconds(healthBinding(\.workMinutes)), language: language, units: [.minutes, .hours])
                }
                HStack {
                    Text(t("每次休息", "Break duration"))
                    Spacer()
                    DurationEditor(seconds: minutesAsSeconds(healthBinding(\.breakMinutes)), language: language, units: [.minutes, .hours])
                }
                CalmDivider(palette: palette)
                SettingHeading(title: t("减少打扰", "Fewer interruptions"))
                Toggle(t("无输入 5 分钟后暂停工作计时", "Pause the work timer after 5 minutes without input"), isOn: healthBinding(\.pauseOnIdle))
                Text(t("锁屏或系统休眠时暂停提醒，恢复后继续；健康提醒使用独立的全局周期。", "Reminders pause while the session is inactive. Wellbeing uses its own global work cycle."))
                    .font(.caption).foregroundStyle(palette.muted).fixedSize(horizontal: false, vertical: true)
                CalmDivider(palette: palette)
                HStack {
                    Text(t("稍后提醒的间隔", "Remind me later after"))
                    Spacer()
                    DurationEditor(seconds: minutesAsSeconds(healthBinding(\.snoozeMinutes)), language: language, units: [.minutes, .hours])
                }
            }
            CalmDivider(palette: palette)
            HStack(spacing: 14) {
                Button(t("预览提醒", "Preview reminder"), action: model.previewHealth)
                Button(t("暂停／继续计时", "Pause / resume timer"), action: model.toggleHealthPause)
                    .disabled(!model.saved.health.enabled)
            }
            Text(t("预览不会推进工作计时。提醒卡片在屏幕中央展示，休息由你主动开始。", "Preview does not advance the timer. The card appears in the center; you choose when to start a break."))
                .font(.caption).foregroundStyle(palette.muted).fixedSize(horizontal: false, vertical: true)
        }
    }

    private var colorContent: some View {
        VStack(alignment: .leading, spacing: 22) {
            Picker(t("色温设置", "Color settings"), selection: $colorTab) {
                Text(t("作息与色温", "Schedule & color")).tag(0)
                Text(t("屏幕与验证", "Displays & validation")).tag(1)
            }.pickerStyle(.segmented)
            SettingHeading(title: t("此开发预览尚未开放色温接管", "Color adjustment is not enabled in this preview"),
                           subtitle: t("需要先在 Mac 实机验证逐屏调节与原色恢复。其他功能可独立使用。", "Per-display adjustment and original-color restoration need Mac hardware validation first. Other features remain available."))
            if colorTab == 0 {
                Label(t("验证通过后，在这里设置各屏幕的工作与休息色温。", "After validation, set work and rest colors for each display here."), systemImage: "sun.horizon")
                    .font(.system(size: 13)).foregroundStyle(palette.muted)
            } else if model.displays.isEmpty {
                noScreens
            } else {
                ForEach(model.displays) { display in
                    HStack(alignment: .top, spacing: 12) {
                        Image(systemName: display.isBuiltIn ? "laptopcomputer" : "display").font(.title2)
                        VStack(alignment: .leading, spacing: 5) {
                            Text(display.title).fontWeight(.medium)
                            Text(t("尚未验证 · 不写入此屏颜色", "Not validated · No color changes applied"))
                                .font(.caption).foregroundStyle(palette.muted)
                        }
                        Spacer()
                    }.padding(16).background(palette.surface, in: RoundedRectangle(cornerRadius: 10))
                }
            }
        }
    }

    private var generalContent: some View {
        VStack(alignment: .leading, spacing: 22) {
            SettingHeading(title: t("外观主题", "Appearance"), subtitle: t("更改主题立即生效，保留各模块尚未保存的设置。", "Theme changes immediately without changing unsaved settings."))
            HStack(spacing: 12) {
                themeButton(.forest, t("静绿深色", "Forest"))
                themeButton(.fog, t("雾白绿", "Fog"))
            }
            CalmDivider(palette: palette)
            SettingHeading(title: t("权限与能力", "Permissions & capabilities"))
            HStack {
                Text(t("辅助功能", "Accessibility"))
                Spacer()
                Text(model.accessibilityGranted ? t("已授权", "Granted") : t("未授权", "Not granted"))
                    .foregroundStyle(palette.muted)
                Button(t("检查／授权", "Review access"), action: model.requestAccessibility)
            }
            Text(t("仅用于判断前台窗口归属，不读取或记录输入内容。未开放的硬件调色与窗口控制不会执行。", "Used to identify the frontmost window’s screen, without recording typed content. Unavailable hardware and window-control actions are not performed."))
                .font(.caption).foregroundStyle(palette.muted).fixedSize(horizontal: false, vertical: true)
            CalmDivider(palette: palette)
            SettingHeading(title: "ScreenKeeper for Mac", subtitle: t("Apple Silicon · macOS 13 及以上 · 开发预览", "Apple Silicon · macOS 13 or later · Development preview"))
            Button(action: model.openWebsite) { Label(t("访问官网", "Visit website"), systemImage: "arrow.up.right") }
            Text(t("关闭设置窗口后，软件继续在菜单栏运行。完全退出请使用菜单栏的退出入口。", "Closing settings keeps ScreenKeeper running in the menu bar. Use its Quit action to exit completely."))
                .font(.caption).foregroundStyle(palette.muted).fixedSize(horizontal: false, vertical: true)
        }
    }

    private func themeButton(_ theme: AppTheme, _ title: String) -> some View {
        let preview = CalmPalette(theme)
        return Button { model.updateTheme(theme) } label: {
            HStack {
                Image(systemName: theme == .forest ? "moon" : "sun.max")
                Text(title)
                Spacer()
                if model.draft.theme == theme { Image(systemName: "checkmark.circle.fill") }
            }.font(.system(size: 13, weight: .medium)).padding(17)
                .foregroundStyle(preview.foreground).background(preview.background, in: RoundedRectangle(cornerRadius: 12))
                .overlay(RoundedRectangle(cornerRadius: 12).stroke(model.draft.theme == theme ? palette.accent : palette.line, lineWidth: 2))
        }.buttonStyle(.plain)
    }

    private var footer: some View {
        VStack(spacing: 0) {
            Rectangle().fill(palette.line).frame(height: 1)
            HStack(spacing: 12) {
                Text(model.errorMessage != nil ? t("请检查上方提示", "Review the message above") : model.isCurrentModuleDirty ? t("当前模块有未保存更改", "Unsaved changes in this module") : t("设置已保存", "Settings saved"))
                    .font(.system(size: 11)).foregroundStyle(palette.muted)
                Spacer(minLength: 5)
                if model.selectedModule != .general && model.selectedModule != .color {
                    Button(t("放弃更改", "Discard"), action: model.discardCurrentModule)
                        .disabled(!model.isCurrentModuleDirty)
                    Button(t("保存", "Save"), action: model.saveCurrentModule)
                        .buttonStyle(CalmPrimaryButton(palette: palette))
                        .keyboardShortcut("s", modifiers: .command)
                        .disabled(!model.isCurrentModuleDirty)
                }
            }.padding(.horizontal, 24).padding(.vertical, 14)
        }
    }

    private var copyButton: some View {
        Button {
            copyTargets = []
            showCopy = true
        } label: { Label(t("应用到其他屏幕", "Apply to other displays"), systemImage: "arrow.up.right") }
            .buttonStyle(.borderless).foregroundStyle(palette.accent)
            .disabled(selectedDisplay == nil || model.displays.count < 2)
    }

    private var copySheet: some View {
        VStack(alignment: .leading, spacing: 18) {
            Text(t("应用到其他屏幕", "Apply to other displays")).font(.title2.bold())
            Text(t("复制当前模块的参数，保留目标屏幕的启用状态。保存后才会生效。", "Copy this module’s settings while retaining each target’s enabled state. Save to apply."))
                .font(.callout).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
            ScrollView {
                VStack(alignment: .leading, spacing: 12) {
                    ForEach(model.displays.filter { $0.id != model.selectedScreenID }) { display in
                        Toggle(display.title, isOn: Binding(get: { copyTargets.contains(display.id) }, set: { checked in
                            if checked { copyTargets.insert(display.id) } else { copyTargets.remove(display.id) }
                        })).disabled(!display.isIdentityReliable || display.isMirrored)
                    }
                }.frame(maxWidth: .infinity, alignment: .leading)
            }.frame(maxHeight: 200)
            HStack {
                Button(t("选择全部", "Select all")) {
                    copyTargets = Set(model.displays.filter { $0.id != model.selectedScreenID && $0.isIdentityReliable && !$0.isMirrored }.map(\.id))
                }
                Spacer()
                Button(t("取消", "Cancel")) { showCopy = false }.keyboardShortcut(.cancelAction)
                Button(t("应用", "Apply")) { model.copyCurrentScreen(to: copyTargets); showCopy = false }
                    .buttonStyle(CalmPrimaryButton(palette: palette)).disabled(copyTargets.isEmpty)
            }
        }.padding(26).frame(width: 420).background(palette.background).foregroundStyle(palette.foreground)
    }

    private var snoozeSheet: some View {
        VStack(alignment: .leading, spacing: 20) {
            Text(model.selectedModule == .privacy ? t("暂缓隐私保护", "Snooze privacy") : t("暂缓屏幕休息", "Snooze screen rest"))
                .font(.title2.bold())
            DurationEditor(seconds: $snoozeSeconds, language: language, units: [.minutes, .hours])
            Text(t("仅影响本次计时，不会唤醒屏幕或解除已经显示的隐私幕。", "Affects this session only. It does not wake the screen or dismiss an existing curtain."))
                .font(.caption).foregroundStyle(palette.muted).fixedSize(horizontal: false, vertical: true)
            HStack {
                Spacer()
                Button(t("取消", "Cancel")) { showSnooze = false }.keyboardShortcut(.cancelAction)
                Button(t("开始暂缓", "Snooze")) { model.snoozeSelected(minutes: snoozeSeconds / 60); showSnooze = false }
                    .buttonStyle(CalmPrimaryButton(palette: palette)).disabled(snoozeSeconds < 60 || snoozeSeconds > 86400)
            }
        }.padding(26).frame(width: 390).background(palette.background).foregroundStyle(palette.foreground)
    }

    private func restBinding<Value>(_ path: WritableKeyPath<RestSettings, Value>) -> Binding<Value> {
        Binding(get: { (model.draft.screens[model.selectedScreenID ?? ""] ?? ScreenSettings()).rest[keyPath: path] }, set: { value in
            guard let id = model.selectedScreenID else { return }
            var screen = model.draft.screens[id] ?? ScreenSettings()
            screen.rest[keyPath: path] = value
            model.draft.screens[id] = screen
        })
    }

    private func privacyBinding<Value>(_ path: WritableKeyPath<PrivacySettings, Value>) -> Binding<Value> {
        Binding(get: { (model.draft.screens[model.selectedScreenID ?? ""] ?? ScreenSettings()).privacy[keyPath: path] }, set: { value in
            guard let id = model.selectedScreenID else { return }
            var screen = model.draft.screens[id] ?? ScreenSettings()
            screen.privacy[keyPath: path] = value
            model.draft.screens[id] = screen
        })
    }

    private func healthBinding<Value>(_ path: WritableKeyPath<HealthSettings, Value>) -> Binding<Value> {
        Binding(get: { model.draft.health[keyPath: path] }, set: { model.draft.health[keyPath: path] = $0 })
    }

    private func minutesAsSeconds(_ minutes: Binding<Int>) -> Binding<Int> {
        Binding(get: { minutes.wrappedValue * 60 }, set: { minutes.wrappedValue = $0 > 0 ? $0 / 60 : -1 })
    }
}
