# 020 · GitHub 完整仓库托管开发计划

对应设计：[020-github-hosting-design.md](020-github-hosting-design.md)。关联 Mac 实施：[019 计划](019-macos-apple-silicon-plan.md)。

1. 检查当前仓库、用户未提交更改、候选上传清单和保密／大文件边界。
2. 用户在内置浏览器登录 GitHub；核对账户，在该账户下创建私有 `ScreenKeeper` 仓库（重名时先核对现有仓库，不能覆盖）。
3. 整理完整项目的 CI 和文档，执行 Windows 隔离构建测试及 Mac 脚本检查，记录实际结果。
4. 提交经审阅的项目源码，使用新的 GitHub remote 上传 `main` 与原产品标签；验证远端提交后，保留 Codeup 为次 remote，GitHub 设为 origin。
5. 核对 GitHub Actions 额度与停止付费设置，启动 Mac 手动构建，处理实际编译／测试和打包错误，下载并核验产物。
6. 更新设计、计划、README、开发说明及对应 AGENTS.md；分别交付 GitHub 托管结果与 Mac 构建结果，不混同两者。

## 当前执行记录

- 已打开 GitHub 登录页，用户正在登录；尚未创建远程仓库或上传。
- Mac workflow 已改为完整私有仓库：checkout／核对精确 `github.sha`，不再要求最小副本专用的根清单；保留 arm64、手动触发、20 分钟和 3 天产物限制。
- 工作区含先前 Windows 修复、发行文档与 Mac 新模块；本次依用户“全部代码托管”要求核对后纳入，运行数据与生成包不纳入。
- 后续实际验证与远端信息将在完成对应步骤后补充。当前尚无可运行的 Mac 测试包。

### 上传前审查与验证

- 初次候选：408 个已跟踪、319 个未忽略新文件，约 170.4 MiB；无 50 MiB 以上文件，未发现嵌套 Git 或 gitlink。当前文本及 HEAD 199 次提交中的 1,165 个文本 blob 版本未发现规则匹配的高置信凭据；规则扫描不能证明不存在所有未知凭据。
- `.gitignore` 增加仅针对推广 ZIP 和重复打包目录的排除，保留本地文件、设计原稿、素材和 QA。`.gitattributes` 固定 Mac shell／command 的 LF，避免 Windows checkout 换行影响命令入口。
- Windows：`.NET SDK 8.0.425`，`dotnet build OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/GitHubMigration/` 通过，0 警告／0 错误；同输出目录 `dotnet test ... --no-build --no-restore` 为 682 通过、2 跳过、0 失败。跳过项需显式原生桌面 QA，本次未启用硬件或窗口实验。日志 `artifacts/github-migration-20261110/`。
- Mac CI：`actionlint 1.7.12` 检查完整仓库版本的 workflow 通过。原生 Swift 编译／XCTest 尚未执行，不能从 Windows 结果推断 Mac 可运行。
- 历史包含已删除的上游显示器工具，不为迁移重写历史；本地还有工具 checkpoint refs，推送时只显式选 `main` 与已确认产品标签，不使用 `--mirror`／`--all`／`--tags`。
