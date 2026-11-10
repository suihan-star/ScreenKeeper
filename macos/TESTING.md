# Mac 开发预览测试 / Mac development preview testing

此版本为首次 Mac 源码交接，尚无 Mac 编译和实机验收记录。请先构建；失败时保留错误，不跳过测试或关闭系统安全机制。首次仅在没有敏感内容的桌面测试。/ This is the first Mac source handoff with no native build or device validation yet. Build first and retain any error. Do not skip tests or disable system security. Use a desktop without sensitive content for initial testing.

后续流程已改为由开发者云端构建：朋友拿到 `.app` ZIP／DMG 后无需执行下面的源码构建段，直接阅读 [测试包使用说明](DISTRIBUTION.txt)，再按本文件场景反馈。云端构建状态须以该包随附的日志／校验值为准。/ The revised workflow builds in the cloud for testers. Recipients of an app ZIP/DMG can skip source building, follow [the package instructions](DISTRIBUTION.txt), and report the scenarios below. Check the evidence supplied with that particular build.

## 构建与诊断 / Build and diagnostics

运行 `bash scripts/build-macos.sh`。成功后，双击生成的 `.app` 打开；所有管理功能默认关闭。测试日志与应用同在 `dist/<构建编号>/`。/ Run the build script, then open the resulting app. Management features start disabled. Logs are kept next to the app.

如果只有 Command Line Tools 不兼容当前 Swift 版本，安装适配本机系统的 Xcode 后，用 `xcode-select` 选择对应开发工具，再运行脚本。请把原始编译错误反馈给开发者，不需要自行修改源码绕过。/ If the installed tools are incompatible, select a compatible Xcode installation and retry. Share the original error instead of patching around it.

只读诊断（将示例路径替换为实际路径）：/ Read-only diagnostics; replace the example path:

```bash
"dist/<build-id>/ScreenKeeper.app/Contents/MacOS/ScreenKeeper" --diagnostics
```

诊断不会改色、调亮度、切换拓扑或读取用户配置；输出系统版本、屏幕尺寸、缩放、镜像／身份状态及 Gamma 读取能力摘要，不包含颜色数组、屏幕 UUID、序列号、窗口标题或截图。读取成功不代表实际调色通过验证。/ Diagnostics do not change displays or read user settings. They report OS and display capabilities without color arrays, display UUIDs, serial numbers, window titles, or screenshots. A successful read is not color-control validation.

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

请记录 Mac 系列、macOS 和 Xcode/Swift 版本、屏幕数量及连接方式、测试步骤、预期与实际行为，以及构建日志或只读诊断。型号用于覆盖兼容场景，不是支持名单。可截取应用自身界面，但不要提供私人桌面内容或整份配置目录。/ Share Mac family, OS/tool versions, display count/connections, reproduction steps, expected/actual results, and build logs or diagnostics. Models inform test coverage, not an allowlist. Avoid private desktop content or full configuration folders.

## 常见问题 / FAQ

**为什么色温不能开启？** 当前只有日程逻辑及只读探测，尚无通过 Mac 验证的写入／恢复后端。其他模块仍可测试。/ **Why is color control unavailable?** Only scheduling logic and read-only probes exist. The Mac write/recovery backend is pending device validation.

**为什么一直不进入休息？** 检查是否开启“鼠标停留”“前台窗口”保持唤醒条件；缺少辅助功能权限时无法判断前台窗口，相关条件也会保守延后。睡眠后请尝试打开设置窗口确认交互会话。/ **Why does rest not start?** Check keep-awake conditions, Accessibility permission, and post-sleep interactive activation.

**能复制 Windows 的 Data 过来吗？** 不能。本版本使用独立配置和屏幕身份，不导入 Windows 恢复记录。/ **Can I copy Windows Data?** No. Mac settings, display identity, and recovery data are separate.

**这是正式安装包吗？** 不是。交付的是源码；朋友在 Mac 构建后得到的是本地开发预览，尚未完成签名公证或完整硬件验收。/ **Is this a release installer?** No. It is source for a locally built preview, not a notarized or fully validated release.
