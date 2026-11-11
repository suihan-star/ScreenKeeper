# ScreenKeeper for macOS

- 本目录为独立 SwiftUI／AppKit 客户端，最低部署目标 macOS 13，产品构建仅 arm64。不修改 Windows 解决方案或复用 Windows 原色、验证、设备身份及恢复文件。
- Swift Package 的 ScreenKeeperCore 只依赖 Foundation，包含配置、计时和日程；ScreenKeeperMac 管理 AppKit／SwiftUI 与系统适配。保持核心可在非 Mac 环境独立测试。
- 本阶段为开发预览，硬件色温、亮度和第三方窗口控制在实际 Mac 验证前不得假装可用。软件遮黑不能描述为物理断电或节能。
- 仅按功能请求必要权限，不记录键值、窗口内容或截屏。权限撤回时停止依赖判断，不能继续显示虚假的正常状态。
- 屏幕用稳定身份关联设置，数量动态变化；镜像和身份不明确时保守限制独立保护。离线不删配置，刷新不覆盖草稿。
- 原生操作均由主线程管理；隐私解除只响应明确按钮，普通输入只退出休息遮黑。休息和隐私分别计时、保存、放弃和暂缓。
- 用户设置默认保存在用户 Application Support/ScreenKeeper；未知 schema、损坏文件和写入失败必须显示错误并保留草稿，不静默覆盖。
- Mac 构建和原生测试须在实际 Mac／Xcode 执行。其他系统的语法或核心测试不能描述为 Mac 界面、权限或硬件验收。
- 关联设计与计划：docs/tasks/019-macos-apple-silicon-design.md 和 019-macos-apple-silicon-plan.md。本地打包不等于签名、公证或正式发布。
- `scripts/build-macos.sh` 用于本地与云端构建，需要 Python 3.10+：先验证／记录源码清单，执行 Swift 测试、arm64 构建、Mach-O 依赖与最低系统检查，ad-hoc 签名和 `--package-check` 后生成 ZIP／DMG；`CI=true` 不打开 Finder。包自检在 NSApplication／实例锁／配置前执行，不访问显示设备。实际源码／环境／测试证据随构建保存。
- `.github/workflows/mac-preview.yml` 在完整产品仓库内手动运行，支持公开或私有仓库，固定标准 `macos-15` arm64、20 分钟超时、3 天产物保留期，不自动发布或配置付费。checkout 明确锁定并核对本次 `github.sha`，构建脚本生成 Mac 输入的 SHA256 清单并记录提交；不要求最小快照专用的根清单。`prepare-cloud-source.py` 和历史最小快照保留为辅助工具，不作为完整仓库的构建前提。源码上传不包含用户数据、运行日志、构建产物或密钥；构建记录及生成包发布前须检查私人信息，CI 编译成功也不能宣称完成真实显示器验证。
