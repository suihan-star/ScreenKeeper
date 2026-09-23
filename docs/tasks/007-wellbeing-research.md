# 007 · 健康提醒与色温调节：竞品研究

研究日期：2026-09-17。关联 [任务设计](007-wellbeing-design.md) 与 [开发计划](007-wellbeing-plan.md)。

状态：研究完成，设计待评审。用户已确认健康提醒采用轻提醒、主动开始后倒计时；色温采用固定作息时间。使用环境为两块屏幕、未开启 HDR，以编程为主、偶尔页面设计。尚未授权实现业务代码。

## 1. 推荐结论

- 新增两个独立模块：健康提醒按当前 Windows 会话统一计时；色温调节使用一份作息表、按屏选择参与。均不依赖现有屏幕管理开关。
- 健康提醒可先实施。借鉴 FadeTop 的渐显、低打扰，但将“出现提示”“开始休息”“倒计时结束”分开，不以无输入或提示停留时间证明用户已经休息。
- 色温先做硬件与恢复验证，再确定可发布的设备范围。借鉴 f.lux 的自动变化、平滑过渡、随时暂停；不承诺所有显卡、HDR、复制屏和校色环境均可控制。
- 两模块服务“提醒离屏休息”和“按个人偏好调整屏幕暖色”。不提供健康评分、诊疗结论或保护视力的效果保证。

## 2. 证据口径与本轮边界

- **源码已实现**：当前工作区能找到相应实现；不等于本轮运行验收通过。
- **官方明确**：产品官网、官方教程或更新记录明确写出；不等于本轮实测。
- **官方历史说明／截图值**：只证明相应版本或图片展示内容，不直接视为当前安装版默认值。
- **设计判断**：我们依据使用场景和工程约束做出的取舍，不是竞品事实。
- 本轮没有安装或运行两款竞品，没有操作真实显示器或读取用户应用内容。没有可报告的本轮运行演示验证。
- FadeTop 的 HTTPS 抓取返回 502；直接读取原始 HTTP 首页、教程、历史页均返回 200。因此采用其原始 HTTP 官方资料，不以搜索摘要替代。
- f.lux 官网、Windows 欢迎页、FAQ 和更新页已访问。FAQ 混合了不同平台和年代的内容，发生冲突时优先采用较新的 Windows 更新说明。

## 3. 我们当前具备什么

当前工作区只有一个已识别 Git 仓库，main 位于上游基线 27f1c77，已有大量产品开发的未提交文件。研究以这些当前文件为依据，未覆盖或提交它们。已读取仓库及父级可发现的 AGENTS.md／CLAUDE.md；没有发现子目录补充规则。

| 范围 | 当前事实与来源 | 对新模块的影响 |
| --- | --- | --- |
| 产品与用户 | [README](../../README.md)：Windows 多显示器用户；net8.0-windows WPF；中英文、静绿主题、托盘常驻 | 面向长时间桌面办公，也允许只开一个新模块 |
| 屏幕休息 | [空闲检测](../../OLED-Sleeper/Features/MonitorIdleDetection/Services/MonitorIdleDetectionService.cs)、[行为处理](../../OLED-Sleeper/Features/MonitorBehavior/Handlers/ApplyMonitorIdleBehaviorCommandHandler.cs)：按屏空闲后遮黑或 DDC 调暗 | 这是屏幕空闲管理，不能直接当作人的工作／休息计时 |
| 隐私保护 | [MonitorPrivacyService](../../OLED-Sleeper/Features/MonitorPrivacy/MonitorPrivacyService.cs)、[输入源](../../OLED-Sleeper/Features/MonitorPrivacy/WindowsPrivacyActivitySource.cs)：独立计时、隐私幕、桌面和指定应用 | 健康提示不得解除隐私，也不能依赖隐私启用后才开始收集活动 |
| UI 与草稿 | [主窗口](../../OLED-Sleeper/MainWindow.xaml)、[设置页](../../OLED-Sleeper/UI/Views/MonitorConfigurationView.xaml)、[MainViewModel](../../OLED-Sleeper/UI/ViewModels/MainViewModel.cs)：选屏、两个页签、保存／放弃、草稿重建 | 全局健康设置应独立；跨模块保存必须说清作用范围 |
| 亮度能力 | [DdcCiCapabilities](../../OLED-Sleeper/Features/MonitorInformation/Models/DdcCiCapabilities.cs)、[DdcCiSession](../../OLED-Sleeper/Features/MonitorDimming/Services/DdcCiSession.cs)：亮度能力与读写 | 支持亮度不证明支持色温，不能复用同一“支持”标志 |
| 拓扑 | [MonitorInfo](../../OLED-Sleeper/Features/MonitorInformation/Models/MonitorInfo.cs)、[MonitorInfoProvider](../../OLED-Sleeper/Features/MonitorInformation/Services/MonitorInfoProvider.cs)：DeviceName 会重分配，HardwareId 用于存储，复制屏可共用逻辑面 | 色温需要识别实际控制范围；不能盲目给复制屏逐屏开关 |
| 存储 | [文件存储](../../OLED-Sleeper/Storage/AppDataFileStore.cs)：单文件临时写入、替换和备份；[设置服务](../../OLED-Sleeper/Features/UserSettings/Services/MonitorSettingsFileService.cs)：schema 1、保留离线屏、写成功才通知 | 可复用原子单文件机制；不能把多个 JSON 的保存称为事务 |
| 恢复 | [启动协调](../../OLED-Sleeper/Infrastructure/Hosting/ApplicationOrchestrator.cs)、[亮度记录](../../OLED-Sleeper/Features/MonitorDimming/Services/OriginalBrightnessStore.cs)：亮度启动／退出恢复 | 色温要独立恢复记录；不能直接照搬未传递写盘失败的亮度记录接口 |

**已规划／待验证**：006 文档记录现有设置布局和隐私扩展已实现，并记录上一轮自动化检查；真实多屏输入、DDC 和混合 DPI 仍有实机验收事项。这些不是本轮验证结果。

**尚未具备**：源码、测试和已有任务文档中未发现健康提醒、色温／Gamma、HDR 色温能力检测实现；未发现可直接复用的会话锁定／系统睡眠事件处理。新增模块不能默认这些基础能力已经存在。

## 4. FadeTop：值得借鉴的是低打扰的休息提醒

| 研究项 | 官方证据与解释 |
| --- | --- |
| 用户与触发 | 面向持续使用电脑的人。官网说明持续使用较久后尝试在不太忙时渐显，避免正在频繁操作或看起来离开的时刻；并非固定整点闹钟。[官网](http://www.fadetop.com/) |
| 入口与暂停 | 托盘点击可预览／收起；右键有 Show Fader、Auto Fade、Options、Exit。关闭 Auto Fade 停止自动提醒。[教程](http://www.fadetop.com/tutorials.html) |
| 提醒与取消 | 半透明颜色／图片覆盖并显示时间等信息；按键或移动鼠标可取消，约一分钟后再尝试。教程用连续使用超过 15 分钟举例；至少显示 20 秒未过早关闭就假定已休息。[教程](http://www.fadetop.com/tutorials.html) |
| 配置 | 基础项包括活动超时、不透明度、前景／背景颜色、全屏应用时阻止提醒；图片、声音等高级配置需编辑 XML。[教程](http://www.fadetop.com/tutorials.html) |
| 多屏与版本 | 官网标示 3.1.0.175；历史页记载 3.1 于 2015-03-27 加入多屏，未给出 build 175 的单独发布日期，也未解释跨屏同步／全屏冲突规则。[官网](http://www.fadetop.com/)、[更新历史](http://www.fadetop.com/history.html) |

**价值判断**：渐显比突然弹窗更符合持续办公，托盘试用让用户先理解效果；只保留少量常用设置能降低配置成本。

**官方静态界面观察**：已视觉检查[设置截图](http://www.fadetop.com/images/screenshot-fadetop-options-dialog.png)，其中显示活动阈值 15 分钟、不透明度 75%、全屏阻止已勾选。这些仅为官方图片展示值，不作为当前安装版默认值。没有运行或动态交互验证。

**局限与适配判断**：持续忙碌时可能很难等到合适提醒机会，具体最大延后算法未披露；无输入可能是在阅读，提示停留也不等于休息。“Health Index”只是产品按提示行为计算的比例，不适合引入为真实健康指标。XML 高级设置的发现和使用成本较高；我们的常用参数应直接可编辑。

**未验证**：安装版真实默认值、当前 Windows 11／混合 DPI 表现、屏幕热插拔、锁屏／恢复、持续打字时最长延后、复制屏、配置写失败和退出恢复。旧作者发布帖中的固定刻钟策略属于历史版本，不用于描述当前 3.x 官网机制。

## 5. f.lux：值得借鉴的是自动安排与可控退出

| 研究项 | 官方证据与解释 |
| --- | --- |
| 核心价值 | 随时间调整屏幕色彩，夜间偏暖；一次配置后自动运行。[官网](https://justgetflux.com/) |
| 初次流程 | 设置位置、通常起床时间，再调整偏好；v4 介绍日间、傍晚、睡前三段。[欢迎页](https://justgetflux.com/news/pages/v4/welcome/)、[v4 介绍](https://justgetflux.com/news/pages/v4/bigupdate/) |
| 使用与退出 | 托盘改设置；可暂停一小时、到早晨，支持指定应用／全屏例外。欢迎页写有 Alt-End 暂停一小时。[欢迎页](https://justgetflux.com/news/pages/v4/welcome/) |
| 默认值边界 | 较早的 Windows 欢迎页写夜间默认 3400K，不足以确认 2026 安装包的全部默认值；本设计参数另行定义。[欢迎页](https://justgetflux.com/news/pages/v4/welcome/) |
| 多屏边界 | 4.100 加入逐屏停用；4.141（2026-04）仍说明部分环境必须共享变换，逐屏校准仍在推进。[更新记录](https://justgetflux.com/news/pages/v4/readme/) |
| 冲突与恢复 | FAQ 涉及夜间模式、校准加载器竞争、驱动问题及退出后残留偏色；正常停用意图是回到原校准。[FAQ](https://justgetflux.com/faq.html) |

**价值判断**：自动安排减少反复操作；渐变减少突变感；限时恢复原色适合设计、看图、演示等任务。界面必须区分“已配置”和“真正生效”。

**局限与适配判断**：位置、时区和起床时间增加初次配置成本；我们已按用户选择采用固定作息。暖色会影响颜色判断，需要明确暂停；多屏停用不代表任意屏幕都能独立准确调色。暂不引入极暖滤镜、灯具联动、昼夜节律评分或专业校准编辑器。

**未验证**：当前安装包默认参数、实际自动过渡、HDR 完整支持范围、各显卡／扩展坞覆盖率和异常退出恢复。未观察到的运行效果不按实测描述。

## 6. 借鉴取舍

| 候选能力 | 结论 | 用户价值、复杂度与关系 |
| --- | --- | --- |
| 渐显、预览、稍后提醒 | 直接借鉴思路 | 每天高频；降低打扰；与隐私幕分开承载，中等复杂度 |
| 按连续工作节律提醒 | 调整后引入 | 全局统一周期，有免打扰和明确休息动作；不依赖选中屏幕 |
| 任意输入取消、约一分钟重试 | 调整后引入 | 改为显式稍后／跳过；避免无意取消和频繁催促 |
| 健康指数／默认判定已休息 | 不建议引入 | 无可靠测量依据；用“提醒已显示／计时已结束”等事实状态 |
| 全屏免打扰 | 调整后引入 | 健康首版支持保守检测和手动暂停；不能声称识别所有会议 |
| 自动色温、平滑变化 | 调整后引入 | 已选固定作息；后端可逆、范围可控才开放，复杂度高 |
| 逐屏参与／临时原色 | 直接借鉴思路 | 适合多屏分工；受实际显示管线能力约束 |
| 每屏独立曲线、应用自动例外 | 暂缓 | 首版共享时间表＋单屏暂停，后续验证进程／屏幕归属 |
| 日出日落／自动定位／睡前三段 | 暂缓 | 用户选固定作息；后续有实际需求再增加 |
| HDR／复制屏全面支持 | 暂缓承诺 | 先验证，不能用 UI 选项替代底层能力 |
| 极暖解锁、自动修改其他调色工具、灯具联动 | 不建议首版引入 | 超出目标，维护面和恢复风险明显增加 |

## 7. 一手技术依据

- 微软明确不推荐传统 Gamma API：可能返回成功却未生效，被其他程序／显示事件覆盖，HDR 与校准交互未定义。[SetDeviceGammaRamp](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-setdevicegammaramp)。因此它只是受限 SDR 预研候选，不是已确定的正式后端。
- 硬件色温需要独立能力及档位检测；改变色温可能连带改变 RGB gain／drive，任意显示器需要实机核验。[SetMonitorColorTemperature](https://learn.microsoft.com/en-us/windows/win32/api/highlevelmonitorconfigurationapi/nf-highlevelmonitorconfigurationapi-setmonitorcolortemperature)、[GetMonitorCapabilities](https://learn.microsoft.com/en-us/windows/win32/api/highlevelmonitorconfigurationapi/nf-highlevelmonitorconfigurationapi-getmonitorcapabilities)。已有 DDC 亮度成功不能替代这些检查。
- GetLastInputInfo 仅覆盖调用方会话，时间戳不保证严格递增。[微软文档](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getlastinputinfo)。应隔离读取、处理回绕／异常，用单调时钟计算时长，不收集输入内容。
- 眼部不适与屏幕光损伤、睡眠效果是不同问题；权威综述也指出此类软件的具体效用仍需研究。[AAO EyeWiki](https://eyewiki.aao.org/Computer_Vision_Syndrome_%28Digital_Eye_Strain%29)。本项目只承诺提醒行为和色彩调节，不承诺医疗效果。

## 8. 本轮验证与限制

实际执行了 Git 状态／仓库识别、规则和相关源码阅读、功能关键词检索，以及上述官方网页读取。仅新增本任务文档，不运行构建／单元测试／竞品／硬件测试；没有业务代码变更需要执行运行回归。

已检查 AGENTS.md，本次无需更新：两个新模块尚属设计，没有产生已实施的模块、构建或长期架构事实。正式实施时应按开发计划同步维护。
