import XCTest
@testable import ScreenKeeperCore

final class HealthTimerTests: XCTestCase {
    func testDisabledTimerNeverPresents() {
        let timer = HealthTimer()
        XCTAssertEqual(tick(timer, 1).state, .disabled)
        timer.startBreak(now: 2)
        XCTAssertFalse(tick(timer, 3).shouldPresent)
    }

    func testWorkCycleRemindsAndUserStartsRest() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 59)
        XCTAssertEqual(tick(timer, 59).state, .working)
        XCTAssertEqual(tick(timer, 60).state, .reminder)
        XCTAssertTrue(timer.snapshot.shouldPresent)
        timer.startBreak(now: 60)
        XCTAssertEqual(tick(timer, 61).state, .breaking)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 59)
        XCTAssertEqual(tick(timer, 120).state, .working)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 60)
    }

    func testOvernightGapDoesNotCountAsWork() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 10)
        let afterSleep = tick(timer, 30_000)
        XCTAssertEqual(afterSleep.state, .working)
        XCTAssertEqual(afterSleep.remainingSeconds, 50)
        XCTAssertEqual(tick(timer, 30_001).remainingSeconds, 49)
    }

    func testLockAndUnlockDoNotAdvanceWorkOrDisplayReminder() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 10)
        XCTAssertEqual(timer.tick(now: 11, sessionAvailable: false, inputIdleSeconds: 0, screenAvailable: true).state, .paused)
        XCTAssertFalse(timer.snapshot.shouldPresent)
        XCTAssertEqual(timer.tick(now: 12, sessionAvailable: false, inputIdleSeconds: 0, screenAvailable: true).remainingSeconds, 50)
        XCTAssertEqual(tick(timer, 13).remainingSeconds, 50)
        XCTAssertEqual(tick(timer, 14).remainingSeconds, 49)
    }

    func testIdleAndUnknownInputPauseWithResumeBoundary() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 10)
        XCTAssertEqual(timer.tick(now: 11, sessionAvailable: true, inputIdleSeconds: 301, screenAvailable: true).state, .paused)
        XCTAssertEqual(timer.tick(now: 12, sessionAvailable: true, inputIdleSeconds: nil, screenAvailable: true).state, .paused)
        XCTAssertEqual(timer.tick(now: 13, sessionAvailable: true, inputIdleSeconds: .nan, screenAvailable: true).state, .paused)
        XCTAssertEqual(tick(timer, 14).remainingSeconds, 50)
        XCTAssertEqual(tick(timer, 15).remainingSeconds, 49)
    }

    func testPauseOnIdleCanBeDisabledButUnavailableInputStillPauses() {
        let timer = HealthTimer(settings: HealthSettings(enabled: true, workMinutes: 1, pauseOnIdle: false))
        XCTAssertEqual(timer.tick(now: 1, sessionAvailable: true, inputIdleSeconds: 600, screenAvailable: true).state, .working)
        XCTAssertEqual(timer.tick(now: 2, sessionAvailable: true, inputIdleSeconds: nil, screenAvailable: true).state, .paused)
    }

    func testNoDisplayDefersDueReminderAndDoesNotRestartCycle() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 59)
        XCTAssertEqual(timer.tick(now: 60, sessionAvailable: true, inputIdleSeconds: 0, screenAvailable: false).state, .deferred)
        XCTAssertFalse(timer.snapshot.shouldPresent)
        XCTAssertEqual(tick(timer, 61).state, .reminder)
    }

    func testPreviewPresentationGateDoesNotConsumeOrSnoozeRealReminder() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 59)
        // The application marks the display unavailable to the real cycle during a preview.
        for second in 60...100 {
            let snapshot = timer.tick(now: Double(second), sessionAvailable: true,
                                      inputIdleSeconds: 0, screenAvailable: false)
            XCTAssertEqual(snapshot.state, .deferred)
            XCTAssertFalse(snapshot.shouldPresent)
        }
        XCTAssertEqual(tick(timer, 101).state, .reminder)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 20)
    }

    func testPresentationGateHoldsAnAlreadyVisibleReminderWithoutApplyingSnooze() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 60)
        XCTAssertEqual(timer.snapshot.state, .reminder)
        for second in 61...90 {
            XCTAssertEqual(timer.tick(now: Double(second), sessionAvailable: true,
                                      inputIdleSeconds: 0, screenAvailable: false).state, .deferred)
        }
        XCTAssertEqual(tick(timer, 91).state, .reminder)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 20)
    }

    func testSnoozeDelaysOnceWhileSkipStartsNewCycle() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 60)
        timer.snooze(now: 60)
        XCTAssertEqual(tick(timer, 61).state, .snoozed)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 59)
        advance(timer, from: 62, through: 120)
        XCTAssertEqual(timer.snapshot.state, .reminder)
        timer.skip(now: 120)
        XCTAssertEqual(tick(timer, 120).state, .working)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 60)
    }

    func testUnansweredReminderAutomaticallySnoozes() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 80)
        XCTAssertEqual(timer.snapshot.state, .snoozed)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 60)
    }

    func testSessionLossHidesReminderAndWaitsUntilAvailable() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 60)
        XCTAssertEqual(timer.tick(now: 61, sessionAvailable: false, inputIdleSeconds: 0, screenAvailable: true).state, .paused)
        XCTAssertFalse(timer.snapshot.shouldPresent)
        XCTAssertEqual(tick(timer, 62).state, .reminder)
    }

    func testManualPausePreservesWorkAndNeverAccumulatesPausedTime() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 10)
        timer.setPaused(true, now: 10)
        XCTAssertEqual(tick(timer, 20).state, .paused)
        timer.setPaused(false, now: 20)
        XCTAssertEqual(tick(timer, 21).remainingSeconds, 50)
        XCTAssertEqual(tick(timer, 22).remainingSeconds, 49)
    }

    func testDisableAndReenableStartsFreshCycle() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 20)
        timer.apply(settings: HealthSettings(), now: 20)
        XCTAssertEqual(tick(timer, 21).state, .disabled)
        timer.apply(settings: HealthSettings(enabled: true, workMinutes: 1), now: 21)
        XCTAssertEqual(tick(timer, 21).remainingSeconds, 60)
    }

    func testInvalidTimeAndSettingsCannotPoisonTimer() {
        let timer = makeTimer()
        advance(timer, from: 1, through: 10)
        timer.apply(settings: HealthSettings(enabled: true, workMinutes: -1), now: 10)
        XCTAssertEqual(tick(timer, .nan).state, .paused)
        XCTAssertEqual(tick(timer, 11).remainingSeconds, 49)
        XCTAssertEqual(tick(timer, 5).remainingSeconds, 49)
    }

    func testBreakCannotDisplayWhileLockedButDeadlineCanComplete() {
        let timer = makeTimer()
        timer.startBreak(now: 0)
        let locked = timer.tick(now: 1, sessionAvailable: false, inputIdleSeconds: 0, screenAvailable: true)
        XCTAssertEqual(locked.state, .breaking)
        XCTAssertFalse(locked.shouldPresent)
        XCTAssertEqual(tick(timer, 70).state, .working)
        XCTAssertEqual(timer.snapshot.remainingSeconds, 60)
    }

    private func makeTimer() -> HealthTimer {
        HealthTimer(settings: HealthSettings(enabled: true, workMinutes: 1, breakMinutes: 1, snoozeMinutes: 1))
    }

    @discardableResult
    private func tick(_ timer: HealthTimer, _ now: Double) -> HealthSnapshot {
        timer.tick(now: now, sessionAvailable: true, inputIdleSeconds: 0, screenAvailable: true)
    }

    private func advance(_ timer: HealthTimer, from start: Int, through end: Int) {
        for second in start...end { tick(timer, Double(second)) }
    }
}
