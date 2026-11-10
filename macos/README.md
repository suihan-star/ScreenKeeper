# 屏幕良伴 · ScreenKeeper for Mac

**开发预览源码，尚未在 Mac 编译或验收，不是可直接安装的正式版。** 目标为 macOS 13 及以上、Apple Silicon（M 系列），不以显示器品牌或固定屏数限制应用。

**Development-preview source, not yet compiled or validated on a Mac.** Targets macOS 13+ on Apple Silicon. It is not a ready-to-install release.

**测试者无需编译。** 现已准备手动云端构建流程；获得已授权 GitHub 私有仓库访问并完成构建后，测试者接收 `.app` ZIP／DMG，按 [简短使用说明](DISTRIBUTION.txt) 运行即可。当前云任务尚未启动，下面的源码构建步骤只给开发者使用。

**Testers do not need to compile.** A manual cloud build is prepared. After an authorized private GitHub build completes, testers receive an app ZIP/DMG and follow [the short instructions](DISTRIBUTION.txt). No cloud build has started yet; the source instructions below are for developers.

## 当前源码范围 / Implemented source

- SwiftUI 设置窗口，静绿／雾白绿，中英文，菜单栏和官网入口。Native settings, two themes, Chinese/English, menu bar, and website link.
- 动态显示器列表、独立草稿与模块保存、跨屏复制、休息／隐私独立暂缓。Dynamic displays, per-module drafts/save, copy settings, and separate snoozes.
- 每屏软件遮黑、隐私幕、内置／纯色／自定义图片及轮播。Per-display software blackout and privacy curtains, with built-in, solid, or custom backgrounds.
- 健康工作周期、居中提醒、开始休息、稍后、跳过及预览。Global work cycles, centered reminders, break/snooze/skip actions, and preview.
- 严格 schema 配置存储、单实例锁、只读屏幕诊断和 38 个核心 XCTest。Strict settings storage, a process lock, read-only display diagnostics, and 38 core XCTest cases.

**尚未开放 / Not enabled:** 实际色温调节、硬件亮度／断电、回到桌面、指定应用、开机启动；健康提醒的全屏免打扰与动态雾光也未迁移。本版本不会写 Gamma、改显示器连接模式或控制第三方窗口。色温日程已有纯逻辑和测试，但不是设备后端。

Color control, hardware brightness/power, desktop/selected-app actions, login startup, fullscreen DND, and animated fog reminders are not enabled. No Gamma writes or display-topology changes occur. The color schedule is logic only, not a working hardware backend.

## 在 Mac 构建 / Build on a Mac

需要 Apple Silicon Mac、macOS 13+、Swift 5.9+（Xcode 15+ 或兼容的 Command Line Tools）及 Python 3.10+。首次安装工具可能需要网络和磁盘空间；本项目没有第三方 Swift 包依赖。

Requires an Apple Silicon Mac, macOS 13+, Swift 5.9+ via Xcode 15+ or compatible Command Line Tools, and Python 3.10+. There are no third-party Swift package dependencies.

1. 解压源码，在 Mac 终端进入包含 `Package.swift` 的目录。Extract the source and open Terminal in the directory containing `Package.swift`.
2. 未安装开发工具时执行 `xcode-select --install`，完成系统安装后再继续。Install Apple's tools if needed, then continue.
3. 执行下方命令，或双击 `Build on Mac.command`。Run the command below, or open `Build on Mac.command`.

```bash
/bin/bash scripts/build-macos.sh
```

脚本先验证源码清单并执行测试，再编译 arm64、生成图标、组装 `.app`、临时签名、包自检并生成 ZIP／DMG。产物及环境、测试、依赖、签名和构建日志在 `dist/<构建编号>/`；本地成功后 Finder 显示 `ScreenKeeper.app`，`CI=true` 时跳过 Finder。脚本只执行不会接管屏幕的 `--package-check`，不会自动启动业务功能或上传文件。

The script verifies source, tests, builds arm64, exports icons, applies an ad-hoc signature, checks the package, and creates ZIP/DMG files under `dist/<build-id>/`. It records environment, dependencies, signing and build logs. CI skips Finder. The package check does not access displays or settings; business functionality is never launched automatically.

此签名仅供本机构建测试，不是 Developer ID 签名或 Apple 公证。不要将此构建作为正式版向公众分发，也不要为了运行它关闭系统安全保护。外部分发需要后续签名、公证和验收。

The ad-hoc signature is for local testing, not Developer ID signing or notarization. Do not distribute it as a public release or disable system security to run it. Public distribution needs signing, notarization, and validation.

仅运行核心测试（有兼容 Swift 工具链时）：

```bash
swift test
```

在非 macOS 主机，Package.swift 不加入原生应用目标，仅测试 Foundation 核心；这不能验证 Mac 界面与系统 API。当前交付者没有 Swift 工具链，因此没有实际执行这些命令；源码包附带的静态检查不等于编译通过。

On non-macOS hosts, the package omits the native app target and tests the Foundation core only. This does not validate Mac UI or system APIs. These build/test commands have not been run by the source author in the Windows environment.

## 数据与权限 / Data and permissions

设置写入 `~/Library/Application Support/ScreenKeeper/mac-settings.json`；图片副本在同目录的 `PrivacyImages/`。所有功能初始关闭。不会读取 Windows 或 OLED Sleeper 的配置、原色和恢复文件。

Settings are stored in `~/Library/Application Support/ScreenKeeper/mac-settings.json`, with imported copies under `PrivacyImages/`. Features start disabled. Windows/OLED Sleeper settings and recovery data are not imported.

辅助功能权限仅在点击授权入口时请求，用于判断前台窗口属于哪块屏幕；不采集键值、窗口内容或截屏。未授权时相关判断保守延后，可在通用设置查看状态。身份不明或镜像屏暂不执行独立保护。

Accessibility permission is requested only on explicit action to identify the display containing the active window. Keystrokes, window content, and screenshots are not collected. Missing permission conservatively defers dependent actions. Ambiguous identities and mirrored screens do not receive independent protection.

软件遮黑不会切断背光或显示器电源。隐私幕不是系统锁屏，安全界面、Spaces 和全屏应用覆盖仍须实测。休眠后可能等待重新激活设置窗口才恢复自动计时；已有隐私幕会保留到明确解除或屏幕断开。

Software blackout does not power off the display. A privacy curtain is not a system lock. Coverage across secure UI, Spaces, and fullscreen apps needs real testing. Automatic timing may wait for an interactive activation after sleep; an existing curtain remains until explicitly dismissed or disconnected.

## 资源 / Assets

`forest-mist.png` 原样复用用户确认的山林背景，SHA256 `F63B615B8860F85967B5E47F54C7B0C59B39F04EB3D60EC2475B50D2609087EC`。`icon.svg` 沿用 A+「叠屏静夜」源图形。Mac 图标导出脚本是本地原生绘制，最终外观待 Mac 验收。

The forest background and A+ overlapping-screen/crescent identity are reused from ScreenKeeper. The native icon export still needs Mac visual review. Upstream MIT attribution is preserved in `Sources/ScreenKeeperMac/Resources/LICENSE`.
