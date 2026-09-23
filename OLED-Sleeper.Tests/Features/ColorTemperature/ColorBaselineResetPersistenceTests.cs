using Moq;
using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Storage.Interfaces;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Tests.Features.ColorTemperature;

public sealed class ColorBaselineResetPersistenceTests
{
    private static readonly ColorDisplayScope Previous = new("fixture-a", "physical-a", "DISPLAY-OLD", "Example", "old-topology", "old-sdr");
    private static readonly ColorDisplayScope Current = Previous with { DeviceName = "DISPLAY-NEW", TopologyFingerprint = "new-topology", ModeFingerprint = "new-sdr" };
    private static ColorValue Linear() => new() { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReconnectedDisplayResetPersistsArchiveAndRequiresVerificationAfterReload(bool blockArchiveWrite)
    {
        using var fixture = new DiskFixture();
        var files = new AppDataFileStore(fixture.Files, fixture.Root);
        var recovery = new ColorRecoveryFileStore(files, fixture.Files, fixture.Root);
        var old = new ColorRecoveryDocument
        {
            Records = [new() { Scope = Previous, SessionId = "old-session", Baseline = GammaTarget.Create(Linear(), 5000, 1), LastApplied = GammaTarget.Create(Linear(), 4500, 1) }],
            ValidationRequiredScreens = ["unrelated-screen"]
        };
        Assert.True(recovery.TryWrite(old));
        ColorValidationRegistry NewRegistry() => new(new IndependentSettingsStore<ColorValidationDocument>(files, fixture.Files, fixture.Root,
            "color-temperature-validation.json", () => new(), document => document.Validate()));
        var registry = NewRegistry();
        Assert.True(registry.TryConfirm(Previous));
        var journalPath = Path.Combine(fixture.Root.DirectoryPath, ColorRecoveryFileStore.FileName);
        var originalBytes = File.ReadAllBytes(journalPath);
        var backend = new Mock<IColorTemperatureBackend>(MockBehavior.Strict);
        backend.Setup(b => b.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { new ColorCapability(Current, ColorCapabilityState.Unverified, "fixture", false, false, true) });
        backend.Setup(b => b.ReadAsync(Current, It.IsAny<CancellationToken>())).ReturnsAsync(() => Linear());
        var service = new ColorTemperatureService(backend.Object, recovery);
        var check = await service.PrepareBaselineResetAsync(delay: (_, _) => Task.CompletedTask);
        Assert.True(check.Ready, check.Message);
        Assert.Empty(Directory.GetFiles(fixture.Root.DirectoryPath, "color-temperature-archive-*.json"));
        Assert.Equal(originalBytes, File.ReadAllBytes(journalPath));

        // Force the actual file open to fail, without mocking the production store's result.
        if (blockArchiveWrite) fixture.Files.BlockArchives = true;
        var result = await service.ConfirmBaselineResetAsync(check.Ticket, registry);
        if (blockArchiveWrite)
        {
            Assert.False(result.Ready);
            Assert.Equal("旧原色记录备份失败，未解除恢复保护。", result.Message);
            Assert.Equal(originalBytes, File.ReadAllBytes(journalPath));
            Assert.True(NewRegistry().IsValidated(Previous));
            Assert.Empty(Directory.GetFiles(fixture.Root.DirectoryPath, "color-temperature-archive-*.json"));
            Assert.True(service.HasRecoveryRecords);
        }
        else
        {
            Assert.True(result.Ready, result.Message);
            var archivePath = Assert.Single(Directory.GetFiles(fixture.Root.DirectoryPath, "color-temperature-archive-*.json"));
            var archive = JsonSerializer.Deserialize<ColorRecoveryArchive>(File.ReadAllText(archivePath))!;
            Assert.Equal(Previous, Assert.Single(archive.Previous.Records).Scope);
            Assert.True(old.Records[0].Baseline.SameAs(archive.Previous.Records[0].Baseline));
            Assert.True(old.Records[0].LastApplied.SameAs(archive.Previous.Records[0].LastApplied));
            Assert.Equal(Current, Assert.Single(archive.ConfirmedColors).Scope);
            Assert.True(Linear().SameAs(archive.ConfirmedColors[0].Value));
            Assert.False(NewRegistry().IsValidated(Previous));
            Assert.False(NewRegistry().IsValidated(Current));
            var reloaded = new ColorRecoveryFileStore(files, fixture.Files, fixture.Root).Read();
            Assert.True(reloaded.IsSafe);
            Assert.Empty(reloaded.Document.Records);
            Assert.Contains(Previous.HardwareId, reloaded.Document.ValidationRequiredScreens);
            Assert.Contains("unrelated-screen", reloaded.Document.ValidationRequiredScreens);
            Assert.Equal(originalBytes, File.ReadAllBytes(journalPath + ".bak"));
            Assert.False(service.HasRecoveryRecords);
        }
        backend.Verify(b => b.DiscoverAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        backend.Verify(b => b.ReadAsync(Current, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        backend.VerifyNoOtherCalls();
    }

    private sealed class DiskFixture : IDisposable
    {
        private readonly string _tempRoot = Path.GetFullPath(Path.GetTempPath());
        public IStorageRoot Root { get; }
        public ArchiveFailureFileSystem Files { get; } = new();
        public DiskFixture()
        {
            var path = Path.Combine(_tempRoot, "screenkeeper-color-reset-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            var root = new Mock<IStorageRoot>();
            root.SetupGet(r => r.DirectoryPath).Returns(path);
            Root = root.Object;
        }
        public void Dispose()
        {
            string path = Path.GetFullPath(Root.DirectoryPath);
            Assert.Equal(_tempRoot.TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(path), ignoreCase: true);
            Assert.StartsWith("screenkeeper-color-reset-test-", Path.GetFileName(path));
            Directory.Delete(path, recursive: true);
        }
    }

    private sealed class ArchiveFailureFileSystem : FileSystem, IFileSystem
    {
        public bool BlockArchives;
        void IFileSystem.WriteAllTextDurable(string path, string contents)
        {
            if (BlockArchives && Path.GetFileName(path).StartsWith("color-temperature-archive-", StringComparison.Ordinal))
            {
                // Opening a directory for writing exercises the real filesystem failure path.
                base.WriteAllTextDurable(Path.GetDirectoryName(path)!, contents);
                return;
            }
            base.WriteAllTextDurable(path, contents);
        }
    }
}
