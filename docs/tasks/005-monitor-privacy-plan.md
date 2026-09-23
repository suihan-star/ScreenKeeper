# 005 · 每屏隐私保护开发计划

对应设计：[005-monitor-privacy-design.md](005-monitor-privacy-design.md)。在当前目录保留已有变更，不提交、推送或发布。

## 2026-09-17：回到桌面失败修复

- 反馈：已保存桌面模式，触发后出现“部分窗口跨屏或无法最小化”的隐私幕；已读取当前配置，确认桌面模式生效。
- 代码缺陷：`WS_MINIMIZEBOX` 被误当作最小化能力，只要一个窗口没有该按钮样式，就在发出任何请求前终止整屏操作。现有测试把这一误判当成预期，未覆盖桌面服务完整流程。
- 现场只读诊断确认：目标为右侧 1920×1080 显示器，桌面外缘 X=4480；有辅助窗口边界到 X=4485。它没有覆盖左屏，却被原有全矩形包含判断误当作跨屏，从而在请求最小化前拦截。诊断仅读取边界/样式/类名，无标题或内容读取，无窗口变更。
- 后续只读检查还发现最大化窗口边框为 (2552,-8)–(4488,1040)，系统所属屏幕从 X=2560 开始，8 像素边框也会被误判；已按系统 DPI 边框指标仅校正最大化窗口。
- 修复后的实际原生适配器只读核验：左右屏的错误跨屏计数均为 0，右屏当前识别到 3 个候选窗口。本次未对用户窗口发出最小化请求；窗口集合可能随用户操作改变。
- 范围与顺序：先提取可测试的原生窗口访问边界，修正判断和同屏所有者窗口处理，再回归测试、构建与格式检查，生成独立修复包。不触碰现用进程、配置或用户窗口。
- 验证：无标准按钮、同屏附属窗口、跨屏拦截、异步延迟、关闭竞态、请求失败和剩余窗口保留隐私幕。系统窗口权限及真实应用行为需新包实机复核。
- 回退：旧试用包与 Data 目录保留；本轮不增加持久化字段，修复包沿用现有设置。
- 验证完成：`dotnet test OLED-Sleeper.sln -c Release --no-restore --verbosity minimal` 构建及 235 项测试通过（隐私相关 43 项，新增 20 项）；覆盖现场 5 像素外缘、最大化 8/12 像素边框、真实跨屏、附属窗口、无标准按钮、延迟响应、关闭竞态、失败兜底和取消操作。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --verbosity minimal` 通过；界面未改，本轮未重复离线 UI 渲染。已审查并更新 AGENTS.md 的窗口访问边界和判定规则，同步更新本设计/计划。
- x64 自包含便携修复包：`artifacts/privacy-desktop-fix/ScreenKeeper-Privacy-Desktop-Fix-Portable-x64.zip`，本地 publish 参数同首版，仅更换输出目录；仍使用缓存还原并关闭本次 NuGet 审计，未变更依赖。包内不含 Data、日志或诊断工具。
- 实机完成的是**只读窗口分类核验**，没有改变、最小化或恢复用户窗口；真实软件响应、管理员窗口、混合 DPI 与 DDC 行为需试用确认。首次沙箱内无法枚举交互桌面窗口，随后已通过只读诊断读取实际窗口几何数据。
- 参考：[窗口样式定义](https://learn.microsoft.com/en-us/windows/win32/winmsg/window-styles)、[异步窗口显示 API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindowasync)、[最大化窗口边框说明](https://devblogs.microsoft.com/oldnewthing/20120326-00/?p=8003)。

1. 持久化字段、独立计时状态与活动来源；以纯状态测试锁定保护和解除规则。
2. 隐私幕宿主、单屏桌面服务与协调服务；接入空闲生命周期、配置更新和遮黑恢复。
3. 中英文设置、只保护不休眠、立即保护和暂缓入口；补齐草稿/校验/保存/放弃。
4. 单元测试、WPF 离线渲染、Release 构建与格式检查；生成可试用便携包。
5. 更新设计、验证记录、README 与 AGENTS.md，说明真实桌面未验证的边界。

## 验证记录

2026-09-16：上述实现已完成，保留当前工作区已有改动，未提交或推送。真实桌面验收待用户试用。

- `dotnet build OLED-Sleeper.sln -c Release --no-restore`：通过，0 警告、0 错误。
- `dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore`：215 项通过（原有 192 项 + 隐私相关 23 项），0 失败/跳过。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore`：通过。首次检查发现新增文件缩进/导入顺序问题，已只对本轮相关文件修正后复查。
- `dotnet run --project artifacts/privacy-ui/Render.csproj -c Release --no-restore`：24 个离线渲染场景通过，绑定日志无错误。覆盖中英文、700×500 窗口、隐私/桌面/仅保护、无效输入、暂缓、空数据/加载/错误，以及隐私幕普通/失败/全黑状态；实测隐私选项双向绑定、时长单位绑定、语言保存失败回退。截图核对了英文长文案、按钮、固定底栏与滚动。没有运行真实窗口最小化或安装输入钩子。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:NuGetAudit=false --ignore-failed-sources -o artifacts/privacy-v1/ScreenKeeper`：成功生成 x64 自包含便携版。此次使用已有缓存还原，NuGet 审计服务先前不可达，因此本地打包关闭审计；未修改项目依赖或持久化 NuGet 配置，不视为漏洞审计通过。
- 产物：`artifacts/privacy-v1/ScreenKeeper-Privacy-v1-Portable-x64.zip`；渲染文件在 `artifacts/privacy-ui/`（构建产物目录不入 Git）。包内含 LICENSE 和试用说明，不含用户配置或日志。
- 已更新设计与 README；已审查并更新根 AGENTS.md，沉淀隐私模块边界、输入内容不留存、唤亮不解除及配置兼容约束。

## 主要变更

- `Features/MonitorPrivacy/`、`Native/PrivacyNative.cs`：独立计时/活动、隐私幕、桌面规划及失败处理。
- `MonitorIdleDetectionService`、`ApplyMonitorIdleBehaviorCommandHandler`、`MonitorBlackoutService`：生命周期、休眠前置保护和唤醒协作。
- `MonitorSettings`、配置 ViewModel、`MonitorConfigurationView.xaml`、主窗口和托盘：持久化、校验、草稿、双语及快捷操作。
- `OLED-Sleeper.Tests/Features/MonitorPrivacy/MonitorPrivacyTests.cs`：隔离桌面和硬件的回归测试。补充连续输入只恢复一次、窗口跨屏一像素也保留遮挡、显示失败按屏重试、过期桌面结果不移除保护。

## 实机验收与限制

1. 使用无敏感信息的测试窗口，在副屏设 10 秒隐私幕；持续操作主屏，确认副屏仍按时遮挡。鼠标静止不延迟；阅读时暂缓有效。
2. 组合遮黑/调暗（含休眠早于隐私），鼠标唤亮后仍显示隐私幕，明确点击按钮才恢复；确认无点击穿透或内容闪现。
3. 桌面模式分别验证普通、最大化、跨屏、不可最小化、管理员窗口；只处理目标屏幕，不关闭或自动还原窗口，无法确认成功时保留遮挡。
4. 验证 100%/150% 混合 DPI、负坐标、副屏插拔、锁屏恢复、全屏应用、UAC 和系统通知。原生输入钩子可能受系统权限/超时影响，真实 DDC/CI 行为依硬件而异。
5. 升级试用时先退出旧进程并备份、保留原 `Data/ScreenKeeper`。应用崩溃/退出、任务切换、系统级界面不属于保护保证；需要访问控制使用 Windows 锁屏。

未进行真实多屏/输入钩子/窗口最小化/DDC 验收，也未重建或安装安装包。第一版不含指定软件切换与密码认证。
