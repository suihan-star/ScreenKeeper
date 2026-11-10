import Foundation
import Darwin

/// A process-lifetime advisory lock also covers direct `swift run` and concurrent launches.
final class InstanceLock {
    private var descriptor: Int32 = -1
    init() throws {
        let directory = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("ScreenKeeper", isDirectory: true)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        let opened = Darwin.open(directory.appendingPathComponent("mac-instance.lock").path, O_CREAT | O_RDWR, 0o600)
        guard opened >= 0 else { throw LockError.unavailable }
        guard flock(opened, LOCK_EX | LOCK_NB) == 0 else {
            Darwin.close(opened)
            throw LockError.alreadyRunning
        }
        descriptor = opened
    }
    deinit {
        if descriptor >= 0 { flock(descriptor, LOCK_UN); Darwin.close(descriptor) }
    }
    enum LockError: Error { case unavailable, alreadyRunning }
}
