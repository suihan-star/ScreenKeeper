import Foundation

public enum AppLanguage: String, Codable, CaseIterable, Sendable { case chinese, english }
public enum AppTheme: String, Codable, CaseIterable, Sendable { case forest, fog }
public enum PrivacyBackground: String, Codable, CaseIterable, Sendable { case builtIn, solid, custom }

public enum SettingsError: Error, Equatable, LocalizedError {
    case invalidValue(String)
    case unsupportedSchema(Int)
    case unreadableSettings
    case writeFailed

    public var errorDescription: String? {
        switch self {
        case .invalidValue(let field): return "设置值无效：\(field)"
        case .unsupportedSchema: return "此配置来自更新的版本，当前版本不会覆盖它。"
        case .unreadableSettings: return "无法读取已有配置，已保留原文件。"
        case .writeFailed: return "设置保存失败，尚未应用本次修改。"
        }
    }
}

public struct QuietConditions: Codable, Equatable, Sendable {
    public var mouseOnScreen: Bool
    public var foregroundOnScreen: Bool
    public var computerInput: Bool

    public init(mouseOnScreen: Bool = true, foregroundOnScreen: Bool = true, computerInput: Bool = false) {
        self.mouseOnScreen = mouseOnScreen
        self.foregroundOnScreen = foregroundOnScreen
        self.computerInput = computerInput
    }

    public var hasSelection: Bool { mouseOnScreen || foregroundOnScreen || computerInput }

    public func validate() throws {
        guard hasSelection else { throw SettingsError.invalidValue("quietConditions") }
    }
}

public struct RestSettings: Codable, Equatable, Sendable {
    public var enabled: Bool
    public var idleSeconds: Int
    public var conditions: QuietConditions
    public var snoozeMinutes: Int

    public init(enabled: Bool = false, idleSeconds: Int = 300,
                conditions: QuietConditions = QuietConditions(), snoozeMinutes: Int = 15) {
        self.enabled = enabled
        self.idleSeconds = idleSeconds
        self.conditions = conditions
        self.snoozeMinutes = snoozeMinutes
    }

    public func validate() throws {
        try validateRange(idleSeconds, 1...86_400, "rest.idleSeconds")
        try validateRange(snoozeMinutes, 1...1_440, "rest.snoozeMinutes")
        // Wake conditions are optional; input in this display always restarts its idle timer.
    }
}

public struct PrivacySettings: Codable, Equatable, Sendable {
    public var enabled: Bool
    public var idleSeconds: Int
    public var quietWhileInUse: Bool
    public var conditions: QuietConditions
    public var snoozeMinutes: Int
    public var background: PrivacyBackground
    public var imagePaths: [String]
    public var slideshowSeconds: Int

    public init(enabled: Bool = false, idleSeconds: Int = 60, quietWhileInUse: Bool = false,
                conditions: QuietConditions = QuietConditions(), snoozeMinutes: Int = 15,
                background: PrivacyBackground = .builtIn, imagePaths: [String] = [],
                slideshowSeconds: Int = 30) {
        self.enabled = enabled
        self.idleSeconds = idleSeconds
        self.quietWhileInUse = quietWhileInUse
        self.conditions = conditions
        self.snoozeMinutes = snoozeMinutes
        self.background = background
        self.imagePaths = imagePaths
        self.slideshowSeconds = slideshowSeconds
    }

    public func validate() throws {
        try validateRange(idleSeconds, 1...86_400, "privacy.idleSeconds")
        try validateRange(snoozeMinutes, 1...1_440, "privacy.snoozeMinutes")
        if quietWhileInUse { try conditions.validate() }
        guard imagePaths.count <= 12, imagePaths.allSatisfy({ !$0.isEmpty && $0.utf8.count <= 16_384 }) else {
            throw SettingsError.invalidValue("privacy.imagePaths")
        }
        if background == .custom {
            guard !imagePaths.isEmpty else { throw SettingsError.invalidValue("privacy.imagePaths") }
            if imagePaths.count > 1 { try validateRange(slideshowSeconds, 5...3_600, "privacy.slideshowSeconds") }
        }
    }
}

public struct ScreenSettings: Codable, Equatable, Sendable {
    public var rest: RestSettings
    public var privacy: PrivacySettings

    public init(rest: RestSettings = RestSettings(), privacy: PrivacySettings = PrivacySettings()) {
        self.rest = rest
        self.privacy = privacy
    }

    public func validate() throws {
        try rest.validate()
        try privacy.validate()
    }
}

public struct HealthSettings: Codable, Equatable, Sendable {
    public var enabled: Bool
    public var workMinutes: Int
    public var breakMinutes: Int
    public var snoozeMinutes: Int
    public var pauseOnIdle: Bool

    public init(enabled: Bool = false, workMinutes: Int = 45, breakMinutes: Int = 5,
                snoozeMinutes: Int = 5, pauseOnIdle: Bool = true) {
        self.enabled = enabled
        self.workMinutes = workMinutes
        self.breakMinutes = breakMinutes
        self.snoozeMinutes = snoozeMinutes
        self.pauseOnIdle = pauseOnIdle
    }

    public func validate() throws {
        try validateRange(workMinutes, 1...1_440, "health.workMinutes")
        try validateRange(breakMinutes, 1...1_440, "health.breakMinutes")
        try validateRange(snoozeMinutes, 1...1_440, "health.snoozeMinutes")
    }
}

public struct AppSettings: Codable, Equatable, Sendable {
    public var schemaVersion: Int
    public var language: AppLanguage
    public var theme: AppTheme
    public var screens: [String: ScreenSettings]
    public var health: HealthSettings

    public init(schemaVersion: Int = 1, language: AppLanguage = .chinese, theme: AppTheme = .forest,
                screens: [String: ScreenSettings] = [:], health: HealthSettings = HealthSettings()) {
        self.schemaVersion = schemaVersion
        self.language = language
        self.theme = theme
        self.screens = screens
        self.health = health
    }

    public func validate() throws {
        guard schemaVersion == 1 else { throw SettingsError.unsupportedSchema(schemaVersion) }
        for (identity, settings) in screens {
            guard !identity.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty,
                  identity.utf8.count <= 1_024 else { throw SettingsError.invalidValue("screen.identity") }
            try settings.validate()
        }
        try health.validate()
    }
}

private func validateRange(_ value: Int, _ range: ClosedRange<Int>, _ field: String) throws {
    guard range.contains(value) else { throw SettingsError.invalidValue(field) }
}
