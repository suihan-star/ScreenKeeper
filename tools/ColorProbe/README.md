# 色温只读探测与受控验证

关联 `docs/tasks/007-wellbeing-design.md` / `007-wellbeing-plan.md`。此独立工具不读取或更改 ScreenKeeper 用户配置，不会把试验结果自动写成正式设备资格。

## 只读

```powershell
dotnet run --project tools/ColorProbe/ColorProbe.csproj
dotnet run --project tools/ColorProbe/ColorProbe.csproj -- --ddc-readonly
```

读取 QueryDisplayConfig 的稳定目标路径、GDI 地址、拓扑及模式指纹、高级颜色状态和 Gamma 可读性。`--ddc-readonly` 额外读取 DDC 色温能力和 RGB gain，不写亮度或颜色；避免在已有 DDC 亮度操作频繁进行时并行执行。没有任何试验参数时始终只读。Gamma 读回和 API 成功均不能证明实际画面正确。

## 经用户明确同意后进行的观察试验

当前只提供针对用户已指定的 **DELL SE2723DS** 的入口；必须同时提供本次只读探测得到的完整 `--scope-id`，不能只凭 DISPLAY 数字。所有路径应绝对定位到工作区的 `artifacts/color-temperature-qa/`，每次用一个新文件名。

```powershell
dotnet tools/ColorProbe/bin/Debug/net8.0-windows/ColorProbe.dll --test-dell15 --scope-id '<本次确认的稳定目标路径>' --recovery-file '<绝对路径>/dell-trial-01.json'
dotnet tools/ColorProbe/bin/Debug/net8.0-windows/ColorProbe.dll --test-dell-service --scope-id '<本次确认的稳定目标路径>' --recovery-file '<绝对路径>/dell-service-trial-01.json'
```

- `--test-dell15`：唯一匹配的 SDR 扩展 DELL 屏，5000 K 候选 15 秒，然后恢复基线。
- `--test-dell-service`：直接调用生产 `ColorTemperatureService`，10 秒渐变到 4500 K → 5000 K 预览 15 秒（含短过渡）自动撤销 → 10 秒渐变回保存的 4500 K → 暂停原色 → 10 秒渐变恢复 4500 K → 正常 Stop 恢复原色。各稳定阶段观察约 3 秒，含读回操作共约 60 秒。
- 两者在写入前先将原色、预期值、精确目标身份和截止时间刷盘并原子保存；启动独立隐藏看护进程后才开始调色。试验自身有 `finally` 恢复，看护分别在 25 秒 / 90 秒截止后检查恢复。
- 恢复仅在身份、拓扑、模式仍匹配，且当前读回属于原色或本试验已知目标时进行。外部调色或模式改变会停止写入并保留记录，不能通过删除恢复记录强行重试。
- 服务试验额外生成 `.service.json`，记录生产服务的写前意图和原色；看护只接受记录中精确匹配的已写或待写渐变帧，不按颜色范围猜测持有权。退出代码 0 只说明 API/读回和服务清理路径通过，不替代用户对目标屏变化、另一屏不变、最终原色恢复的确认。
- 不关闭夜间模式、ICC 或其他调色工具；不修改显示器 OSD，不诱发故障退出。拔插、HDR 切换、外部竞争、强杀及异常恢复测试需单独明确授权。

## 后端判断

Gamma 支持连续软件通道变换，但存在静默失败、共享管线和外部校色竞争，因此生产 `WindowsGammaBackend` 默认 `UnverifiedColorValidationRegistry`，不允许实际写入。恢复协调用假后端测试不授予硬件资格。

DDC 色温需要独立色温档位及 RGB gain/drive 的可恢复性证据，不能从亮度支持推导。没有已验证的 DDC 色温写后端，不提供假渐变或橙色遮罩。

2026-09-17 只读检测：Q27P10 与 DELL SE2723DS 均非复制、未启用高级颜色，Gamma 可读。Q27P10 的 DDC 高级能力读取失败；DELL 可读取三通道 gain（范围 0–100，当前均 100），色温档位 flags 为 0。CIM 显卡/连接参数读取在当前环境被拒绝，因此不据此推定显卡、驱动、接口或 ICC 环境。具体观察试验结果由 007 计划和本地试验 JSON 记录。
