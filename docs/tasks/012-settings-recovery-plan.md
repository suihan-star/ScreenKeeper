# 012 · 设置直接展示与色温恢复入口计划

关联 [012 设计](012-settings-recovery-design.md)，取代 008 中这两处「更多设置」折叠安排，并补充 010 的只读重建边界。

1. 已完成只读核查：两屏开关关闭、AOC 旧恢复记录与当前模式不一致；不更改真实记录或显示器。
2. 修改隐私和健康 XAML，保留绑定，仅调整标题、分隔和展开形式；同步渲染夹具。
3. 为模式变化后的只读重建增加回归；采集当前匹配物理屏幕的精确作用域，保持确认时重新复核、先归档再撤资格与日志的顺序。
4. 处理验证入口的跨屏阻塞说明、重建确认框状态，加入双语资源；构建后运行相关回归、WPF 截图与范围格式检查，必要时完整回归。
5. 更新设计实施结果与 AGENTS，交付独立输出程序；保留工作区已有变更，不自动重启、不提交或推送。

## 实施与验收（2026-09-22）

- 隐私更多设置改为上下横线分隔的常显区域；移除原有代码隐藏的折叠控制。健康提醒节奏移除“工作与休息”标题，减少打扰与更多设置用横线分区，保留原有绑定与保存行为。
- `ColorTemperatureService.BaselineReset` 在只读准备阶段识别唯一的同一物理屏幕，再使用当前作用域采集和复核；旧记录完整留在归档内容中。新模式在观察或确认中再次改变、颜色改变、屏幕身份冲突、HDR／复制／断开仍拒绝。
- 验证弹窗显示具体待处理屏幕及“处理恢复记录”入口；重建弹窗在只读检查通过后才展示确认项，失败显示原因与重新检查。双语文案同步。
- 回归首先在旧实现上复现同屏重连后无法准备确认的失败；修正后相关 80 项测试通过。完整回归 573 项：571 通过，2 个需要交互桌面的原生窗口用例按既有规则跳过。范围格式检查通过（工作区加载警告仍存在）；构建只有已有 NU1900 漏洞源访问警告。
- 核验 WPF 截图的中英文、静绿深色／雾白绿、1000×736 与 700×500 布局，以及重建阻塞／可确认、验证恢复入口状态；滚动与固定底栏正常。截图仅使用虚构屏幕信息。
- 当前实机只读检查：链接本次生产源码，以禁止任何设备写入、存储写入和归档的适配器读取真实配置副本。两屏已保存开关关闭；对 Q27P10 旧记录执行准备并立即取消，返回 `Ready=true`，提示当前连接／显示模式已变化且只读检查完成。设备写入 0、存储写入 0，检查前后配置与恢复文件校验一致。这证明当前状态可到观察确认，不证明实际调暖效果或原色视觉正确。

验证命令（隔离输出避免替换正在运行的客户端）：

```powershell
dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/SettingsRecovery/ --filter 'FullyQualifiedName~ColorBaselineResetTests|FullyQualifiedName~ColorValidationTests|FullyQualifiedName~ColorLiveSettingsTests|FullyQualifiedName~SettingsLayoutTests|FullyQualifiedName~WellbeingSettingsTests' --logger 'trx;LogFileName=targeted.trx' --results-directory artifacts/settings-recovery-tests
dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore -p:BaseOutputPath=bin/SettingsRecovery/ --logger 'trx;LogFileName=full.trx' --results-directory artifacts/settings-recovery-tests
dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/Features/ColorTemperature/ColorTemperatureService.BaselineReset.cs OLED-Sleeper/Features/ColorTemperature/ColorTemperatureService.Validation.cs OLED-Sleeper/UI/Views/ColorBaselineResetWindow.cs OLED-Sleeper/UI/Views/ColorValidationWindow.cs OLED-Sleeper/UI/Views/PrivacySettingsView.xaml.cs OLED-Sleeper.Tests/Features/ColorTemperature/ColorBaselineResetTests.cs OLED-Sleeper.Tests/Features/ColorTemperature/ColorValidationTests.cs OLED-Sleeper.Tests/UI/ViewModels/ColorLiveSettingsTests.cs OLED-Sleeper.Tests/UI/ViewModels/SettingsLayoutTests.cs
```

渲染时 `SCREENKEEPER_RENDER_QA=artifacts/settings-recovery-qa` 的绝对路径；TRX 与只读临时探针保存在被忽略的 `artifacts/`。交付输出：`OLED-Sleeper/bin/SettingsRecovery/Release/net8.0-windows/ScreenKeeper.exe`。

未执行真实记录重建、资格撤销或 15 秒调色；需要用户在新客户端确认当前 Q27P10 画面正常后处理记录，再逐屏观察验证。没有修改已安装配置、自动启用调色或重启运行中的客户端。设计、README 与 AGENTS 已同步常显分区和只读重建边界；无 schema 变更，无提交、推送或发布。

## 备份失败补修计划（2026-09-22）

承接设计中的“备份失败补修”，范围为恢复归档存储及其集成回归。

1. 在生产 `ColorRecoveryFileStore` 上复现旧／新模式不同导致确认时归档失败；检查写入失败仍保留恢复保护。
2. 将归档证据配对改为完整、唯一的物理身份匹配，保留两份不同作用域，不放宽设备写入与最终观察复核。
3. 使用独立临时目录执行准备→确认→真实持久化归档→撤销资格→更新恢复日志→重新加载的完整链路；覆盖真实存储故障，断言无设备写入。
4. 运行相关回归与构建，审查 AGENTS 并补充长期边界；交付新的隔离输出，不自动重启或更改正在运行的客户端配置。

### 补修结果

- 根因确认：生产归档校验使用 `SameScope`，在写文件前已拒绝，因此本次日志中没有对应文件写入异常，也没有新增归档。与磁盘权限或其他调色软件无关。
- `ColorRecoveryFileStore.ValidObservation` 按唯一物理身份配对旧记录和新观察，仍要求完整的当前作用域、合法颜色、无缺失／重复屏幕。旧记录与当前观察分别深复制保存；未改变恢复及验证服务对设备写入的门禁。
- 新增 12 个回归实例。旧实现上 3 项失败，其中生产存储完整流程准确复现“旧原色记录备份失败，未解除恢复保护。”；修复后色温与存储相关 179 项全部通过，无跳过。真实临时磁盘测试覆盖归档 JSON 回读、旧颜色／作用域保留、资格撤销、活动日志及其旧备份、重启读取和真实文件写入失败。所有设备接口仅为只读模拟。
- 另使用本机恢复／资格文件的独立副本及本次生产程序集，完整运行准备、确认、归档及日志更新；设备后端禁用所有写入，文件适配器禁止访问临时目录之外。结果：准备与确认成功、副本目录生成 1 个归档、旧作用域保留、旧记录退出活动状态、资格撤销且要求重新验证。设备写入 0；真实设置、恢复记录与资格文件前后校验一致。此项不会代替用户在实际客户端确认当前画面，也不证明真实调暖效果。
- 范围 `dotnet format --verify-no-changes --no-restore` 通过；隔离构建成功，仍有已有 NU1900 漏洞源访问警告。此次没有界面改动，不重复之前的截图验收；没有重新运行无关模块的完整回归。

```powershell
dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/RecoveryArchiveFix/ --filter 'FullyQualifiedName~Features.ColorTemperature|FullyQualifiedName~AppDataFileStoreTests|FullyQualifiedName~IndependentSettingsStoreTests' --logger 'trx;LogFileName=targeted.trx' --results-directory artifacts/recovery-archive-tests
dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/Features/ColorTemperature/ColorRecoveryFileStore.cs OLED-Sleeper.Tests/Features/ColorTemperature/ColorRecoveryFileStoreTests.cs OLED-Sleeper.Tests/Features/ColorTemperature/ColorBaselineResetPersistenceTests.cs
```

交付修复版：`OLED-Sleeper/bin/RecoveryArchiveFix/Release/net8.0-windows/ScreenKeeper.exe`。本机副本探针与 TRX 位于被忽略的 `artifacts/`。已更新关联设计、计划和 AGENTS；README 已核查，既有行为说明仍适用，无需修改。未改用户配置、未自动重启、未提交或推送。
