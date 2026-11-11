# 020 · GitHub 完整仓库托管开发计划

对应设计：[020-github-hosting-design.md](020-github-hosting-design.md)。关联 Mac 实施：[019 计划](019-macos-apple-silicon-plan.md)。

开源脱敏说明：本记录中的旧提交标识和 ci-1-1 校验值对应当时真实构建，不改写为清理后的提交，也不冒称已重新构建。原始证据私有留存，公开历史的变更见 [021 计划](021-open-source-privacy-plan.md)。

1. 检查当前仓库、用户未提交更改、候选上传清单和保密／大文件边界。
2. 用户在内置浏览器登录 GitHub；核对账户，在该账户下创建私有 `ScreenKeeper` 仓库（重名时先核对现有仓库，不能覆盖）。
3. 整理完整项目的 CI 和文档，执行 Windows 隔离构建测试及 Mac 脚本检查，记录实际结果。
4. 提交经审阅的项目源码，使用新的 GitHub remote 上传 `main` 与原产品标签；验证远端提交后，保留 Codeup 为次 remote，GitHub 设为 origin。
5. 核对 GitHub Actions 额度与停止付费设置，启动 Mac 手动构建，处理实际编译／测试和打包错误，下载并核验产物。
6. 更新设计、计划、README、开发说明及对应 AGENTS.md；分别交付 GitHub 托管结果与 Mac 构建结果，不混同两者。

## 首次私有迁移的执行记录

- 首次迁移时，用户已完成 GitHub 登录和 CLI 授权，账号为 `suihan-star`；当时的私有仓库已接收完整源码、历史与原产品标签。该次 GitHub API 核验确认默认分支为 main、仓库为私有，681 个源码文件与本地冻结提交数量一致，树查询未截断。此后原仓库已私有留档，[现公开仓库](https://github.com/suihan-star/ScreenKeeper) 使用独立的脱敏历史，见 [021 计划](021-open-source-privacy-plan.md)。
- Mac workflow 已改为完整私有仓库：checkout／核对精确 `github.sha`，不再要求最小副本专用的根清单；保留 arm64、手动触发、20 分钟和 3 天产物限制。
- 工作区含先前 Windows 修复、发行文档与 Mac 新模块；本次依用户“全部代码托管”要求核对后纳入，运行数据与生成包不纳入。
- Mac 云端构建及下载后核验已完成，已交付 ci-1-1 的 arm64 ZIP／DMG 开发预览；具体结果和未验收边界见下文。

### 上传前审查与验证

- 初次候选：408 个已跟踪、319 个未忽略新文件，约 170.4 MiB；无 50 MiB 以上文件，未发现嵌套 Git 或 gitlink。当前文本及 HEAD 199 次提交中的 1,165 个文本 blob 版本未发现规则匹配的高置信凭据；规则扫描不能证明不存在所有未知凭据。
- `.gitignore` 增加仅针对推广 ZIP 和重复打包目录的排除，保留本地文件、设计原稿、素材和 QA。`.gitattributes` 固定 Mac shell／command 的 LF，避免 Windows checkout 换行影响命令入口。
- Windows：`.NET SDK 8.0.425`，`dotnet build OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/GitHubMigration/` 通过，0 警告／0 错误；同输出目录 `dotnet test ... --no-build --no-restore` 为 682 通过、2 跳过、0 失败。跳过项需显式原生桌面 QA，本次未启用硬件或窗口实验。日志 `artifacts/github-migration-20261110/`。
- Mac CI：`actionlint 1.7.12` 检查完整仓库版本的 workflow 通过。原生 Swift 编译／XCTest 尚未执行，不能从 Windows 结果推断 Mac 可运行。
- 历史包含已删除的上游显示器工具，不为迁移重写历史；本地还有工具 checkpoint refs，推送时只显式选 `main` 与已确认产品标签，不使用 `--mirror`／`--all`／`--tags`。

### 远端创建与连接核验

- 完整源码已本地提交为 `319a29ac41483cddd098dfb209398d4b7f2b663b`，681 个跟踪文件。暂存差异检查通过；只修剪一份既有发布说明末尾多余空行，没有更改其验证结论。
- 已创建并核对目标仓库，完成 Git 认证，再执行源码推送与远端校验。公开记录不保留个人账户用量、账单设置、邮箱复核、设备授权、代理或私人截图等内部操作信息。
- GitHub CLI 2.103.3 来自官方发布并通过 SHA256 校验；认证凭据不进入源码。根 AGENTS.md、README 与开发说明在远端核验成功后同步主仓库事实。

### GitHub 上传与首次 Mac 构建

- 已推送 `main` 的 `319a29ac41483cddd098dfb209398d4b7f2b663b`，远端 `ls-remote` 与本地完全一致；`screenkeeper-v1.0.0` 标签对象 `336ab71dc859c2f9468292cfe110d753986c3fd1`、指向提交 `6654714143cc93dba8d128f7dc59f1f2f11e0af9` 均保持不变。未推上游标签、工具 refs 或创建 Release。
- 远端核验后，GitHub 成为 `origin`，原 Codeup 保留为 `codeup`，`upstream` 不变；`main` 已跟踪 `origin/main`。没有删除或改写 Codeup 远端历史。
- 手动运行 Mac 构建 42943387164（脱敏前历史记录），锁定上述源码提交，已成功完成。云端为 macOS 15.8.2 arm64、Xcode 16.4、Swift 6.1.2、Python 3.15.2；实际 `swift test` 为 38 通过、0 失败，打包辅助测试 18 通过、0 跳过。
- `swift build -c release --arch arm64 --product ScreenKeeper`、Info.plist、Mach-O arm64／最低 macOS 13.0／系统依赖检查、ad-hoc 签名验证、`--package-check`、源码清单复核及 `hdiutil verify` 均通过。自检明确 `displayAccessed=false`、`settingsAccessed=false`、`hardwareValidated=false`；没有启动业务功能或把云端结果当作真实显示器兼容证明。
- 开发包构建编号 `ci-1-1`，源码清单 SHA256 为 `40e6af2daf7e7223aaa385e44cc79542c9a93c25fd73ba4031e1b210943148ad`。ZIP／DMG 与中英使用说明下载后原样复制到 `artifacts/macos-preview-20261111/release/`，未重新压缩或修改签名。
- ZIP：`ScreenKeeper-Mac-0.1.0-Dev-ci-1-1-arm64.zip`，2,641,018 字节，SHA256 `85a76bf5586701453964fa9ecb36bb3115dc5612e4930239d21c14ae8c7037a0`。
- DMG：`ScreenKeeper-Mac-0.1.0-Dev-ci-1-1-arm64.dmg`，3,018,822 字节，SHA256 `1d268958f20ec5ca5e0975db9a7a71d9bd841502908a19a18d1baca9a305cadd`。本地交付副本哈希与云端 `SHA256SUMS.txt` 一致。
- 流水线完整日志及元数据在 `artifacts/github-migration-20261110/mac-run-42943387164.*`，下载证据在 `artifacts/macos-preview-20261111/ci-1-1/`。GitHub 产物保留 3 天，本地副本独立保留；没有创建公共发行版或启用付费。
- 下载后独立核验报告为 `artifacts/macos-preview-20261111/verification.json`：ZIP 的 14 项 CRC／安全路径、主程序 0755 可执行位、Info.plist、直接解析的 Mach-O arm64／macOS 13.0／29 个系统库、签名 CodeDirectory 与云日志均一致。34 个源码输入逐一与冻结 Git 提交的对象字节匹配，包内与随附清单相同。
- 根 AGENTS.md、README、开发说明及 Mac 当前使用说明已按实际结果同步；019 设计／计划更新当前编译状态，历史源码交接与未执行结论保留在原记录中。后续文档提交不改变此包绑定的 `319a29a` 源码及既有校验值。

### 尚未完成的产品验收

- Mac 主界面、辅助功能权限、单／多屏覆盖、全屏／Spaces／台前调度、锁屏／睡眠／合盖／过夜及插拔，仍需真实 Mac 验收；本地 Windows 不能执行 `.app`。
- 此包为 `0.1.0` 开发预览，未做 Developer ID 签名与 Apple 公证，不能作为正式公开发行版。实际色温、硬件亮度／断电、桌面／指定应用控制、开机启动、健康全屏免打扰与动态雾光尚未开放。
