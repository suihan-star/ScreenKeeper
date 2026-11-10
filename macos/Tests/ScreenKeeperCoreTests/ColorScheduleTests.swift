import Foundation
import XCTest
@testable import ScreenKeeperCore

final class ColorScheduleTests: XCTestCase {
    func testDisabledScheduleDoesNotRequestCustomColor() throws {
        let target = try ColorSchedule.evaluate(ColorProfile(), minuteOfDay: 1_200)
        XCTAssertEqual(target.phase, .disabled)
        XCTAssertEqual(target.customStrength, 0)
    }

    func testDefaultWorkOriginalAndRestCustomCrossMidnight() throws {
        let profile = ColorProfile(enabled: true, transitionMinutes: 0)
        let work = try ColorSchedule.evaluate(profile, minuteOfDay: 600)
        XCTAssertEqual(work.phase, .work)
        XCTAssertEqual(work.customStrength, 0)
        let evening = try ColorSchedule.evaluate(profile, minuteOfDay: 1_200)
        XCTAssertEqual(evening.phase, .rest)
        XCTAssertEqual(evening.customStrength, 1)
        XCTAssertEqual(evening.kelvin, 4_500)
        let afterMidnight = try ColorSchedule.evaluate(profile, minuteOfDay: 60)
        XCTAssertEqual(afterMidnight.phase, .rest)
        XCTAssertEqual(afterMidnight.minutesUntilNextChange, 480)
    }

    func testTransitionBlendsAgainstOriginalWithoutTreatingItAs6500K() throws {
        let profile = ColorProfile(enabled: true)
        let warming = try ColorSchedule.evaluate(profile, minuteOfDay: 1_090)
        XCTAssertEqual(warming.phase, .warming)
        XCTAssertEqual(warming.customStrength, 0.5, accuracy: 0.0001)
        XCTAssertEqual(warming.kelvin, 4_500)
        let cooling = try ColorSchedule.evaluate(profile, minuteOfDay: 550)
        XCTAssertEqual(cooling.phase, .cooling)
        XCTAssertEqual(cooling.customStrength, 0.5, accuracy: 0.0001)
        XCTAssertEqual(cooling.kelvin, 4_500)
    }

    func testRestMayUseOriginalWhileWorkHasCustomColor() throws {
        let profile = ColorProfile(enabled: true, workUsesOriginal: false, restUsesOriginal: true,
                                   workKelvin: 5_300)
        let work = try ColorSchedule.evaluate(profile, minuteOfDay: 600)
        XCTAssertEqual(work.customStrength, 1)
        XCTAssertEqual(work.kelvin, 5_300)
        let rest = try ColorSchedule.evaluate(profile, minuteOfDay: 1_200)
        XCTAssertEqual(rest.customStrength, 0)
        let transition = try ColorSchedule.evaluate(profile, minuteOfDay: 1_090)
        XCTAssertEqual(transition.customStrength, 0.5, accuracy: 0.0001)
        XCTAssertEqual(transition.kelvin, 5_300)
    }

    func testBothOriginalRetainsDistinctSchedulePhasesWithoutTakingOwnership() throws {
        let profile = ColorProfile(enabled: true, restUsesOriginal: true)
        let work = try ColorSchedule.evaluate(profile, minuteOfDay: 600)
        let rest = try ColorSchedule.evaluate(profile, minuteOfDay: 1_200)
        XCTAssertEqual(work.phase, .work)
        XCTAssertEqual(rest.phase, .rest)
        XCTAssertEqual(work.customStrength, 0)
        XCTAssertEqual(rest.customStrength, 0)
    }

    func testCustomToCustomTransitionAndEqualTargetsKeepTimePhase() throws {
        var profile = ColorProfile(enabled: true, workUsesOriginal: false, workKelvin: 6_000, restKelvin: 4_000)
        XCTAssertEqual(try ColorSchedule.evaluate(profile, minuteOfDay: 1_090).kelvin, 5_000)
        profile.restKelvin = 6_000
        XCTAssertEqual(try ColorSchedule.evaluate(profile, minuteOfDay: 600).phase, .work)
        XCTAssertEqual(try ColorSchedule.evaluate(profile, minuteOfDay: 1_200).phase, .rest)
    }

    func testNightShiftWorkersCanInvertSchedule() throws {
        let profile = ColorProfile(enabled: true, workStartMinute: 1_320, restStartMinute: 480, transitionMinutes: 0)
        XCTAssertEqual(try ColorSchedule.evaluate(profile, minuteOfDay: 60).phase, .work)
        XCTAssertEqual(try ColorSchedule.evaluate(profile, minuteOfDay: 600).phase, .rest)
    }

    func testEqualTimesInvalidTransitionAndNonfiniteTimeAreRejected() throws {
        XCTAssertThrowsError(try ColorProfile(workStartMinute: 500, restStartMinute: 500).validate())
        XCTAssertThrowsError(try ColorProfile(workStartMinute: 500, restStartMinute: 510, transitionMinutes: 20).validate())
        XCTAssertThrowsError(try ColorSchedule.evaluate(ColorProfile(), minuteOfDay: .nan))
        XCTAssertThrowsError(try ColorSchedule.evaluate(ColorProfile(), minuteOfDay: 1_440))
    }

    func testHiddenTemperatureDoesNotBlockOriginalChoice() throws {
        XCTAssertNoThrow(try ColorProfile(workUsesOriginal: true, workKelvin: 0).validate())
        XCTAssertThrowsError(try ColorProfile(workUsesOriginal: false, workKelvin: 0).validate())
        XCTAssertNoThrow(try ColorProfile(restUsesOriginal: true, restKelvin: 0).validate())
    }

    func testDateEvaluationUsesExplicitLocalCalendar() throws {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = TimeZone(secondsFromGMT: 8 * 3_600)!
        let date = try XCTUnwrap(calendar.date(from: DateComponents(year: 2026, month: 11, day: 10, hour: 18, minute: 10)))
        let target = try ColorSchedule.evaluate(ColorProfile(enabled: true), at: date, calendar: calendar)
        XCTAssertEqual(target.phase, .warming)
        XCTAssertEqual(target.customStrength, 0.5, accuracy: 0.0001)
    }
}
