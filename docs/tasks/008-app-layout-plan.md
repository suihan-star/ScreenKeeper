# 008 · 全应用布局优化执行计划

日期：2026-09-18。对应 [008 设计提案](008-app-layout-design.md)。

## 当前授权范围

用户先要求出方案，随后明确要求“先落地设计，看下效果”。2026-09-19 用户已确认 v4 和顶部双主题入口，明确授权客户端开发；阶段二至四已完成。

- [x] 读取规则、README、已有任务设计、设计记录及当前布局；检查工作区，保护已有变更。
- [x] 提出主导航、屏幕管理、健康提醒、色温调节及统一状态／保存方案。
- [x] 记录范围、依赖、验证与回退；完成 AGENTS.md 维护审查，无需更新未确认布局。
- [x] 用户授权按提案制作可点击设计稿。
- [x] 2026-09-19 用户确认 v4，明确进入客户端开发。

## 阶段一：可点击设计稿

状态：**已完成原型与浏览器核验，用户已批准**。入口为 `designs/screenkeeper-calm/settings-v4.html`；客户端结果见文末实施记录。

确认结构后，在 `designs/screenkeeper-calm/` 新增 `settings-v4.html` 及必要的独立资源，用 baoyu-design 沿用现有静绿组件和产品标识；保留 index、settings-v2、settings-v3 和既有元数据记录。

范围：三个模块的页面切换、各自草稿、选屏、通用设置、外观局部示意与模拟预览、色温设置／处理引导。只用示例数据，不读取用户配置、窗口或 Gamma，不执行真实验证或保护动作。

验收场景：

- 中文和英文，1000×736 与 700×500，长屏幕名、一个／多个／无屏幕、屏幕断开。
- 开启待保存／已保存开启／关闭／运行中／临时暂停的状态能够区分。
- 切换模块、屏幕与页签后草稿保留；批量复制提示保存范围；保存失败保留草稿。
- 健康节奏与外观相互独立、方式专属选项正确；预览入口唯一且可退出。
- 色温固定作息和手动模式；未选择试调屏幕、正在试调、原色对比和结束恢复的模拟状态。
- 待验证、策略开启阻塞、恢复冲突、重新建立原色、模式不支持、取消和失败。每个状态都有真实可理解的下一步，不模拟无条件成功。
- 键盘可操作、焦点可见、无横向溢出；真实 HTTP 预览可访问，控制台无脚本错误，截图核验布局。

本阶段原型不是 WPF 窗口行为或硬件兼容性的验证。设计确认后记录批准版本，再进入客户端开发。

## 阶段二：主框架与屏幕管理（已完成）

依赖阶段一确认。预计涉及 MainWindow、MainViewModel、MonitorConfigurationView、Calm 与本地化资源。

- 一级导航改为模块；屏幕选择移到屏幕管理内部，保存拓扑更新和 HardwareId 草稿合并逻辑。
- 保持屏幕休息为首个默认页签，隐私 v3 条件内容不回退。
- 收敛标题、状态与固定保存条；屏幕保存范围仍为所有屏幕草稿；明确立即保护全部屏幕的范围。
- 保持 WindowChrome 拖动、最大化／还原、中英文切换、关闭到托盘和退出未保存提示。
- 纳入用户 2026-09-19 追加的双主题：默认深绿、可选雾白绿，标题栏快捷入口及通用设置预览卡同步切换，偏好与语言独立于业务草稿。实施前明确 UI 偏好兼容与写入失败行为，核验所有弹窗与状态；不让主题切换改变隐私和健康场景外观。

优先运行已有导航／草稿／批量复制／保存失败相关用例及最小窗口渲染；新增测试只覆盖实际改变的状态关系，不复制视图布局实现。

## 阶段三：健康提醒布局（已完成）

依赖新框架。预计涉及 HealthSettingsView、必要的 ViewModel 命令及局部预览呈现。

- 拆分提醒节奏、提醒外观；运行操作独立呈现。
- 聚合外观选项与唯一真实预览入口；在原型确认后确定是否新增阶段直达预览命令。
- 保持健康全局计时、隐私优先、手动预览不推进计时、保存后下一周期生效等行为。

运行已有健康设置／预览生命周期／显示门禁测试，中英文各方式、关闭、运行、暂停和窄窗口渲染；新入口如改变预览状态切换，再补充相应测试。

## 阶段四：色温日常页面与引导（已完成）

依赖新框架。预计涉及 ColorSettingsView、ColorSettingsViewModel 及验证／原色重建窗口的入口组织。先完成静态布局，再调整状态呈现和引导，避免同时重写硬件服务。

- 共用作息与两组色温相邻；试调目标和反馈唯一，离开页签可见区域结束试调。
- 参与屏幕列表集中验证与临时原色操作；异常提示可展开处理步骤。
- 复用原有验证、只读重查和重建流程。保存策略开启时仍明确要求关闭并保存，不隐式保存草稿或自动放行。
- 如发现引导必须改变服务 API、持久化或全局策略控制，应先更新设计与范围，再实施；不得用删除恢复记录、接纳未知颜色等办法简化流程。

运行色温资格、恢复、预览取消、原色对比、草稿、模式变化、保存失败相关测试和中英文渲染。真实改色仍需用户选定目标并观察，禁止用原型模拟或假设备测试代替。

## 集成验证与交付

客户端实施阶段按实际改动运行已有有效测试，最后执行项目既有 Release 构建／测试与格式检查；按需启用 WPF 渲染 fixture，不在无人观察时进行硬件写入。

验证重点：跨模块草稿与保存范围；临时操作不保存设置；异常可理解且不会绕过服务限制；高 DPI、中英文和 700×500 操作可达；屏幕插拔和页面切换不丢草稿、不残留预览。

交付实际测试结果、未验证项和试用入口；同步对应设计／计划，审查 AGENTS.md，仅在方案已确认并实施后更新长期布局事实。不自动提交、推送、创建 PR、安装或发布。

## 回退与风险

- 提案和原型阶段：保留旧文档与已批准原型；使用旧入口即可回退，不修改运行数据。
- 客户端阶段：保留当前版本输出，仅撤回本任务界面／命令改动，不能覆盖工作区既有变更。现有配置、资格与恢复记录完整保留。
- 色温试调中不能直接切换旧程序；先结束试调，确认按现有流程恢复，再回退。旧版未必识别新行为，因此本提案默认不改变 schema。
- 主要代价是从全局模块访问某屏多一次点击；主要风险是布局拆分后遗漏入口、误导保存范围或隐藏仍在进行的试调。以场景验收约束这些问题。

## 本轮验证记录

首轮完成提案与文档关联检查。用户授权落地设计后，新增 `settings-v4.html`、`settings-v4.css`、`settings-v4-ui.jsx`／`.js`、`settings-v4.jsx`／`.js`，更新设计目录 README 与资产记录。旧 HTML／脚本和 WPF 代码未修改。

2026-09-18 原型检查：

- 已有 Babel standalone 编译两份 JSX 成功；`node --check designs/screenkeeper-calm/settings-v4.js` 与 `node --check designs/screenkeeper-calm/settings-v4-ui.js` 通过。
- HTML 引用的本地资源存在；`Invoke-WebRequest` 检查入口、脚本、样式、React、图标和示例图共 10 个 URL 均返回 HTTP 200。仅监听 127.0.0.1 的 4311 端口。
- `record-asset.mjs ... --status needs-review` 成功，新版与原有四项设计资产记录同时保留；未将 v4 标为 approved。
- 浏览器实际点击：模块切换、隐私图集与轮播条件、跨屏复制生成草稿、应用选择、保存／放弃、保存失败保留数值均通过；色温编辑、导航结束试调、保存清除当前模块脏状态通过。
- 色温 15 秒试调自然结束后，界面显示已恢复策略，输入框的实际 DOM value 仍为 4300，待保存状态仍保留；此处仅验证原型状态，不代表真实 Gamma 恢复。
- 15 秒验证完整模拟：未勾选准备确认不能开始；倒计时结束进入观察；三项确认后显示通过，不自动开启调节。已保存策略开启时先展示关闭并保存的引导。
- 恢复冲突模拟：只读重查 → 仍不同的说明 → 列出两块受影响屏幕 → 明确确认后重新建立 → 回到逐屏验证；未勾选观察确认时重建按钮不可执行。所有行为仅为内存模拟，无真实归档／改色。
- 实际查看无屏幕、关闭、HDR 不支持与屏幕断开状态。健康预览打开后焦点进入退出按钮，Esc 关闭后回到预览入口；覆盖 100% 可选，渐显与缓慢雾光动画在浏览器内呈现。
- 截图目视核验中文主界面、1000×736 标准健康外观双栏、验证与重建弹窗、英文 700×500 页面／弹窗。六个英文页签逐一检查，正文／底栏无横向溢出，保存栏在窗口边界内可见。临时浏览器视口覆盖已撤销。
- 浏览器控制台检查无 error／warn。新增原型文件和两份任务文档的 `git diff --no-index --check` 无空白错误（新增文件差异退出码 1 为预期）。

未执行 WPF 构建／测试或真实多屏、Gamma、原色恢复验证；未测试用户本地图片的实际选择和 Windows 高 DPI。原型的模拟结果不能替代客户端或硬件验收。独立恢复到默认中文首页，保留可点击预览供用户评审。

已同步本设计与计划，已检查 AGENTS.md，本次无需更新：新增内容仍为待评审原型，未改变产品长期布局事实或开发命令。未提交、推送、安装或发布。

## 2026-09-19 双主题原型增量

授权：用户明确指定深绿为默认、允许选择最初的雾白绿方案；仍在设计阶段，不进入 WPF 开发。

- [x] 提取原始雾白绿配色，新增 `settings-v4-themes.css`，适配主界面、控件和弹窗状态。
- [x] 通用设置增加双主题预览卡与即时切换，独立浏览器偏好记忆；保留模块草稿。
- [x] 标题栏使用适合浅色背景的现有图标；场景画面隔离界面主题，保留独立配色。
- [x] 编译与静态语法检查；初始默认、切换、刷新记忆、草稿隔离和场景前景色检查。
- [x] 完成小窗口／英文补充核验并交付。

原型改动只在 `designs/screenkeeper-calm/`；旧参考页 `index.html` 不变。回退使用深绿选择，或撤回本次主题 CSS 与选择入口；浏览器主题键不影响客户端配置。

已同步设计、计划与设计目录说明。AGENTS.md 新增已确认的主题产品原则，并明确当前 WPF 仍未接入；没有把原型结果表述成客户端能力。未提交、推送或发布。

本次验证：两份 JSX 使用既有 Babel 编译成功，两份 JS 的 `node --check` 通过，主题样式 HTTP 200。浏览器确认首次为 forest，选择 fog 后刷新仍为 fog；切换两次后业务草稿数值仍为 7，待保存状态保持。已目视检查双主题选择器、雾白绿主界面／健康外观／恢复冲突弹窗；场景文字继续为独立浅绿前景，未随设置主题变成暗字。700×500 英文六页签无横向溢出，主题卡可用方向键切换，控制台无 error／warn。最后恢复默认深绿并打开主题入口，供用户查看。

本轮仅验证浏览器原型；WPF 资源切换、实际产品偏好持久化和 Windows 高 DPI 未实施／未验证。原有用户正在操作的预览页保留，使用新预览页验证，未清除旧页草稿。

## 2026-09-19 标题栏主题入口增量

用户要求将外观主题放在顶部中英文切换旁；本次仍只更新 v4 设计稿。关联设计第 10 节。

- [x] 标题栏增加紧凑主题下拉框，与通用设置预览卡共用状态及浏览器偏好记忆；不改业务草稿。
- [x] 同步设计、设计目录说明和 AGENTS.md 的已确认主题入口原则。
- [x] JSX 使用既有 Babel 编译成功，`node --check designs/screenkeeper-calm/settings-v4.js` 通过。浏览器确认顶部与预览卡双向同步；切换保留空闲时长 7 的待保存草稿，刷新保留主题选择。700×500 中英文标题栏无溢出，品牌与窗口按钮无重叠；控制台无 error／warn。已目视检查深绿英文小窗口、雾白绿中文小窗口及标准窗口。测试后恢复用户本轮开始时选择的雾白绿并清除本轮演示草稿。

回退可移除标题栏入口，通用设置主题卡继续可用。客户端多主题和 Windows 高 DPI 仍未实施／未验证，无新增产品风险；不提交或推送。

## 2026-09-19 客户端实施记录

已获用户授权实施完整 v4，包含顶部双主题入口，沿用当前目录且保留已有变更。依赖顺序：主题与主框架 → 屏幕管理 → 健康节奏／外观 → 色温作息／设备引导 → 集成核验。原有界面快照保留在忽略目录 artifacts/008-before-client，回退只恢复本任务相关界面改动，不能恢复或改写真实产品数据。

基线：`dotnet test OLED-Sleeper.sln -c Release --no-restore --verbosity quiet`，477 通过／2 跳过／0 失败。环境已有 NuGet 漏洞源网络不可达和 MinVer 工作目录警告，后续单独核对，不扩大到发布配置。

- [x] 主框架、屏幕管理与主题偏好兼容
- [x] 健康两页签、阶段外观与局部示意
- [x] 色温两页签、试调退出与处理引导
- [x] 行为测试、双语双主题窗口渲染、Release 构建与格式检查
- [x] 文档和 AGENTS.md 同步交付

### 客户端最终验证与交付

- `dotnet build OLED-Sleeper.sln -c Release --no-restore --verbosity quiet`：成功，0 错误。使用仅当前命令生效的 GIT_CONFIG_COUNT／safe.directory 环境项解决 MinVer 在沙箱内的目录信任警告，未改用户 Git 配置。
- `dotnet test OLED-Sleeper.sln -c Release --no-build --verbosity quiet`：486 通过、2 跳过、0 失败，总计 488。新增／扩展用例覆盖 UI 偏好旧文件和未知主题回退、语言主题互相保留、失败选择器回退、现有弹窗动态配色、业务草稿隔离、休息阶段直达预览不推进周期、色温换页取消试调、服务单调倒计时，以及恢复引导必须先关闭并保存策略。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes --verbosity quiet`：退出码 0。只对本次改动的两份测试文件执行了必要的空白格式修正，没有全仓格式化。
- 设置 `SCREENKEEPER_RENDER_QA=artifacts/008-render` 运行 UI 用例，检查 1000×736／700×500、中英文、双主题下的屏幕管理、隐私各方式、健康节奏／外观、色温作息／设备页。截图仅包含 fixture 示例数据。主选择器能显示屏幕名称；语言切换后等待 WPF 布局完成再截图，避免将尚未重排的文字截断误认为实际窗口问题。
- `SCREENKEEPER_NATIVE_QA=1`、`SCREENKEEPER_RENDER_QA=artifacts/008-native-render`，运行 `SettingsLayoutTests.LayoutRendersInBothLanguages_AndCurtainContainsOnlyReleaseButton`：1 通过。仅创建测试自己的窗口，验证原生标题栏命中、最大化／还原、模态设置入口；不调用真实改色。
- `git diff --check`：通过。设计记录已将 v4 标为 approved，更新设计、计划、设计目录 README、根 README 及 AGENTS.md 的实际布局、主题偏好和场景隔离规则。

主要实现文件：MainWindow.xaml／MainWindow.xaml.cs；UI/Themes/{Calm,Forest,Fog}.xaml 与 UiTheme.cs；UiPreferencesService；主窗口及健康／色温 ViewModel 和 View；ThemePickerView；SettingsDialogs.Color；HealthReminderService 仅扩展预览阶段，ColorTemperatureService 仅增加只读试调状态。隐私业务、Gamma 写入和恢复门禁没有放宽。

本地运行入口：`OLED-Sleeper/bin/Release/net8.0-windows/ScreenKeeper.exe`。此输出需要已安装 .NET 8 Windows Desktop Runtime；如已运行旧版本，请先从托盘正常退出后再打开，不能强杀正在恢复颜色的进程。

未验证／限制：本次未进行真实显示器调暖、真实用户图集、长时间常驻及不同 DPI 屏幕间拖动测试；未构建安装包或执行安装、发布。NuGet 漏洞源因网络不可达持续产生 NU1900，构建与测试均成功，但在线漏洞数据未刷新。现有配置、验证资格、恢复记录不迁移或清理。没有提交或推送。

### 2026-09-20 标题栏中文名称修正

关联设计第 12 节。原因：标题栏使用固定英文文本，没有接入现有实时语言资源。范围：WPF 标题栏、英文资源及 v4 原型标题，保留工作区已有变更。使用现有双语双主题渲染验证和 Release 构建，原型重新生成脚本并检查中英文切换；回退只需恢复本项显示文案，无配置迁移。

- 主要变更：`OLED-Sleeper/MainWindow.xaml`、`OLED-Sleeper/UI/Localization/English.json`、`designs/screenkeeper-calm/settings-v4.jsx` 及其生成脚本 `settings-v4.js`。
- 常规 `dotnet build OLED-Sleeper.sln -c Release --no-restore --verbosity quiet` 因运行中的 ScreenKeeper.exe 占用输出文件失败（MSB3027／MSB3021）。未终止正在运行的客户端。追加 `-p:OutputPath=<workspace>/artifacts/008-brand-build/` 后构建成功，0 错误、1 个既有 NU1900 漏洞源网络警告。
- `dotnet test OLED-Sleeper.sln -c Release --no-build --verbosity quiet -p:OutputPath=<workspace>/artifacts/008-brand-build/ --filter FullyQualifiedName~SettingsLayoutTests.LayoutRendersInBothLanguages_AndCurtainContainsOnlyReleaseButton`：1 通过，0 失败。设置 `SCREENKEEPER_RENDER_QA=artifacts/008-brand-render` 生成双语、双主题和两种窗口尺寸截图，目视确认中文“屏幕良伴”、英文“ScreenKeeper”完整显示。
- `node --check designs/screenkeeper-calm/settings-v4.js` 与标题栏文件 `git diff --check` 通过；浏览器实际切换中英文，显示准确，结束时保留中文。
- 已同步任务设计和本计划，AGENTS.md 增加已确认的标题栏名称规则。未安装或重启真实客户端，未提交、推送。新版可执行文件位于 `artifacts/008-brand-build/ScreenKeeper.exe`；先从托盘正常退出旧版，再打开新版即可查看。

### 2026-09-20 选屏状态辨识修正

关联设计第 13 节。原因：选屏背景差异较小，客户端通用 ListBoxItem 的悬停触发还会覆盖已选中背景。实施专用选屏样式，仅供 MonitorList 使用；v4 原型同步实心底色、边框、勾选与辅助技术选中状态。验证双主题、中英文、两屏切换及窄窗口回退，复用现有 WPF 渲染用例并构建到独立目录，避免占用运行中的客户端。回退只恢复本次模板和 CSS/JSX，不涉及配置。

- 主要文件：`OLED-Sleeper/MainWindow.xaml`、`OLED-Sleeper/UI/Themes/Calm.xaml`；原型 `settings-v4.jsx/js/css/html`、`settings-v4-themes.css`。HTML 同步更新资源版本，防止浏览器继续显示缓存的旧样式。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore --verbosity quiet -p:OutputPath=<workspace>/artifacts/008-selection-build/`：通过，0 错误，仅既有 NU1900 网络警告。
- 使用同一 OutputPath 运行 `dotnet test OLED-Sleeper.sln -c Release --no-build --verbosity quiet --filter FullyQualifiedName~SettingsLayoutTests.LayoutRendersInBothLanguages_AndCurtainContainsOnlyReleaseButton`：1 通过、0 失败。`SCREENKEEPER_RENDER_QA=artifacts/008-selection-render` 的截图覆盖中英文、双主题、标准／700×500 窗口，目视确认两套选中配色与勾选标记。
- 原型实际切换 Q27P10／DELL、主题及语言，选中颜色和 aria-pressed 一致；窄窗口下拉选屏正常，键盘焦点描边与当前选择可区分。控制台无错误，`node --check designs/screenkeeper-calm/settings-v4.js`、标题栏文件 `git diff --check` 均通过。
- 任务设计与计划已更新；已检查 AGENTS.md，本次无需更新：仅落实既有清晰反馈及双主题原则，未新增架构、配置或开发流程。未安装或重启真实客户端，未执行实机鼠标悬停／多 DPI 检查；未提交、推送。运行文件位于 `artifacts/008-selection-build/ScreenKeeper.exe`。

### 2026-09-20 色温验证入口文案

关联设计第 14 节，将按钮“开始设置”改为“开始验证”，英文同步为 Start verification。仅修改 ColorSettingsViewModel 的显示资源键、English.json、v4 原型 JSX/生成 JS 与资源版本，不改验证逻辑。回退仅恢复文案。

- `dotnet build OLED-Sleeper.sln -c Release --no-restore --verbosity quiet -p:OutputPath=<workspace>/artifacts/008-verification-label-build/`：通过，0 错误，保留既有 NU1900 网络警告。
- `node --check designs/screenkeeper-calm/settings-v4.js`：通过。浏览器进入“色温 · 首次设置”场景，确认两个屏幕分别显示“开始验证”，切换英文后均显示 Start verification；结束时保留中文验证页。
- 已更新设计与计划；已检查 AGENTS.md，本次无需更新：仅调整入口措辞，未改变既有规则。未执行真实屏幕验证、未安装或重启客户端、未提交或推送。

### 2026-09-20 试调屏幕验证入口文案

关联设计第 14 节，将作息页“设置试调屏幕…”改为“验证试调屏幕… / Verify a trial display…”。修改 ColorSettingsView.xaml、English.json，并将原型对应的“管理屏幕”入口同步；保留点击后进入屏幕与验证页的行为。仅文案与生成脚本变更，无配置迁移；回退恢复对应文案即可。计划核验 Release 构建、JS 语法及双语入口和跳转。

验证结果：`dotnet build OLED-Sleeper.sln -c Release --no-restore --verbosity quiet -p:OutputPath=<workspace>/artifacts/008-trial-label-build/` 成功（0 错误，既有 NU1900 漏洞源网络警告）；`node --check designs/screenkeeper-calm/settings-v4.js` 通过。浏览器确认中英文新文案及点击后进入“屏幕与验证”，结束时保留中文作息页。已同步设计与计划；已检查 AGENTS.md，本次无需更新，原因是仅改文案，未改变架构或流程。未进行真实显示器验证，未安装或重启客户端，未提交、推送。

### 2026-09-20 色温保存未应用与作息示意修正

关联设计第 15 节。只读本产品配置确认总开关开启、工作和休息均 3900K、参与屏幕为空；未修改真实配置或颜色。先修正运行状态与草稿参与提示，再实现按时段划分的作息图及双语文本，最后同步原型。验证覆盖空参与、选择后保存、保存失败、放弃、同色温、跨午夜、反向作息、分钟边界及无效输入；使用模拟后端核实只有参与屏幕才按工作目标执行，不进行真实调色。回退仅恢复界面和提示相关代码，无 schema 或恢复记录变更。

实施完成：作息页增加参与数量／未选择提示及“选择参与屏幕…”入口。已保存参与为空时顶部状态和已保存策略明确提示未应用。作息图移除以暖色深浅代表时段的整点条，改为按两个实际边界分成三段、以分钟比例布局，补充工作／休息的时间和目标图例；原色、反向班次和同色温均可辨认，无效时间显示输入提示。作息图随草稿更新，保存状态读取保存基线。

主要变更：`UI/ViewModels/ColorSettingsViewModel.Schedule.cs`、`ColorSettingsViewModel.cs`、`UI/Views/ColorSettingsView.xaml`、`Infrastructure/Hosting/WellbeingCoordinator.cs`、`UI/Localization/English.json`；原型 `settings-v4.jsx/js/html`、`settings-v4-themes.css`。没有修改 Gamma 后端、验证资格或调度算法。

验证：
- `dotnet test OLED-Sleeper.sln -c Release --no-restore --verbosity quiet -p:OutputPath=<workspace>/artifacts/008-color-schedule-build/ --filter 'FullyQualifiedName~ColorLiveSettingsTests|FullyQualifiedName~ColorTemperatureServiceTests|FullyQualifiedName~ColorScheduleTests'`：95 通过，0 失败。模拟后端确认未参与不写入，参与后按工作目标执行，再保存其他工作色温后更新输出；保存失败不清除未参与警告。
- 同一命令移除 filter 执行 Release 构建和全量测试：492 通过、2 跳过、0 失败。网络限制仍产生既有 NU1900 漏洞源警告。
- `dotnet format OLED-Sleeper.sln --no-restore --verify-no-changes --verbosity quiet`：退出码 0。`node --check designs/screenkeeper-calm/settings-v4.js` 通过。
- `SCREENKEEPER_RENDER_QA=artifacts/008-color-schedule-render`：检查双语、双主题、460／720 内容宽度截图，验证同为 3900K 时工作／休息色块仍有区别、分钟边界宽度和文本即时更新。无效时间、反向作息、午夜边界、原色、放弃及失败保存均有回归用例。
- 浏览器实际将两段设为 3900K，使用时间输入的键盘操作将工作开始改为 08:01，确认图例更新并保存；再取消全部参与并保存，确认显示“设置已保存，但没有参与屏幕，尚未自动调色”。页面控制台无错误。浏览器 time 输入的 fill 未提交控件变化，改用真实键盘交互后完成核验，未将未提交的值当作通过。

任务设计、计划和 AGENTS.md 已同步维护作息图及保存反馈规则。新版入口 `artifacts/008-color-schedule-build/ScreenKeeper.exe`，打开前需从托盘正常退出旧版。本轮未安装／重启客户端，未进行真实 Gamma 改色、跨 DPI 及长时间运行测试；没有代用户选择参与屏幕或修改本地策略。未提交、推送。

## 2026-09-21 独立休息／隐私菜单可点击稿计划

关联 008 设计的「独立屏幕休息／隐私保护菜单 v5」。仅授权设计。

1. 复制现有 Calm 原型壳到 settings-v5，新增独立休息／隐私工作区，复用字体、语义色板和控件，保持其他模块入口。
2. 实现共享选屏、独立草稿／保存／放弃、复制参数、使用时不打扰、保护方式专属内容、暂缓分钟／小时及联合休息选项。
3. 实现已保存状态、暂缓倒计时、恢复及托盘预览；提供操作／阅读／离开、异常、空数据、加载、多屏、保存失败等演示。
4. 本地 Babel 编译和 JS 语法检查；浏览器检查主要路径、双主题双语、700×500、倒计时、输入校验与控制台；记录真实验证结果。
5. 注册 needs-review 资产并打开交付，维护原型说明；审查 AGENTS.md，未确认提案不写成已实施架构。不改 WPF、设置、硬件，不提交／推送。

设计稿已完成并登记 needs-review。新增 settings-v5 HTML、CSS、两个 JSX 及生成 JS；同步原型 README 和设计文档。未改 WPF、真实配置、输入识别或设备行为。

实际验证：
- 使用仓库已有本地 Babel 编译两个 JSX；`node --check designs/screenkeeper-calm/settings-v5-screens.js`、`node --check designs/screenkeeper-calm/settings-v5-shell.js` 均通过。
- 浏览器核验中文深绿、英文雾白绿、700×500 及弹窗滚动；选屏、双模块草稿保留、仅保存当前模块、放弃、批量复制保留目标开关均符合设计。
- 核验阅读状态、15 分钟／1 小时联合暂缓、主动恢复、1 分钟暂缓自然到期、托盘状态；空闲到期保护后，暂缓不会解除现有保护，明确进入后重新计时。
- 核验隐私幕专属外观、单图隐藏轮播、多图显示轮播、指定应用选择、保护预览键盘焦点，以及四屏下拉、无调暗能力、空数据、加载、输入异常、非法时长、保存失败保留草稿。修正场景切换残留错误提示后复验通过。
- 导航至健康、色温、通用设置均可打开。本轮未运行 .NET 构建／测试，未进行真实鼠标键盘归属、跨屏窗口、睡眠联动或多 DPI 实机验证；原来的“操作中误触发”仍需在批准实施后独立复现排查。

已检查 AGENTS.md，本次无需更新：所有新增交互仍是待确认提案，尚未形成已实施的长期架构事实。未提交、推送、安装或重启客户端。回退只需不采用 v5 设计入口；既有版本与客户端保持原状。

### v5 复选式使用免打扰调整

用户要求取消固定判定逻辑，改为与保持唤醒类似的三条件任选。范围仅 settings-v5 原型：增加逐屏复选草稿、按任一条件命中的状态示意和独立场景；关闭保留选项，全未选提供校验。同步设计与原型说明。验证开关展开／收起、三条件独立作用、保存／放弃／复制、双语双主题及 700×500；不修改客户端或实机输入逻辑。回退恢复本次原型局部变更即可。

已完成：新增 quietConditions 仅用于原型内存；替换固定前台与整机输入组合，增加仅鼠标／仅前台／仅其他屏操作的演示场景。隐私计时单独记录免打扰结束后的等待基点，避免影响休息示意。初始勾选前两项，第三项可选；不改客户端默认值。

验证：本地 Babel 编译成功，两个生成 JS 的 `node --check` 均通过。浏览器逐项确认鼠标、前台、整机输入单独生效，未选条件不阻止计时；关闭收起、重新开启保留选择、全未选阻止保存、保存／放弃、复制条件到 DELL 后保留其原开关均通过。已检查英文雾白绿 700×500 与中文深绿画面，浏览器无错误或警告。原用户页面有未保存选择，保留原页并打开独立评审标签。未运行 .NET 测试或真实输入测试，本轮没有客户端变更。

设计、计划、原型 README 及 needs-review 登记同步更新；已检查 AGENTS.md，本次无需更新，原因是仍处于设计确认阶段。未提交、推送。

### v5 屏幕休息暂缓入口修正

问题：独立菜单后，休息页的“暂缓与免打扰”仍跳转隐私页，入口语义与所在模块不符。仅修订原型：休息页就地打开“暂缓休息”，隐私页仍打开“暂缓保护”；默认各自操作，明确勾选后才联合暂缓。临时截止分开记录，单项操作保留另一项计时；联合暂缓明确可恢复两项。验证不跳页、当前屏幕、时长输入、独立和联合状态／恢复、隐私关闭时休息仍可暂缓及双语窄窗口。保持草稿与客户端不变，回退仅恢复本轮原型局部变更。

已完成：修改 settings-v5-screens.jsx／生成 JS、HTML 资源版本、原型 README 与资产描述；每屏临时状态分为隐私 until 和休息 restUntil。保持唤醒与免打扰选项不改。

验证：Babel 编译及 `node --check designs/screenkeeper-calm/settings-v5-screens.js` 通过。浏览器确认休息弹窗就地打开、显示选中屏幕、零时长不可开始；休息 1 分钟与隐私 30 分钟可独立暂缓，恢复休息后隐私截止保留；从任一页面联合暂缓均同步显示“恢复两项”，恢复后两项均回到自动规则。DELL 隐私未开启时联合选项禁用，但可独立暂缓休息，1 分钟自然到期后恢复，隐私仍关闭。检查英文雾白绿 700×500 和中文深绿弹窗，控制台无错误或警告。未执行 .NET 或实机测试，本轮只改设计。

设计、计划和说明已同步；已检查 AGENTS.md，本次无需更新，尚未批准或实施客户端架构变化。未提交、推送。

### v5 色温试调入口对齐

仅移除作息页试调行的“固定作息”标签，单个试调按钮自然左对齐。共享色温原型组件新增默认关闭的呈现参数，仅 settings-v5 启用，其他原型保持原样。编译后核验语法及浏览器文案、位置、展开／收起和窄窗口；不修改客户端或真实作息。

完成：修改 color-v5-flow.jsx／JS 的可选呈现参数、settings-v5-shell.jsx／JS 的调用及 HTML 资源版本。Babel 编译成功；两份生成 JS 的 `node --check` 通过。浏览器确认该行仅剩试调按钮，标准窗口与 700×500 下按钮左边缘均与行起点一致，展开／收起正常，控制台无错误或警告。保留原评审页未保存的色温草稿，在新评审页展示修改。未运行 .NET 或硬件测试。设计与计划已同步；已检查 AGENTS.md，本次无需更新，仅原型局部排版调整。未提交、推送。

### v5 色温保存摘要色块

仅调整色温开关下摘要，将单行已保存状态改为工作／休息两个小色块，保持保存基线、草稿和运行异常的区分。共享组件通过可选呈现参数启用，样式限定 settings-v5；核验原色／自定义、切屏、保存／放弃、关闭／暂停、双主题双语和 700×500。设计改动不涉及 WPF 或真实改色，回退关闭呈现参数并恢复旧资源引用。

完成：color-v5-flow.jsx／JS 增加默认关闭的 savedPeriodSummary 呈现参数；settings-v5-shell.jsx／JS 启用；settings-v5.css 添加工作绿／休息暖沙色摘要及雾白绿配色，HTML 更新引用版本。原色按保存基线显示“保持原色”，自定义按保存数值显示“使用 N K”，并保留关闭／临时原色提示。

验证：Babel 编译与两个生成文件的 `node --check` 通过。浏览器确认工作原色／休息 4500 K，工作草稿改成 4500 K 时摘要仍为原色，保存成功后两块均为 4500 K；继续改成 5000 K 后放弃，摘要与编辑值回到已保存值。切换 DELL 显示其 5500／4200 K 和未开启提示；临时原色时摘要不变且保留暂停说明。中文深绿标准窗口和英文雾白绿 700×500 截图核验通过，控制台无错误／警告。未进行 .NET 或真实硬件测试，本次仅设计稿。

设计与计划及资产登记已同步；已检查 AGENTS.md，本次无需更新，仅待确认原型的摘要呈现变化。未提交、推送。

### v5 两时段独立原色／自定义选择

按用户要求，将休息时编辑器改成与工作时相同的显式双选项；新建默认工作原色、休息自定义 4500 K。原型增加 nightOriginal，保留各自自定义数值，原色切换取消试调，联动保存摘要及时间轴。仅 settings-v5 开启新交互；核验默认值、切换保值、切屏、保存／放弃、原色试调与取消、手动模式及窄窗口双语。只改设计稿，不修改 WPF、真实配置或硬件；回退原型参数及资源版本即可。

完成：color-v5-flow.jsx／JS 增加默认关闭的 restOriginalChoice 选项，由 settings-v5-shell.jsx／JS 启用；HTML 更新资源版本。两时段采用相同双选项，休息原色状态参与草稿、保存摘要、时间轴与试调目标；仅校验实际使用的自定义数值，手动模式保持自定义调节。

验证：本地 Babel 编译及两个生成 JS 的 `node --check` 通过。浏览器确认初始工作原色／休息自定义 4500 K；休息改为 4200 K 后切原色、保存、切回自定义仍保留 4200 K，放弃后回到保存的原色状态。草稿切屏保留；原色试调入口显示正确目标，自定义试调切原色后结束；手动模式仍显示保留的数值和滑杆。中文深绿标准窗口、英文雾白绿 700×500 截图检查通过，控制台无错误／警告。保留原用户标签页的未保存草稿，另开评审页。未执行 .NET 或实际硬件测试，本轮只改设计稿。

设计、计划及 needs-review 资产登记已同步；已检查 AGENTS.md，本次无需更新，原因是本次仅调整待确认原型，尚未形成客户端实现事实。未提交、推送。

## v5 客户端开发（已获批准，2026-09-21）

关联设计：008-app-layout-design.md 的 v5 客户端实施决策。范围为独立休息／隐私菜单、模块草稿与保存／复制、三条件免打扰、独立及联合暂缓、过期休息请求防护，以及色温双时段原色／自定义和保存摘要。不实施未选定的实时预览 v6 或健康正文。

顺序：1. 设置兼容与色温调度；2. 模块保存边界；3. 活动与暂缓服务及休眠联动；4. WPF 布局、托盘和双语；5. 定向与全套测试、双主题／窄窗口渲染核验；6. 文档与 AGENTS 维护。跨模块依赖先明确后台边界再接界面。

基线：dotnet test OLED-Sleeper.sln -c Release --no-restore，512 通过、2 项显式实机测试跳过、0 失败。当前构建有 NuGet 漏洞源不可达及 MinVer 目录权限警告。已有未提交修改全部保留，当前目录实施，不提交／推送。回滚依设计：仅回退本轮相关文件；真实配置降级前关闭功能并备份，不能覆盖恢复记录。

### v5 客户端完成与验收

已实现批准范围：
- MainWindow、MainViewModel.Wellbeing／Snooze、MonitorConfigurationView 与 PrivacySettingsView 实现独立导航、开关、模块保存／放弃／复制、运行状态及暂缓入口；切屏停留当前菜单。
- MonitorSettingsScope、MonitorSettingsSaveService 与 MonitorConfigurationViewModel.Modules 以另一模块保存基线合并写入；旧 PrivacyEnabled 缺失兼容原行为，旧隐私专用“不休眠”在 UI 变为休息关闭；复制此类休息参数时为已启用目标选用遮黑，保留目标开关。
- ScreenSnoozeService 统一独立／联合单调截止，SettingsDialogs.Snooze 提供可校验时长，TrayIconService 提供按屏幕入口；不唤醒、不解除现有保护、不保存草稿。
- PrivacyOptions、WindowsPrivacyActivitySource、MonitorPrivacyService 接入三个用户可选条件、会话与输入保护，MonitorIdleDetectionService 和 ApplyMonitorIdleBehaviorCommandHandler 在真正执行前拒绝过期请求。
- ColorTemperatureSettings／ColorSchedule、ColorSettingsViewModel、ColorSettingsView 与主题资源支持工作／休息原色双选、保存摘要、过渡及左侧试调入口。隐藏的无效输入在保存原色时使用最近有效候选；失败保持原草稿，手动模式仍校验可见值。
- English.json 补齐文案；本轮没有新增依赖、安装或更新运行中的客户端。

实际验证（最终代码）：
1. dotnet test OLED-Sleeper.sln -c Release --no-restore --logger 'trx;LogFileName=v5-client-final.trx' --results-directory artifacts/v5-client-tests：Release 编译成功，531 通过、0 失败、2 项需要真实桌面的测试跳过（共 533）。报告位于 artifacts/v5-client-tests/v5-client-final.trx。最终执行通过进程内 Git safe.directory 配置解决 MinVer 权限警告，未修改全局 Git 设置。
2. 定向测试覆盖旧文件兼容、独立开关、模块保存失败／另一页无效草稿、复制范围、三个免打扰条件及退出等待、联合与独立暂缓／自然到期／提前恢复、已有幕布不解除、过期休息请求、色温保存摘要／原色过渡／候选保留／手动模式。Gamma 使用模拟后端，未读取或改写用户真实调色配置。
3. SCREENKEEPER_RENDER_QA=artifacts/v5-client-qa 生成虚构数据 WPF 图片；检查中文深绿、英文雾白绿、1000×736／700×500 主窗口，展开免打扰、保存栏、双时段摘要／双选项、无验证屏幕和恢复阻塞状态，以及 400×360 暂缓弹窗滚动／底部按钮。修正了隐私选屏跳回休息、隐藏原色输入阻止保存、测试窗口 Owner 尚未初始化、暂缓时间单位动态翻译和渲染夹具未展开隐私内容的问题。未把截图视作真实显示器行为证明。
4. git diff --check 通过。dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore 仍报告已有测试段落的 21 项空白格式问题（ColorProfileTests.cs 的 13 项、ColorTemperatureServiceTests.cs 原有逐屏策略测试的 8 项）；未为通过检查改写无关测试。排除这两文件的同一格式检查通过，其余源码及新增测试无报告。明细 artifacts/v5-client-format.log。
5. NuGet 漏洞数据源因当前网络不可达仍有 NU1900 警告；编译和测试不依赖本次联网更新，未改变包版本。

未验证与风险：未执行真实多屏键鼠归属、跨屏／混合 DPI、锁屏／系统睡眠、DDC/CI 或实际 Gamma 观察，也未启动安装版进行托盘实操；本轮两个原生测试按显式门禁跳过。已修正已知过期请求与休息前隐私免打扰路径，但不能据此声称用户“操作时误保护”的所有实机原因均已排除。客户端试用时需要观察连续输入／静态阅读、条件离开后完整等待和单独／联合暂缓；降级风险按设计处理，新字段不得交给不理解它们的旧客户端使用。

交付为 OLED-Sleeper/bin/Release/net8.0-windows/ScreenKeeper.exe（需要 .NET 8 Windows Desktop Runtime）。未替换正在运行的版本、未修改真实配置、未提交／推送／发布。

已同步 008 设计与计划、010 色温增量、README 和设计资产 approved 状态。已检查并更新 AGENTS.md：记录独立模块保存边界、可选免打扰、单调暂缓、过期请求防护与双时段原色／保存摘要等长期事实；未将待确认 color-v6 或健康正文写成既定要求。

## v5 隐私页布局对齐计划（2026-09-21）

关联 008 设计的隐私页布局对齐修正。先对照 settings-v5-screens.jsx／settings-v5.css 与 WPF，再重排状态卡和正文层级、合并外观弹窗和应用偏好，接入本屏快捷保护与运行状态，最后用虚构数据检查中英文、双主题、700×500、关闭／三方式／错误及弹窗取消。追加针对单屏操作范围和弹窗草稿的行为验证；构建后执行相关测试，记录实际截图、限制和 AGENTS 审查。本轮在当前目录保留已有修改，不提交或推送。


### v5 隐私页布局对齐完成与核验

已按现有批准稿修正 PrivacySettingsView.xaml：状态卡固定于正文滚动区之外，开关、时长和免打扰采用左右对齐行；三个条件缩进，解释折叠，何时／如何保护有独立分区和分隔线，方式选中为实色。背景细项移到 SettingsDialogs.Privacy 的可取消草稿弹窗，主页仅预览／摘要／调整入口；应用启动／移动许可移入应用选择弹窗；跨屏复制在正文末尾。MainWindow 去除旧的散落操作区。

必要接入：MainViewModel.PrivacyPresentation 显示服务的只读运行快照，MonitorPrivacyService.GetSnapshot 区分等待、实际操作、免打扰、暂缓、已保护与异常，等待显示限制于配置时长；本屏立即保护严格使用当前目标，托盘仍提供全部保护。MonitorConfigurationViewModel 只将确认的外观字段写入草稿，取消、缺图失败与放弃均不覆盖保存基线。未修改配置版本、调色、原有自动触发条件或真实数据。

实际验证：
- 首次默认 Release 构建被正在运行的 ScreenKeeper.exe 占用阻止（MSB3027／MSB3021），未终止用户程序。改用独立输出参数 -p:BaseOutputPath=bin/PrivacyLayout/ 后编译成功。
- 相关测试初轮 38 通过；全套回归发现 SharedBackgroundTests 仍查找已迁入弹窗的三项内联按钮，已将它更新为新入口及草稿改变后的缩略图／回退验证。
- 最终 dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/PrivacyLayout/ --logger 'trx;LogFileName=privacy-layout-final.trx' --results-directory artifacts/privacy-layout-tests：533 通过、0 失败、2 项实机测试跳过，总计 535。新增覆盖本屏操作隔离、运行状态、暂缓恢复倒计时及外观弹窗取消／错误／草稿提交。
- 使用 SCREENKEEPER_RENDER_QA=artifacts/privacy-layout-qa 核验中文深绿与英文雾白绿、1000×736／700×500、关闭／三种方式、免打扰展开、滚动时状态卡固定、外观弹窗及固定保存按钮。图片使用虚构屏幕。已目视检查 privacy-v5-final-*、privacy-v5-appearance-* 和 privacy-appearance-dialog-*；没有横向溢出。
- 本轮相关 C# 文件格式检查通过，git diff --check 通过。NuGet 漏洞数据源网络不可达仍有 NU1900 警告，没有改动依赖。

交付为 OLED-Sleeper/bin/PrivacyLayout/Release/net8.0-windows/ScreenKeeper.exe。当前运行实例未重启；查看修正版需要从托盘退出旧实例后再启动此目录中的程序，避免互斥机制只是重新打开旧实例。没有进行真实幕布触发、最小化应用或实际输入观察；截图和模拟后端只证明布局与隔离行为，不替代实机测试。

008 设计／计划与 README 已更新；已检查并更新 AGENTS.md，沉淀批准布局的固定状态区、正文层级、外观弹窗、单屏快捷操作与运行快照边界。本轮修复客户端，不另改已批准 HTML 稿。未提交、推送、安装或修改真实配置。

## 屏幕名称统一计划（2026-09-21）

关联 008 设计「屏幕名称统一」。先扩展现有显示器只读枚举的型号元数据，再统一屏幕管理与色温的格式器及相关选择入口。验证不同型号、同型号／不同大小写、无型号、复制屏、主屏、中英文切换和色温只显示已验证子集时的名称稳定性；检查中英文、双主题和窄窗口渲染。使用独立构建目录避开正在运行实例，不修改真实配置、不写显示器、不提交／推送。完成后记录实际验证与 AGENTS 审查。

### 屏幕名称统一完成与核验

MonitorInfoProvider 复用 DisplayConfig 读取型号并标记重复型号；MonitorTitleFormatter 统一管理页与色温页命名，同型号使用系统显示编号。ColorSettingsViewModel 判重使用完整连接列表，含尚未验证屏幕；验证弹窗同步使用格式器，管理页、复制、暂缓与托盘沿用现有共同名称入口。增加窄窗口选择框全文提示。未改变 HardwareId、色温 scope、配置文件或恢复记录。

验证：
- 首次编译发现新增测试引用不存在的配置属性及验证回调的可空警告，已改为现有 ToSettings 身份检查和能力列表取名；最终构建无新增 C# 警告。
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/DisplayNames/ --filter 'FullyQualifiedName~MonitorTitleFormatterTests|FullyQualifiedName~ColorLiveSettingsTests|FullyQualifiedName~SettingsLayoutTests'`：Release 编译成功，31 项通过、0 失败、0 跳过。记录在 artifacts/display-names-tests/display-names-targeted.trx。覆盖型号／回退／主屏／复制屏、同型号不同大小写、列表顺序变化、验证子集及草稿身份保持。
- 使用 SCREENKEEPER_RENDER_QA=artifacts/display-names-qa 生成虚构数据截图，目视核验中文深绿 1000×736、英文雾白绿 700×500 的型号选择；无横向溢出。
- 本轮 9 个 C# 文件的定向 `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include ...` 通过；`git diff --check` 通过。现有 NU1900 漏洞数据源网络警告仍在，没有修改依赖。本轮未重复全套回归。

交付 OLED-Sleeper/bin/DisplayNames/Release/net8.0-windows/ScreenKeeper.exe；未重启正在运行实例、安装或修改真实配置。真实显示器型号读取、同型号实机组合及插拔未做现场观察；型号由驱动提供，读取失败会回退编号。本轮不涉及实际 DDC/CI 或 Gamma 写入。

008 设计／计划和 README 已同步；已检查并更新 AGENTS.md，记录型号优先、全连接集合判重和显示元数据不作为身份的长期规则。未提交或推送。

## 指定应用选项外移计划（2026-09-21）

关联 008 设计「指定应用选项外移」。在当前目录保留已有修改；将两项许可与两句说明移至隐私主页面的独立分隔区，移除选择器内重复控件和确认时对许可的写入。复用现有布局核验，检查指定应用／其他方式、双语双主题及窄窗口；执行现有布局、隐私设置及指定应用相关测试，独立输出以避开当前运行程序。无需新依赖或配置迁移。开始前原 DisplayNames 构建的 SettingsLayoutTests 基线 4 项通过。完成时同步文档和 AGENTS，不提交或推送。

### 指定应用选项外移完成与核验

PrivacySettingsView.xaml 新增 ApplicationOptionsSection，置于应用选择／测试之后、更多设置之前，复用横向分隔线和主题色；两项勾选直接绑定本屏 LaunchIfMissing／MoveFromOtherScreen 草稿，说明文字随语言切换并支持换行。非指定应用方式和隐私关闭时随对应内容隐藏。SettingsDialogs.cs 移除原弹窗中的两项许可与说明，确认应用时只更新应用身份，保留页面上已有许可草稿。既有 SettingsLayoutTests 仅调整截图滚动位置，未为控件搬移增加重复测试。

实际验证：
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/ApplicationOptions/ --filter 'FullyQualifiedName~SettingsLayoutTests|FullyQualifiedName~SettingsV2Tests|FullyQualifiedName~SettingsV3Tests|FullyQualifiedName~ScreenApplicationServiceTests'`：编译成功，31 项通过、0 失败／跳过。记录 artifacts/application-options-tests/application-options.trx。补充截图位置后重跑 SettingsLayoutTests，4 项通过。
- SCREENKEEPER_RENDER_QA=artifacts/application-options-qa 生成双语、双主题和 1000×736／700×500 虚构屏幕渲染。目视检查深绿中文及雾白绿英文的小窗口和完整分隔区；文字未溢出，页尾保存栏固定，两个许可位于更多设置之前。交付预览 application-options-forest-zh-CN-1000.png。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/UI/Views/SettingsDialogs.cs OLED-Sleeper.Tests/UI/ViewModels/SettingsLayoutTests.cs` 通过；`git diff --check` 通过。现有 NU1900 网络警告仍在，未修改依赖。

交付 OLED-Sleeper/bin/ApplicationOptions/Release/net8.0-windows/ScreenKeeper.exe，未替换／重启当前进程。未操作真实应用窗口进行启动或跨屏移动；本轮只调整呈现，原有第三方应用的权限／窗口兼容限制仍适用。未修改真实配置、提交、推送或安装。008 设计／计划、README 与 AGENTS.md 已同步，已将旧的弹窗内许可规则更新为主页面独立分隔区规则。

## 跨屏应用入口统一计划（2026-09-22）

关联 008 设计「跨屏应用入口统一」。提取隐私页现有链接样式供两页共用，将休息入口移至正文末尾并同步弹窗标题；复用既有复制逻辑。随后执行模块复制／设置布局相关测试，补充休息页末尾及关闭状态渲染，核验双语双主题和窄窗口；使用独立输出避免占用正在运行程序。完成后记录结果并审查 AGENTS.md，不提交／推送，不更改真实配置或重启程序。

### 跨屏应用入口统一完成与核验

MonitorConfigurationView.xaml 将入口移至滚动正文末尾；与 PrivacySettingsView.xaml 共用 Calm.xaml 的 ScreenSettingsLink，文案、左对齐、边距和按钮反馈相同。SettingsDialogs.cs 统一弹窗标题，原有全部／指定目标及按模块复制逻辑保持不变。现有布局夹具增加休息页底部、关闭状态渲染，未为纯呈现搬移新增重复测试。

实际验证：
- `dotnet test OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/ScreenApply/ --filter 'FullyQualifiedName~ScreenModuleTests|FullyQualifiedName~SettingsLayoutTests|FullyQualifiedName~SettingsV2Tests|FullyQualifiedName~SettingsV3Tests'`：Release 构建成功，28 项通过、0 失败／跳过。首轮新增截图代码误将内部控件滚动区也计入，已改为按 RestScroll 名称定位后重跑通过。记录 artifacts/screen-apply-tests/screen-apply-final.trx。
- SCREENKEEPER_RENDER_QA=artifacts/screen-apply-qa 生成中英文、两套主题及 1000×736／700×500 虚构屏幕截图；目视核验中文深绿休息／隐私页底部一致、英文雾白绿窄窗口和中文关闭状态。入口可见且无横向溢出，保存条保持固定。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore --include OLED-Sleeper/UI/Views/SettingsDialogs.cs OLED-Sleeper.Tests/UI/ViewModels/SettingsLayoutTests.cs`、三个 XAML 的 XML 解析、`git diff --check` 均通过。现有 NU1900 漏洞数据源网络警告仍在，未改动依赖。

交付 OLED-Sleeper/bin/ScreenApply/Release/net8.0-windows/ScreenKeeper.exe；未重启当前运行实例、修改真实配置或实操屏幕休眠。本轮仅呈现调整，实际硬件行为未重新验证，没有新增配置或硬件控制风险。008 设计和计划已同步；已检查并更新 AGENTS.md，记录两模块统一底部入口与共用样式规则。未提交或推送。
