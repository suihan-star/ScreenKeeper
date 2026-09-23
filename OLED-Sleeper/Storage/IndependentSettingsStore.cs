using OLED_Sleeper.Storage.Interfaces;
using System.IO;
using System.Text.Json;

namespace OLED_Sleeper.Storage;

/// <summary>Schema-aware module files. Never downgrade an unknown schema or replace unreadable data.</summary>
public sealed class IndependentSettingsStore<T>(IAppDataFileStore files, IFileSystem fileSystem,
    IStorageRoot root, string fileName, Func<T> defaults, Func<T, string?> validate, int maxSchema = 1) where T : class
{
    public bool IsAvailable { get; private set; } = true;
    public string? NoticeKey { get; private set; }
    public T Load()
    {
        var path = Path.Combine(root.DirectoryPath, fileName);
        if (!fileSystem.FileExists(path) && !fileSystem.FileExists(path + ".bak")) return defaults();
        var first = Read(path);
        if (first.Unknown) return Block("配置版本较新，此模块暂不可用。原文件已保留。");
        if (first.Value != null) return first.Value;
        var backup = Read(path + ".bak");
        if (backup.Value != null)
        {
            NoticeKey = "已从备份读取设置，请检查后保存。";
            return backup.Value;
        }
        return Block("无法读取此模块设置，原文件已保留。请检查配置文件。");
    }

    private T Block(string key) { IsAvailable = false; NoticeKey = key; return defaults(); }

    private (T? Value, bool Unknown) Read(string path)
    {
        try
        {
            if (!fileSystem.FileExists(path)) return (null, false);
            var json = fileSystem.ReadAllText(path);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return (null, false);
            if (!document.RootElement.TryGetProperty("Schema", out var schema) || !schema.TryGetInt32(out var version)) return (null, false);
            if (version < 1 || version > maxSchema) return (null, true);
            var value = JsonSerializer.Deserialize<T>(json);
            return value != null && validate(value) == null ? (value, false) : (null, false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        { return (null, false); }
    }

    public bool TrySave(T value)
    {
        if (!IsAvailable || validate(value) != null) return false;
        if (!files.TryWrite(fileName, value)) { NoticeKey = "保存失败，草稿已保留。请检查存储权限或空间。"; return false; }
        NoticeKey = null;
        return true;
    }
}
