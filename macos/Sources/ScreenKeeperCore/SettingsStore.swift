import Foundation

/// Ordinary settings only. Color ownership and recovery require a separate durable journal.
public final class SettingsStore {
    public let fileURL: URL
    private let fileManager: FileManager
    private struct SchemaHeader: Decodable { let schemaVersion: Int }

    public init(directory: URL, fileManager: FileManager = .default) {
        fileURL = directory.appendingPathComponent("mac-settings.json", isDirectory: false)
        self.fileManager = fileManager
    }

    public func load() throws -> AppSettings {
        let data: Data
        do {
            data = try Data(contentsOf: fileURL)
        } catch let error as NSError {
            // A failed read must never turn an inaccessible or corrupt file into default settings.
            if error.domain == NSCocoaErrorDomain && error.code == NSFileReadNoSuchFileError {
                return AppSettings()
            }
            throw SettingsError.unreadableSettings
        }
        return try decode(data)
    }

    public func save(_ settings: AppSettings) throws {
        try settings.validate()
        // Recheck the current file on every save, including after a failed initial load.
        // An unknown schema or malformed document is evidence to preserve, not permission to reset.
        _ = try load()
        let encoder = JSONEncoder()
        encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
        do {
            let data = try encoder.encode(settings)
            try fileManager.createDirectory(at: fileURL.deletingLastPathComponent(),
                                            withIntermediateDirectories: true)
            try data.write(to: fileURL, options: .atomic)
        } catch {
            throw SettingsError.writeFailed
        }
    }

    private func decode(_ data: Data) throws -> AppSettings {
        do {
            let decoder = JSONDecoder()
            let header = try decoder.decode(SchemaHeader.self, from: data)
            guard header.schemaVersion == 1 else { throw SettingsError.unsupportedSchema(header.schemaVersion) }
            let settings = try decoder.decode(AppSettings.self, from: data)
            try settings.validate()
            return settings
        } catch let error as SettingsError {
            throw error
        } catch {
            throw SettingsError.unreadableSettings
        }
    }
}
