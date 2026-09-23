# 003 · 静绿 UI 设计计划

关联设计：[003-calm-ui-design.md](003-calm-ui-design.md)。

- [x] 阅读仓库约束、现有计划和客户端界面，检查并保留已有工作区变更。
- [x] 从官方仓库固定提交安装并读取 baoyu-design。
- [x] 查看 /zeroz 实际页面，与用户确定深绿主方案＋雾白绿对照稿。
- [x] 建立设计理由、功能边界和本计划。
- [x] 完成两种配色的可交互设计原型，资产标记为 needs-review。
- [x] 检查主要状态、紧凑布局、文字对比度与浏览器错误。
- [x] 提供可预览稿；用户确认待后续回复。
- [x] 用户确认并授权 WPF 实施，结果见下方开发阶段记录。

## 原型阶段验证记录

2026-09-16，本轮只修改设计文件和本任务文档：

- `node --check designs/screenkeeper-calm/prototype.js`：通过；JSX 使用 Babel 7.29.0 编译为原型 JS。
- `python -m http.server 4311 --bind 127.0.0.1 --directory designs`：本地 HTTP 预览已启动，浏览器成功加载全部本地资源。
- 浏览器实际操作：时长 0 显示校验并禁用保存；全部取消唤醒条件显示校验；切换显示器保留未保存值；保存与放弃状态正确；关闭提示未保存；保存失败保留更改；不支持亮度时禁用调暗和亮度归零。
- 截图：`deep-green.png`、`fog-green.png`、`compact.png`、`error.png`，保存在设计目录；加载、无显示器和读取失败文案亦已检查。
- 700×500 外框实测内容区宽 688px、scrollWidth 688px，无横向溢出；内容纵向滚动，底栏位于窗口边界内。HTML 截图不代替 WPF 验收。
- 核算正文/次文字/按钮对比度：深绿 13.00 / 6.86 / 8.50；雾白绿 10.76 / 5.31 / 6.83。此为指定色对的计算，非完整无障碍认证。
- 浏览器警告/错误日志：检查时无记录。
- `git diff --check`：通过；仅提示已有文件的 LF/CRLF 转换，不修改其内容。
- 未修改客户端源码，因此未重复 .NET 构建、测试或硬件验证。WPF 模板、DPI、真实多显示器行为均未在本轮实现或验证。
- 未提交、推送或发布；正式配色、主题切换能力和新图标仍待用户确认。

## 语言入口设计补充（2026-09-16）

- [x] 标题栏新增固定可见的“语言 / Language”入口，仅中文和英文两个选项。
- [x] 原型窗口内文案、条件、单位、校验和确认弹窗支持即时切换；评审工具仍为中文。
- [x] 浏览器验证：9 小时在中英文切换后保持原值；保存状态独立于语言；英文关闭确认、保存成功、读取失败及数值校验正常。
- [x] 英文 700×500 截图检查；内容区 width 与 scrollWidth 均为 688px，没有横向溢出。
- [x] `node --check designs/screenkeeper-calm/prototype.js` 与 `node --check designs/screenkeeper-calm/translations.js` 通过。
- [x] 浏览器无警告/错误；`git diff --check` 通过。
- 本次只变更原型 JSX/JS/CSS、入口 HTML、双语文案表及关联设计文档；未开发或测试 WPF 本地化。语言偏好暂不落盘。

## 设计阶段维护结论

已检查 AGENTS.md，本次无需更新：本轮属于设计稿迭代，语言需求与实施边界已记录在 003 设计中，当前客户端事实未变。

## WPF 开发阶段（2026-09-16，已获用户确认）

- [x] 落地深绿主题、固定标题栏/保存栏和可滚动内容区；显示器仍按真实拓扑排列，支持键盘选择。
- [x] 实现中英文即时切换，涵盖窗口、校验、应用自有确认弹窗、托盘菜单；新增独立 `ui-preferences.json`，成功写入后才切换。
- [x] 重建布局保留未保存草稿和放弃更改的基线；有更改时禁用显式刷新。
- [x] 保留非法输入文本并阻止保存，防止存入上次合法值；校验时长换算上限及无 DDC 的调暗配置。
- [x] 修复原文件服务吞掉失败、UI 误标已保存的问题，保存接口返回结果，失败保留草稿。
- [x] 完成构建、191 项测试、全解决方案格式检查、差异检查。
- [x] 生成实际 WPF 截图并核验；更新 AGENTS.md、README、设计与计划。
- [x] 生成便携试用包并核对产物：`artifacts/calm-portable/ScreenKeeper-calm-Portable-x64.zip`，66,801,349 字节；包含自包含 EXE、WPF 原生库、LICENSE 和试用说明。

### 主要文件

`MainWindow.xaml`、`UI/Views/MonitorConfigurationView.xaml`、`MonitorLayoutView.xaml`、`AppDialog.cs`、`UI/Themes/Calm.xaml`；`UI/Localization/UiText.cs`、`English.json`；`UI/Services/UiPreferencesService.cs` 及相关 ViewModel、保存服务和托盘服务。新增 `UiPreferencesServiceTests`、`CalmEditingTests`，并扩充保存失败测试。

### 已执行验证

- `dotnet restore OLED-Sleeper.sln`：成功，现有依赖可用；NuGet 审计服务器不可达，出现 NU1900，不代表漏洞审计通过。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore`：成功，0 错误；2 条 NU1900 环境警告。
- `dotnet test OLED-Sleeper.sln -c Release --no-build`：191 通过、0 失败、0 跳过。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore`：通过。初次报告了本次新增代码的格式问题，已仅格式化相关文件并复查。
- `git diff --check`：通过，已有 LF/CRLF 提示不属于错误。
- `dotnet run --project artifacts/calm-ui/Render.csproj -c Release --no-restore`：13 个主窗口状态和中英两张确认弹窗截图生成，WPF 绑定错误为 0。检查正常/调暗/未选择/加载/空/错误/无 DDC/校验/最小窗口/150% DPI/英文状态。
- 同一渲染程序验证了实际 WPF ComboBox 在语言保存失败时回到原语言，以及时间单位选择和持久化枚举不变；不是仅验证 HTML 原型。
- WPF 截图及渲染程序在 `artifacts/calm-ui/`，绑定日志为 `bindings.log`。截图暴露的浅色按钮文字对比和系统滚动条颜色问题已修正并重新渲染。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/calm-portable/ScreenKeeper --ignore-failed-sources`：成功，随后生成 ZIP 并检查条目。NU1900 仍为审计网络警告；未实际启动本包控制显示器。本次没有重新生成安装包。

### 未验证与边界

尚未在真实多显示器上执行遮黑/DDC 调暗、托盘常驻/开机启动全流程、安装升级卸载或物理高 DPI 屏幕验证；150% 为离屏渲染。正式版本仍由上游标签派生，未迁移 CI 或版本策略。没有自动运行客户端以改变真实显示器状态，没有提交、推送或发布。

### 开发阶段 AGENTS.md 维护

已更新根 AGENTS.md：沉淀主题与本地化目录、独立语言偏好、固定操作栏、布局重建保留草稿及保存失败语义。原有用户变更保留。
