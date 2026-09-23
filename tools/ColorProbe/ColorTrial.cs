using OLED_Sleeper.Features.ColorTemperature;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

// This tool is not registered with the application. The single observed trial cannot qualify production support.
internal static class ColorTrial
{
    private sealed class TrialQualification(ColorDisplayScope scope) : IColorValidationRegistry
    {
        public bool IsValidated(ColorDisplayScope current) => WindowsGammaBackend.SameScope(scope, current);
    }

    internal sealed class TrialRecord
    {
        public int Schema { get; set; } = 1;
        public string TrialId { get; set; } = "";
        public ColorDisplayScope Scope { get; set; } = new("", "", "", "", "", "");
        public ColorValue Baseline { get; set; } = new();
        public ColorValue Target { get; set; } = new();
        public ColorValue? AlternateTarget { get; set; }
        public DateTimeOffset Deadline { get; set; }
        public bool Completed { get; set; }
        public bool Conflict { get; set; }
        public bool GammaWriteReturnedSuccess { get; set; }
        public bool TargetReadbackMatched { get; set; }
        public bool BaselineReadbackMatched { get; set; }
        public string Status { get; set; } = "Prepared";
        public List<string> ServiceChecks { get; set; } = [];
    }

    public static async Task<int> RunAsync(string[] args)
    {
        string? path = Value(args, "--recovery-file");
        if (path == null || !Path.IsPathFullyQualified(path)) { Console.Error.WriteLine("必须指定绝对路径 --recovery-file。"); return 2; }
        if (args.Contains("--trial-watchdog")) return await WatchAsync(path);
        string? expectedScope = Value(args, "--scope-id");
        if (string.IsNullOrWhiteSpace(expectedScope)) { Console.Error.WriteLine("必须指定此前只读检测得到的 --scope-id。"); return 2; }
        if (File.Exists(path)) { Console.Error.WriteLine("测试记录已存在。请使用新文件名；本工具不会覆盖旧恢复记录。"); return 2; }
        TrialRecord? record = null;
        bool serviceTrial = args.Contains("--test-dell-service");
        try
        {
            var readOnly = new WindowsGammaBackend();
            var capabilities = await readOnly.DiscoverAsync(CancellationToken.None);
            var targets = capabilities.Where(c => c.Scope.DisplayName == "DELL SE2723DS" &&
                c.Scope.ScopeId.Equals(expectedScope, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (targets.Length != 1 || targets[0] is not { AdvancedColorEnabled: false, IsCloned: false, GammaReadable: true, State: ColorCapabilityState.Unverified })
                throw new InvalidOperationException("没有唯一匹配、可读且符合 SDR 扩展条件的 DELL SE2723DS。未写入。");
            var target = targets[0];
            var baseline = await readOnly.ReadAsync(target.Scope, CancellationToken.None) ?? throw new InvalidOperationException("原色读取失败。未写入。");
            record = new() { Scope = target.Scope, TrialId = Guid.NewGuid().ToString("N"), Baseline = baseline,
                Target = GammaTarget.Create(baseline, serviceTrial ? 4500 : 5000, 1),
                AlternateTarget = serviceTrial ? GammaTarget.Create(baseline, 5000, 1) : null,
                Deadline = DateTimeOffset.UtcNow.AddSeconds(serviceTrial ? 90 : 25) };
            AtomicWrite(path, record);
            var watchdog = StartWatchdog(path);
            using var watchProcess = watchdog;
            await Task.Delay(600);
            if (watchdog.HasExited) throw new InvalidOperationException("独立恢复看护未启动。未写入。");
            var backend = new WindowsGammaBackend(new TrialQualification(target.Scope));
            if (!baseline.SameAs(await backend.ReadAsync(target.Scope, CancellationToken.None)))
                throw new InvalidOperationException("接管前原色已变化。未写入。");
            if (serviceTrial)
            {
                await RunServiceTrialAsync(path, record, backend);
                return record.Completed && record.TargetReadbackMatched ? 0 : 1;
            }
            Console.WriteLine($"开始 15 秒观察：{target.Scope.DisplayName} / {target.Scope.DeviceName}，5000 K 候选；请同时确认另一屏不变。");
            record.Status = "WriteDispatched";
            AtomicWrite(path, record);
            bool written = await backend.WriteAsync(target.Scope, record.Target, CancellationToken.None);
            bool matched = record.Target.SameAs(await backend.ReadAsync(target.Scope, CancellationToken.None));
            record.GammaWriteReturnedSuccess = written;
            record.TargetReadbackMatched = matched;
            record.Status = written && matched ? "Observing" : "WriteOrReadbackFailed";
            AtomicWrite(path, record);
            Console.WriteLine($"API 返回：{written}；目标读回匹配：{matched}。这不能证明肉眼画面或作用范围正确。");
            if (written && matched) await Task.Delay(TimeSpan.FromSeconds(15));
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"测试停止：{error.Message}");
        }
        finally
        {
            if (record != null && File.Exists(path)) await RestoreAsync(path, record);
        }
        return record?.Completed == true && record.GammaWriteReturnedSuccess && record.TargetReadbackMatched ? 0 : 1;
    }

    private static async Task<int> WatchAsync(string path)
    {
        // Independent bounded process protects this one trial if its console is closed. No crash is induced by the test.
        for (int i = 0; i < 200; i++)
        {
            var record = Read(path);
            if (record == null) return 2;
            if (record.Completed || record.Conflict) return 0;
            if (DateTimeOffset.UtcNow >= record.Deadline) { await RestoreAsync(path, record); return record.Completed ? 0 : 1; }
            await Task.Delay(500);
        }
        return 1;
    }

    private static async Task RestoreAsync(string path, TrialRecord record)
    {
        using var mutex = new Semaphore(1, 1, "Local\\ScreenKeeper-ColorTrial-" + record.TrialId);
        if (!mutex.WaitOne(TimeSpan.FromSeconds(5))) return;
        try
        {
            var latest = Read(path);
            if (latest?.Completed == true || latest?.Conflict == true) { record.Completed = latest.Completed; record.Conflict = latest.Conflict; return; }
            var backend = new WindowsGammaBackend(new TrialQualification(record.Scope));
            var currentCapabilities = await backend.DiscoverAsync(CancellationToken.None);
            if (!currentCapabilities.Any(c => c.State == ColorCapabilityState.Supported && WindowsGammaBackend.SameScope(record.Scope, c.Scope)))
                throw new InvalidOperationException("显示身份或模式已改变，保留记录，禁止盲恢复。");
            var current = await backend.ReadAsync(record.Scope, CancellationToken.None);
            if (current == null) throw new InvalidOperationException("无法读取当前颜色，保留记录。");
            ColorRecoveryRecord? serviceRecord = null;
            if (File.Exists(path + ".service.json"))
            {
                var serviceJournal = new TrialRecoveryStore(path + ".service.json").Read();
                if (!serviceJournal.IsSafe) throw new InvalidOperationException("服务恢复记录无法确认，未覆盖当前颜色。");
                serviceRecord = serviceJournal.Document.Records.FirstOrDefault(r => WindowsGammaBackend.SameScope(r.Scope, record.Scope));
                if (serviceRecord?.Conflicted == true) throw new InvalidOperationException("服务已检测外部调色，未覆盖当前颜色。");
            }
            if (!current.SameAs(record.Baseline))
            {
                bool knownServiceFrame = serviceRecord != null && serviceRecord.Baseline.SameAs(record.Baseline) &&
                    (current.SameAs(serviceRecord.LastApplied) || current.SameAs(serviceRecord.PendingValue));
                if (!current.SameAs(record.Target) && !current.SameAs(record.AlternateTarget) && !knownServiceFrame)
                {
                    record.Conflict = true;
                    record.Status = "ExternalConflict";
                    AtomicWrite(path, record);
                    throw new InvalidOperationException("检测到外部调色，未用旧基线覆盖。");
                }
                if (!await backend.WriteAsync(record.Scope, record.Baseline, CancellationToken.None))
                    throw new InvalidOperationException("恢复 API 失败，保留记录。");
            }
            record.BaselineReadbackMatched = record.Baseline.SameAs(await backend.ReadAsync(record.Scope, CancellationToken.None));
            record.Completed = record.BaselineReadbackMatched;
            record.Status = record.Completed ? "RestoredReadback" : "RestoreReadbackFailed";
            AtomicWrite(path, record);
            Console.WriteLine($"已尝试恢复接管前原色；基线读回匹配：{record.BaselineReadbackMatched}。请用户确认肉眼恢复。");
        }
        catch (Exception error) { Console.Error.WriteLine($"恢复待处理：{error.Message} 记录：{path}"); }
        finally { mutex.Release(); }
    }

    private sealed class TrialRecoveryStore(string path) : IColorRecoveryStore
    {
        public ColorRecoveryReadResult Read()
        {
            if (!File.Exists(path)) return new(new(), true);
            try
            {
                var document = JsonSerializer.Deserialize<ColorRecoveryDocument>(File.ReadAllText(path));
                return document is { Schema: 1 } ? new(document, true) : new(new(), false, "试验恢复记录版本不匹配。");
            }
            catch (Exception) { return new(new(), false, "试验恢复记录无法读取。"); }
        }

        public bool TryWrite(ColorRecoveryDocument document)
        {
            try { WriteJson(path, document); return true; }
            catch (Exception) { return false; }
        }
    }

    private static async Task RunServiceTrialAsync(string path, TrialRecord record, WindowsGammaBackend backend)
    {
        string journalPath = path + ".service.json";
        if (File.Exists(journalPath)) throw new InvalidOperationException("该服务试验恢复记录已存在，未写入。");
        var service = new ColorTemperatureService(backend, new TrialRecoveryStore(journalPath));
        try
        {
            var settings = new ColorTemperatureSettings { Enabled = true, Mode = ColorTemperatureMode.Manual,
                NightKelvin = 4500, ParticipatingScreens = new() { [record.Scope.HardwareId] = true } };
            Console.WriteLine("阶段 1/6：10 秒渐变到保存的 4500 K，再观察 3 秒。");
            await service.ApplySettingsAsync(settings);
            await WaitForAsync(record.Target, 13);
            await CheckAsync("4500K saved policy", record.Target);
            await Task.Delay(3000);
            Console.WriteLine("阶段 2/6：15 秒预览 5000 K（含 10 秒渐变），随后自动撤销。");
            var previewClock = Stopwatch.StartNew();
            if (!await service.PreviewAsync(record.Scope.HardwareId, 5000)) throw new InvalidOperationException("服务未能开始预览。");
            await WaitForAsync(record.AlternateTarget!, 13);
            await CheckAsync("5000K preview", record.AlternateTarget!);
            if (previewClock.ElapsedMilliseconds < 16000) await Task.Delay((int)(16000 - previewClock.ElapsedMilliseconds));
            Console.WriteLine("阶段 3/6：自动渐变回保存的 4500 K，再观察 3 秒。");
            await WaitForAsync(record.Target, 13);
            await CheckAsync("Preview returned to saved 4500K", record.Target);
            await Task.Delay(3000);
            Console.WriteLine("阶段 4/6：临时原色，观察 3 秒。");
            if (!await service.PauseAsync(record.Scope.HardwareId, DateTimeOffset.Now.AddMinutes(2))) throw new InvalidOperationException("暂停记录保存失败。");
            await CheckAsync("Pause restored baseline", record.Baseline);
            await Task.Delay(3000);
            Console.WriteLine("阶段 5/6：10 秒渐变恢复保存的 4500 K，再观察 3 秒。");
            if (!await service.ResumeAsync(record.Scope.HardwareId)) throw new InvalidOperationException("恢复策略失败。");
            await WaitForAsync(record.Target, 13);
            await CheckAsync("Resume applied saved 4500K", record.Target);
            await Task.Delay(3000);
        }
        finally
        {
            Console.WriteLine("阶段 6/6：正常停止服务并恢复接管前原色。");
            await service.StopAsync();
        }
        await CheckAsync("Normal Stop restored baseline", record.Baseline);
        if (service.States.Any(s => s.State is ColorControlState.RecoveryPending or ColorControlState.Conflict or ColorControlState.Failed) ||
            new TrialRecoveryStore(journalPath).Read() is not { IsSafe: true, Document.Records.Count: 0 })
            throw new InvalidOperationException("服务仍报告恢复待处理，不能把读回匹配记作完整恢复。");
        record.GammaWriteReturnedSuccess = true;
        record.TargetReadbackMatched = true;
        record.BaselineReadbackMatched = true;
        record.Completed = true;
        record.Status = "ServiceTrialReadbacksPassed";
        AtomicWrite(path, record);

        async Task CheckAsync(string name, ColorValue expected)
        {
            if (DateTimeOffset.UtcNow >= record.Deadline) throw new InvalidOperationException("试验已超过观察期限，停止后续调色。");
            bool matched = expected.SameAs(await backend.ReadAsync(record.Scope, CancellationToken.None));
            if (!matched) throw new InvalidOperationException(name + "：读回不匹配。");
            record.ServiceChecks.Add(name + ": readback matched");
            AtomicWrite(path, record);
            Console.WriteLine(name + "：读回匹配；仍需要肉眼确认。");
        }

        async Task WaitForAsync(ColorValue expected, int seconds)
        {
            var elapsed = Stopwatch.StartNew();
            while (elapsed.Elapsed < TimeSpan.FromSeconds(seconds))
            {
                if (DateTimeOffset.UtcNow >= record.Deadline) throw new InvalidOperationException("试验超过截止时间。");
                if (service.States.Any(s => s.State is ColorControlState.Failed or ColorControlState.Conflict or ColorControlState.RecoveryPending))
                    throw new InvalidOperationException("服务停止了调色，保留恢复记录。");
                if (expected.SameAs(await backend.ReadAsync(record.Scope, CancellationToken.None))) return;
                await Task.Delay(500);
            }
            throw new InvalidOperationException("单调短过渡未在预期时间内到达目标。");
        }
    }

    private static TrialRecord? Read(string path)
    {
        try
        {
            var record = JsonSerializer.Deserialize<TrialRecord>(File.ReadAllText(path));
            return record is { Schema: 1, Baseline.IsValid: true, Target.IsValid: true } ? record : null;
        }
        catch (Exception) { return null; }
    }

    private static Process StartWatchdog(string path)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--trial-watchdog");
        start.ArgumentList.Add("--recovery-file");
        start.ArgumentList.Add(path);
        return Process.Start(start) ?? throw new InvalidOperationException("无法启动独立恢复看护。");
    }

    private static void AtomicWrite(string path, TrialRecord record) => WriteJson(path, record);

    private static void WriteJson<T>(string path, T record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, record, new JsonSerializerOptions { WriteIndented = true });
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
        else File.Move(temporary, path);
    }

    private static string? Value(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
