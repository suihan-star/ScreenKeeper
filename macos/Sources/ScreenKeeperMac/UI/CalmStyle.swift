import SwiftUI
import ScreenKeeperCore

struct CalmPalette {
    let isDark: Bool
    init(_ theme: AppTheme) { isDark = theme == .forest }
    var background: Color { isDark ? Color(hex: 0x11251E) : Color(hex: 0xEAF0E8) }
    var sidebar: Color { isDark ? Color(hex: 0x0D1D17) : Color(hex: 0xE0E8DE) }
    var surface: Color { isDark ? Color(hex: 0x1A3228) : Color(hex: 0xF5F8F1) }
    var foreground: Color { isDark ? Color(hex: 0xEDF3E7) : Color(hex: 0x193B2B) }
    var muted: Color { isDark ? Color(hex: 0xA9BEB0) : Color(hex: 0x52685A) }
    var accent: Color { isDark ? Color(hex: 0xB4D6A1) : Color(hex: 0x315F43) }
    var onAccent: Color { isDark ? Color(hex: 0x173321) : .white }
    var line: Color { isDark ? Color(hex: 0x395245) : Color(hex: 0xC8D5C5) }
}

extension Color {
    init(hex: UInt32) {
        self.init(red: Double((hex >> 16) & 255) / 255,
                  green: Double((hex >> 8) & 255) / 255,
                  blue: Double(hex & 255) / 255)
    }
}

struct CalmPrimaryButton: ButtonStyle {
    var palette: CalmPalette
    @Environment(\.isEnabled) private var isEnabled
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 13, weight: .semibold))
            .padding(.horizontal, 17).padding(.vertical, 9)
            .foregroundStyle(palette.onAccent)
            .background(palette.accent, in: RoundedRectangle(cornerRadius: 9))
            .opacity(!isEnabled ? 0.4 : configuration.isPressed ? 0.75 : 1)
    }
}

struct CalmDivider: View {
    let palette: CalmPalette
    var body: some View { Rectangle().fill(palette.line).frame(height: 1).padding(.vertical, 8) }
}

struct SettingHeading: View {
    let title: String
    var subtitle: String = ""
    var body: some View {
        VStack(alignment: .leading, spacing: 5) {
            Text(title).font(.system(size: 15, weight: .semibold))
            if !subtitle.isEmpty {
                Text(subtitle).font(.system(size: 12)).foregroundStyle(.secondary).fixedSize(horizontal: false, vertical: true)
            }
        }
    }
}

enum DurationUnit: Int, CaseIterable, Identifiable {
    case seconds = 1, minutes = 60, hours = 3600
    var id: Int { rawValue }
    func title(_ language: AppLanguage) -> String {
        switch self {
        case .seconds: return language == .chinese ? "秒" : "sec"
        case .minutes: return language == .chinese ? "分钟" : "min"
        case .hours: return language == .chinese ? "小时" : "hr"
        }
    }
}

struct DurationEditor: View {
    @Binding var seconds: Int
    let language: AppLanguage
    var units: [DurationUnit] = DurationUnit.allCases
    @State private var unit: DurationUnit = .minutes
    @State private var text = ""
    @State private var initialized = false

    var body: some View {
        VStack(alignment: .leading, spacing: 3) {
            HStack(spacing: 7) {
                TextField("", text: $text)
                    .textFieldStyle(.roundedBorder).frame(width: 70)
                    .accessibilityLabel(language == .chinese ? "时长" : "Duration")
                    .onChange(of: text) { _ in updateValue() }
                Picker(language == .chinese ? "单位" : "Unit", selection: $unit) {
                    ForEach(units) { Text($0.title(language)).tag($0) }
                }.labelsHidden().frame(width: 86)
                    .onChange(of: unit) { _ in updateValue() }
            }
            if seconds <= 0 {
                Text(language == .chinese ? "请输入正整数" : "Enter a positive integer")
                    .font(.caption).foregroundStyle(.orange)
            }
        }
        .onAppear {
            guard !initialized else { return }
            unit = units.reversed().first(where: { seconds > 0 && seconds % $0.rawValue == 0 }) ?? units[0]
            text = String(max(0, seconds) / unit.rawValue)
            initialized = true
        }
        .onChange(of: seconds) { value in
            guard value > 0, value != computedValue else { return }
            let nextUnit = units.reversed().first(where: { value % $0.rawValue == 0 }) ?? units[0]
            unit = nextUnit
            text = String(value / nextUnit.rawValue)
        }
    }

    private var computedValue: Int {
        guard let number = Int(text), number > 0 else { return -1 }
        let value = number.multipliedReportingOverflow(by: unit.rawValue)
        return value.overflow ? -1 : value.partialValue
    }

    private func updateValue() {
        guard initialized else { return }
        seconds = computedValue
    }
}
