# 004 · 托盘图标设计计划

客户端状态：第六轮用户已确认任务栏和托盘统一采用 A，主题适配代码、构建和相关检查已完成。图标形状与配色沿用第五轮；最新构建为 `OLED-Sleeper/bin/TaskbarTheme/Release/net8.0-windows/ScreenKeeper.exe`。

对应设计：[004-tray-icon-design.md](004-tray-icon-design.md)。

## 范围与顺序
1. 保留既有工作区修改，读取现有视觉方案与 baoyu-design。
2. 制作三个方向、各深浅两套 SVG。
3. 用 DesignCanvas / DCSection / DCArtboard 制作对照画布，提供画布之外的真实尺寸预览和 SVG 下载。
4. 编译 JSX，通过本地 HTTP 在浏览器检查加载、方案切换、图标尺寸和视觉效果。
5. 用户选定后再实施 ICO 与客户端接入；本轮不提交、推送或更换应用图标。

## 验证记录
- JSX 已用既有 Babel standalone 编译，页面通过本地 HTTP 成功加载。
- 浏览器检查 A/B/C 方案切换、对应说明和 SVG 下载目标；真实尺寸区域 DOM 实测为 16/20/24/32 CSS 像素，图片均加载成功。
- 深浅任务栏、三个放大轮廓已截图检查；评审截图：designs/screenkeeper-calm/tray-icons/review.png。浏览器未记录 warning/error。
- 尚未接入应用，未执行本轮无关的 .NET 构建或单元测试；原生托盘、系统 DPI、ICO 渲染与主题自动适配未验证。

## 回滚与维护
本轮为新增设计素材和任务文档，不涉及用户配置、显示器控制或现有原型行为；撤销仅需移除本轮新文件及对应资产元数据条目。
AGENTS.md 已审查：本轮为待确认的图标提案，无新的长期架构或构建约束，无需更新。

## 第二轮记录
- 已新增 A+ 两套 SVG，调整 review.jsx / review.js / review.css，复用原评审入口。
- node --check 通过；浏览器已检查组合轮廓、深浅背景和 16–32 px 预览，保留原 A、B 对照。
- 原生托盘仍未接入或验证；未提交、推送。AGENTS.md 已复审，本次仅设计迭代，无需更新。

## 第三轮：实施计划
用户已确认 A+。依次：保存 SVG 与生成脚本 → 生成两套多尺寸 ICO → 托盘主题适配与标题栏接入 → 资源解码/渲染核验 → Release 构建和现有测试 → 更新便携试用包与文档。
验证范围包含 ICO 帧尺寸、System.Drawing/WPF 解码、透明背景和图标引用；真实 Explorer 托盘与系统主题切换若无法操作，明确列为未验证。
回滚仅恢复本轮图标资源与接入代码，不更改已有身份、语言、用户配置或显示器管理实现。

### 实施结果与验收
- 已完成：两套 SVG / 9 帧 ICO、生成脚本、托盘系统主题选择及偏好通知刷新、旧 Icon 释放、窗口标题栏替换。程序和安装器的既有 icon.ico 引用同步获得新标识。
- `powershell -NoProfile -ExecutionPolicy Bypass -STA -File tools/Generate-Icons.ps1` 成功；重复生成 icon.ico 的 SHA256 相同。脚本执行策略只对本次进程有效。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore` 成功；现有 NU1900（NuGet 漏洞数据源无法访问）仍存在。
- `dotnet test OLED-Sleeper.sln -c Release --no-build`：192 通过，0 失败。新增 ApplicationIconTests 验证程序集里的两套图标、九种 WPF 帧及透明背景、16–128 px System.Drawing 解码；256 px 由 WPF 验证（System.Drawing 的帧选择器未选择 ICO 中以零字节表示的 256 px 帧）。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 通过；`git diff --check` 通过，仅已有 LF/CRLF 提示。
- 离线 WPF renderer 完成 15 个窗口/弹窗状态，bindings.log 为空；截图 artifacts/calm-ui/normal.png 已核验标题栏。原生 Icon 解码对照图为 artifacts/tray-icon/native-frames.png。
- `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -o artifacts/tray-icon/ScreenKeeper --no-restore` 成功。
- 便携包：artifacts/tray-icon/ScreenKeeper-Aplus-Portable-x64.zip，含许可证和试用说明；SHA256：10AFB049E81A24B5FD6BF648D237129F7AAC8FB49BCDBC58D72B7D808F363AA7。
- 未启动实际应用、未触发显示器操作。真实 Explorer 托盘交互、动态切换系统主题、高对比度/跨屏 DPI 和本轮安装器构建未验收；Explorer 旧图标缓存可能延迟刷新。16 px 月牙细节较小，保持用户确认的形状。
- 已同步任务设计、README 与 AGENTS.md；AGENTS 增加最终图标源文件、生成脚本和主题资源约束。未提交、推送或发布。

## 第四轮：亮度修正
- 原因：原浅绿色在用户的深色任务栏上观感偏暗；当前用户系统主题读取结果为深色。
- 范围：深色任务栏 SVG / ICO、A+ 预览、资源说明；沿用 A+ 形状，不修改显示器行为。
- 计划：生成资源 → 原生小尺寸新旧渲染比较 → Release 构建及图标资源测试 → 发布本地便携包。
- 回滚：恢复 icon-dark.svg 的旧色 #B4D6A1 并重新生成 ICO。
- 结果：Generate-Icons.ps1 成功；Release build 成功；`dotnet test OLED-Sleeper.sln -c Release --no-build --filter FullyQualifiedName~ApplicationIconTests` 1 通过，0 失败；本轮仅修改素材，未重复运行无关功能测试。
- 已检查原生 Icon 新旧渲染图 `artifacts/tray-bright/brightness-comparison.png`（16/20/24/32/48 px）；新包发布成功：`artifacts/tray-bright/ScreenKeeper-Aplus-Bright-Portable-x64.zip`。包含 LICENSE 与试用说明。
- 当前真实用户主题已只读核对；未自动替换或重启用户运行中的程序，真实托盘主观亮度仍需用户查看新包。构建仍有此前 NU1900 数据源警告。
- 已检查 AGENTS.md：本次仅调整现有资源配色，未新增长期架构或构建约束，无需更新；设计和计划已同步，未提交或推送。

## 第五轮：恢复原色并放大
- 用户已否决第四轮提亮；恢复原浅绿色，增大双屏、月牙和底座在既有画布中的占比。
- 执行顺序：更新两套 SVG / 生成器 → 重新生成 ICO → 原色新旧尺寸对照 → 构建与图标测试 → 打包新便携版。
- 回滚：恢复第三轮的 A+ 几何形状与原色，重新生成 ICO；保留用户配置与其他工作区变更。
- 验证：生成器成功；Release build 成功；`dotnet test OLED-Sleeper.sln -c Release --no-build --filter FullyQualifiedName~ApplicationIconTests` 通过（1 项，覆盖两套 ICO 的全部 WPF 帧及原生小尺寸解码）；`git diff --check` 通过。
- 已核验 `artifacts/tray-large/size-comparison.png`，在深浅背景对比原色原尺寸与放大后的 16/20/24/32/48 px 图标。颜色回退已落实到两处 SVG 源及 ICO，前屏/月牙/底座变大，未裁掉轮廓。
- 本地自包含便携发布成功，最新包为 `artifacts/tray-large/ScreenKeeper-Aplus-Large-Portable-x64.zip`，含许可证和试用说明。第四轮 Bright 包为已取消的历史产物，不再推荐使用。
- 保留此前 NU1900 网络警告。未更改系统任务栏设置，未自动重启用户应用；真实托盘效果需使用最新包确认。
- 已同步设计、计划及资源说明。已检查 AGENTS.md：现有图标资源与生成约束仍适用，本轮无需更新。未提交或推送。
- 最终确认复核：应用 SVG 与预览均为原色放大版；包 SHA256 与上一轮产物一致：957D79478AB6619C917617DC320AAF3DBDAF03BEE8043F00A86BFDBD6B7A87EF。本次仅整理确认记录，未变更产品代码，沿用上一轮成功的构建和图标测试结果。

## 第六轮：任务栏可见度设计

- 对应设计：004-tray-icon-design.md 第六轮。仅新增独立设计稿及素材，保留已确认第五轮客户端和其他未提交变更。
- 顺序：核对截图与图标引用 → 复用 A+ 绘制三种 SVG 方向 → 截图位置模拟和实际尺寸对比 → 编译 JSX、HTTP 浏览器核验 → 登记 needs-review 资产。
- 验证：JavaScript 语法、页面及本地资源加载、三方案与原图切换、深浅背景、窄窗口、截图核验。此阶段不运行无关 .NET 构建／测试，不宣称实际 Explorer 显示已验证。
- 回滚：移除本轮新设计文件及资产登记，恢复本轮文档追加；不改客户端配置或现有资源。
- AGENTS.md 维护：待确认的视觉提案不沉淀为长期规则，现有图标资源约束继续有效。
- 完成设计：新增 `taskbar-icon-v6.html` 和 `taskbar-icons-v6/`，复用已有 React 与 DesignCanvas；新增资产登记为 needs-review。客户端 SVG／ICO 与引用未改变。
- 核验结果：Babel 编译成功；`node --check designs/screenkeeper-calm/taskbar-icons-v6/review.js` 通过；`git -c core.safecrlf=false diff --check` 通过。浏览器无警告或错误，所有图片成功加载；切换 A／B／C、原图对照、浅色背景和画布预览按钮均有效。
- 已核对深浅背景截图及 16/20/24/32 CSS 像素尺寸；700×700 下修复了截图的横向撑开，截图在自身区域滚动并默认露出右侧目标图标，页面无横向溢出。画布使用独立变换平移，避免聚焦按钮时浏览器原生滚动裁掉标题；最终完整页面截图已核验。
- 未验证：真实 Windows 任务栏、通知区域、高 DPI 与图标缓存更新；本轮为浏览器设计，未构建、打包或替换客户端。用户选定后实施再验收。
- 任务设计、开发计划和原型 README 已同步。已检查 AGENTS.md，本次无需更新（待确认设计没有产生新的长期约束）；未提交、推送或发布。

### A 方案实施计划

- 根因：MainWindow.Icon 固定深绿，未复用托盘的系统主题逻辑。
- 范围与顺序：提取系统主题读取 → 任务栏窗口初始化／系统偏好变化／关闭生命周期接入 → 更新已确认设计稿默认选项与说明 → 独立输出目录 Release 构建及现有图标／布局核验 → 记录结果。
- 两套图标资源不变，不重新绘制或生成 ICO；不改变客户端主题选择、用户配置、显示器操作或当前运行进程。
- 回滚：恢复 MainWindow 的静态 Icon 和 TrayIconService 原主题读取，撤回新增窗口图标辅助代码即可；无需配置迁移。
- 验证重点：多尺寸透明 ICO、WPF 窗口布局与有效图标资源、共享系统主题选择及事件退出路径。普通窗口关闭到托盘继续跟随系统，真正关闭移除订阅；系统主题切换和固定任务栏状态未经实际观察不声明通过。

### A 方案实施结果

- `UI/Helpers/SystemTaskbarTheme.cs` 统一系统主题读取，`TrayIconService` 复用该入口；`MainWindow.Icon.cs` 在显示前设置并冻结相应 ICO，订阅系统偏好变化并通过 UI dispatcher 刷新，关闭时移除订阅、忽略迟到刷新。XAML 保留浅绿后备图标，客户端 ThemeIcon 绑定不变。
- 设计稿默认 A，任务栏与托盘示意均改为 A，资产标记 approved；B／C 保留作历史比较。没有修改 SVG／ICO 二进制、安装器、真实配置或显示器状态。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/TaskbarTheme/`：成功，0 错误；保留现有 NU1900 漏洞数据源不可达警告。
- `dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore -p:BaseOutputPath=bin/TaskbarTheme/ --filter 'FullyQualifiedName~ApplicationIconTests|FullyQualifiedName~SettingsLayoutTests'`：5 通过、0 失败、0 跳过；TRX 位于 `artifacts/taskbar-theme-tests/targeted.trx`。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/UI/Helpers/SystemTaskbarTheme.cs OLED-Sleeper/MainWindow.Icon.cs OLED-Sleeper/MainWindow.xaml.cs OLED-Sleeper/UI/Services/TrayIconService.cs`：退出码 0；工作区加载有现有警告。`node --check designs/screenkeeper-calm/taskbar-icons-v6/review.js` 通过。
- 已查看 `artifacts/taskbar-theme-qa/forest-zh-CN-1000-0.png` 和 `fog-zh-CN-700-0.png`，窗口内部图标保持应用主题适配，布局无本轮引入的溢出；浏览器确认默认 A、两处 A 示意及无控制台错误。上述渲染不替代真实任务栏验收。
- 尚未运行新版客户端、切换系统主题或验证固定分组／不同 DPI；旧程序仍在运行，未强行退出、覆盖或重启。需要用户正常退出旧版并打开本轮构建查看真实效果。
- 已更新设计、计划、资源说明、README 和 AGENTS.md；AGENTS 沉淀了任务栏／托盘共用系统主题及窗口事件生命周期约束。未提交、推送或发布。
