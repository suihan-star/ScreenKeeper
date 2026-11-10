import Foundation

/// Candidate schedule only: this type grants no permission to modify a display.
public struct ColorProfile: Codable, Equatable, Sendable {
    public var enabled: Bool
    public var workStartMinute: Int
    public var restStartMinute: Int
    public var workUsesOriginal: Bool
    public var restUsesOriginal: Bool
    public var workKelvin: Int
    public var restKelvin: Int
    public var transitionMinutes: Int

    public init(enabled: Bool = false, workStartMinute: Int = 540, restStartMinute: Int = 1_080,
                workUsesOriginal: Bool = true, restUsesOriginal: Bool = false,
                workKelvin: Int = 6_500, restKelvin: Int = 4_500, transitionMinutes: Int = 20) {
        self.enabled = enabled
        self.workStartMinute = workStartMinute
        self.restStartMinute = restStartMinute
        self.workUsesOriginal = workUsesOriginal
        self.restUsesOriginal = restUsesOriginal
        self.workKelvin = workKelvin
        self.restKelvin = restKelvin
        self.transitionMinutes = transitionMinutes
    }

    public func validate() throws {
        guard (0..<1_440).contains(workStartMinute), (0..<1_440).contains(restStartMinute),
              workStartMinute != restStartMinute else { throw SettingsError.invalidValue("color.schedule") }
        let restLength = (workStartMinute - restStartMinute + 1_440) % 1_440
        guard (0...120).contains(transitionMinutes),
              transitionMinutes <= min(restLength, 1_440 - restLength) else {
            throw SettingsError.invalidValue("color.transitionMinutes")
        }
        if !workUsesOriginal && !(3_500...6_500).contains(workKelvin) {
            throw SettingsError.invalidValue("color.workKelvin")
        }
        if !restUsesOriginal && !(3_500...6_500).contains(restKelvin) {
            throw SettingsError.invalidValue("color.restKelvin")
        }
    }
}

public enum ColorSchedulePhase: String, Equatable, Sendable { case disabled, work, warming, rest, cooling }

public struct ColorScheduleTarget: Equatable, Sendable {
    public var phase: ColorSchedulePhase
    /// 0 means the captured original, not a synthetic 6500 K table.
    public var customStrength: Double
    /// Meaningful only when customStrength is greater than zero.
    public var kelvin: Int
    public var minutesUntilNextChange: Double

    public init(phase: ColorSchedulePhase, customStrength: Double, kelvin: Int, minutesUntilNextChange: Double) {
        self.phase = phase
        self.customStrength = customStrength
        self.kelvin = kelvin
        self.minutesUntilNextChange = minutesUntilNextChange
    }
}

public enum ColorSchedule {
    public static func evaluate(_ profile: ColorProfile, at date: Date,
                                calendar: Calendar = .current) throws -> ColorScheduleTarget {
        let parts = calendar.dateComponents([.hour, .minute, .second], from: date)
        let minute = Double((parts.hour ?? 0) * 60 + (parts.minute ?? 0)) + Double(parts.second ?? 0) / 60
        return try evaluate(profile, minuteOfDay: minute)
    }

    public static func evaluate(_ profile: ColorProfile, minuteOfDay: Double) throws -> ColorScheduleTarget {
        try profile.validate()
        guard minuteOfDay.isFinite, (0..<1_440).contains(minuteOfDay) else {
            throw SettingsError.invalidValue("color.minuteOfDay")
        }
        guard profile.enabled else {
            return ColorScheduleTarget(phase: .disabled, customStrength: 0, kelvin: profile.restKelvin,
                                       minutesUntilNextChange: 0)
        }
        let elapsed = (minuteOfDay - Double(profile.restStartMinute) + 1_440).truncatingRemainder(dividingBy: 1_440)
        let restLength = Double((profile.workStartMinute - profile.restStartMinute + 1_440) % 1_440)
        let transition = Double(profile.transitionMinutes)
        if transition > 0 && elapsed < transition {
            return target(profile, restFraction: elapsed / transition, phase: .warming, next: transition - elapsed)
        }
        if elapsed < restLength {
            return target(profile, restFraction: 1, phase: .rest, next: restLength - elapsed)
        }
        if transition > 0 && elapsed < restLength + transition {
            return target(profile, restFraction: 1 - (elapsed - restLength) / transition,
                          phase: .cooling, next: restLength + transition - elapsed)
        }
        return target(profile, restFraction: 0, phase: .work, next: 1_440 - elapsed)
    }

    private static func target(_ profile: ColorProfile, restFraction: Double,
                               phase: ColorSchedulePhase, next: Double) -> ColorScheduleTarget {
        if profile.restUsesOriginal {
            return ColorScheduleTarget(phase: phase, customStrength: profile.workUsesOriginal ? 0 : 1 - restFraction,
                                       kelvin: profile.workKelvin, minutesUntilNextChange: next)
        }
        if profile.workUsesOriginal {
            return ColorScheduleTarget(phase: phase, customStrength: restFraction,
                                       kelvin: profile.restKelvin, minutesUntilNextChange: next)
        }
        // Match the bounded Windows schedule interpolation without sharing hardware-specific ramps.
        let fraction = (restFraction * 60).rounded(.toNearestOrEven) / 60
        let kelvin = Int((Double(profile.workKelvin) + Double(profile.restKelvin - profile.workKelvin) * fraction)
            .rounded(.toNearestOrEven))
        return ColorScheduleTarget(phase: phase, customStrength: 1, kelvin: kelvin, minutesUntilNextChange: next)
    }
}
