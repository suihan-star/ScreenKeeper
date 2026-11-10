import SwiftUI
import ScreenKeeperCore

struct HealthCardView: View {
    let language: AppLanguage
    let remainingSeconds: Int
    let isBreaking: Bool
    let onStart: () -> Void
    let onSnooze: () -> Void
    let onSkip: () -> Void
    private let palette = CalmPalette(.forest)

    private func text(_ chinese: String, _ english: String) -> String {
        language == .chinese ? chinese : english
    }

    var body: some View {
        VStack(spacing: 22) {
            Image(systemName: isBreaking ? "leaf" : "sun.haze")
                .font(.system(size: 32, weight: .light)).foregroundStyle(palette.accent)
            Text(text(isBreaking ? "给自己一点休息时间" : "该休息一下了",
                      isBreaking ? "A moment for yourself" : "Time for a little break"))
                .font(.system(size: 23, weight: .semibold))
                .multilineTextAlignment(.center).frame(maxWidth: .infinity)
            Text(text("专注了这么久，辛苦啦。眨眨眼，伸个懒腰，让紧绷的自己放松片刻...",
                      "You’ve been focused for a while. Blink, stretch, and give yourself a moment to unwind…"))
                .font(.system(size: 15)).lineSpacing(6)
                .multilineTextAlignment(.center).foregroundStyle(palette.muted)
                .fixedSize(horizontal: false, vertical: true)
            if isBreaking {
                Text(String(format: "%02d:%02d", max(0, remainingSeconds) / 60, max(0, remainingSeconds) % 60))
                    .font(.system(size: 40, weight: .light, design: .rounded)).monospacedDigit()
                    .accessibilityLabel(text("剩余休息时间", "Break time remaining"))
                Button(action: onSkip) {
                    Text(text("结束休息", "Finish break")).frame(maxWidth: .infinity)
                }.buttonStyle(CalmPrimaryButton(palette: palette))
            } else {
                Button(action: onStart) {
                    Label(text("开始休息", "Start break"), systemImage: "leaf")
                        .frame(maxWidth: .infinity).padding(.vertical, 5)
                }.buttonStyle(CalmPrimaryButton(palette: palette))
                HStack(spacing: 24) {
                    Button(action: onSnooze) {
                        HStack(spacing: 9) {
                            Image(systemName: "pause.fill").font(.system(size: 15, weight: .semibold))
                                .frame(width: 34, height: 34)
                                .background(palette.line, in: RoundedRectangle(cornerRadius: 6))
                            Text(text("稍后", "Later"))
                        }.frame(maxWidth: .infinity, minHeight: 42).contentShape(Rectangle())
                    }.buttonStyle(HealthSecondaryButtonStyle())
                    Button(action: onSkip) {
                        HStack(spacing: 8) {
                            Image(systemName: "arrow.right").font(.system(size: 21, weight: .medium))
                                .frame(width: 34, height: 34)
                            Text(text("跳过本次", "Skip this time"))
                        }.frame(maxWidth: .infinity, minHeight: 42).contentShape(Rectangle())
                    }.buttonStyle(HealthSecondaryButtonStyle()).foregroundStyle(palette.muted)
                }.font(.system(size: 13, weight: .medium))
            }
        }
        .padding(30).frame(width: 430)
        .foregroundStyle(palette.foreground)
        .background(palette.background, in: RoundedRectangle(cornerRadius: 24))
        .overlay(RoundedRectangle(cornerRadius: 24).stroke(palette.line, lineWidth: 1))
        .preferredColorScheme(.dark)
    }
}

private struct HealthSecondaryButtonStyle: ButtonStyle {
    @State private var hovered = false
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .padding(.horizontal, 4)
            .background(Color.white.opacity(configuration.isPressed ? 0.14 : hovered ? 0.07 : 0),
                        in: RoundedRectangle(cornerRadius: 8))
            .onHover { hovered = $0 }
    }
}
