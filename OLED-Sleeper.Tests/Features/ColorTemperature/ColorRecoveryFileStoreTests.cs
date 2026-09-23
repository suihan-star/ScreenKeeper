using Moq;
using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Storage.Interfaces;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Tests.Features.ColorTemperature
{
    public class ColorRecoveryFileStoreTests
    {
        [Fact]
        public void MissingJournalIsSafeAndDurableApiIsRequired()
        {
            var files = new Mock<IFileSystem>();
            var store = new Mock<IAppDataFileStore>();
            var root = new Mock<IStorageRoot>();
            root.SetupGet(r => r.DirectoryPath).Returns("example");
            var subject = new ColorRecoveryFileStore(store.Object, files.Object, root.Object);
            Assert.True(subject.Read().IsSafe);
            store.Setup(s => s.TryWriteDurable(ColorRecoveryFileStore.FileName, It.IsAny<ColorRecoveryDocument>())).Returns(true);
            Assert.True(subject.TryWrite(new()));
            store.Verify(s => s.TryWrite(ColorRecoveryFileStore.FileName, It.IsAny<ColorRecoveryDocument>()), Times.Never);
        }

        [Theory]
        [InlineData("broken json")]
        [InlineData("null")]
        [InlineData("{}")]
        [InlineData("{\"Schema\":1}")]
        [InlineData("{\"Schema\":2,\"Records\":[]}")]
        [InlineData("{\"Schema\":1,\"Records\":[{}]}")]
        [InlineData("{\"Schema\":1,\"Records\":[],\"ValidationRequiredScreens\":null}")]
        [InlineData("{\"Schema\":1,\"Records\":[],\"ValidationRequiredScreens\":[\"\"]}")]
        [InlineData("{\"Schema\":1,\"Records\":[],\"ValidationRequiredScreens\":[\"a\",\"A\"]}")]
        public void DamagedOrFutureJournalNeverFallsBackToEmptyTakeover(string json)
        {
            var files = new Mock<IFileSystem>();
            var store = new Mock<IAppDataFileStore>();
            var root = new Mock<IStorageRoot>();
            root.SetupGet(r => r.DirectoryPath).Returns("example");
            var path = Path.Combine("example", ColorRecoveryFileStore.FileName);
            files.Setup(f => f.FileExists(path)).Returns(true);
            files.Setup(f => f.ReadAllText(path)).Returns(json);
            store.Setup(s => s.Read<ColorRecoveryDocument>(ColorRecoveryFileStore.FileName)).Returns(new ColorRecoveryDocument());
            var subject = new ColorRecoveryFileStore(store.Object, files.Object, root.Object);
            Assert.False(subject.Read().IsSafe);
            Assert.False(subject.TryWrite(new()));
            store.Verify(s => s.TryWriteDurable(ColorRecoveryFileStore.FileName, It.IsAny<ColorRecoveryDocument>()), Times.Never);
        }

        [Fact]
        public void ValidPrimaryReadsPauseAndUsesIndependentFile()
        {
            var files = new Mock<IFileSystem>();
            var store = new Mock<IAppDataFileStore>();
            var root = new Mock<IStorageRoot>();
            root.SetupGet(r => r.DirectoryPath).Returns("example");
            var path = Path.Combine("example", ColorRecoveryFileStore.FileName);
            var document = new ColorRecoveryDocument { PausedUntil = new() { ["offline-screen"] = DateTimeOffset.UtcNow.AddHours(1) } };
            files.Setup(f => f.FileExists(path)).Returns(true);
            files.Setup(f => f.ReadAllText(path)).Returns(JsonSerializer.Serialize(document));
            var result = new ColorRecoveryFileStore(store.Object, files.Object, root.Object).Read();
            Assert.True(result.IsSafe);
            Assert.Single(result.Document.PausedUntil);
            store.VerifyNoOtherCalls();
        }

        [Fact]
        public void ArchivesUseUniqueDurableFilesAndDeepCopyEvidenceWithoutReplacingActiveJournal()
        {
            var files = new Mock<IFileSystem>(); var store = new Mock<IAppDataFileStore>(); var root = new Mock<IStorageRoot>();
            root.SetupGet(r => r.DirectoryPath).Returns("example");
            var writes = new List<(string Name, ColorRecoveryArchive Archive)>();
            store.Setup(s => s.TryWriteDurable(It.IsAny<string>(), It.IsAny<ColorRecoveryArchive>()))
                .Callback<string, ColorRecoveryArchive>((name, value) => writes.Add((name, value))).Returns(true);
            var subject = new ColorRecoveryFileStore(store.Object, files.Object, root.Object); Assert.True(subject.Read().IsSafe);
            var scope = new ColorDisplayScope("a", "physical-a", "DISPLAY-A", "Example", "topology", "sdr");
            var ramp = new ColorValue { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };
            var archive = new ColorRecoveryArchive
            {
                ConfirmedUtc = DateTimeOffset.UtcNow,
                Previous = new() { Records = [new() { Scope = scope, Baseline = ramp.Copy(), LastApplied = ramp.Copy(), Conflicted = true }], ValidationRequiredScreens = ["b"] },
                ConfirmedColors = [new(scope, ramp.Copy())]
            };
            Assert.True(subject.TryArchive(archive)); Assert.True(subject.TryArchive(archive));
            Assert.NotEqual(writes[0].Name, writes[1].Name);
            Assert.All(writes, item => { Assert.StartsWith("color-temperature-archive-", item.Name); Assert.EndsWith(".json", item.Name); });
            archive.Previous.Records[0].Baseline.Channels[400] = 19; archive.ConfirmedColors[0].Value.Channels[400] = 20;
            archive.Previous.ValidationRequiredScreens.Clear();
            Assert.True(ramp.SameAs(writes[0].Archive.Previous.Records[0].Baseline));
            Assert.True(ramp.SameAs(writes[0].Archive.ConfirmedColors[0].Value));
            Assert.Equal("b", Assert.Single(writes[0].Archive.Previous.ValidationRequiredScreens));
            store.Verify(s => s.TryWrite(It.IsAny<string>(), It.IsAny<ColorRecoveryArchive>()), Times.Never);
            store.Verify(s => s.TryWriteDurable(ColorRecoveryFileStore.FileName, It.IsAny<ColorRecoveryDocument>()), Times.Never);
        }

        [Fact]
        public void RequiredVerificationSurvivesRoundTripAndCopiesIndependently()
        {
            var document = new ColorRecoveryDocument { ValidationRequiredScreens = ["a", "b"] };
            var copy = document.Copy(); copy.ValidationRequiredScreens.Remove("a");
            Assert.Equal(2, document.ValidationRequiredScreens.Count);
            Assert.Equal(document.ValidationRequiredScreens, JsonSerializer.Deserialize<ColorRecoveryDocument>(JsonSerializer.Serialize(document))!.ValidationRequiredScreens);
            Assert.Empty(JsonSerializer.Deserialize<ColorRecoveryDocument>("{\"Schema\":1,\"Records\":[]}")!.ValidationRequiredScreens);
        }

        [Theory]
        [InlineData("same-physical", true)]
        [InlineData("identity-case", true)]
        [InlineData("wrong-hardware", false)]
        [InlineData("wrong-path", false)]
        [InlineData("missing-observation", false)]
        [InlineData("duplicate-observation", false)]
        [InlineData("missing-mode", false)]
        [InlineData("missing-topology", false)]
        [InlineData("missing-address", false)]
        [InlineData("invalid-colors", false)]
        public void ArchivePairsEveryOldRecordWithTheSamePhysicalDisplayInItsCurrentMode(string condition, bool expected)
        {
            var files = new Mock<IFileSystem>(); var store = new Mock<IAppDataFileStore>(); var root = new Mock<IStorageRoot>();
            root.SetupGet(r => r.DirectoryPath).Returns("example");
            store.Setup(s => s.TryWriteDurable(It.IsAny<string>(), It.IsAny<ColorRecoveryArchive>())).Returns(true);
            var subject = new ColorRecoveryFileStore(store.Object, files.Object, root.Object);
            var old = new ColorDisplayScope("a", "physical-a", "DISPLAY-OLD", "Example A", "old-topology", "old-sdr");
            var other = old with { HardwareId = "b", ScopeId = "physical-b", DeviceName = "DISPLAY-B" };
            var current = old with { DeviceName = "DISPLAY-NEW", TopologyFingerprint = "new-topology", ModeFingerprint = "new-sdr" };
            var ramp = new ColorValue { Channels = Enumerable.Range(0, 768).Select(i => (ushort)(i % 256 * 257)).ToArray() };
            switch (condition)
            {
                case "identity-case": current = current with { HardwareId = "A", ScopeId = "PHYSICAL-A" }; break;
                case "wrong-hardware": current = current with { HardwareId = "different" }; break;
                case "wrong-path": current = current with { ScopeId = "different" }; break;
                case "missing-mode": current = current with { ModeFingerprint = "" }; break;
                case "missing-topology": current = current with { TopologyFingerprint = "" }; break;
                case "missing-address": current = current with { DeviceName = "" }; break;
            }
            var archive = new ColorRecoveryArchive
            {
                Previous = new() { Records = new[] { old, other }.Select(s => new ColorRecoveryRecord { Scope = s, Baseline = ramp.Copy(), LastApplied = ramp.Copy() }).ToList() },
                ConfirmedColors = [new(current, ramp.Copy()), new(other, ramp.Copy())]
            };
            if (condition == "missing-observation") archive.ConfirmedColors.RemoveAt(1);
            if (condition == "duplicate-observation") archive.ConfirmedColors[1] = archive.ConfirmedColors[0].Copy();
            if (condition == "invalid-colors") archive.ConfirmedColors[0].Value.Channels = [];
            Assert.Equal(expected, subject.TryArchive(archive));
            store.Verify(s => s.TryWriteDurable(It.IsAny<string>(), It.IsAny<ColorRecoveryArchive>()), expected ? Times.Once : Times.Never);
            store.VerifyNoOtherCalls();
        }
    }
}
