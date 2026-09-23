# 013 · 首版仓库整理与 Codeup 接入计划

关联：[任务设计](013-first-version-design.md)。

## 实施项

- [x] 核对单一 Git 仓库、main 分支、上游基线与已有未提交变更。
- [x] 获得本次提交和推送的明确授权。
- [x] 整理 README、用户指南、开发说明、文档索引与首版变更记录。
- [x] 完成忽略规则、独立版本前缀与工作流整理；用户确认首版为 1.0.0，使用 screenkeeper-v1.0.0 标签，不创建线上发行版。
- [x] 修复已发现的两个测试文件空白问题，完成构建、完整测试与格式门禁。
- [x] 审查待提交清单、文档链接和敏感信息；本地提交范围为全部已确认的首版开发成果。
- [x] 核对 SSH 与远程分支，向已配置的 origin 普通推送 main 和首版产品标签，并核对提交一致。
- [x] 更新验证结果并完成 AGENTS.md 维护审查。

## 整理前基线

- HEAD `27f1c77`，分支 main；只有 upstream，没有 origin。全程保留上游历史。
- 初始候选文件 400 个、约 7.97 MB；artifacts、bin、obj 已忽略。多数变更属于本会话已确认的首版开发成果。
- 全仓 `dotnet format --verify-no-changes --no-restore` 发现 ColorProfileTests.cs 和 ColorTemperatureServiceTests.cs 共 21 处空白问题；不涉及功能失败。
- 首次 SSH 严格检查报告企业域名未登记。自动登记曾被自动审批拒绝，原因是该域名 ED25519 主机指纹未可信核对；当时未绕过限制，先完成本地工作，后续处理见 Git 接入结果。

## 验证记录

### 源码整理与验证

- 根 README 改为首版产品入口；详细使用内容移至 docs/user-guide.md，上游原文归档 docs/upstream/README.md，新增开发说明、任务索引和 CHANGELOG。
- MinVer 产品标签前缀改为 `screenkeeper-v`，上游 `v*` 不再参与计算。用户随后确认正式版本号为 `1.0.0`，由提交上的 `screenkeeper-v1.0.0` 产品标签派生；此前完整测试构建为未标记的 `0.0.0-alpha.0.33`。标签建立后重新构建成功，确认 FileVersion 为 `1.0.0.0`，ProductVersion 为 `1.0.0+6654714143cc93dba8d128f7dc59f1f2f11e0af9`；不把源码标签等同于线上发行版。
- GitHub CI 面向 main 和手动触发；原自动发布流程完整归档，现 release.yml 只支持手动构建与上传产物。Codeup 流水线没有配置或运行。
- .gitignore 补充运行数据、恢复日志、个人工具状态及密钥／签名文件；没有删除本地文件。
- 仓库可纳入 Git 的文件共 408 个，约 7.99 MB；本次实际新增／修改 301 个文件，包含此前已确认的首版成果。凭据模式扫描没有命中（只输出文件／行号，不记录匹配值）；新主文档本地链接全部有效。
- `dotnet restore OLED-Sleeper.sln` 成功；保留 NU1900 漏洞数据源不可达警告，未宣称漏洞审计通过。
- `dotnet build OLED-Sleeper.sln -c Release --no-restore -p:BaseOutputPath=bin/FirstVersion/` 成功，0 错误，1 项上述网络警告。
- `dotnet test OLED-Sleeper.sln -c Release --no-build --no-restore -p:BaseOutputPath=bin/FirstVersion/`：总计 585，583 通过、2 跳过、0 失败；跳过项为需显式启用的原生桌面测试。TRX：`artifacts/first-version-tests/full.trx`。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 退出码 0；此前两个测试文件仅用 `dotnet format whitespace --include ...` 修复空白。
- 首次暂存检查还发现 13 个设计稿／文档文件共 14 处行尾空白或文件末尾多余空行，已作最小清理；上游说明文案不变。
- PowerShell AST 解析 build.ps1 成功；`git diff --check` 通过；`git fsck --connectivity-only` 退出码 0，存在本机检查点产生的 dangling tree，未清理或改变历史。
- GitHub 工作流完成静态审阅，未远程执行；没有本地 YAML 解析器，未宣称 YAML 工具校验通过。没有生成安装包、改变运行客户端或操作显示器。
- 已更新 AGENTS.md 的文档职责、产品版本前缀、远程与工作流边界；原产品身份、存储 schema、安装 AppId 和模块职责未变化。

### Git 接入结果

- origin 已配置为用户提供的企业 Codeup SSH 仓库，upstream 地址保持不变。
- 用户明确允许信任展示给他的企业域名 ED25519 指纹后，只登记与该指纹完全匹配的主机公钥。后续连接始终使用 `StrictHostKeyChecking=yes`。这是用户批准的首次信任，不声称已通过官方来源核对。
- 私钥有口令保护，用户在本地交互窗口通过 ssh-add 解锁临时代理；口令未由助手读取，也未进入仓库或日志。没有修改私钥或 Windows 系统服务。
- `git ls-remote --symref origin HEAD refs/heads/* refs/tags/*` 成功，推送前确认为空仓库。
- `git push --atomic --set-upstream origin main refs/tags/screenkeeper-v1.0.0` 成功；main 跟踪 origin/main，远程默认分支为 main，未强推或上传上游标签。
- 推送后的 `git ls-remote --symref` 确认远程 main 和标签解引用均为首版提交 `6654714143cc93dba8d128f7dc59f1f2f11e0af9`。文档收尾另用普通后续提交同步 main，不移动已发布的源码标签。
- 本次收尾只更新任务记录，未改变已验证的程序源码，不重复执行实机测试或生成发行包；AGENTS.md 已审查，无新增需要沉淀的产品或构建规则。
