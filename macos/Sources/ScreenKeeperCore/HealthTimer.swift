import Foundation

public enum HealthState: String, Equatable, Sendable {
    case disabled, working, paused, reminder, breaking, snoozed, deferred
}

public struct HealthSnapshot: Equatable, Sendable {
    public var state: HealthState
    public var remainingSeconds: Double
    public var shouldPresent: Bool

    public init(state: HealthState, remainingSeconds: Double, shouldPresent: Bool) {
        self.state = state
        self.remainingSeconds = max(0, remainingSeconds)
        self.shouldPresent = shouldPresent
    }
}

/// A monotonic, global work cycle. UI polling passes uptime, never wall-clock time.
public final class HealthTimer {
    private enum Phase { case working, reminder, breaking, snoozed }
    private var settings: HealthSettings
    private var phase: Phase = .working
    private var workElapsed: Double = 0
    private var snoozeRemaining: Double = 0
    private var breakEnd: Double = 0
    private var reminderEnd: Double = 0
    private var lastTick: Double
    private var wasBlocked = false
    private var manuallyPaused = false
    public private(set) var snapshot: HealthSnapshot

    public init(settings: HealthSettings = HealthSettings(), now: Double = 0) {
        self.settings = (try? settings.validate()) != nil ? settings : HealthSettings()
        lastTick = now.isFinite ? now : 0
        snapshot = HealthSnapshot(state: self.settings.enabled ? .working : .disabled,
                                  remainingSeconds: Double(self.settings.workMinutes * 60), shouldPresent: false)
    }

    public func apply(settings: HealthSettings, now: Double) {
        guard (try? settings.validate()) != nil, now.isFinite else { return }
        let newCycleRequired = settings.enabled != self.settings.enabled
        self.settings = settings
        if newCycleRequired || !settings.enabled {
            resetCycle()
            manuallyPaused = false
        }
        workElapsed = min(workElapsed, workTarget)
        lastTick = now
        if !settings.enabled { snapshot = result(.disabled, remaining: 0) }
    }

    @discardableResult
    public func tick(now: Double, sessionAvailable: Bool, inputIdleSeconds: Double?,
                     screenAvailable: Bool) -> HealthSnapshot {
        guard now.isFinite else {
            snapshot = result(settings.enabled ? .paused : .disabled, remaining: remaining(at: lastTick))
            return snapshot
        }
        var delta = now - lastTick
        lastTick = now
        // A sleeping machine or suspended process must not accumulate an entire night of work.
        if !delta.isFinite || delta < 0 || delta > 5 { delta = 0 }
        let inputAvailable = inputIdleSeconds.map { $0.isFinite && $0 >= 0 } ?? false
        let idle = settings.pauseOnIdle && (inputIdleSeconds ?? 0) >= 300
        let blocked = !sessionAvailable || !inputAvailable || idle || manuallyPaused
        if blocked || wasBlocked { delta = 0 }
        wasBlocked = blocked

        guard settings.enabled else {
            snapshot = result(.disabled, remaining: 0)
            return snapshot
        }
        let canPresent = !blocked && screenAvailable
        if phase == .breaking {
            // A user-started rest countdown may include sleep time; it does not prove actual rest.
            if now >= breakEnd {
                resetCycle()
                delta = 0
            } else {
                snapshot = result(manuallyPaused ? .paused : .breaking,
                                  remaining: breakEnd - now, present: canPresent)
                return snapshot
            }
        }
        if blocked {
            holdReminder()
            snapshot = result(.paused, remaining: remaining(at: now))
            return snapshot
        }
        if phase == .reminder {
            if !canPresent { holdReminder() }
            else if now >= reminderEnd { snooze(now: now); delta = 0 }
            else {
                snapshot = result(.reminder, remaining: reminderEnd - now, present: true)
                return snapshot
            }
        }
        if phase == .snoozed {
            snoozeRemaining = max(0, snoozeRemaining - delta)
            if snoozeRemaining > 0 {
                snapshot = result(.snoozed, remaining: snoozeRemaining)
                return snapshot
            }
            phase = .working
            workElapsed = workTarget
        } else {
            workElapsed = min(workTarget, workElapsed + delta)
        }
        if workElapsed < workTarget {
            snapshot = result(.working, remaining: workTarget - workElapsed)
        } else if !canPresent {
            snapshot = result(.deferred, remaining: 0)
        } else {
            phase = .reminder
            reminderEnd = now + 20
            snapshot = result(.reminder, remaining: 20, present: true)
        }
        return snapshot
    }

    public func startBreak(now: Double) {
        guard settings.enabled, !manuallyPaused, now.isFinite, phase != .breaking else { return }
        phase = .breaking
        breakEnd = now + Double(settings.breakMinutes * 60)
        lastTick = now
    }

    public func snooze(now: Double) {
        guard settings.enabled, !manuallyPaused, now.isFinite else { return }
        phase = .snoozed
        snoozeRemaining = Double(settings.snoozeMinutes * 60)
        lastTick = now
    }

    public func skip(now: Double) {
        guard settings.enabled, now.isFinite else { return }
        resetCycle()
        lastTick = now
    }

    public func setPaused(_ paused: Bool, now: Double) {
        guard now.isFinite else { return }
        if paused && phase == .breaking { resetCycle() }
        manuallyPaused = paused
        holdReminder()
        lastTick = now
        wasBlocked = true
    }

    private var workTarget: Double { Double(settings.workMinutes * 60) }

    private func holdReminder() {
        if phase == .reminder {
            phase = .working
            workElapsed = workTarget
        }
    }

    private func resetCycle() {
        phase = .working
        workElapsed = 0
        snoozeRemaining = 0
    }

    private func remaining(at now: Double) -> Double {
        switch phase {
        case .working: return max(0, workTarget - workElapsed)
        case .reminder: return max(0, reminderEnd - now)
        case .breaking: return max(0, breakEnd - now)
        case .snoozed: return max(0, snoozeRemaining)
        }
    }

    private func result(_ state: HealthState, remaining: Double, present: Bool = false) -> HealthSnapshot {
        HealthSnapshot(state: state, remainingSeconds: remaining, shouldPresent: present)
    }
}
