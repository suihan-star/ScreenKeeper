# ScreenKeeper · 静绿设计提案

## 独立休息／隐私菜单 v5（已确认并实施客户端）

入口：[settings-v5.html](settings-v5.html)，本地预览 http://127.0.0.1:4311/screenkeeper-calm/settings-v5.html 。使用 baoyu-design 沿用 Calm 深绿／雾白绿和双语布局，方案已确认并接入 WPF 客户端；浏览器稿继续独立保留为交互示意。

- 左侧拆成屏幕休息、隐私保护、健康提醒、色温调节，通用设置保留底部。休息与隐私共享当前选屏，各有独立开关和保存范围。
- 隐私页按选屏、当前运行状态、何时保护、如何保护排列；“使用时不打扰”开启后可独立勾选鼠标停留、前台窗口、整机输入三个条件，任一满足即暂不保护。关闭收起并保留选择，启用但未选择时提示补选；状态显示已保存规则命中的条件。休息页“暂缓休息”和隐私页“暂缓保护”均在本页打开，不再跨菜单跳转；可自定义分钟／小时，默认只操作当前模块，勾选后才同时暂缓另一项。独立计时互不影响，联合暂缓显示“恢复两项”。倒计时、主动恢复和托盘快捷操作均可点击演示。
- 窗口外可切换操作／阅读／离开场景，以及多屏、输入异常、加载、无屏幕和保存失败示例。所有计时、保护、复制和保存均在网页内模拟；不采集输入，不控制真实屏幕，不写产品配置。
- 外观、指定应用和复制参数保留入口；图片使用本地样例演示。健康与色温沿用既有模块展示，本轮不确认其他待审设计。
- 文件：`settings-v5.html`、`settings-v5.css`、`settings-v5-screens.jsx/js`、`settings-v5-shell.jsx/js`，复用现有本地资源。设计、兼容边界和验证记录见 `docs/tasks/008-app-layout-design.md` 与 `008-app-layout-plan.md` 的 v5 章节。

## 全应用布局 v4（已确认并实施客户端）

2026-09-19 增加双主题：**标题栏语言旁 → 主题切换**，通用设置保留同步的外观预览卡；默认深绿，可选雾白绿（沿用原 `style.css` 的 `.app.fog` 配色）。主界面与弹窗即时切换，浏览器记住主题；不会保存或放弃业务草稿。刷新仍重置示例配置。`settings-v4-themes.css` 承载浅色适配与主题预览卡；健康和隐私场景保持独立外观。WPF 客户端已接入多主题，使用独立产品 UI 偏好持久化；实际验证见 008 执行计划。

入口：[settings-v4.html](settings-v4.html)，本地预览 http://127.0.0.1:4311/screenkeeper-calm/settings-v4.html 。使用 baoyu-design 将三个模块整理为一级导航，健康与色温各有两个页签；加入外观示意、色温设置引导及异常处理分支。关联 `../../docs/tasks/008-app-layout-design.md` 和 `008-app-layout-plan.md`。

可体验中英文、标准／700×500 窗口、模块草稿与保存／放弃、批量复制、隐私图片与指定应用、健康渐显预览、色温试调和 15 秒验证模拟。窗口外场景选择可查看未验证、恢复冲突、HDR、断开、关闭、无屏幕和保存失败；切换场景及刷新会重置示例数据。

`settings-v4.css` 为独立样式；`settings-v4-ui.jsx` 是呈现组件，`settings-v4.jsx` 集中管理原型状态。对应 `.js` 使用已存在的 `artifacts/design-tools/babel.min.cjs`、`transform(source, {presets:['react']})` 生成。直接预览使用本地 React 与已生成 JS，不需要安装依赖。

复用本目录的 `settings-v2-components.js` 图标、`settings-v2-icon.svg`、`curtain-hills.svg`、`curtain-grove.svg`，配色取自当前 Calm 主题及 v3。旧版本保持不变。所有设备、验证、保存和恢复操作仅在原型内存中模拟；不会读取产品配置、改变 Gamma、归档真实文件或操作真实窗口。


## 隐私设置 v3（已确认并实施）

入口：[settings-v3.html](settings-v3.html)，本地预览 http://127.0.0.1:4311/screenkeeper-calm/settings-v3.html 。使用 baoyu-design 新增独立版本，旧稿保留；用户已确认，WPF 实施及验证已完成。通用设置中的独立配置说明按最终反馈移除。

启用开关取代关闭选项，等待时间统一置顶，三种方式只展示各自设置；幕布提供小预览和独立图片管理，暂缓和应用行为折叠到更多设置。关闭后记住选择，保存后生效；本次暂缓不修改默认值。初始展示隐私页便于评审，屏幕休息仍为第一个页签。

新增 `settings-v3.html`、`settings-v3.css`、`settings-v3-privacy.js` 和 `settings-v3.js`，复用 v2 只读基础资产。测试与配置只在网页内存中模拟，不操作真实窗口；刷新恢复初始数据。关联 006 设计与计划的 v3 章节。

## 设置布局 v2（已确认并实施）

新增入口：[settings-v2.html](settings-v2.html)，本地预览 http://127.0.0.1:4311/screenkeeper-calm/settings-v2.html 。沿用静绿与 A+ 图标，左侧选屏、右侧隐私/休息分类，底部固定保存；全部三轮方案已于 2026-09-17 确认，WPF 客户端已实施。

- 可点击选屏、管理开关、隐私/休息方式、等待时间、保存/放弃、通用设置及中英文切换。
- 第二轮：屏幕休息为首个默认页签；隐私等待支持秒/分钟/小时，暂缓支持分钟/小时；标题区可将当前设置应用到全部或指定屏幕，保留目标管理开关，保存后生效。
- 第三轮：“隐私保护 → 指定应用”可选择示例运行窗口或填写程序路径，配置未运行时启动及跨屏移动偏好。“测试切换”演示成功与失败保留幕布，不执行真实程序。新增 `settings-v2-app.js`；对应客户端能力已实施；网页仍只演示虚构数据。
- 隐私幕外观支持默认纯色、选择本地单张/多张图片、缩略图移除及轮播间隔；所有幕布预览只显示继续使用按钮。新增 `settings-v2-extras.js` 与 `curtain-hills.svg`、`curtain-grove.svg` 两张本地示例，选择图片不会上传。
- 顶部评审工具切换普通/最大化/700×500。模拟标题栏支持拖动、双击、最大化/还原；窗口隐藏后可从页面中的托盘演示入口恢复。
- 演示面板提供无显示器、检测中、检测失败、不支持调暗、保存失败及重置。所有数据仅保留在网页内存中，刷新页面即可还原，不读写真实配置。
- 文件：`settings-v2.html`、`settings-v2.css`、`settings-v2-components.js`、`settings-v2.js`、`settings-v2-icon.svg`；复用本目录 `vendor/`，没有网络运行时依赖。
- 设计及验证记录：[006 设计](../../docs/tasks/006-settings-layout-design.md)、[006 计划](../../docs/tasks/006-settings-layout-plan.md)。客户端实施及真实 WPF/原生窗口测试结果见 006 计划。

## 原始静绿方案（已实施）

状态：用户已确认，客户端已按深绿主方案实施。此目录仍是设计原型，不接入真实显示器；实际 WPF 结果与测试见 003 计划。

## 依据与假设

- 用户选择：深绿主方案＋雾白绿对照稿；在当前目录保留已有变更；确认设计后再开发。
- 参考：[Awwwards /zeroz](https://www.awwwards.com/sites/zeroz-brand-site)、[品牌官网](https://otsuka-air.jp/)。已查看官网实际渲染，提取深绿空间、绿色层次与简洁排版；下面色值是为桌面工具重新配制，并非官网精确取样。
- 功能以现有 WPF 界面为准：按显示器配置、遮黑/调暗、空闲时长、三个保持唤醒条件、保存/放弃。数据全部为演示，不写入产品配置。
- 无外部 UI 库；使用 baoyu-design 方法与本地 React 原型运行时。标识仅为本轮概念符号，不作为已确认的新应用图标。

## 设计理由

两种配色共用布局，先选显示器，再设置行为。森林绿压低大面积背景亮度，浅鼠尾草绿突出选中与主操作；雾白绿用淡绿灰取代纯白。正文保持足够对比度，不把“低对比”误当作舒适。无闪烁、无光晕、无持续动画。舒适感为设计目标，不代表医学护眼效果。

原型入口：`index.html`，包含主题、状态与窗口尺寸切换。底部保存栏固定，内容溢出时内部滚动。预览控制栏在窗口外，不属于未来客户端。

## 本地预览

在仓库根目录执行 `python -m http.server 4311 --bind 127.0.0.1 --directory designs`，打开 http://127.0.0.1:4311/screenkeeper-calm/index.html 。不需要安装 npm 依赖，运行时已随原型附带。

设计与实施边界见 `../../docs/tasks/003-calm-ui-design.md`，验收记录见 `../../docs/tasks/003-calm-ui-plan.md`。

## 文件与素材

- `index.html`：原型入口；`style.css`：两套主题；`prototype.jsx`：交互设计源文件；`prototype.js`：已编译可直接运行的版本。
- `deep-green.png` / `fog-green.png`：标准窗口；`compact.png`：紧凑窗口滚动后；`error.png`：读取失败。
- `vendor/`：固定 React / ReactDOM 18.3.1，来自 npm 官方包的 unpkg 分发，MIT 许可证已附。没有引入项目包管理器或产品运行时依赖。
- JSX 本轮使用 `artifacts/design-tools/babel.min.cjs`（Babel standalone 7.29.0）及 `transform(source, {presets:['react']})` 编译。直接查看设计不需要 Babel。
- `_d_meta.json` 由 baoyu-design 的 `record-asset.mjs` 维护；已确认的原始方案、图标与 v2 为 `approved`，隐私设置 v3 为 `approved`。
- `translations.js`：英文原型文案。窗口右上角“语言 / Language”提供简体中文和 English；切换即时生效并保留显示器编辑状态。语言只保留于当前预览内存，不写真实产品配置；外围评审工具仍为中文。`english.png` 为英文预览截图。

## 评审重点

### 托盘图标（已确认并实施）

入口：`tray-icon.html`，本地地址 http://127.0.0.1:4311/screenkeeper-calm/tray-icon.html 。最终方案为 A+「叠屏静夜」原色放大版；支持实际尺寸深浅背景预览及 SVG 下载，原 A / B 供历史对照。

素材位于 `tray-icons/`：透明 SVG、review.jsx / review.js / review.css 和历史评审截图。design-canvas.jsx 来自本机 baoyu-design 的 starter-components/design-canvas.jsx；其编译版本与既有本地 React 配合，无外部网络依赖。客户端已接入最终图标；最新原生对照图为 `../../artifacts/tray-large/size-comparison.png`。设计和验证记录见 `../../docs/tasks/004-tray-icon-design.md` 与 `../../docs/tasks/004-tray-icon-plan.md`。

深绿界面、布局及中英文入口已经确认并实施；雾白绿仅作同布局对照。浏览器原型与原生客户端的字体、下拉框、高 DPI 表现可能不同。

第二轮图标：默认预览 A+「叠屏静夜」（A 叠屏轮廓＋B 月牙），原 A / B 仍可切换对照。新增 stackmoon-dark.svg、stackmoon-light.svg；review-moon.png 为本轮截图。

A+「叠屏静夜」已获用户确认并接入客户端，应用最终 SVG / ICO 位于 OLED-Sleeper/Assets，生成方式和实施验收见 004 文档。此 HTML 继续作为视觉评审稿。

## 任务栏图标可见度 v6（2026-09-23，已确认 A）

入口：[taskbar-icon-v6.html](taskbar-icon-v6.html)，本地预览 http://127.0.0.1:4311/screenkeeper-calm/taskbar-icon-v6.html 。用户最终确认任务栏和托盘均采用 A 雾绿轮廓：深色任务栏用浅绿，浅色任务栏用深绿。已接入客户端并完成相关构建／检查，实际 Explorer 表现待打开新版确认。原型默认 A，B／C 仅保留为历史对照；仍可切换截图中的模拟图标、原图和深浅背景，提供 16/20/24/32 CSS 像素预览及放大稿。

素材及交互源文件位于 `taskbar-icons-v6/`。用户提供的 `taskbar-reference.png` 为原截图直接复制；图标替换仅使用 HTML 叠加图层。SVG 沿用本项目 A+ 源路径，React／画布／Babel 复用已有文件。没有新运行时依赖。设计与浏览器核验记录见 `../../docs/tasks/004-tray-icon-design.md`、`../../docs/tasks/004-tray-icon-plan.md`。原已确认 `tray-icon.html` 保持不变。

## 色温实时预览 v6（待确认）

入口 `color-v6.html`，复用已确认 v5 的两页签与每屏设置。主方案 A 拖动即预览，B 对照方案先开启预览后展示四个操作；评审切换位于产品窗口外。两方案均只在设计稿内部模拟暖色，不改变真实屏幕。支持 15 秒恢复反馈、保留草稿、两主题、两语言和 700×500。

本轮新增 `color-v6-flow.jsx/js`、`color-v6-shell.jsx/js`、`color-v6.css`；v5 入口及客户端保持原状。设计及核验记录见 `../../docs/tasks/010-color-onboarding-design.md` 和同编号计划。

## 健康卡与隐私幕效果稿

入口 `health-card-v2.html`。展示居中 430 像素卡片、实心／描边／文字三类按钮、仅含「进入此屏幕」按钮的隐私幕和三组可切换润色正文。卡片布局与按钮文案已同步到客户端，三组正文仍待用户选择；不操作真实屏幕或计时。使用目录内既有 `forest-mist.png`，没有新增外部资源或依赖。见 007 设计及计划。

## settings-v5 客户端实施（2026-09-21）

用户已确认 settings-v5 的独立屏幕休息／隐私保护菜单、复选式使用免打扰、单项与联合暂缓及色温双时段原色／自定义、保存摘要与试调位置，资产登记为 approved。上述交互已接入 WPF 客户端；设计与实际验证见 ../../docs/tasks/008-app-layout-design.md 和 ../../docs/tasks/008-app-layout-plan.md。浏览器原型仍只模拟，不写真实配置或屏幕。color-v6 以及健康关怀正文继续待确认。
