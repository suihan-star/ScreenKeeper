# 2026-10-13 · 过夜后软件未运行与 AOC 输出未启用

关联 [018 设计](018-crash-color-investigation-design.md)、[018 计划](018-crash-color-investigation-plan.md)；历史相似现象见 [先前诊断](011-aoc-display-diagnosis-2026-09-23.md)。公开记录使用相对事件顺序，不保留真实用户的作息、登录时刻或转储文件中的个人时间线。

## 结论

本次没有发现构建 20261009.3 自行崩溃的证据。指定目录的程序先有序退出，随后 Windows 关机并再次启动；现场确认这是主动重启。本产品的开机启动项不存在，该目录之后没有新启动日志，排查时也没有 ScreenKeeper 进程。因此再次使用时软件不在运行，符合重启后未再打开的情况。

现场还确认副屏在重启后继续正常使用，经过一次关屏间隔后才发现黑屏；AOC Q27P10 使用 HDMI，在 NVIDIA 控制面板重新勾选后恢复。现有证据不支持“ScreenKeeper 在该间隔中崩溃或持续运行时停用副屏”。仍无法确定关开显示器电源后的连接、HDMI 握手或驱动恢复中哪个环节导致输出未启用；不能仅凭静态源码断言不存在任何历史或间接影响。

NVIDIA 的复选框用于启用不活动的显示输出，所以重新勾选后恢复说明当时输出未启用，但不说明是谁改变了状态。[NVIDIA 官方说明](https://nvidia.custhelp.com/app/answers/detail/a_id/3148)

## 核查范围

- 用户指定 `artifacts/color-fix-preview-20261009.3/release/ScreenKeeper-1.0.1-alpha.0.1-Test-20261009.3-Portable-x64/ScreenKeeper`。VERSION.txt 为 1.0.1-alpha.0.1／20261009.3；7 个 EXE／DLL／config 的 SHA256 与冻结构建 payload 一致。
- 该目录只保留本次重启前使用周期的日志，从启动开始并以正常退出结束；修复版投入测试后，Application 1000／1001／1002／1026 未查到 ScreenKeeper 新报告，现有 ScreenKeeper 转储和最新 WER 崩溃报告仍属于较早的修复前调查。
- 当前 Windows 11 build 26300；本次系统启动位于上述应用有序退出之后。HKCU Run 的 ScreenKeeper 值及对应 StartupApproved 值均不存在。没有改动注册表或自动启动程序。
- 当前 NVIDIA GTX 1650 驱动 32.0.16.1088；SetupAPI 显示其安装早于本次重启，不能说是此次重启时刚升级，也不能仅凭驱动变化归因本次问题。先前诊断已出现相似 AOC 现象。
- 交流电的自动关闭显示、睡眠、休眠时长均为 0；这不涵盖显示器自身的电源按钮行为。

## 关键时间线

| 相对顺序 | 证据 | 解释与限制 |
| --- | --- | --- |
| 本次应用启动前 | AOC 的 Kernel-PnP / Device Management 1010：总线报告缺少，设备被意外删除 | 早于本次指定目录启动；不说明具体操作人或断开原因 |
| 重启前的显示变化阶段 | 应用确认显示变化，重新扫描后仍有 AOC／DELL 两屏，DDC 探测成功 | 应用在观察显示变化后重新枚举 |
| 随后的枚举阶段 | 枚举新增第三显示目标；AOC DDC 探测失败，但仍列于 3 个活动显示目标中 | 通信失败不是禁用输出证据；新增目标有物理电视型号，不能仅凭标识将其称为虚拟屏 |
| 上述枚举阶段内 | AOC 色温恢复记录更新为冲突，无待写值 | 保存策略 AOC 开启、另外两屏关闭；不能由更新时间推断是谁改色或何时停用输出 |
| 系统重启前的应用退出阶段 | 清理遮罩、停止调度、恢复检查、Application Exiting 完整日志链 | 属有序退出；应用未记录入口来源，单看应用日志不能区分托盘退出与系统结束会话 |
| 随后系统结束会话 | USER32 1074：winlogon.exe 代表 SYSTEM 请求关闭电源，原因码 0x500ff；会话注销 | 结合现场确认，属于主动重启过程；未发现此窗口内 Windows Update 发起重启的证据 |
| 关机及再次启动 | Kernel-General 13 关机、12 新启动 | 不能把观察间隔描述为始终未发生系统重启 |
| 新启动阶段 | Kernel-PnP 219：第三方虚拟显示设备的 WUDFRd 加载失败 | 设备归属为虚拟显示驱动，不是 AOC；排查时该设备状态正常，不足以解释副屏掉线 |
| 后续关屏间隔中 | DELL 的 PnP 1010：总线报告缺少 | 当时 ScreenKeeper 已退出；记录对应 DELL，不能误写为 AOC |
| 再次使用阶段 | 现场发现 AOC 黑屏，NVIDIA 勾选后恢复 | 故障发生的准确起点和取消活动输出者未知 |
| 发现故障后的处理阶段 | AOC 的 PnP 1010：总线报告缺少 | 位于发现后的处理时段，不能当成初始故障起点或软件操作证据 |

排查时 AOC 已存在且设备问题码为 0。新增第三显示目标当前未连接，PnP 记录的父设备为 NVIDIA PCI 显卡；另一虚拟显示设备属于独立的第三方驱动。设备名称与父设备只能描述枚举关系，不能据此证明某款远控工具导致了 AOC 故障。

## 图形错误报告去重

本次调查范围内，WER 反复提交两组历史报告：141 共 38 条，引用同一报告 ID 与同一份历史 WATCHDOG 转储；193 共 32 条，引用另一相同报告 ID 与另一份历史 WATCHDOG 转储。不能把这些重复提交当作本次关屏间隔中的几十次 GPU 新故障。

193 表示由图形内核触发的实时转储，并不等于发生过系统蓝屏。[Microsoft 说明](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0x193--video-dxgkrnl-livedump)。WATCHDOG 目录读取被 Windows 拒绝访问；未修改访问权限、复制或解析转储，不能进一步指定故障驱动或业务调用栈。

## 源码核对

关键生命周期、自启动、显示枚举、亮度及色温源码与 20261009.3 快照核验一致：

- App.xaml.cs 的 SessionEnding 响应系统注销／关机并执行 ShutdownApp；锁屏路径只更新暂停状态，不退出。
- StartupHelper 使用当前用户 Run 的 ScreenKeeper 值启动当前 EXE；本次值不存在，未启用开机启动。没有自动将便携版加入启动项。
- 应用与工具源检索未发现 SetDisplayConfig、ChangeDisplaySettings、NvAPI、DisplaySwitch、PnP 禁用、显示器电源 VCP、创建虚拟屏或关闭／重启 Windows 的入口。
- 显示硬件写入口为亮度 VCP 0x10 与 Gamma。AOC 当前休息配置日志明确“不改硬件亮度”；软件遮罩只能覆盖桌面内容，不直接取消 NVIDIA 活动输出。

## 已执行验证与后续

已执行 Get-WinEvent（Application、System、Kernel-PnP、DeviceSetupManager、WindowsUpdateClient、会话日志）、Get-CimInstance／Get-PnpDevice 及必要设备属性查询、powercfg /query、Get-FileHash、SetupAPI 相关安装段和源码 rg 检索。部分不存在的日志查询、WATCHDOG 访问返回错误，已保留边界；没有将缺失证据写成检查通过。

本次只更新任务文档，不更改生产代码，不运行构建或单元测试；未启动／终止 ScreenKeeper，未更改显示、Gamma、驱动、自启动或用户 Data。

建议先在正常使用的新版本中按需开启「通用设置 → 开机启动」，解决主动重启后的自动运行需求。副屏问题单独观察：下次出现时先记录时间和 NVIDIA 启用状态，再进行拔插等恢复；只改变一个因素做对照，例如换一根现有 HDMI 线或另一个可用接口，不同时更换驱动和多项设置。本次没有实际执行硬件对照，也没有确认副屏问题已修复。

已检查 AGENTS.md，本次无需更新：没有新的已确认产品规则或架构变化，一次性现场证据保留在本诊断与关联设计／计划。未提交或推送。
