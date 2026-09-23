using Moq;
using OLED_Sleeper.Features.HealthReminder;
using OLED_Sleeper.Storage;
using OLED_Sleeper.Storage.Interfaces;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Storage;

public sealed class IndependentSettingsStoreTests
{
    private const string Path = @"C:\FakeAppData\ScreenKeeper\health-reminder.json";
    private readonly FakeFileSystem _fs = new();
    private IndependentSettingsStore<HealthReminderSettings> Store()
    {
        var root = new StorageRoot(_fs, false);
        return new(new AppDataFileStore(_fs, root), _fs, root, "health-reminder.json", () => new(), s => s.Validate().FirstOrDefault());
    }
    [Fact]
    public void MissingModuleIsDisabledAndDoesNotCreateFile()
    {
        var store = Store(); Assert.False(store.Load().Enabled); Assert.True(store.IsAvailable); Assert.Null(_fs.Contents(Path));
    }
    [Theory]
    [InlineData("{broken")]
    [InlineData("{\"Schema\":1,\"WorkMinutes\":0}")]
    [InlineData("null")]
    public void CorruptPrimaryUsesValidBackup(string json)
    {
        _fs.AddFile(Path, json); _fs.AddFile(Path + ".bak", """{"Schema":1,"WorkMinutes":25} """);
        var store = Store(); Assert.Equal(25, store.Load().WorkMinutes); Assert.True(store.IsAvailable); Assert.NotNull(store.NoticeKey);
        Assert.Equal(json, _fs.Contents(Path));
    }
    [Fact]
    public void NewSchemaDoesNotDowngradeToBackupOrOverwrite()
    {
        _fs.AddFile(Path, """{"Schema":9} """); _fs.AddFile(Path + ".bak", """{"Schema":1} """);
        var store = Store(); store.Load(); Assert.False(store.IsAvailable); Assert.False(store.TrySave(new())); Assert.Contains("9", _fs.Contents(Path));
    }
    [Fact]
    public void BothUnreadableDisableModuleAndKeepFiles()
    {
        _fs.AddFile(Path, "{broken"); var store = Store(); Assert.False(store.Load().Enabled); Assert.False(store.TrySave(new())); Assert.Equal("{broken", _fs.Contents(Path));
    }
    [Fact]
    public void AtomicReplaceFailureDoesNotChangeBaseline()
    {
        _fs.AddFile(Path, """{"Schema":1,"WorkMinutes":50} """); var store = Store(); store.Load(); _fs.ReplaceFails = true;
        Assert.False(store.TrySave(new() { WorkMinutes = 20 })); Assert.Contains("50", _fs.Contents(Path));
    }
    [Fact]
    public void DurableWriteFlushesBeforeAtomicMove()
    {
        var fs = new Mock<IFileSystem>(MockBehavior.Strict); var root = Mock.Of<IStorageRoot>(r => r.DirectoryPath == @"C:\store");
        var sequence = new MockSequence();
        fs.InSequence(sequence).Setup(f => f.WriteAllTextDurable(@"C:\store\journal.json.tmp", It.IsAny<string>()));
        fs.InSequence(sequence).Setup(f => f.FileExists(@"C:\store\journal.json")).Returns(false);
        fs.InSequence(sequence).Setup(f => f.Move(@"C:\store\journal.json.tmp", @"C:\store\journal.json"));
        Assert.True(new AppDataFileStore(fs.Object, root).TryWriteDurable("journal.json", new { Schema = 1 })); fs.VerifyAll();
    }
    [Fact]
    public void DurableWriteUnsupportedFailsClosed()
    {
        var root = new StorageRoot(_fs, false);
        Assert.False(new AppDataFileStore(_fs, root).TryWriteDurable("journal.json", new { Schema = 1 }));
        Assert.Null(_fs.Contents(root.DirectoryPath + @"\journal.json"));
    }
}
