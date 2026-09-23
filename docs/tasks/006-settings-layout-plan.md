# 006 · 设置布局客户端实施计划

## 2026-09-22 指定应用故障修复计划

关联 006 设计的企业微信装饰窗口修复。已核对真实保存选项与只读窗口元数据，不读取标题或内容，不触发真实窗口操作。先补充装饰窗口识别和动态排除，再补回归覆盖合法装饰、真实弹窗、错进程／所有者／类名／几何、移动前后变化及取消；增加具体失败说明。执行隐私／草稿／布局现有测试与定向格式检查，使用 bin/PrivacyApplicationFix/ 独立输出，保留运行实例。最后更新设计、计划和 AGENTS.md；不提交或推送，真实第三方应用切换需与用户观察结果区分。

### 指定应用修复结果（2026-09-22）

- 只读确认正在运行 HealthCardFinal 开发版；保存规则为指定应用，企业微信程序存在，启动及跨屏移动均已开启。首次原生快照中一个 WeWorkWindow 主窗口可见，拥有 TitleBarWindow（顶部 28 px）和 PerryShadowWnd（主窗口外扩 18 px），均同进程、分层、工具窗口且不可激活。旧代码对任意目标附属窗口直接返回 false，确定会在此条件下保留幕布；未读取窗口标题或内容。
- 新增 ApplicationWindowFamilyPolicy.cs，ApplicationWindowAccess.cs 提供进程／样式／DPI 瞬时元数据，ScreenApplicationService.cs 动态复核窗口族并保留未知弹窗门禁。每阶段失败有具体中英文说明及固定原因日志；成功清理此前失败状态。新增边界与时序回归，未改 schema、真实配置或启动／跨屏权限。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/PrivacyApplicationFix/ --filter 'FullyQualifiedName~MonitorPrivacy|FullyQualifiedName~SettingsLayoutTests|FullyQualifiedName~CalmEditingTests|FullyQualifiedName~MonitorSettingsSaveServiceTests'`：130 通过、0 失败、1 原生用例按门禁跳过。构建成功，保留环境既有 NU1900 漏洞源联网警告。
- 补充窗口消失时的准确失败原因后，`dotnet test ... --filter 'FullyQualifiedName~ScreenApplicationServiceTests|FullyQualifiedName~ApplicationWindowFamilyPolicyTests'`：29 通过、0 失败。覆盖合法装饰、真实／未知弹窗、装饰上的弹窗、身份／路径／类名／样式／几何不符、DPI、异步最小化中新弹窗、移动后装饰变化、取消、失败提示及成功清理。
- `SCREENKEEPER_NATIVE_QA=1 dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore -p:BaseOutputPath=bin/PrivacyApplicationFix/ --filter 'FullyQualifiedName~ApplicationWindowAccessTests.NativePlacementAndMinimization_WorkOnAnOwnedFixtureProcess'`：1 通过、0 失败、0 跳过。只操作测试创建的 WPF 窗口，确认定位、最小化与恢复链路及无标题枚举；没有操作用户企业微信。TRX 位于 artifacts/privacy-app-diagnosis/tests/。
- 生产枚举器的只读补充检查确认唯一匹配主窗口、标题读取数为 0；该时刻主窗口已最小化，附属窗口不可见，因此没有将这次检查描述为真实企业微信切换成功。实际第三方窗口移动、恢复后装饰跟随及完整覆盖，仍待用户运行新版观察；发现其他限制时继续保留幕布并显示具体原因。
- 独立输出 OLED-Sleeper/bin/PrivacyApplicationFix/Release/net8.0-windows/ScreenKeeper.exe，保留当前实例及设置。任务设计、计划和 AGENTS.md 已检查并更新；未提交、推送、安装或自动重启。
- 定向 `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include`（本轮 3 个生产 C# 文件和 2 个测试文件）通过；`git diff --check`、English.json 解析及全部应用失败文案英文键覆盖检查通过。未执行全仓格式重写或依赖升级。

关联 [006 设计](006-settings-layout-design.md)。2026-09-17 用户确认全部方案并授权客户端开发；当前目录保留已有变更，不提交或推送。

1. 配置模型、验证、草稿与批量复制兼容扩展。
2. 自定义暂缓、图片导入/幕布，以及隔离原生调用的指定应用保护服务。
3. 左侧选屏、休息/隐私页签、通用设置、对话框与原生标题栏。
4. 中英文资源、边界测试、构建及界面核验。
5. 同步设计/计划、README 和 AGENTS 架构事实与验证结果。

依赖顺序如上；指定应用依赖既有桌面隔离服务和幕布生命周期。配置写入失败保留草稿，图片不可变导入避免破坏回滚引用；原生动作失败保留遮挡，不自动恢复窗口。


## 客户端完成记录（2026-09-17）

全部三轮方案已实施于 WPF 客户端。保留当前目录的既有改动，未提交、推送、创建 PR 或发布。

主要文件：
- `OLED-Sleeper/MainWindow.xaml(.cs)`、`UI/Views/MonitorConfigurationView.xaml(.cs)`、`SettingsDialog.cs`、`SettingsDialogs.cs`、`UI/Themes/Calm.xaml`：固定选屏、休息/隐私页签、通用设置、批量/暂缓/图片/应用选择及测试入口、WindowChrome。
- `UI/ViewModels/MonitorConfigurationViewModel.Options.cs` 与既有配置/MainViewModel：选项深复制、单位、保存基线、重建草稿、跨屏能力回退、冲突校验和即时双语。
- `Features/MonitorPrivacy/PrivacyOptions.cs`、`PrivacyImageStore.cs`、`ScreenApplicationService.cs`、`ApplicationWindowAccess.cs`，及原隐私服务/幕布：可选 JSON 扩展、不可变图片、应用匹配/启动/移动/验证/取消、失败保留幕布。
- `OLED-Sleeper.Tests/Features/MonitorPrivacy/` 与 `UI/ViewModels/SettingsLayoutTests.cs`：兼容、草稿/复制、时间范围、隐私服务、应用策略、图片和原生窗口核验。旧刷新选择测试按新设计改为自动选第一屏。

### 实际验证

- `dotnet restore OLED-Sleeper.sln` 完成。最初受沙箱网络限制产生 NU1900；后续获准联网的便携构建成功重新恢复，最终 Release 构建为 **0 警告、0 错误**。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore` 通过。受仓库安全目录识别影响的 MinVer 警告通过当前进程 `GIT_CONFIG_COUNT/GIT_CONFIG_KEY_0/GIT_CONFIG_VALUE_0` 配置解决，未修改全局 Git 配置。
- `$env:SCREENKEEPER_NATIVE_QA='1'; dotnet test OLED-Sleeper.sln -c Release --no-build`：**263 通过、0 失败、0 跳过**。在获准的交互桌面执行；仅操纵自建 PowerShell/WPF 测试进程窗口，未操作用户应用或读取用户配置。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --verbosity quiet` 通过。只对本次新增测试文件修正格式，未全仓格式化。
- `git diff --check` 通过；新增界面英文字典键检查无缺项。
- WPF 渲染：中文/英文，1000×736 与 700×500，两个页签、滚动到指定应用区，保存栏位置；批量应用、暂缓、通用设置、选应用、图片及测试对话框。截图在 `artifacts/settings-v2-qa/`，全部为虚构数据。隐私幕视觉树只包含一个解除按钮，不透明背景。
- 原生：标题栏空白命中为 HTCAPTION；最大化/还原及对应图标通过；独立测试进程的窗口移动、覆盖验证、最小化再恢复通过。
- 图片：有效 PNG 导入、删除源文件后仍可解码、损坏文件拒绝、路径穿越拒绝；纯色遮挡不会因图片失败变透明。
- 完成 x64 自包含便携构建：`dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/settings-v2-portable/ScreenKeeper`。输出包含 `ScreenKeeper.exe`、WPF 原生运行库及根 `LICENSE`，没有用户数据；须保留整个输出目录运行。

### 尚未覆盖及边界

- 未替换或重启用户正在使用的客户端；未构建/安装安装包。便携输出尚未以真实用户配置启动验收。
- 真实第三方程序的权限差异、应用自身后续弹窗/焦点行为、独占全屏、跨屏窗口与混合 DPI 的组合仍需实机试用；自动化中的原生窗口是专用测试程序。
- WebP 取决于 Windows 解码器，本次未验证其在所有 Windows 安装上的支持；原图损坏/缺失时保留纯色幕布。已导入但不再引用的图片暂不自动清理。
- 物理鼠标从最大化标题栏向下拖动的混合 DPI 体验未逐设备验收；当前使用 Windows 原生 WindowChrome 行为，并已验证标题栏命中与状态切换。

已检查并更新 `AGENTS.md`：沉淀设置布局、窗口边界、PrivacyOptions、图片存储、应用身份/安全回退及测试开关。设计、计划、README、原型说明已同步，原型资产状态更新为 approved。

---

## 原型阶段历史记录

# 006 · 设置布局交互稿计划

关联 [006 设计](006-settings-layout-design.md)。用户授权范围为可点击设计稿；不修改客户端、不构建安装包、不提交或推送。

1. 阅读现有界面、设计资产和 baoyu-design 技能，沿用静绿风格及产品图标。
2. 新增原型：左侧选屏、分类设置、草稿/保存状态、语言、窗口交互及演示面板。
3. 浏览器验证普通/最大化/700×500、中英文、切换保留草稿、校验/保存失败/放弃、空屏/加载/错误状态；截图核验。
4. 注册原型资产为 needs-review，呈现预览，等待用户设计确认后再安排客户端实现。

## 第二轮调整与验证

按用户反馈更新同一原型：屏幕休息默认首项；隐私等待支持秒/分钟/小时；暂缓时长可配置分钟/小时；跨屏应用；纯色与单图/多图隐私幕。新增 `settings-v2-extras.js` 收纳时间输入、跨屏应用和幕布预览组件，`curtain-hills.svg` 与 `curtain-grove.svg` 是本地示例图库。

- 已在浏览器验证首个页签为屏幕休息，隐私等待的小时单位、默认暂缓小时单位参与草稿和复制。
- 已验证指定屏幕模式未选择目标时禁用应用；应用到显示器 3 后保留其管理关闭状态，启用后能看到来源的 2 小时等待及 1 小时默认暂缓。全部应用写入两个目标的草稿；放弃恢复保存基线。
- 已截图核验默认幕布仅一个继续使用按钮，背景不透明；Escape 不会解除。示例图库为空时阻止保存，选择两张后可预览并从第一张切换到第二张。
- 通过浏览器文件选择器实际读取项目已有 `deep-green.png`，图库增加至三张；删除后回到两张。不使用个人图片进行验证。
- 已截图核验中文暂缓弹窗、700×500 英文批量应用弹窗。设置 1 分钟暂缓后保存默认值，活动暂缓仍保留。
- 三份 JS 的 `node --check` 均通过。
- 1 分钟暂缓到期后，已观察到状态自动回到已保存规则、按钮恢复为“暂缓”；页面 error/warn 日志为空。原型依赖与示例图片 HTTP 均返回 200，SVG XML 解析和逐文件行尾空白检查通过。

本轮仅 HTML 原型：图片不上传也不写磁盘；浏览器刷新会丢失图片选择和演示设置。大尺寸图片上限、损坏图片及正式 Windows 多屏的持久化/硬件行为未做完整验证，留待客户端实现。AGENTS.md 已审查，无需更新：没有新增已落地的客户端架构事实。

## 验证记录

### 第三轮：指定应用

已按关联设计新增第四种隐私方式及 `settings-v2-app.js`：选择/搜索示例窗口、程序路径输入、未运行与其他屏幕的处理偏好、测试切换及失败幕布预览。设置参与保存、草稿和跨屏复制；待整体方案确认后与前两轮一起开发客户端。

- 浏览器验证：未选应用时禁用保存；非法路径报错，绝对 .exe 路径可选；选择日历后可保存并测试。
- 正常场景显示成功；未运行且不允许启动时保留幕布，允许启动时展示成功；多窗口无法确定、其他屏幕未授权移动时保留幕布。失败预览只显示继续使用按钮。
- 复制应用到其他屏幕后，两个启用屏幕选择同一窗口会阻止保存；更换为不同窗口后可成功保存。
- 已截图核验常规中文选择弹窗与 700×500 英文测试失败弹窗，后者内容可内部滚动。页面 error/warn 日志为空。
- 本轮没有调用真实应用、读取真实窗口标题或执行程序；真实启动、移动、Z 序、焦点和覆盖安全性未验证。原型中的成功是示意结果。
- 设计、计划及设计目录说明已同步。AGENTS.md 无需更新，原因是本轮仍为待评审原型，没有客户端架构变更。

2026-09-17：原型已完成，状态为 needs-review，等待用户评审。

- 浏览器实际点击验证：切换显示器保留草稿；保存前顶部继续显示旧规则、保存后更新；无效等待时间禁用保存；放弃恢复已保存值；模拟保存失败保留修改。
- 中英文切换、通用设置入口、调暗时显示亮度控件、不休眠折叠相关选项、不支持 DDC/CI 时禁用调暗，均已核验。
- 已检查普通、最大化及 700×500 布局，英文紧凑窗口、通用设置、隐私幕演示、加载和检测失败画面；截图在浏览器核验。空显示器状态及失败重试也已点击检查。
- 标题栏拖动前后窗口坐标发生变化；最大化按钮切换为还原图标；向下拖动最大化窗口、双击标题栏、点击还原均已验证。
- 隐私幕演示按 Escape 不关闭，明确点击继续使用后解除；关闭时的未保存提示、不保存后隐藏、从托盘演示入口重新打开已验证。
- 页面控制台 error/warn 查询无记录。
- `node --check designs/screenkeeper-calm/settings-v2.js` 与 `node --check designs/screenkeeper-calm/settings-v2-components.js` 通过；本地资源存在性、原型文件行尾空白及 needs-review 元数据检查通过。限定设计目录与 006 文档的 `git diff --check` 无报错（这些文件当前尚未跟踪，另做了上述逐文件空白检查）。

以上均为 HTML 原型行为，不代表真实窗口操作或隐私保护已实现。暂缓、保存、启动项及屏幕动作只在内存中模拟，刷新页面恢复演示数据。真实 WPF 标题栏、Windows 多屏/DPI、原生控件及硬件行为留待方案确认后的实施验证；本轮不运行 .NET 构建或测试。

已检查 AGENTS.md，本次无需更新：本轮是待确认设计，没有新增已落地的产品架构或开发约束。设计和计划已同步；既有客户端变更保留，未提交或推送。


## 隐私设置 v3 · 原型迭代（2026-09-17）

关联 006 设计的“隐私设置 v3”章节。v2 已实施历史保持不变，v3 是新的待评审设计。

1. 新增独立 HTML/JS/CSS，复用 v2 本地基础控件、图标和演示数据，不修改原入口及客户端。
2. 实现启用后展开、关闭记忆、模式专属内容、图库弹窗、更多设置、草稿和已保存运行状态区分。
3. 验证三种模式、关闭/重新开启、保存/放弃、图片空态、应用选择及失败、暂缓、语言与紧凑布局；HTTP、JS 语法和浏览器渲染检查。
4. 注册为 needs-review，展示新入口。只在确认后安排客户端开发。检查 AGENTS.md，不把待确认设计写入长期规则。


### v3 验证结果

- `node --check designs/screenkeeper-calm/settings-v3.js` 与 `node --check designs/screenkeeper-calm/settings-v3-privacy.js` 通过。
- 使用本机 Chrome 的独立无头实例点击验证，未连接用户正在使用的浏览器。脚本 `node artifacts/settings-v3-qa/verify.cjs` 与 `node artifacts/settings-v3-qa/final-check.cjs` 均退出 0；结果和截图位于 `artifacts/settings-v3-qa/`。
- 已验证关闭/重新启用保留模式与时间、关闭保存后移除暂缓入口、保存前显示旧生效规则、整数校验、保存失败保留、放弃、指定屏幕复制及目标管理开关保持。
- 三种方式只显示专属选项；切换支持左右方向键。普通 1040×700 窗口首屏可见更多设置，700×500 英文内容区无横向溢出；图库、应用选择、更多设置及关闭态已截图核验。
- 已验证应用选择、显式允许启动、多窗口失败、桌面成功/失败、失败幕布和 Escape 不解除；这些均为虚构演示，不代表真实系统操作结果。
- 已验证实际本地 PNG 文件选择、空图库阻止保存、单图隐藏轮播、多图实际切换和幕布只展示继续按钮。图片未上传。
- 已验证暂缓采用已保存默认时长，本次调整不产生默认值修改；屏幕休息页无暂缓入口。中英文切换清理旧语言的瞬时提示。
- 空显示器、检测中、检测失败与模拟保存失败已点击检查；浏览器控制台 error/warn 为空。
- 新入口及全部引用资源 HTTP 返回 200。本地文件存在性、UTF-8/逐行尾部空白检查通过；限定设计目录与 006 文档的 `git diff --check` 无报错。设计文件尚未跟踪，所以同时执行逐文件空白检查。
- 通过 baoyu-design 的 record-asset.mjs 登记 `ScreenKeeper 隐私设置 v3` 为 needs-review；保留 v2 approved 元数据及旧入口。内置浏览器工具连接暂不可用，已通过独立 Chrome 完成上述验证，并向 Codex 发出新入口展示请求（返回 queued）。

边界：本次只验证 HTML 原型，没有运行 .NET 构建/测试，也没有验证真实多屏、窗口操作或配置持久化。网页刷新恢复初始演示数据；预览服务停止时需重新启动设计目录的 HTTP 服务。

任务设计、计划及设计目录 README 已同步。已检查 AGENTS.md，本次无需更新：v3 尚待确认，未产生已落地的客户端事实或长期约束。本轮未修改客户端，未提交或推送。


## v3 客户端开发计划（2026-09-17，已获授权）

关联 006 设计“v3 客户端实施”。在当前目录保留已有修改；不提交/推送。

1. 核对当前 UI、草稿/保存链路与服务；基线 `dotnet test OLED-Sleeper.sln -c Release --no-build` 通过 262 项，1 项原生桌面测试按环境条件跳过。
2. 实现可持久化的上次模式、条件校验、已保存的暂缓默认值；重排隐私页并集中图片弹窗，移除通用设置指定文案及英文显示。
3. 补充持久化/草稿/校验回归，扩展 WPF 实际渲染检查；运行 Release 构建、完整测试和格式检查。
4. 更新 approved 原型记录、README、设计计划及 AGENTS 长期 UI 规则，生成 v3 便携版，报告验证与实机边界。


### v3 客户端完成及验证

- 已新增 `UI/Views/PrivacySettingsView.xaml(.cs)`，完成独立启用开关、等待时间、三段方式选择、方式专属内容、折叠更多设置与隐私页运行状态/暂缓。公共主题保持不变，局部使用静绿分段控件和折叠箭头。
- 图片管理集中到 `SettingsDialogs.Privacy.cs`，显示方式与多图间隔在弹窗内编辑，确认写入草稿，取消不改动；单图隐藏轮播。桌面测试复用 IScreenDesktopService，应用测试共用取消/幕布处理。解除幕布后上一轮操作未结束时仍禁用再次测试，避免重叠。
- PrivacyOptions.LastEnabledMode 保存上次非关闭方式，旧文件兼容；关闭不清除时长、图片与应用，复制/放弃/布局重建保持基线。暂缓从 SavedPrivacyOptions 副本读取默认值，本次调整不修改草稿。
- 通用设置中的独立配置说明已从中文界面、英文资源和 v3 设计稿移除，产品存储隔离行为未改变。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore`：0 警告、0 错误。首次构建因 Git 目录信任环境导致 MinVer 警告，后续命令仅使用进程内 `GIT_CONFIG_COUNT/GIT_CONFIG_KEY_0/GIT_CONFIG_VALUE_0` 信任当前工作区，未更改全局 Git 配置，最终无该警告。
- `SCREENKEEPER_NATIVE_QA=1`、`SCREENKEEPER_RENDER_QA=artifacts/settings-v3-wpf-qa` 下运行 `dotnet test OLED-Sleeper.sln -c Release --no-build`：268 通过，0 失败、0 跳过。新增 5 项持久化/记忆/校验/草稿/暂缓回归，扩展现有 WPF 渲染和取消重入检查。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore`：通过。仅对本轮 C# 变更文件执行过格式化。
- 已检查真实 WPF 控件渲染：中英文、1000×736 与 700×500、隐私幕/桌面/应用/关闭状态、更多设置和各弹窗。保存与语言栏固定可见，窄窗口内部滚动。截图存于 `artifacts/settings-v3-wpf-qa/`，只使用虚构屏幕/窗口数据。
- 修正渲染中发现的空错误提示占高及固定高度文本框内边距裁剪；再次运行布局测试 2 项通过，最后完整测试同样通过。
- XAML XML 解析、新增界面的英文键覆盖、指定文案移除、`node --check designs/screenkeeper-calm/settings-v3.js` 和 `git diff --check` 通过；未跟踪的新源文件另做逐行空白检查。Git 的 LF/CRLF 提示是工作区既有换行策略提示。
- v3 设计资产已标记 approved。README、006 设计/计划及 AGENTS.md 已同步，长期记录独立隐私开关、可持久化模式记忆、方式专属内容、条件校验和已保存暂缓默认值。

验证边界：原生检查只操作测试创建的窗口；未在用户真实第三方应用、跨屏混合 DPI、独占全屏和真实 DDC/CI 硬件上做人工验收。没有安装/替换已安装实例，没有改写用户真实配置，没有提交或推送。便携构建使用新 v3 目录，保留 v2 目录；试用时应退出旧实例，且保留同目录运行时 DLL。


最终便携构建：`dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/settings-v3-portable/ScreenKeeper` 成功退出 0。已核对 ScreenKeeper.exe、LICENSE 和 5 个 WPF 原生运行时 DLL 均存在。未启动正式便携实例读取真实显示器或配置，运行行为由上述隔离测试覆盖。
