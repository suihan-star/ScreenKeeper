# AOC 关屏后黑屏与 NVIDIA 输出未启用排查

关联 [011 设计](011-blackout-recovery-design.md) 和 [011 计划](011-blackout-recovery-plan.md)。公开记录已将真实用户的日期、作息和日志时刻改为相对事件顺序，保留先后关系、技术证据与结论。

## 结论与边界

没有发现 ScreenKeeper 直接禁用显示器输出的代码或证据。关开显示器电源后的 HDMI 连接／显卡输出恢复异常值得优先调查；但初始黑屏发生时没有现场快照，且当时 AOC 色温已开启，不能完全排除软件、显示驱动或颜色管理之间的间接影响。

用户先遇到黑屏、拔插 HDMI，再发现控制面板未勾选。未勾选可能是初始状态，也可能是拔插后未恢复输出的结果，不能直接将两者等同。NVIDIA 官方将该复选框定义为启用非活动显示器，因此重新勾选后恢复能证明当时输出未启用，不能证明是谁停用或最初为何黑屏。[NVIDIA 说明](https://nvidia.custhelp.com/app/answers/detail/a_id/3148)

## 核查事实

- 现场确认：主机保持运行，重新打开显示器电源后发现黑屏；拔插 AOC HDMI 无效，NVIDIA 控制面板勾选 AOC 后恢复。
- 当前运行 `OLED-Sleeper/bin/RecoveryArchiveFix/Release/net8.0-windows/ScreenKeeper.exe`。显卡 GTX 1650，驱动 32.0.15.9186；当前 AOC 使用 HDMI、DELL 使用 DP，两屏均活动。
- 系统最近启动早于本次显示器关开电源周期。查询故障前至排查时的系统事件，没有新启动／关机、睡眠恢复、Display／nvlddmkm 错误。交流电的自动关闭显示、睡眠及休眠时长均为 0（永不）。这些不能覆盖显示器自身的电源按钮或未被记录的驱动异常。
- AOC 已保存休息为 30 秒遮黑，`LowerBrightnessOnBlackout=false`；日志明确未写硬件亮度。隐私指定应用有多窗口匹配失败，保留隐私幕；这能影响屏幕显示内容，但不能据此解释 NVIDIA 未启用输出。
- 故障前 AOC 色温已保存为开启，工作和休息两个时段均为自定义色温；DELL 关闭。AOC 恢复记录旧备份早于再次连接后的主记录，后者被标记为颜色冲突且没有待写值。记录时间不等于逐次原生调用日志，不能据此精确重建全部 Gamma 调用。

## 时间线

| 相对顺序 | 来源与观察 | 能说明什么 |
| --- | --- | --- |
| 前一使用周期末 | Kernel-PnP 1010：DELL 被总线报告缺少 | 消失的是 DELL，不能误写成 AOC 在该阶段掉线 |
| 紧接上述事件 | 应用检测布局变化，随后仅列 AOC，活动屏幕数 1 | 应用在系统连接变化之后重新扫描 |
| 再次恢复连接初期 | 应用重新列出两屏，AOC DDC/CI 检测失败；DELL 正常 | 初期 AOC 仍被枚举为活动屏，但硬件通信异常，不能用枚举成功证明可见画面正常 |
| 两屏被重新枚举后 | AOC 原色恢复记录被标记冲突 | 当前颜色不再匹配软件记录；不能把它当作停用输出的原因 |
| 随后达到空闲条件 | AOC 软件遮黑，明确不改硬件亮度 | 软件随后确实有内容遮挡行为 |
| 后续排查操作期间 | 布局再次变化，应用仅列 DELL，活动屏幕数 1 | 此时 AOC 已不在活动桌面中；与现场拔插操作重合但无法精确定位先后 |
| 紧接上述布局变化 | Kernel-PnP 1010：AOC 被总线报告缺少 | 是排查操作期间的移除证据，不足以归因初始黑屏 |
| 程序退出阶段 | 软件清理遮罩、提示原色恢复未完成、正常退出 | 此次退出有完整完成记录 |
| 重新启用输出后 | 软件重新启动，两屏均被列出、DDC/CI 均可读 | 与现场重新启用 AOC 后恢复的状态一致 |

## 源码审计

对 `OLED-Sleeper` 和 `tools` 的 C#／PowerShell 源码检查（包含未提交文件，排除构建产物）：未发现 `SetDisplayConfig`、`ChangeDisplaySettings*`、`DisplaySwitch`、NvAPI、PnP 禁用、显示拓扑注册表写入、显示器电源 VCP 0xD6 或 `SC_MONITORPOWER` 调用。

- `Features/MonitorBlackout/Services/MonitorBlackoutService.cs`：创建／关闭 WPF 遮罩窗口。
- `Features/MonitorDimming/Services/DdcCiSession.cs`：唯一硬件写入控制码是亮度 0x10；本次 AOC 遮黑设置未启用该写入。
- `Features/ColorTemperature/WindowsGammaBackend.cs`：色温写入仅为 `SetDeviceGammaRamp`，显示拓扑使用只读查询；资格与恢复要求作用域匹配。
- `Features/ColorTemperature/ColorTemperatureService.cs`：连接／模式变化失去资格时停止策略写入，外部颜色冲突保留记录，不恢复未知颜色。

接口用途不同不代表能完全排除驱动交互。微软明确指出 Gamma 会被连接／断开、分辨率变化和其他颜色管理操作重置；因此本次颜色冲突也可能是显示变化的结果，而非原因。[微软 Gamma 文档](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdevicegammaramp)

## 已执行检查与限制

执行 `rg --no-ignore` 源码检索、`Get-WinEvent`（System、Application、Kernel-PnP、DeviceSetupManager）、`Get-CimInstance`（操作系统、显卡、显示连接）、`powercfg /query SCHEME_CURRENT SUB_VIDEO`、`powercfg /query SCHEME_CURRENT SUB_SLEEP`、`powercfg /lastwake`，并读取产品必要配置字段与日志。

Application 中有重复的 LiveKernelEvent 141 报告，但它们引用相同报告 ID 与同一份历史 WATCHDOG 转储，不能当作本次间隔内发生多次新 GPU 超时的证据。尝试只读报告时间字段时受 Windows 权限限制，未读取转储内容或修改权限，原始故障发生时间未进一步确认。部分详细显示诊断通道未启用，现有日志没有记录“哪个进程停用了哪个输出”的归因信息。

本次只增加文档。没有改变显示器设置、驱动、Gamma、恢复记录、程序配置或运行状态，没有复现关电源／拔插操作，没有新的构建或单元测试。

## 最小对照方法（尚未执行）

1. 在方便观察时从托盘完整退出 ScreenKeeper，保持线材、端口和显示设置不变，按相同方式关闭／再次打开显示器电源。若再次黑屏，先查看 Windows／NVIDIA 的显示器启用状态，再做拔插，记录时间。
2. 若软件未运行时仍能复现，说明问题可以在没有 ScreenKeeper 持续操作的情况下发生，应优先检查 HDMI／显卡驱动／显示器开关电源后的检测。一次未复现不等于排除。
3. 若多次只在软件运行时复现，再分开比较色温关闭与开启；不要同时改线材、驱动和多个设置，否则无法判断因素。调色测试必须人工观察，不能自动进行。现有恢复冲突仍按保护规则处理，不删除旧记录强制启用。

AGENTS.md 与 README 已审查：没有新的已确认产品规则或架构变更，本次无需更新。保留工作区已有改动，未提交或推送。
