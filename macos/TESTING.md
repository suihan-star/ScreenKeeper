# Mac 开发预览测试 / Mac development preview testing

开发预览 `ci-1-1` 已在 Mac 云端构建 42943387164（脱敏前历史记录） 完成编译与打包，源码提交为 `319a29ac41483cddd098dfb209398d4b7f2b663b`。38 个 XCTest、18 个打包辅助测试，以及 arm64／最低系统／依赖／ad-hoc 签名／资源／DMG 检查通过；交互与实机显示器验收仍未完成。/ Development preview `ci-1-1` was built and packaged in the linked Mac run from commit `319a29ac41483cddd098dfb209398d4b7f2b663b`. All 38 XCTest cases, 18 packaging-helper tests, and architecture/minimum-OS/dependency/ad-hoc-signature/resource/DMG checks passed. Interactive and physical-display validation is still pending.

测试者使用开发者提供的 `ScreenKeeper-Mac-0.1.0-Dev-ci-1-1-arm64.zip` 或同名 `.dmg`，无需编译或安装 Xcode。先阅读 [测试包使用说明](DISTRIBUTION.txt)，再按本文件场景反馈。首次仅在没有敏感内容的桌面测试，不关闭系统安全机制；每个构建的状态以随附日志与校验值为准。/ Use the developer-supplied `ScreenKeeper-Mac-0.1.0-Dev-ci-1-1-arm64.zip` or matching `.dmg`; no compilation or Xcode installation is required. Follow [the package instructions](DISTRIBUTION.txt), then report the scenarios below. Start on a desktop without sensitive content and keep system security enabled. Check the evidence supplied with each particular build.

## 运行与诊断 / Run and diagnostics

从 DMG 将 `ScreenKeeper.app` 拖入 Applications，或解压 ZIP 后移动应用，再打开；所有管理功能默认关闭。/ Drag `ScreenKeeper.app` from the DMG into Applications, or extract the ZIP and move the app, then open it. Management features start disabled.

只读诊断（将示例路径替换为实际路径）：/ Read-only diagnostics; replace the example path:

```bash
"/Applications/ScreenKeeper.app/Contents/MacOS/ScreenKeeper" --diagnostics
```

诊断不会改色、调亮度、切换拓扑或读取用户配置；输出系统版本、屏幕尺寸、缩放、镜像／身份状态及 Gamma 读取能力摘要，不包含颜色数组、屏幕 UUID、序列号、窗口标题或截图。读取成功不代表实际调色通过验证。/ Diagnostics do not change displays or read user settings. They report OS and display capabilities without color arrays, display UUIDs, serial numbers, window titles, or screenshots. A successful read is not color-control validation.

## 开发者重新构建（可选）/ Optional developer rebuild

仅开发者需要在源码的 `macos/` 目录运行 `bash scripts/build-macos.sh`；日志及产物位于 `dist/<构建编号>/`。工具链要求见 [README](README.md)。如遇编译错误，保留原始日志，不跳过测试或修改源码绕过。测试者使用上面的应用包即可。/ Only developers need to run `bash scripts/build-macos.sh` from the source `macos/` directory. Logs and artifacts are stored in `dist/<build-id>/`; see [README](README.md) for toolchain requirements. Retain original errors instead of skipping tests or patching around failures. Testers can use the supplied app package.

## 建议顺序 / Suggested order

| 场景 / Scenario | 预期 / Expected |
| --- | --- |
| 首次打开、关闭窗口再点菜单栏 / Launch, hide, reopen | 无自动保护；设置窗口可再次打开 / No automatic protection; settings reopen |
| 切换中英文与主题 / Language and theme | 即时切换，未保存业务草稿保留 / Immediate update, business drafts retained |
| 单屏、多个不同或相同型号屏 / One or multiple displays | 动态列表、同型号编号、各屏设置独立 / Dynamic list, unique labels, independent settings |
| 编辑休息与隐私，只保存其中之一 / Edit two modules, save one | 仅当前模块落盘，另一模块仍有草稿 / Only the current module saves |
| 隐私设为 10 秒，先不启用免打扰 / Privacy after 10 seconds | 停止本屏操作后出现不透明幕布；普通输入不解除 / Opaque curtain; ordinary input does not dismiss it |
| 点击“进入此屏幕” / Enter this screen | 幕布撤销并重新等待 / Curtain closes; idle timing restarts |
| 开启使用时不打扰并分别选条件 / Quiet conditions | 满足任一选中条件时不自动保护 / Any selected condition defers protection |
| 休息遮黑、同时启用隐私 / Blackout with privacy | 先有隐私再遮黑；唤醒只去黑，不撤隐私 / Curtain precedes blackout; wake preserves privacy |
| 暂缓、恢复、跨屏应用设置 / Snooze, resume, copy | 不改现有幕布；复制保留目标启用状态 / Existing curtains stay; copied settings retain target toggles |
| 导入 1 张与多张图 / One or multiple images | 单图不轮播，多图切换；缺图保持不透明纯色 / Static or slideshow; missing image stays opaque |
| 健康提醒预览 / Reminder preview | 居中且不抢焦点；按钮只关闭预览，不消费正式周期 / Centered, nonactivating; actions close preview only |
| 健康工作 1 分钟、休息 1 分钟 / One-minute work and break | 开始后才倒计时；稍后延迟一次、跳过重新计工作 / Break starts explicitly; snooze and skip differ |
| 权限拒绝／授权／撤回 / Permission changes | 明确提示；相关判断保守暂停，其他功能可用 / Clear feedback, conservative dependent behavior |
| 拔插屏、旋转、缩放、镜像 / Hotplug, rotation, scale, mirror | 不套错配置、不崩溃；镜像不冒充逐屏独立 / No mismatched settings/crashes; mirrored screens limited |
| 真实锁屏、关屏、合盖、过夜 / Lock, power-off, lid, overnight | 不立即补算整晚；已有隐私幕不会因普通输入解除 / No overnight catch-up; curtains require explicit dismissal |
| 原生全屏、Spaces、台前调度 / Fullscreen, Spaces, Stage Manager | 记录幕布实际覆盖与失败情况，不仅看程序显示状态 / Observe actual coverage, not status alone |
| 有草稿且幕布显示时退出 / Quit with drafts and curtains | 退出确认可见；取消继续运行，确认后清理全部自有窗口 / Visible confirmation; cleanup only on confirmed exit |

测试中如果无法操作屏幕，可使用 macOS 自带的“强制退出应用程序”结束 ScreenKeeper；本预览不写硬件颜色或亮度，进程结束会移除自有窗口。请记录复现步骤，不能据此认定硬件问题。/ If interaction is blocked, use macOS Force Quit to stop ScreenKeeper. This preview makes no hardware color or brightness changes; its windows disappear on process exit. Record the steps without assuming hardware failure.

## 反馈信息 / Feedback

请记录应用构建编号、Mac 系列、macOS 版本、屏幕数量及连接方式、测试步骤、预期与实际行为，以及错误或只读诊断；仅自行构建时另附 Xcode/Swift 版本及构建日志。型号用于覆盖兼容场景，不是支持名单。可截取应用自身界面，但不要提供私人桌面内容或整份配置目录。/ Share the app build ID, Mac family, macOS version, display count/connections, reproduction steps, expected/actual results, and errors or diagnostics. Add Xcode/Swift versions and build logs only for a self-built app. Models inform test coverage, not an allowlist. Avoid private desktop content or full configuration folders.

## 常见问题 / FAQ

**为什么色温不能开启？** 当前只有日程逻辑及只读探测，尚无通过 Mac 验证的写入／恢复后端。其他模块仍可测试。/ **Why is color control unavailable?** Only scheduling logic and read-only probes exist. The Mac write/recovery backend is pending device validation.

**为什么一直不进入休息？** 检查是否开启“鼠标停留”“前台窗口”保持唤醒条件；缺少辅助功能权限时无法判断前台窗口，相关条件也会保守延后。睡眠后请尝试打开设置窗口确认交互会话。/ **Why does rest not start?** Check keep-awake conditions, Accessibility permission, and post-sleep interactive activation.

**能复制 Windows 的 Data 过来吗？** 不能。本版本使用独立配置和屏幕身份，不导入 Windows 恢复记录。/ **Can I copy Windows Data?** No. Mac settings, display identity, and recovery data are separate.

**这是正式安装包吗？** 不是。已经提供可直接运行的开发预览 ZIP／DMG，但仅作 ad-hoc 签名，未经 Developer ID 签名与 Apple 公证，交互和真实显示器验收仍待完成。/ **Is this a release installer?** No. A ready-to-run development-preview ZIP/DMG is available, but it is only ad-hoc signed, not Developer ID signed or notarized. Interactive and physical-display validation remains pending.
