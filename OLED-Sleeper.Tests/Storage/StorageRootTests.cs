using OLED_Sleeper.Storage;
using OLED_Sleeper.Tests.TestDoubles;

namespace OLED_Sleeper.Tests.Storage
{
    public class StorageRootTests
    {
        private readonly FakeFileSystem _fileSystem = new();

        [Theory]
        [InlineData(false, @"C:\FakeAppData\OLED-Sleeper")]
        [InlineData(true, @"C:\FakeApp\Data")]
        public void Store_WhenUpstreamFilesExist_DoesNotReadOrModifyThem(bool portable, string upstreamDirectory)
        {
            var root = new StorageRoot(_fileSystem, portable);
            var store = new AppDataFileStore(_fileSystem, root);
            foreach (var fileName in new[] { "settings.json", "brightness_state.json" })
            {
                var legacyPath = System.IO.Path.Combine(upstreamDirectory, fileName);
                const string original = "{\"Source\":\"upstream\"}";
                _fileSystem.AddFile(legacyPath, original);
                _fileSystem.AddFile(legacyPath + ".bak", original);

                Assert.True(new StorageRootPreparer(_fileSystem, root).TryPrepare(out _));
                Assert.Null(store.Read<Dictionary<string, string>>(fileName));
                Assert.True(store.TryWrite(fileName, new Dictionary<string, string> { ["Source"] = "ScreenKeeper" }));
                Assert.Equal("ScreenKeeper", store.Read<Dictionary<string, string>>(fileName)!["Source"]);
                Assert.Equal(original, _fileSystem.Contents(legacyPath));
                Assert.Equal(original, _fileSystem.Contents(legacyPath + ".bak"));
            }
        }

        [Fact]
        public void DirectoryPath_WhenNotPortable_IsTheApplicationDataFolder()
        {
            // Act
            var root = new StorageRoot(_fileSystem, isPortable: false);

            // Assert
            Assert.Equal(@"C:\FakeAppData\ScreenKeeper", root.DirectoryPath);
        }

        [Fact]
        public void DirectoryPath_WhenPortable_IsTheDataFolderBesideTheExecutable()
        {
            // Act
            var root = new StorageRoot(_fileSystem, isPortable: true);

            // Assert
            Assert.Equal(@"C:\FakeApp\Data\ScreenKeeper", root.DirectoryPath);
        }
    }
}
