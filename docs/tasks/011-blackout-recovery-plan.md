# 011 · 黑屏恢复与退出遮罩清理计划

关联 [011 设计](011-blackout-recovery-design.md)。

1. 只读检查进程、事件顺序及必要配置，确认用户已通过拔插恢复，保留真实设置不操作。
2. 先增加异步排队遮黑的回归场景，再传递空闲复核到 UI 执行点，拒绝过期请求及后续调暗。
3. 增加遮罩终止生命周期及隐私释放后的拒绝逻辑，启动器先清理遮罩后恢复硬件和显示结果；核验重复退出及延迟请求。
4. 独立输出构建，运行相关与完整回归及相关文件格式检查。记录实际结果、未复现部分和 AGENTS 审查，不提交／推送或自动重启软件。

## 实施结果（2026-09-22）

用户进一步确认是先黑屏，再尝试退出，因此不能用退出提示遮挡解释初始故障。新增模拟显示器查询延迟测试，使用真实空闲命令处理器、遮黑处理器和遮罩服务（只创建 FakeOverlayWindow）：先完成唤醒，再让旧查询返回，原实现两种亮度选项下均残留黑窗，2 项失败，记录 before-fix.trx。这稳定证明代码时序缺陷，但不等于取得此次实机的线程证据。

- ApplyBlackoutOverlayCommand 传递 StillIdle 和拒绝结果；MonitorBlackoutService.TryShowBlackoutOverlayAsync 在 UI 执行点复核并返回是否接纳。取消后不创建遮罩、不继续调暗，空闲检测重新等待。ReminderDisplayGate 的遮黑标记改为实际接纳后设置。
- 遮罩 Stop 同步清理全部自有窗口和句柄并拒绝迟到请求；隐私服务 Dispose 可重复调用，取消在途操作、清空状态并拒绝重新配置／显示。普通唤醒仍不解除隐私幕。
- ApplicationBootstrapper 退出确认后先清理软件遮罩，再等待硬件恢复；恢复提示显示于屏幕中央并可从任务栏找到，增加清理和提示的固定阶段日志，避免第二次退出重入。保留原色恢复所有权检查、恢复日志与原有硬件等待上限。

## 实际验证

- 编写复现测试时首次编译因缺少 ActivityReason 参数失败，修正夹具后运行原代码，2 项按预期暴露黑窗残留。修复后 `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/BlackoutRecovery/ --filter 'FullyQualifiedName~MonitorBlackout|FullyQualifiedName~MonitorPrivacyTests|FullyQualifiedName~MonitorBehavior|FullyQualifiedName~ApplicationShutdownTests'`：83 项通过，无失败／跳过。
- `dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore -p:BaseOutputPath=bin/BlackoutRecovery/`：564 项通过、0 失败、2 项原生桌面测试按门禁跳过，总计 566。记录 artifacts/blackout-recovery-tests/full-regression.trx。
- 修正导入顺序后最终重新编译并运行 6 项新增场景，全部通过，记录 final-targeted.trx。覆盖延迟查询、UI 排队、关闭两个遮罩、终止前后迟到显示、提醒占用、隐私操作取消、重复退出及启动器在硬件等待前清理遮罩的顺序。
- 对本轮 11 个 C# 文件执行 `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include ...` 通过；`git diff --check` 通过。现有 NU1900（漏洞数据源无法访问）警告仍在，未改动依赖。

交付：OLED-Sleeper/bin/BlackoutRecovery/Release/net8.0-windows/ScreenKeeper.exe。没有启动真实策略、写入显示器、拔插设备、重启软件或修改用户配置；真实 AOC 黑屏、驱动状态与原生退出提示仍需观察新版，不能宣称已完全重现现场或排除硬件／驱动问题。

011 设计与计划、README 已同步；已检查并更新 AGENTS.md，记录实际 UI 执行点复核、退出先清理遮罩和迟到请求隔离原则。保留已有未提交修改，未提交／推送。

## 2026-09-23 只读排查实施

按照关联设计新增的“关屏后输出未启用”范围，已完成当前源码的显示配置写入路径审计，核对运行中的 RecoveryArchiveFix 版本、应用日志、保存设置、Windows System／Application／Kernel-PnP 记录、显卡与连接信息及电源设置。结果和未确认项见 [排查记录](011-aoc-display-diagnosis-2026-09-23.md)。

没有改动产品源码或用户设置，因此未执行新的构建或单元测试，不能将此前测试作为此次硬件故障的复现证明。仅补充本设计／计划与现场记录；已审查 README 和 AGENTS.md，现有事实仍适用，没有新的已确认架构或长期约束，本次无需更新。未提交、推送、重启软件、升级驱动或进行真实改色。
