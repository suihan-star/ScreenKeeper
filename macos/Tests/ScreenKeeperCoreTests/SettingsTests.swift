import Foundation
import XCTest
@testable import ScreenKeeperCore

final class SettingsTests: XCTestCase {
    func testFreshSettingsAreOptInAndBilingualPreferencesAreIndependent() throws {
        var settings = AppSettings()
        XCTAssertFalse(settings.health.enabled)
        XCTAssertFalse(ScreenSettings().rest.enabled)
        XCTAssertFalse(ScreenSettings().privacy.enabled)
        settings.theme = .fog
        XCTAssertEqual(settings.language, .chinese)
        settings.language = .english
        XCTAssertEqual(settings.theme, .fog)
        try settings.validate()
    }

    func testAtomicRoundTripPreservesOfflineScreensAndIndependentModules() throws {
        try withStore { store in
            var settings = AppSettings(language: .english, theme: .fog)
            settings.screens["display-a"] = ScreenSettings(rest: RestSettings(enabled: true, idleSeconds: 75))
            settings.screens["offline-b"] = ScreenSettings(privacy: PrivacySettings(enabled: true, idleSeconds: 90))
            settings.health = HealthSettings(enabled: true, workMinutes: 30, breakMinutes: 3)
            try store.save(settings)
            XCTAssertEqual(try store.load(), settings)

            var restDraft = settings
            restDraft.screens["display-a"]?.rest.idleSeconds = 120
            XCTAssertEqual(settings.screens["display-a"]?.rest.idleSeconds, 75)
            try store.save(restDraft)
            let saved = try store.load()
            XCTAssertEqual(saved.screens["offline-b"], settings.screens["offline-b"])
            XCTAssertEqual(saved.health, settings.health)
            XCTAssertEqual(saved.screens["display-a"]?.rest.idleSeconds, 120)
        }
    }

    func testOnlyMissingFileLoadsDefaults() throws {
        try withStore { store in
            XCTAssertEqual(try store.load(), AppSettings())
            try Data().write(to: store.fileURL)
            XCTAssertThrowsError(try store.load())
        }
    }

    func testUnknownSchemaIsIdentifiedBeforeDecodingOtherFieldsAndCannotBeOverwritten() throws {
        try withStore { store in
            let future = Data("{\"schemaVersion\": 99, \"futureSetting\": true}".utf8)
            try future.write(to: store.fileURL)
            XCTAssertThrowsError(try store.load()) { error in
                XCTAssertEqual(error as? SettingsError, .unsupportedSchema(99))
            }
            XCTAssertThrowsError(try store.save(AppSettings()))
            XCTAssertEqual(try Data(contentsOf: store.fileURL), future)
        }
    }

    func testCorruptExistingFileCannotBeOverwrittenEvenWithoutPriorLoad() throws {
        try withStore { store in
            let corrupt = Data("{\"schemaVersion\":1,\"screens\":".utf8)
            try corrupt.write(to: store.fileURL)
            XCTAssertThrowsError(try store.save(AppSettings()))
            XCTAssertEqual(try Data(contentsOf: store.fileURL), corrupt)
        }
    }

    func testStructurallyValidButInvalidSettingsArePreserved() throws {
        try withStore { store in
            var invalid = AppSettings()
            invalid.health.workMinutes = -1
            let original = try JSONEncoder().encode(invalid)
            try original.write(to: store.fileURL)
            XCTAssertThrowsError(try store.load())
            XCTAssertThrowsError(try store.save(AppSettings()))
            XCTAssertEqual(try Data(contentsOf: store.fileURL), original)
        }
    }

    func testInvalidSavePreservesPreviousBytes() throws {
        try withStore { store in
            try store.save(AppSettings(theme: .fog))
            let original = try Data(contentsOf: store.fileURL)
            var invalid = AppSettings(theme: .forest)
            invalid.health.workMinutes = 0
            XCTAssertThrowsError(try store.save(invalid))
            XCTAssertEqual(try Data(contentsOf: store.fileURL), original)
        }
    }

    func testExternalCorruptionAfterSuccessfulLoadStillBlocksSave() throws {
        try withStore { store in
            try store.save(AppSettings())
            let draft = try store.load()
            let changed = Data("{\"schemaVersion\": 2}".utf8)
            try changed.write(to: store.fileURL)
            XCTAssertThrowsError(try store.save(draft))
            XCTAssertEqual(try Data(contentsOf: store.fileURL), changed)
        }
    }

    func testDirectoryAtFilePathIsNotTreatedAsMissing() throws {
        try withStore { store in
            try FileManager.default.createDirectory(at: store.fileURL, withIntermediateDirectories: false)
            XCTAssertThrowsError(try store.load())
            XCTAssertThrowsError(try store.save(AppSettings()))
        }
    }

    func testEnabledQuietModeRequiresASelectedCondition() throws {
        let none = QuietConditions(mouseOnScreen: false, foregroundOnScreen: false, computerInput: false)
        XCTAssertThrowsError(try PrivacySettings(quietWhileInUse: true, conditions: none).validate())
        XCTAssertNoThrow(try PrivacySettings(quietWhileInUse: false, conditions: none).validate())
        XCTAssertNoThrow(try RestSettings(enabled: true, conditions: none).validate())
    }

    func testPrivacyImagesAndSlideshowValidateOnlyRelevantOptions() throws {
        XCTAssertThrowsError(try PrivacySettings(background: .custom).validate())
        XCTAssertNoThrow(try PrivacySettings(background: .solid, slideshowSeconds: 0).validate())
        XCTAssertNoThrow(try PrivacySettings(background: .custom, imagePaths: ["one.png"], slideshowSeconds: 0).validate())
        XCTAssertThrowsError(try PrivacySettings(background: .custom, imagePaths: ["one.png", "two.png"],
                                                slideshowSeconds: 0).validate())
        XCTAssertThrowsError(try PrivacySettings(imagePaths: Array(repeating: "one.png", count: 13)).validate())
    }

    func testTimingBoundsAndInvalidIdentityAreRejected() throws {
        XCTAssertThrowsError(try RestSettings(idleSeconds: 0).validate())
        XCTAssertThrowsError(try PrivacySettings(idleSeconds: 86_401).validate())
        XCTAssertThrowsError(try HealthSettings(breakMinutes: 0).validate())
        XCTAssertThrowsError(try AppSettings(screens: ["  ": ScreenSettings()]).validate())
        XCTAssertNoThrow(try AppSettings(screens: ["uuid-one": ScreenSettings(), "uuid-two": ScreenSettings()]).validate())
    }

    private func withStore(_ body: (SettingsStore) throws -> Void) throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent("ScreenKeeper-tests-\(UUID())")
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: false)
        defer { try? FileManager.default.removeItem(at: directory) }
        try body(SettingsStore(directory: directory))
    }
}
