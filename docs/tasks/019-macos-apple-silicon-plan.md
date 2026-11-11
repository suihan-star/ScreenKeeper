# 019 · macOS Apple Silicon 版开发计划

对应 [设计](019-macos-apple-silicon-design.md)。日期：2026-11-10，状态更新于 2026-11-11。首轮原生预览已由云端 Apple Silicon Mac 编译、执行 38 项核心 XCTest 并生成应用 ZIP／DMG，朋友无需编译源码，只需实机测试。A 的显示器、权限和交互验收未完成，B 为开发预览；不将云构建通过当作 C 硬件能力的放行依据。实际提交、构建和产物证据见 [020 计划](020-github-hosting-plan.md)。

## 本轮已完成

- [x] 读取项目约束、README、既有色温与布局设计，检查 Git 状态，保留已有未提交修改。
- [x] 确认现有项目为 Windows WPF，核对界面、显示控制与恢复规则的迁移边界。
- [x] 按用户澄清，将所有 M 系列与动态外接屏适配作为产品目标，不以测试机型号限定应用支持范围。
- [x] 核对 Microsoft／Apple 官方资料，记录能力检测、失败反馈和实机验证要求。
- [x] 新建设计与计划草案，并加入文档索引。
- [x] 采用 SwiftUI + AppKit、macOS 13 部署目标、Swift tools version 5.9；产品构建限定原生 arm64。
- [x] 已使用云端 macOS 15.8.2 arm64／Xcode 16.4 构建和执行核心测试；显示器交互测试机仍待朋友提供。测试设备信息只用于覆盖矩阵，不是产品型号白名单。

## 阶段与依赖

### A · 平台能力验证，先降低关键风险

依赖：Mac 开发环境。先实现只读显示枚举、稳定身份候选、屏幕位置／缩放与会话事件记录，诊断不采集用户内容。

验证每屏软件遮黑与隐私幕在桌面、全屏、Spaces、台前调度的表现；验证输入归属和按需权限。另行安排用户观察的色温试验：先持久化原色，再指定单屏短时调整和自动恢复。色温验证与核心设置开发可并行，但未验证能力不进入默认启用状态。

输出：实测能力表、失败状态与降级规则、最低系统版本决定、明确可开放的首版功能。若原生窗口或颜色接口不能满足要求，先更新设计，不承诺与 Windows 所有底层效果一致。

### B · 首个可使用的原生版本

依赖：A 的显示身份、覆盖层和权限验证通过。

建立独立 Mac 应用与测试目标；实现主设置窗口、中文／英文、双主题、菜单栏、独立配置和退出清理。随后实现逐屏软件休息、隐私幕、暂缓／免打扰、跨屏应用设置以及全局健康提醒。动态新增／断开屏幕必须保留设置与草稿；不能以位置或屏幕序号代替稳定身份。

验收：屏幕数量变化、不同缩放／旋转、同型号屏、权限拒绝和撤回、缺图、保存失败、重复退出及迟到请求。设置窗口不能因单项设备能力失败而不可用。

### C · 窗口控制与颜色能力

依赖：A 对相应系统能力验证通过，B 的存储和生命周期可用。

逐步实现回到桌面、指定应用和逐屏色温。先遮挡后操作，失败保留幕布；应用存在多个候选窗口时不猜测。色温继承原色保护、作息和预览交互规则，使用 Mac 自身身份和颜色模型；重点覆盖在途取消、快速锁／解锁、断开重连、颜色被系统或第三方改写、日志损坏和进程意外退出。

硬件亮度／独立硬件休眠根据实测结果决定是否纳入首版，不用私有接口或全局操作悄悄替代逐屏能力。功能尚不可用时给出明确原因，不展示没有效果的开关。

### D · 长期运行与发行验证

依赖：B 和所选 C 功能通过测试。

使用有代表性的不同 M 系列／系统版本、内置和外接显示器、直连与扩展坞执行兼容矩阵；没有固定测试设备不等于拒绝其他设备。优先覆盖真实锁屏关屏过夜、合盖／开盖、休眠唤醒、多次热插拔与长期运行。

在 Mac 上执行项目实际提供的 Swift Package 构建与测试命令，核验产物 arm64 架构、资源与许可、签名公证、干净账户安装／升级／卸载。不预称存在 Xcode scheme 或已通过未执行的命令。双语帮助说明权限、能力边界和恢复方式。保留独立构建标识与源码清单，正式发布另行确认。

## 验证策略

- 纯逻辑：以现有 Windows 日程、时段原色、单调计时、暂缓、模块草稿和恢复竞态场景为行为参照，迁移为 Mac 自动测试；不是直接运行 WPF 测试。
- 原生集成：只操作测试自己创建的窗口，验证跨屏归属、取消与权限失败；自动测试不能代替真实软件的窗口兼容验收。
- 硬件：受控观测逐屏生效、其他屏不变及原色恢复；至少包含睡眠与关屏重连后的重复验证，不把 API 返回成功当作可见效果。
- 用户界面：两种语言／主题、1 屏与多屏、空状态、等待恢复、权限缺失、长型号及窄窗口；保留可读状态和安全退出入口。

## 回滚与工作区保护

本轮新增 macos/ 独立模块并维护对应文档，不改 Windows 源码、包、配置或设备状态，不提交、不推送。Mac 模块与 Windows 构建隔离，根 AGENTS.md 登记模块并以 macos/AGENTS.md 约束其实现。未来写设备试验必须有恢复看护和独立日志，无法确定所有权时停止写入，不能强制恢复未知颜色。

## 本轮核验与维护审查

已执行只读项目／Git 状态核对；Python 检查两份新文档及索引的 UTF-8 和本地链接通过，新文档行尾空白检查通过；`git diff --check -- docs/README.md` 通过（只有仓库既有 CRLF 转换提示）。没有执行 Mac 编译、测试、打包或硬件实验，也不重复运行与本轮文档无关的 Windows 测试。

已检查并更新 AGENTS.md：新增独立 Mac 模块、Swift 构建与平台隔离边界，根文件登记入口，具体规则写入 macos/AGENTS.md。上述前期文档检查不构成后续代码验证；源码核验记录如下。

## 首轮源码实施记录

- `macos/Package.swift`：Core／Mac 分离，无第三方 Swift 依赖。Foundation 核心含严格 schema 配置、原子写入、健康周期及色温日程；38 个 XCTest 已编写，尚未执行。
- `macos/Sources/ScreenKeeperMac`：原生界面、菜单栏、模块草稿、动态屏幕、独立隐私／休息暂缓、图片导入／轮播、健康卡及主线程生命周期。
- 平台实现区分键盘与鼠标活动归属；权限或跨屏归属未知时保守延后。已显示隐私幕在普通会话暂停时保留，休息唤醒不撤隐私；终止后拒绝迟到窗口请求。
- 集成审查修正预览消费真实健康提醒、显示器断开后立即迁移、长采样间隔补算空闲、默认屏记录误判脏状态、退出确认被遮挡和重复实例写配置风险。
- `macos/scripts/build-macos.sh`、`Build on Mac.command`：先测试再构建，生成 arm64 `.app`、本地签名和 ZIP；保留日志和校验值，不自动运行或上传。源码交接 ZIP 另附逐文件清单。
- `macos/README.md`／`TESTING.md`：双语构建、权限、功能边界、验收步骤和 FAQ。色温接管与第三方窗口控制明确待验证，不提供假功能入口。

待完成：Mac 原生编译与 XCTest、UI 渲染、权限／锁屏／Spaces 实机、热插拔／过夜、图标效果、后续设备后端，以及正式签名公证。当前交付仅为可交接的开发预览源码，尚非通过验收的 Mac 产品。

### Windows 主机已执行的源码检查

- Tree-sitter 0.25.2 + tree-sitter-swift 0.0.1 对 22 个 Swift 文件解析：0 个 ERROR／missing 节点。工具只安装在被忽略的 `artifacts/macos-source-qa/tooling`，没有安装系统 Swift 或 Xcode；这不是 Swift 编译、SDK 类型检查或 XCTest 执行。
- `bash -n macos/scripts/build-macos.sh` 与 `bash -n 'macos/Build on Mac.command'`：通过。
- 在 Windows Git Bash 执行构建脚本：按预期以退出码 1 拒绝非原生 Apple Silicon Mac，未开始编译或生成假 Mac 包。
- 新模块 UTF-8、行尾空白、shell LF、本地帮助链接与背景原图 SHA256：通过。`git diff --check -- AGENTS.md README.md docs/README.md`：通过，只有 CRLF 转换提示。
- 已编写 38 个 XCTest（配置 12、健康 16、色温日程 10），当前实际执行数为 0。源码检查结果保存在 `artifacts/macos-source-qa/validation.json`。

### 源码交接包 20261110.1

产物：`artifacts/macos-source-20261110.1/ScreenKeeper-Mac-0.1.0-Dev-20261110.1-Source.zip`，2,168,345 字节、34 个文件。仅包含 Mac 独立源码、素材许可、双语帮助、静态核验摘要和来源清单；不是 `.app` 或安装包，不含用户 Data、运行配置及 Windows 构建产物。

- ZIP SHA256：`7cb9a04ebd9f6ef7991223e3855a07547e0f4556d696c3bfcbf7151d7a45cec4`。
- 逐文件清单 SHA256：`7cd21183fa86d66923d0634ee1a246cde17da2532f76f3f3a1b812430eca27cb`。
- 基线提交 `88492d243a65314f013ef1290ff9897543ce63ff`；Mac 模块为未提交工作区源码，不冒充已有标签产物。
- 已检查 ZIP CRC、全部包内字节、清单中的每个 SHA256，以及 `.command`／shell 可执行属性。归档后未改变 Mac 源文件。
- 朋友需在真实 Apple Silicon Mac 上安装兼容开发工具并运行构建入口。原生编译、38 个 XCTest 和实机测试结果待其反馈；未自动提交、推送或发布。

## 2026-11-10 · 改为开发者云端构建

**后续路线更新：** 用户已要求新建 GitHub 完整项目仓库，通过内置浏览器登录；当前执行改用 [020 托管计划](020-github-hosting-plan.md)，不再创建只含 Mac 的产品副本。下文最小快照为已完成的本地准备记录，未上传；Mac 实际构建仍须在新仓库就绪后执行。

上述 20261110.1 源码包是历史交接方案，用户已要求减轻测试者负担。后续由开发者构建应用包，朋友只安装、运行和反馈，不再要求朋友完成源码编译。

实施顺序：

1. 核对可用账号与平台：当前只有本地 Windows 项目，无连接的 Mac；Git 只配置 Codeup origin 与上游参考，GitHub 凭据列表为空，GitHub 插件尚待连接。
2. 准备最小 Mac 快照、手动 GitHub workflow 和无界面打包脚本；保留原源码 ZIP，不覆盖历史校验值。
3. 明确 GitHub 私有目标仓库和上传范围，检查可用额度／停止付费边界；获准后才上传并触发。无连接不能开始云任务，不通过公开代码换取免费运行。
4. 持续检查实际 Mac 的编译和 XCTest，修复构建失败；核验 arm64、最低系统、动态库依赖、资源、启动自检、ZIP／DMG 与签名状态，下载后校验哈希。
5. 交付朋友可运行包及简短双语说明，区分 ad-hoc 与已公证产物；显示器功能验收仍由实机完成。

当前官方依据：[GitHub arm64 runner](https://docs.github.com/en/actions/reference/runners/github-hosted-runners)、[计费与包含额度](https://docs.github.com/en/billing/concepts/product-billing/github-actions)、[手动触发要求](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow)、[Apple 公证](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution)。2026-11-10 查询；金额与账户余量必须在实际启动前核对，不复用旧的“2000 分钟”假设。

### 当前进度与路线澄清

用户选择继续使用当前项目或 Codeup，故 Mac 主源码保留本仓库 `macos/`，不另建产品仓库。重新查证[云效构建集群](https://help.aliyun.com/zh/yunxiao/user-guide/build-a-cluster)：公共／VPC 托管环境为 Linux，macOS 属于需自行提供主机的私有集群。当前没有已连接的 Mac，GitHub 私有构建副本及服务连接仍待用户选择；未上传、未创建仓库、未触发云任务，也未改 Codeup origin。

本地已完成：

- `.github/workflows/mac-preview.yml`：手动 arm64 构建、源码校验、辅助脚本测试、20 分钟超时和 3 天产物保留；固定已核验的 checkout／upload-artifact 提交。只适用于含根清单的私有构建快照，当前 Codeup 不执行此文件。
- `macos/scripts/build-macos.sh`／`source-manifest.py`／`verify-binary.py`／`prepare-cloud-source.py`：无界面打包、Python 3.10 版本门禁、源码冻结、二进制与依赖检查、签名自检和 ZIP／DMG。修正 Windows／Mac 路径排序差异，快照内 `.gitattributes` 保持原始字节，避免换行转换破坏校验。
- `PackageCheck.swift`：在 UI／配置／设备初始化前执行包内资源及架构自检；此入口只完成源码实施，未在 Mac 执行。
- `DISTRIBUTION.txt`、README、TESTING、设计及本计划同步测试者无需编译的交付方式；已审查并更新 `macos/AGENTS.md` 的长期构建事实，根 AGENTS.md 的独立 Mac 模块边界无需再扩展。

本轮实际核验：

- `python macos/scripts/test_build_helpers.py`：18 项，17 通过、1 因 Windows 无真实符号链接创建权限跳过；模拟越界与符号链接分支通过。日志 `artifacts/macos-cloud-qa/build-helper-tests-final.txt`。
- `actionlint 1.7.12 -shellcheck= -pyflakes= .github/workflows/mac-preview.yml`：通过；版本文件经官方 SHA256 校验，未安装系统服务。
- 两个 shell 入口 `bash -n` 通过；23 个 Swift 文件 tree-sitter 解析无错误；Python 语法检查通过。Swift 类型检查、38 个 XCTest、实际应用启动和硬件验证仍未执行。
- 本地待上传快照 `artifacts/macos-cloud-build-20261110.1/repository/` 共 44 个文件，根清单校验 43 项、Mac 清单 34 项、ZIP CRC／字节／执行位检查通过；只含 Mac 范围，不含 Windows 源码、用户配置或密钥。ZIP SHA256 `0dc785b07cbda0dc9df4905dd1c1df285f75b4bbbe792761f4f01d38cd8d2cc9`。这是开发侧暂存，不能作为朋友可运行包交付。
- 历史源码 ZIP `20261110.1` 的 SHA256 未改变。`git diff --check -- AGENTS.md README.md docs/README.md` 通过，仅既有换行提示。

下一步依赖：用户选定可访问的 Mac 构建环境。若借用 GitHub，先连接账号并核对额度／停止付费边界，再上传已审阅的最小私有副本，持续处理实际编译和测试结果；若只用 Codeup，则等待可接入的 Mac 主机。没有原生构建结果前不能宣称已完成可运行测试包。
