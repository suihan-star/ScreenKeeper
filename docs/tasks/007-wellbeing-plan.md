# 007 · 健康提醒与色温调节开发计划

日期：2026-09-17。对应 [任务设计](007-wellbeing-design.md)，依据 [竞品研究](007-wellbeing-research.md)。

## 2026-09-18 旧原色重新建立修复

关联设计同名修复；范围为恢复归档、资格撤销、明确确认的重建流程与入口。用户配置和真实显示器只读检查，不提交、不推送。

- [x] 耐久归档与批量撤销指定资格，保留旧恢复证据和失败阻塞。
- [x] 串行只读检查、稳定快照及单次确认凭据；恢复门禁、失效条件与零硬件写入测试。
- [x] 就近重建入口、中英文确认弹窗、资格更新及再次验证流程。
- [x] 测试、格式、渲染、便携输出，更新 README 与 AGENTS.md。

顺序：存储／资格 → 服务 → UI → 验证与交付。回滚保留归档和日志，旧程序不会自动读取归档；不把历史调色结果自动授予资格。

本次证据与验证：

- 用户已退出 f.lux；只读进程检查为 0 个实例，色温已保存为关闭。Q27P10 与 DELL 当前 768 个 Gamma 样本均等于线性曲线，而旧原色各有 511 个样本不同；中点旧值为 R/G/B = 32896/28712/24640，当前为 32896/32896/32896。旧原色确实偏暖，符合外部调色期间被采集的情况；不能要求当前线性输出回到旧暖色后才放行，也不能仅据线性曲线断言物理画面或专业校色正常。
- 实现：`ColorTemperatureService.BaselineReset.cs` 隔离只读准备、单次确认与归档／撤销／活动日志更新顺序；`ColorRecoveryFileStore.cs` 耐久保存随机唯一归档，`ColorValidationRegistry.cs` 批量撤销限定身份的资格；`ColorBaselineResetWindow.cs` 展示处理范围及明确观察确认；设置页和协调器提供入口。`ColorTemperatureContracts.cs` 的可选 ValidationRequiredScreens、发现与验证确认逻辑，防止旧资格备份复活；`tools/ColorProbe/ColorProbe.csproj` 同步链接新服务分部文件。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore --filter 'FullyQualifiedName~ColorTemperature|FullyQualifiedName~ColorLiveSettings|FullyQualifiedName~WellbeingSettings'`：143 通过；最终 `dotnet test OLED-Sleeper.sln -c Release --no-restore --verbosity quiet --logger 'console;verbosity=minimal'`：**477 通过、0 失败、2 跳过，共 479**，包含 Release 构建。两个跳过仍为未开启的原生应用窗口用例和前台不可用的健康卡片焦点用例。
- 新增覆盖外部调色旧基线、保留不相关资格、归档先于资格撤销和活动日志更新、稳定性／模式／断连／HDR／读取异常／过期／取消／锁定／排队设置改变／不可用资格文件／各持久化失败、零硬件写入、深复制、未知确认标记阻断及新试验后逐屏移除标记。UI 测试确认未勾选观察项无法执行，列出两屏，只有点击确认才归档；假后端未发生设备写入。
- WPF 中英文重建弹窗、选屏入口及既有最小窗口渲染位于 `artifacts/color-baseline-reset-qa`，已目视核验标题、说明、屏幕列表、复选框及按钮换行。新增源码／XAML 文案的英文资源检查无缺失。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes --verbosity quiet` 通过；`dotnet build tools/ColorProbe/ColorProbe.csproj -c Release --no-restore --verbosity quiet` 成功，0 警告、0 错误。未执行真实重建或调暖，用户需在新版中观察并确认。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --no-restore --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/color-baseline-reset-portable/ScreenKeeper --verbosity quiet` 成功，EXE 为 68,527,441 字节。与原便携目录的 7 个支持文件哈希一致，将新 EXE 另存为 `artifacts/wellbeing-scenes-portable/ScreenKeeper/ScreenKeeper-color-reset.exe`，复制后哈希一致，未覆盖现有 EXE／数据或自动启动。NU1900 仍表示 NuGet 漏洞数据源不可达，审计未完成。`git diff --check` 通过。
- 已同步 README、设计与计划；已审查并更新 AGENTS.md，明确人工确认的重建例外、归档不自动恢复与强制重新验证标记。未提交、推送或发布。

## 2026-09-18 验证与试调入口反馈修复

关联设计同名修复；保留当前工作区变更与用户恢复记录，不自动实机改色。

- [x] 选屏与可试调分离，就近显示原因、验证和重新检查入口。
- [x] 验证前置条件显式反馈，只读核对原色后安全解除已恢复的日志阻塞。
- [x] 状态／恢复边界测试、中英文渲染、构建与便携输出。
- [x] 同步 README、设计／计划及 AGENTS.md 维护审查。

顺序：服务门禁／只读恢复 → 界面与双语 → 自动验证 → 试用输出。回滚不改变恢复日志格式；没有将历史测试授予 DELL 生产资格。

本次证据与交付：

- 只读检测：两屏均为 SDR 扩展、Gamma 可读，身份／拓扑／模式仍与恢复记录匹配；Q27P10 有匹配资格，DELL 没有。已保存的色温总开关开启，恢复文件两屏均为 Conflicted，当前 Gamma 与原基线各有 511 个采样点不同。确认程序拒绝接管的条件成立，无法从这些数据确定具体哪个工具或系统事件改变了颜色；没有删除或改写这些真实记录。
- 根因：试调条目直接按 CanPreview 禁用；验证开始前未显式展示已保存总开关与恢复门禁；冲突记录即使原色恢复也没有用户重查入口。渲染另发现 DisplayMemberPath 在当前模板的选中展示中退回内部类型名，改用本页显式 ItemTemplate。
- 主要文件：`ColorSettingsViewModel.cs` / `.Preview.cs` 与 `ColorSettingsView.xaml` 分离选屏和试调、显示原因及入口；`ColorValidationWindow.cs` 提前展示前置条件；`ColorTemperatureService.Validation.cs` 集中原因与只读原色重查；`WellbeingCoordinator.cs` 转发门禁、重查结果，恢复异常优先于“已验证”提示。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore --filter 'FullyQualifiedName~ColorTemperature|FullyQualifiedName~ColorLiveSettings|FullyQualifiedName~WellbeingSettings'`：116 通过。最终 `dotnet test OLED-Sleeper.sln -c Release --no-restore --verbosity quiet --logger 'console;verbosity=minimal'`：**450 通过、0 失败、2 跳过，共 452**，包含 Release 构建。跳过项目仍为未显式开启的原生窗口用例和当前会话前台不可用的焦点用例。
- 新增测试证明：未验证与冲突屏幕能选中且不能调色；验证条件入口可打开；保存策略开启时开始前显示原因；重查只清理目标屏幕，精确原色、旧会话及当前会话冲突可解除，外部颜色／原已应用暖色／模式改变／读取失败／持久化失败／未知日志／已启用策略均不能被重查绕过。假后端确认重查不写硬件。
- 中英文 460 宽设置内容及验证弹窗渲染在 `artifacts/color-entry-fix-qa`，已目视核验选中屏幕名、状态、换行和验证阻塞提示；现有最小窗口回归也通过。相关源码和 XAML 中文文案均有英文资源。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes --verbosity quiet` 与 `git diff --check` 通过；`dotnet build tools/ColorProbe/ColorProbe.csproj -c Release --no-restore --verbosity quiet` 成功、0 警告、0 错误。工具只运行默认只读检测；另只读核对当前 Gamma，未运行真实调暖或重查写日志操作。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --no-restore --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/color-entry-fix-portable/ScreenKeeper --verbosity quiet` 成功。与现有便携目录的 7 个支持文件哈希一致，将 68,517,302 字节 EXE 另存为 `artifacts/wellbeing-scenes-portable/ScreenKeeper/ScreenKeeper-color-fix.exe`，复制后哈希一致；未覆盖旧 EXE、自动启动或安装。NU1900 表示 NuGet 漏洞数据源不可达，审计未完成。
- 已更新 README、任务设计、计划与 AGENTS.md（长期规则：可选屏不代表可写设备，禁用需解释，原色重查不可绕过所有权）。未提交、推送或发布。真实冲突仍保留，需要用户恢复原色后在客户端逐屏复核；实际 DELL 15 秒调暖与恢复本轮未重做。

## 2026-09-18 固定作息双目标与滑杆试调

关联设计同名增量。在当前目录保留已有变更，不提交、不推送。范围：色温模型／日程、单屏临时试调、设置 UI、协调与自动验证；不联动健康休息、不替换显示后端。

- [x] 可选工作目标、旧文件兼容、双目标固定作息与边界测试。
- [x] 即时单屏试调、最新请求合并、单调超时、按住原色对比和取消竞争防护。
- [x] 双语滑杆／整数输入／明确试调目标，草稿保存与放弃、页面生命周期及失败状态。
- [x] 相关测试、Release 构建、格式与 WPF 渲染，便携试用输出。
- [x] 同步 README、设计／计划与 AGENTS.md 维护审查。

顺序：模型和日程 → 串行服务与取消 → ViewModel／UI → 测试与渲染 → 文档和便携输出。回滚按设计先停用恢复，再退回旧程序；不改写用户当前配置，也不自动实机调色。

本次交付与验证：

- 模型与日程：`ColorTemperatureSettings.cs` 新增可选工作目标及原色开关；`ColorSchedule.cs` 按固定作息计算两组目标与过渡，旧 JSON 保持白天原色。`ColorSettingsView.xaml` 提供两条滑杆、整数输入、明确的试调屏幕和原色对比；`ColorSettingsViewModel.Preview.cs` 合并最新编辑、取消待执行请求并保留草稿。
- `ColorTemperatureService.cs` 保持设备串行访问和耐久恢复日志，单屏即时试调使用独立请求版本与单调 15 秒超时；原色对比保留所有权，避免外部改色被误记为新基线。`WellbeingCoordinator.cs` 同步 UI 与服务预览状态。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore --verbosity quiet --logger 'console;verbosity=minimal'`：**438 通过、0 失败、2 跳过，共 440**，包含 Release 构建。跳过原生应用窗口用例（未启用 `SCREENKEEPER_NATIVE_QA`）及健康卡片焦点用例（当前会话无法获取前台窗口）。新增覆盖双目标时段、反向作息、旧 JSON、单屏范围、最新请求、取消竞争、系统时钟变化、对比期间外部修改、写入失败与草稿保存／放弃。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes --verbosity quiet`：通过；`dotnet build tools/ColorProbe/ColorProbe.csproj -c Release --no-restore --verbosity quiet`：成功，0 警告、0 错误。未运行 ColorProbe 实机改色。
- `git diff --check`：通过；色温设置 XAML 的 29 个本地化键均存在英文资源。
- `SCREENKEEPER_RENDER_QA=artifacts/color-sliders-qa`：检查中英文 460/720 宽内容区、700×500/1000×736 主窗口的空屏及未验证状态；滑杆、数值、按钮换行及固定底栏无溢出。渲染使用虚构屏幕，不能证明实际 Gamma 效果或连续拖动的硬件响应延迟。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --no-restore --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/color-sliders-portable/ScreenKeeper --verbosity quiet`：成功，EXE 为 68,514,574 字节。NU1900 仍为 NuGet 漏洞数据源不可达，漏洞审计未完成。
- 核验 7 个配套 DLL／配置／LICENSE 文件与既有试用目录哈希一致后，将新 EXE 另存为 `artifacts/wellbeing-scenes-portable/ScreenKeeper/ScreenKeeper-color.exe`，复制前确认目标不存在，复制后哈希一致。该入口沿用原目录 `Data/ScreenKeeper`；没有替换旧 EXE、自动启动或改写用户配置。试用时从托盘退出旧版，再运行新入口；完整独立输出在 `artifacts/color-sliders-portable/ScreenKeeper`。
- 已同步 README、任务设计与计划；已检查并更新 AGENTS.md，沉淀双目标兼容、固定作息独立性、试调取消与原色所有权规则。未提交、推送、安装或发布。实际屏幕的拖动观感、按住对比及连续使用仍需用户试用确认。

## 2026-09-18 雾光动态与覆盖范围细化

关联设计首节的雾光细化补充；仅调整健康提醒场景、覆盖校验与设置控件，不修改色温和隐私行为。在当前目录保留已有变更，不提交、不推送。

- [x] 覆盖范围扩为 10–100%，保留默认 30%，同步双语校验和最大值保存/放弃测试。
- [x] 深绿多层雾光错速漂移与舒缓伸缩，尊重动态开关和系统动画偏好；离开场景释放动画。
- [x] 验证 30%/100%、开关动态、不同动画帧及中英文最小窗口，执行相关测试与构建，提供便携输出。

顺序：范围与场景实现 → 配置/渲染验证 → 本地便携输出与文档。回滚只撤回本次视觉改动；如退回上限 60% 的旧版，先在新版把覆盖调至不超过 60% 并保存，避免旧版校验拒绝新值。

本次交付与验证：

- 主要文件：`HealthSceneWindow.cs` 的深绿不透明底色、三层错速渐变雾光及卸载动画清理；`HealthReminderSettings.cs` / `HealthSettingsView.xaml` / `English.json` 的 100% 上限；`HealthAppearanceTests.cs` / `WellbeingSettingsTests.cs` 的范围、保存/放弃、动态帧及 UI 渲染验证。WPF 动画通过场景 Border 的实际 Background 属性寻址，保证变化作用于正在显示的画刷。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore --filter 'FullyQualifiedName~HealthReminder|FullyQualifiedName~WellbeingSettings'`：**61 通过、0 失败、1 跳过，共 62**。跳过项为当前会话无法获取前台窗口的原生焦点用例；没有运行其他模块全量回归。
- `SCREENKEEPER_RENDER_QA=artifacts/wellbeing-fog-qa`：已核验 700×500/1280×800 场景、中英文 700×500/1000×736 设置页。动画测试寻址实际 WPF 时间线的 0 秒/6 秒帧，确认启用时像素变化、停用时一致、100% 所有像素不透明及卸载后取消可控时钟。渲染只使用虚构内容，未观察用户实际显示器上的连续播放和性能。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes --include`（本次四份 C# 文件）：通过。`git diff --check`：通过。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --no-restore --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/wellbeing-fog-portable/ScreenKeeper`：成功，EXE 为 68,508,143 字节；NU1900 仍为 NuGet 漏洞数据源不可达，漏洞审计未完成。
- 当前旧便携程序仍运行。核验配套 DLL/配置/LICENSE 与旧目录文件哈希一致后，将新 EXE 另存为 `artifacts/wellbeing-scenes-portable/ScreenKeeper/ScreenKeeper-fog.exe`，与旧版共用原 `Data/ScreenKeeper`，未改写用户配置、替换运行中 EXE 或自动启动实例。试用时从托盘退出旧版，再运行此文件；完整独立包仍在 `artifacts/wellbeing-fog-portable/ScreenKeeper`。
- 已同步任务设计、计划与 README；已检查 AGENTS.md，本次无需更新：没有新增模块边界、存储方式或长期协作约束。没有提交、推送或发布。

## 2026-09-18 增量实施计划

关联设计首节“渐显提醒与设备验证入口”；在当前目录保留既有变更，不提交、不推送。

- [x] 健康外观配置与独立图片副本，草稿预览/保存/放弃及双语。
- [x] 分离输入穿透的渐显场景与操作卡，主动休息全屏场景，减少动画支持，预览不受全屏免打扰影响。
- [x] 色温生产注册表、串行受控测试、恢复复核和人工观察确认入口；异常不授予资格。
- [x] 相关自动测试、Release 构建、格式检查与 WPF 场景渲染；记录真实硬件未验证范围。
- [x] 更新 README、设计偏差及 AGENTS.md，生成可试用的便携输出。

依赖顺序：模型/存储 → 服务/原生边界 → 设置 UI → 自动验证/渲染 → 文档/便携输出。设备测试不自动执行；由客户端选定目标并开始。回滚按设计要求先恢复色温并保留恢复日志。

### 本增量交付与验证（2026-09-18）

- 外观由 HealthAppearance / HealthSceneWindow / HealthReminderPresenter 实现；内置场景为原生 WPF 矢量远山和月亮，不读取或模糊真实桌面。用户图片复用不可变导入实现，目录独立为 HealthImages。提醒背景输入穿透；明确开始休息后阻挡隐藏应用的鼠标输入并聚焦操作卡；无鼠标/键盘交互 6 秒后操作区淡出，重新交互恢复。
- 修复手动预览沿用全屏免打扰及手动暂停门槛的问题；预览支持进入示例休息场景，保留真实周期。实际开始休息先保留被点击卡片的屏幕，避免旧 HWND 关闭时暂时失去前台导致画面不出现。
- 色温原先的默认拒绝注册表由持久化资格注册表替代；新验证流程使用生产串行队列、原色日志、外部冲突及恢复逻辑。只有完成本次软件恢复复核和人工三项观察确认才能写入资格，确认票据一次使用、5 分钟有效；写失败、取消、模式变化不授予资格。取得资格后仍须勾选屏幕、启用并保存。验证入口位于设置页上方，未验证时隐藏无效的调色操作按钮。
- 恢复 API 与生产调色授权分开，严格匹配的原色恢复记录即使没有正式资格也能恢复，避免测试中断后被资格门槛挡住。HDR/复制/远程/自定义校色仍不在本次兼容承诺内。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore`：成功，0 错误；NU1900 为已有 NuGet 漏洞数据源不可达，漏洞审计未完成。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore`：**414 通过、0 失败、2 跳过，共 416**。新增试验资格测试覆盖刷盘先于写入、取消、停止、恢复失败、前次中断恢复、其他屏幕被影响、外部调色、模式变化、结果过期及资格保存失败；健康覆盖旧配置、外观草稿、输入样式和预览隔离。
- 额外设置 `SCREENKEEPER_NATIVE_QA=1`：414 通过、1 失败、1 跳过。失败是已有 `NativePlacementAndMinimization_WorkOnAnOwnedFixtureProcess` 在 EnumWindows 阶段失败；单独重试相同。只读探测为 EnumWindows=False、Count=0、GetLastError=0、ForegroundAvailable=False。没有修改此测试或原隐私实现来掩盖失败；当前执行会话下真实前台/窗口枚举交互仍未验证。
- `SCREENKEEPER_RENDER_QA=artifacts/wellbeing-followup-qa`：已生成并检查中英文、700×500/1000×736 设置页及 700×500/1280×800 场景、验证弹窗。仅使用虚构工作区。静态渲染不代替真实显示器上的渐显、焦点和低速动画体验测试。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes`：通过；只对本轮三份新修改文件修正格式。`git diff --check` 通过；新增 XAML 可解析且本轮 XAML 文案英文键无缺失。
- `dotnet build tools/ColorProbe/ColorProbe.csproj -c Release --no-restore`：通过，已补共享服务的 partial 文件引用。**没有运行任何真实调色命令，也没有把旧 DELL 观察结果写入用户的资格文件。**
- x64 自包含便携输出：`artifacts/wellbeing-scenes-portable/ScreenKeeper/ScreenKeeper.exe`，`dotnet publish ... -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none --no-restore` 成功；已核验 EXE、5 个 WPF 原生 DLL、配置和 LICENSE 存在。未启动新实例、未覆盖安装版或原便携配置。
- 已审查并更新根 AGENTS.md：沉淀分离场景/操作卡、手动预览边界、资格持久化及恢复入口规则；README 和关联设计同步。无提交、推送、PR 或发布。

状态：2026-09-17 用户已通过研究任务授权开发。研究记录保留；下列实施清单按本轮实际证据更新，不沿用 006 测试数作为本轮验证。

## 本轮实施清单

- [x] P0：完整读取 007 三份文档，核对当前工作区；采用可编辑首版默认值。
- [x] 核对构建与测试基线；基线 267 通过、1 原生用例跳过，构建成功；NuGet 漏洞服务不可达产生 NU1900。
- [x] P1 代码及只读探测：能力门槛、恢复／日程／短过渡与测试已实现；DELL 首次受控观察通过，正式资格验收未完成。
- [x] P2：独立模块存储、草稿、导航及当前模块保存；关闭到托盘保留草稿。
- [x] P3：健康状态机、轻提醒／倒计时、会话适配、托盘与隐私协调，自动测试和 WPF 核验完成；原生焦点行为保留会话限制。
- [x] P4 代码接入：能力、日程、预览及恢复状态完成；生产资格保持未验证，尚不能视为真实自动色温交付。
- [x] P5 软件检查：定向及全量测试、中英文与 700×500 核验、验收矩阵及 README／AGENTS.md。
- [ ] P1／P4 实机后续：正式服务约 60 秒观察（加入短过渡后较原方案略长），暂停／预览／Stop 恢复，以及异常退出／拓扑／外部调色验收。当前待用户方便观察时确认，未执行。

分工：健康核心和色温核心分别在独立 Features 目录推进，主集成负责共享存储、ViewModel、导航、生命周期及完整构建。健康可独立交付；色温实机验证未通过不得宣称完成。

## 1. 范围、依赖与顺序

单仓库，涉及 Features、Native、UI、Storage、测试和项目规则；不改上游历史、安装身份或发布流程。

```text
P0 设计评审与设备范围
 ├─ P1 色温后端与恢复验证 ─→ 能力通过 ─→ P4 色温最小版本
 └─ P2 独立模块入口／存储 ─→ P3 健康最小版本
P3 + 已通过的 P4 ─→ P5 集成验收、文档和回退验证
```

健康模块可独立完成，不等色温攻克所有兼容环境。技术预研未通过时不得用半透明橙色遮罩冒充色温，不把系统设置入口当作自动色温交付。

## 2. 当前已完成的研究工作

- [x] 识别仓库、工作树和已有未提交修改，读取适用规则、README、005／006 任务文档。
- [x] 核对现有输入计时、隐私优先级、亮度访问、拓扑、设置保存和恢复边界。
- [x] 访问 FadeTop HTTP 官网／教程／版本历史，f.lux 官方介绍／帮助／更新记录和微软 API 资料。
- [x] 区分产品官方说明、旧版本默认值、源码事实、设计推断和未验证事项。
- [x] 收到用户选择：轻提醒、主动开始倒计时；固定作息自动色温。
- [x] 收到实际环境：两块屏幕、未开启 HDR、编程及偶尔页面设计；优先验收双屏 SDR 与临时原色。
- [x] 创建本任务研究、设计和计划文档；保留其他选择为待评审。
- [x] 审查 AGENTS.md：本轮没有已实施的新长期事实，无需修改。

## 3. 最小可用版本

| 阶段 | 工作与交付 | 相对复杂度 | 完成门槛 |
| --- | --- | --- | --- |
| P0 | 确认默认节律、模块入口／保存范围；核验已确认双屏 SDR 环境的型号／显卡／接口及扩展或复制模式；把反馈同步到 007 设计 | 小 | 待决策项有明确状态；未验证兼容范围不当承诺 |
| P1 | 独立色温验证程序／测试夹具；比较候选后端，验证作用域、基线、写盘失败、外部竞争及恢复 | 高，主要不确定性 | 至少目标用户设备上的独立控制、实际变化、暂停／退出／异常恢复有记录；决定支持范围或暂停路线 |
| P2 | 独立导航、ViewModel 草稿、分模块原子存储、状态标签、中英文 | 中 | 保存失败保草稿、模块独立，旧配置不受影响 |
| P3 | 全局健康状态机、单调时钟、会话／电源／无输入暂停、轻提醒、稍后／跳过／倒计时、全屏与手动免打扰、托盘 | 中 | 设计 H01–H08、U01–U02 通过，未启用隐私也能运行 |
| P4 | 已验证设备的共享固定时间表、逐屏参与、手动模式、渐变／实际能力说明、限时原色、可撤销预览、恢复记录 | 高 | C01–C08 通过；没有支持声明超出硬件验证 |
| P5 | 与隐私／遮黑／亮度集成、混合 DPI 界面、异常恢复、迁移／降级和文档 | 中至高 | R01 与实机矩阵完成，失败项明确，不带硬件阻断问题发布 |

不估算日历工期：用户已确认当前不启用 HDR，但色温后端和具体硬件兼容性尚未验证。P1 结束后再按实际路线拆工时。

## 4. P1 必须回答的问题

1. 一次 API 写入实际影响哪些屏幕？扩展／复制／混合 GPU 下能否稳定映射？是否改变未选择屏幕？
2. SDR 下是否存在有效且可读回的原色基线？已有 ICC／调色软件参与时是否可以判断冲突？无法判断时如何禁用接管？
3. 能否安全平滑过渡，还是仅离散色温档位？是否触动 RGB gain／drive、硬件模式或其他校色设置？
4. 读取／写入失败、成功但无效、基线持久化失败是否能安全停止？所有硬件调用能否避免阻塞 UI 和输入回调？
5. 预览到期、暂停、退出、系统睡眠、拔插、HDR 切换、进程被强制结束后如何恢复？哪些情形必须交由人工处理？
6. 色温和现有亮度是否需要共用硬件 I/O 排队？只复用必要的并发控制，不把两者恢复记录合并。

P1 只对明确选定的测试设备操作，先记基线和人工恢复步骤。不得自行关闭用户 f.lux／夜间模式、改校准配置或终止用户程序；测试启动与强制结束仅针对测试程序。测试后恢复并核对设备状态。

## 5. 验证计划

### 5.1 自动化

- 假时间源：健康状态迁移、重试去重、持续输入最大等待、锁屏／睡眠／全屏／手动暂停、计时重启、时钟回绕；不得模拟无输入后断言“确实休息”。
- 纯日程计算：跨午夜、夜班、夏令时、时区改变、时间跳变、过渡重叠、暂停截止、睡眠跨多个阶段。
- 假后端／存储故障：基线首写失败不接管、同范围写串行、过期请求取消、逐屏部分失败、名称重分配、身份冲突、外部更改、异常恢复记录。可检测的读回异常用自动化覆盖；读回正常但视觉未生效由 P1 实机矩阵覆盖，不假设运行时能识别所有 Gamma 静默失败。
- 保存与迁移：旧文件默认关闭、新文件往返、未知版本不覆盖、备份恢复、离线屏保留、分模块保存失败和深复制。
- UI／集成：双语、700×500、空屏／加载／错误／不支持／恢复待处理、键盘焦点／减少动画；健康窗口与隐私／桌面／指定应用交互。

实施后优先按项目实际测试类运行定向测试，再执行现有门禁：

```powershell
dotnet restore OLED-Sleeper.sln
dotnet build OLED-Sleeper.sln -c Release --no-restore
dotnet test OLED-Sleeper.sln -c Release --no-build
dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore
```

`SCREENKEEPER_RENDER_QA` 用于现有 WPF 渲染 QA，截图只用虚构数据；`SCREENKEEPER_NATIVE_QA=1` 仅适用于项目已有、只操作测试自建窗口的桌面测试，不能当成已具备色温实机测试入口。色温测试入口及使用约束在 P1 确定后更新相关规则。

### 5.2 硬件与运行验收

| 场景 | 验证目的 | 发布要求 |
| --- | --- | --- |
| 用户实际主副屏、SDR 扩展、混合 DPI | 目标屏暖色、未选屏不变、UI 不抢焦点 | 支持声明内必须通过 |
| 笔记本内屏＋外屏、不同 GPU／接口 | 控制范围与恢复可重复 | 只发布已验证范围，其他显示未验证／不支持 |
| 复制屏、USB 扩展坞／DisplayLink、远程桌面 | 不能逐屏时准确拒绝或说明联动范围 | 首版不静默扩大写入范围 |
| HDR／自动颜色管理／ICC | 检测、阻止未验证接管，不破坏校色 | 未完成验证就保持不可用 |
| f.lux／夜间模式／其他加载器 | 外部覆盖时停止竞争、保留当前状态 | 不闪烁、不循环抢占；完全自动检测能力不得夸大 |
| 拔插、锁屏、睡眠、重启、日期时区变化 | 没有错误重放、错屏、重复提醒 | 不丢设置或隐私状态 |
| 进程异常终止、恢复日志失败 | 不遗失可用恢复线索，不能盲目覆盖外部状态 | 有可重复恢复或明确停止条件与人工路径 |

屏幕截图只能核验 UI，无法单独证明输出 Gamma／硬件色温；需要实际观察并记录显示模式、设备、驱动和后端。颜色数值只作为控制目标，不作为测得的白点。

## 6. 后续增强与暂不实施

**后续增强**：短／长休息合并调度、额外预设；可选择更明显的休息覆盖；前台应用色温例外；每屏独立暖色强度或曲线；经过验证的 HDR／复制屏／扩展坞支持；确有需求后引入日出日落模式。每项另写增量验收，不一次并入首版。

**暂不实施**：强制无法跳过的休息、健康评分／注视识别、活动排行、云端行为统计、自动定位、专业校色编辑器、智能灯联动、极端色域解锁、私有注册表接管 Windows 夜间模式、用橙色遮罩模拟色温。

## 7. 回退与长期文档维护

- 产品回退：健康关闭后取消窗口和待提醒；色温先停止调度、恢复仍持有的控制范围，再关闭；恢复失败保留记录并提示，不宣称恢复成功。
- 版本回退：降级前执行色温停止／恢复并备份新模块文件；旧版不读两个新配置文件，既有 settings.json schema 1 保持不变，不删除亮度和隐私数据。
- 代码回退：仅回退本任务实际变更，不重置整个工作区；本轮不提交，未来提交也只能包含用户授权的任务文件。
- 正式新增模块、服务边界、启动停止或测试入口后，更新根 AGENTS.md、README 与 007 设计／计划；不能继续保留已放弃的架构描述。
- 研究轮曾审查 AGENTS.md 且无需更新；本轮已新增模块，须同步实现边界及验证状态。

## 8. 前一研究轮的检查记录（历史，不作为开发验证）

- 已执行：`git -c safe.directory=<workspace> status --short`、`rev-parse --show-toplevel`、`worktree list`；只在命令中处理沙箱所有权差异，未修改 Git 全局配置。
- 已执行：`rg --files` 查找规则／说明，`rg -n -i` 检索健康／色温／Gamma／HDR 及对应代码、测试和任务文档。
- 已执行：直接 HTTP `Invoke-WebRequest` 读取 FadeTop 首页、教程、历史页，均 200；f.lux 和微软来源通过网页工具读取。
- 已执行文档检查：三份文档的 22 个本地链接、代码块配对、尾随空白检查通过；`git diff --check -- docs/tasks/007-wellbeing-research.md docs/tasks/007-wellbeing-design.md docs/tasks/007-wellbeing-plan.md` 返回 0。由于新文档未跟踪，另用逐文件检查覆盖了 Git 不会检查的新增内容。
- 已对比研究开始与交付前的完整 Git 状态：只新增本任务三份文档，已有变更状态未减少或替换。另经只读设计审查，修正预览恢复、Esc 作用域及 Gamma 静默失败验收边界。
- 未执行：构建、单元测试、格式门禁、WPF 渲染、竞品操作、真实色温／DDC／HDR／故障恢复。原因是本轮仅研究文档，没有业务实现；上述均不能标成已通过。

## 9. 开发实现与实际验证（2026-09-17）

本轮在现有未提交产品基线上新增代码，不提交、不推送、不安装或替换已安装实例。此前通用设置指定的独立配置说明仍保持移除。

### 实现范围

- `Features/HealthReminder`：默认关闭的全局周期，单调计时、会话／空闲／全屏适配、轻提醒／稍后／跳过／主动倒计时、预览隔离。自建卡片尊重减少动画；倒计时可收起，托盘悬停显示剩余时间。已运行周期保留，修改时长用于下一周期。
- `Features/ColorTemperature`：固定本地作息、手动模式、60 档日程输出、10 秒单调追赶、15 秒可撤销预览、能力门槛、后台串行设备 I/O、外部冲突检测、独立恢复记录。候选 Gamma 后端默认全部未验证，不产生生产写入；DDC 仅做能力研究，不将亮度能力当色温能力。
- `WellbeingCoordinator`、两个设置 ViewModel／WPF View、`MainViewModel.Wellbeing`：独立入口与当前模块保存、跨模块草稿、错误／离线／恢复待处理状态、作息时间轴、临时原色 1 小时或至下次恢复时间、双语及托盘入口。当前模块保存失败不清脏；退出逐模块保存，不是跨文件原子事务。
- `IndependentSettingsStore` 与可选 `TryWriteDurable`：普通模块按 schema 1 检查、合法备份回退、未知版本和无法确认的文件保留。色温恢复写先 Flush(true) 后原子替换；任何日志状态不明确时不接管。
- `ReminderDisplayGate`：隐私动作开始前收起健康卡；后台不同步等待 UI。恢复 handler 等待遮黑与亮度恢复，拓扑重建同样更新门槛；仍有亮度恢复记录时不允许健康提示。原宿主失效不会在同一轮移到另一屏。

### 软件验证结果

- `dotnet restore OLED-Sleeper.sln`：成功；NuGet 漏洞数据源 `api.nuget.org` 当前不可达，出现 NU1900，不将其表述为已完成漏洞审计。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore`：成功，0 编译错误；保留上述 NU1900 环境警告。MinVer 使用进程内 Git safe.directory 参数，未改全局 Git 配置。
- 定向健康／色温／模块存储与草稿测试曾通过 124 项，1 原生用例跳过；随后补充回归再执行全量。
- `SCREENKEEPER_NATIVE_QA=1`、`SCREENKEEPER_RENDER_QA=artifacts/wellbeing-wpf-qa` 下 `dotnet test OLED-Sleeper.sln -c Release --no-build`：**395 通过、0 失败、1 跳过，共 396 项**。
- 被跳过项是健康卡“不抢前台”的真实窗口用例：首次执行在 `card.Show` 之前，自建 fixture 就未能取得前台句柄；单独重跑一致。只读探针确认 UserInteractive=true、输入桌面 Default 可读，但 GetForegroundWindow=0。测试增加环境前置检查并明确跳过；卡片显示后的严格前台断言没有删除。当前不能声称真实焦点行为通过，也不能断言用户锁屏。
- 本轮真实 WPF 渲染检查覆盖中英文、1000×736／700×500、健康卡三种状态、新页面、空屏／检测／未验证／失败／恢复待处理。语言与保存栏保持可见，正文滚动。修正了独立 DataContext 导致两个页面重叠的绑定问题，并补条件可见性断言。截图仅含虚构数据，位于 `artifacts/wellbeing-wpf-qa/`。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 已通过；格式化只指定本轮涉及的 C# 文件，未全仓重写。

### 色温实机证据和未验证项

1. 只读设备探测：当前 Q27P10（DISPLAY13）和 DELL SE2723DS（DISPLAY14），均为非复制、AdvancedColorEnabled=false、Gamma 可读。CIM 路径被环境拒绝后改用 QueryDisplayConfig。Q27P10 的 DDC 能力调用失败；DELL 未报告色温档位，RGB Gain 可读 0–100、当前 100。没有执行 DDC 写入。
2. 用户明确选择 DELL 后，执行 `ColorProbe --test-dell15`，严格匹配该次稳定物理目标。先将基线和目标刷盘保存，再启动独立隐藏恢复看护，5000K 候选保持 15 秒，finally 恢复。进程退出 0；API 返回、目标读回和原色读回均匹配。
3. 用户确认：**DELL 变暖，另一屏不变，随后恢复正常**。证据文件 `artifacts/color-temperature-qa/dell-trial-01.json`；这只证明本次设备／拓扑／模式组合的这次观察，不证明其他设备或全部运行情形。
4. 后续正式服务观察工具已准备，含 10 秒渐变、15 秒预览、返回已保存策略、暂停、恢复和正常 Stop，约 60 秒，独立看护 90 秒。尚未得到本轮观察的回复，因此未执行。具体步骤及恢复约束见 `tools/ColorProbe/README.md`。
5. 未测：真实强制结束／故障恢复、拔插和 DISPLAY 交换、HDR／ICC 切换、外部调色竞争、整晚定时运行、混合 DPI 与第三方程序焦点。生产 `UnverifiedColorValidationRegistry` 保持关闭；不能把色温框架或第一轮观察称作完整色温功能已可发布。

### 验收矩阵

| 编号 | 状态及证据 |
| --- | --- |
| H01、H02、H04–H06、H08 | 状态机、假活动／时钟、保存失败与预览隔离测试通过；多屏现场长期使用仍需试用 |
| H03 | 无激活配置、局部 Esc、按钮语义与窗口渲染通过；真实前台行为因会话条件未验证 |
| H07 | 可用性门槛、隐私优先、宿主失效、倒计时不迁移等回归通过；真实复杂桌面竞争未覆盖 |
| C01 | 独立模块及设备门槛通过；DELL 单次 5000K 独立作用域观察通过；正式接管保持关闭 |
| C02、C03 | 跨夜／时点／重叠校验／墙钟跳变／单调追赶与保存规则测试通过，未跑整晚实机 |
| C04、C05 | 假后端的基线／持有权／预览／暂停／Stop／日志失败测试通过；实机仅首轮恢复通过 |
| C06–C08 | 假后端／身份／恢复日志场景通过；真实拓扑、外部竞争与故障退出未测 |
| U01 | 双语及两种尺寸渲染通过；减少动画遵循系统参数；原生焦点及混合 DPI 留待实机 |
| U02 | 切换、保存失败、独立放弃、离线偏好及恢复记录可见性检查通过 |
| R01 | 原有隐私／桌面／指定应用／亮度测试回归通过；未引入新输入钩子 |

README、007 设计／计划、根 AGENTS.md 已更新。新增模块、生命周期、文件布局与长期交互原则已沉淀；未验证的硬件资格仍明确保留，不沉淀为已支持事实。

### 最终本地交付

- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/wellbeing-portable/ScreenKeeper` 成功退出 0，保留 NU1900 警告。已核对 ScreenKeeper.exe、LICENSE 与 5 个 WPF 原生运行时 DLL；没有启动正式便携实例读取／改写真实产品配置。
- 最后一次 `dotnet format ... --verify-no-changes --no-restore` 与 `git diff --check` 均退出 0。XAML 解析、新页面静态英文资源覆盖、新文件尾随空白及通用设置指定文案移除检查通过。
- 便携测试入口：`artifacts/wellbeing-portable/ScreenKeeper/ScreenKeeper.exe`。应先退出旧实例，再运行新目录中的程序；保留整个目录及 Data，不单独移动 EXE。健康提醒和色温默认关闭，色温还受设备验证门槛限制。

## 2026-09-21 提醒卡与隐私幕文案调整计划

关联 007 设计的「提醒卡居中与按钮层级」，隐私文案沿用 006 的幕布交互。

1. 同步幕布实际按钮、设置内示意及中英文资源的「进入此屏幕」。
2. 复用健康卡局部深绿色板与现有控件模板，放大卡片并按目标屏幕区域居中；三类操作按实心／描边／文字区分，不改变动作语义与焦点规则。
3. baoyu-design 独立可点击稿展示居中提醒、隐私幕、三按钮操作以及六组正文候选；正文待用户选定再应用。
4. 更新现有渲染测试尺寸和隐私文字断言；执行相关测试并查看虚构数据截图，核验原型。
5. 更新本计划的实际验证结果并审查 AGENTS.md。不提交、不推送、不自动安装或改真实屏幕颜色。

### 本轮实际完成与验证

- 已修改 `HealthReminderWindow.cs` 的卡片宽度／留白／字号、基于 FullLeft/Top/Right/Bottom 的居中位置（使用各轴 DPI）、按钮层级；提醒保持 ShowActivated=false。隐私幕实际按钮、设置示意、English.json 同步「进入此屏幕」。
- 现有提醒渲染测试使用卡片真实宽度；场景合成截图的提醒和休息卡均居中。虚构数据中／英文单卡及 700×500 场景已目视核验，三按钮在同一行、无文字裁切。原有动作回调和周期不改。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore --filter 'FullyQualifiedName~HealthReminder|FullyQualifiedName~SettingsLayoutTests'`：59 通过、1 跳过、0 失败。跳过项为需显式启用的原生桌面前台不激活测试；本轮未验证真实多屏／混合 DPI 放置。渲染截图在 `artifacts/health-card-v2-render/`。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release --no-build --no-restore --self-contained false -o artifacts/health-card-v2-client` 成功，产物仅在本机，未安装或启动新实例。NuGet NU1900 漏洞索引联网警告仍在，不能声明依赖安全扫描通过。
- 独立 `health-card-v2.html` 及资产索引已更新。内联脚本 `vm.Script` 语法检查通过；浏览器已检查文案切换、休息倒计时、提前结束、隐私幕单按钮。原六组候选按用户提供的句子收敛为三组，仍待选择。
- 已更新 007 设计／计划及 README；AGENTS.md 维护审查后更新「进入此屏幕」和居中操作卡／按钮层级的已实施事实。未改色温 v6 方案，未修改真实配置、计时或 Gamma；未提交、推送或对外发布。

## 2026-09-21 确认正文与按钮形状实施计划

关联 007 设计同名增量。复用现有草稿与设计风格，在当前目录修改；先写确定正文和英文，再通过局部按钮圆角与两行布局区分操作结果，更新 baoyu-design v3 原型及资产索引。执行健康模块和主窗口布局现有测试，查看中英文卡片及小屏场景，并通过浏览器验证原型三个操作。采用 bin/HealthCardShapes/ 独立构建，避免覆盖当前运行程序，不修改真实配置、不提交或推送。开始前现有 HealthReminderWindowTests 基线 1 通过、1 原生前台测试按门禁跳过。

### 确认正文与按钮形状完成记录

- HealthReminderWindow.cs 与 English.json 已应用指定正文和三个结果提示；开始休息为 46 DIP 高、圆角 12 的整行主按钮，下方稍后为圆角 22 的胶囊、跳过为平面无框文字。Calm.xaml 增加 ButtonCornerRadius 默认 7，健康提醒按钮在自身资源局部覆盖，不改变其他页面的默认圆角。既有回调、计时、预览专属说明和窗口生命周期不改。
- baoyu-design 延续现有颜色／山林资源，新增 health-card-v3.html 并用 record-asset.mjs 登记，保留 v2 候选稿。原型三项动作分别呈现倒计时、稍后说明、跳过并新开周期说明，语言可切换；仅示例状态，不读取或操作产品配置。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/HealthCardShapes/ --filter 'FullyQualifiedName~HealthReminder|FullyQualifiedName~SettingsLayoutTests'`：Release 构建成功，61 通过、0 失败、1 原生前台测试按门禁跳过。结果 artifacts/health-card-shapes-tests/health-card-shapes.trx。没有为文案与圆角新增重复实现的测试，复用现有行为／布局测试。
- SCREENKEEPER_RENDER_QA=artifacts/health-card-shapes-qa 已目视核验中英文提醒卡和 700×500 场景，按钮形状与两行位置正确，文字与卡片未越界；其他主页面双主题渲染一并通过。未在真实多屏、混合 DPI 或实际工作周期中触发提醒，原生不抢焦点行为本轮仍未实测。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/UI/Views/HealthReminderWindow.cs`、`git diff --check` 通过。原型内联脚本 vm.Script 与英文 JSON 解析通过；浏览器实际点击开始／稍后／跳过并切换英文，结果正确，最终恢复中文并打开供评审。保留既有 NU1900 漏洞源网络警告，未升级依赖。
- 交付 OLED-Sleeper/bin/HealthCardShapes/Release/net8.0-windows/ScreenKeeper.exe；没有重启当前实例、安装或修改真实配置。007 设计／计划、README 和 AGENTS.md 已更新，已审查并沉淀按钮语义与局部样式规则。未提交、推送或发布。

## 2026-09-21 图形按钮探索计划

关联 007 设计的「暂停与跳过的图形按钮」。复制 v3 为 v4，聚焦稍后与跳过的造型对比；保留正文、休息主操作和结果演示。提供箭头／闪电／感叹号选择，同步中英文及窄屏，登记 baoyu-design 资产。检查 HTML 内联脚本语法、本地 HTTP、浏览器造型切换及三种操作反馈；只修改原型、资产索引及本设计／计划，不构建或更新客户端。回退可重新打开保留的 v3。

### 图形按钮探索完成记录

- 已新增 health-card-v4.html、更新 _d_meta.json，保留此前原型与客户端代码。稍后为带暂停图标的方块，跳过可选择箭头／闪电／感叹号轮廓，文字可见，完整按钮区域可点击。
- Node `vm.Script` 内联脚本语法检查通过；本地 HTTP 请求返回 200；浏览器已查看三个造型及中英文截图，点击稍后／跳过／开始分别显示对应结果或示例倒计时。当前桌面视口未发现布局溢出；窄屏和客户端实机尚未核验。本轮没有客户端代码变更，未执行 .NET 构建或测试。
- 设计与计划已同步。已检查 AGENTS.md，本次无需更新：新增的是待评审的造型探索，尚未成为已确认客户端规范。不提交、不推送、不重启客户端或改真实配置。

## 2026-09-21 标题居中与 A 方案实施计划

关联 007 设计同名定稿。在现有工作区保留已有修改，先同步原型标题居中与确认状态，再实现客户端等宽两侧留白和暂停／跳过专用模板；不修改其他模块或动作语义。使用 bin/HealthCardFinal/ 独立构建与现有健康／布局测试输出截图，核验中英文所有卡片状态和小屏场景。更新 README、设计／计划与 AGENTS.md 已确认长期规范；不自动安装、重启、提交或推送。

### 标题居中与 A 方案完成记录

- HealthReminderWindow.cs 标题采用左右各 40 DIP 的等宽留白，主标题居中，关闭按钮在右侧；提醒、倒计时、预览共用对齐方式。暂停与跳过使用 HealthReminder.xaml 的专用模板，分别为暂停方块与向右箭头，整块图文区域可点击；仍调用原 Snooze／Skip 动作，没有更改计时、焦点或配置。
- health-card-v4.html 同步居中并登记为 approved，客户端只实施 A，B/C 继续作为设计对照。README、007 设计和计划已同步；已检查并更新 AGENTS.md，记录已确认的标题、图形按钮及局部样式边界。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/HealthCardFinal/ --filter 'FullyQualifiedName~HealthReminder|FullyQualifiedName~SettingsLayoutTests'`：构建成功，61 通过、0 失败、1 原生桌面前台用例按门禁跳过。结果位于 artifacts/health-card-final-tests/health-card-final.trx；保留原有 NU1900 漏洞数据源网络警告。
- 设置 SCREENKEEPER_RENDER_QA=artifacts/health-card-final-qa，已目视核验中文／英文提醒、英文预览和 700×500 中英文场景。发现原合成截图夹具将内容从窗口分离后会丢失窗口局部资源，因此 HealthAppearanceTests 在离屏合成时保留这些字典；修正后定向重跑 ReminderAndRestScenesRenderAtBothSizesWithoutReadingDesktopContent：1 通过、0 失败，形状轮廓正常。该修正只让截图环境与真实窗口一致。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/UI/Views/HealthReminderWindow.cs OLED-Sleeper.Tests/Features/HealthReminder/HealthAppearanceTests.cs` 通过；原型内联 vm.Script 语法、样式 XML 解析及 `git diff --check` 通过。浏览器已重载核验标题居中，并将已确认 A 稿保留供查看。
- 新程序 OLED-Sleeper/bin/HealthCardFinal/Release/net8.0-windows/ScreenKeeper.exe 已生成；没有替换运行实例、安装或改真实配置。本轮未实测原生桌面焦点、混合 DPI 或真实周期提醒；没有把离屏截图当作这些行为的实机验收。未提交、推送或发布。
