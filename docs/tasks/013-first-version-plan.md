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

## 追加交付：1.0.0 便携 ZIP

用户在源码推送完成后要求提供可直接解压运行的 ZIP，范围见关联设计的便携交付章节。

- [x] 从首版标签导出源码，在新目录生成 Windows x64 自包含便携成品。
- [x] 包含中文使用说明、产品许可及随包依赖许可，排除用户数据、日志和开发产物。
- [x] 核对 EXE 版本、便携配置、运行依赖、ZIP 解压与 SHA256，记录未执行的实机项目。
- [x] 更新用户入口与交付记录，完成 AGENTS.md 维护审查；本轮不自动提交、推送或上传 ZIP。

### 便携交付验证结果

- 通过 `git archive screenkeeper-v1.0.0` 导出应用和 LICENSE，源码身份为 `6654714143cc93dba8d128f7dc59f1f2f11e0af9`；构建目录为 `artifacts/portable-1.0.0-20260923/source`，未覆盖工作区源码或历史产物。
- 发布命令：`dotnet publish artifacts/portable-1.0.0-20260923/source/OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64 --self-contained true -o artifacts/portable-1.0.0-20260923/payload/ScreenKeeper -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:MinVerVersionOverride=1.0.0 -p:SourceRevisionId=6654714143cc93dba8d128f7dc59f1f2f11e0af9 --nologo` 成功。归档构建显式指定标签版本和源提交，不混入 main 上的后续文档提交身份。
- EXE FileVersion 为 `1.0.0.0`，ProductVersion 为 `1.0.0+6654714143cc93dba8d128f7dc59f1f2f11e0af9`；发布配置包含 .NET 与 Windows Desktop 8.0.31，编译常量 IsPortable 为 true。
- 随包保留 5 个 WPF 原生 DLL、EXE 配置、中文使用说明、VERSION、上游 MIT LICENSE，以及 8 个运行时 NuGet 组件和 2 个运行时包的许可／包内声明。缺少的许可正文从 nuspec 指定的官方仓库提交补齐；MinVer 与 ILLink 为构建工具，不是随包运行组件。
- ZIP 为 `artifacts/portable-1.0.0-20260923/ScreenKeeper-1.0.0-Portable-x64.zip`，大小 68,882,268 字节（约 65.7 MiB），26 个文件。ZIP 完整性检查通过，重新解压后的每个文件均与打包源 SHA256 一致；未包含 Data、日志、PDB、构建目录或密钥。
- ZIP SHA256：`3136ea0f58b2492b96a80f9763dc9971c0b95b26424938f60e9a9f56bfef0fd9`；同目录提供 `ScreenKeeper-1.0.0-SHA256SUMS.txt`。
- 解压后的 EXE 使用临时 startup hook 作入口前检查，子进程指定无效 DOTNET_ROOT 并关闭多级查找，成功使用包内 .NET 8.0.31 / X64，读到正确程序集版本、便携常量和中英文／WPF 资源。检查在 Main 前退出，没有创建 Data 或启动显示器控制服务；测试辅助 DLL 不进入 ZIP。这只证明运行时与程序集加载，不替代完整设置窗口和硬件验收。
- 发布保留 NU1900 漏洞源不可达警告，未声称漏洞检查通过。EXE 当前未签名；未在无 .NET 的干净 Windows 机器完整操作 UI，也未重复多屏、安装或硬件验收。
- 本轮仅生成便携成品并更新 README／013 任务设计与计划，不修改应用代码或构建脚本；已检查 AGENTS.md，本次无需更新，便携路径和已有构建机制未改变。

## 追加交付：双语快速说明与 FAQ

关联设计的“双语帮助追加交付”章节；保留前一轮未提交的 README 和 013 文档修改。

- [x] 在 docs/portable 编写中英双语快速说明与 FAQ，对照客户端文案与行为复核。
- [x] 便携打包步骤自动复制文本帮助，更新文档入口及 AGENTS.md 的长期打包约定。
- [x] 在新目录重打 1.0.0 帮助版 ZIP，核对帮助正文与源码一致、原程序／依赖不变、ZIP 解压与 SHA256。
- [x] 记录实际检查及限制；不自动提交、推送或发布。

### 双语帮助验证结果

- 新增 `docs/portable/使用说明-README.txt` 和 `常见问题-FAQ.txt`，均为 UTF-8 文本、中文／英文分区。FAQ 两种语言各 14 组，编号 Q01—Q14 一致，覆盖启动与依赖、托盘退出、保存与升级、休息条件、隐私免打扰／应用回退、健康提醒、色温验证／恢复／试调、黑屏及反馈。
- 对照客户端 XAML、英文资源和存储／运行规则复核文案，保留“在此屏幕试调”先启动再拖滑杆的实际行为；不把原色验证描述为专业校色认证，不把历史黑屏写成确定的软件或硬件归因。
- `build.ps1` 新增压缩前复制 `docs/portable/*.txt` 的步骤。本轮通过 PowerShell `Parser.ParseFile` 语法检查，并抽取该新增代码块在新 payload 上实际执行成功；没有执行整个安装器／发布构建流程。
- 文档检查：两份文本 UTF-8 严格解码成功、无替换字符及行尾空白，中英问题编号逐项一致；README、文档索引、使用指南和开发说明共 77 个本地链接目标存在。`git diff --check` 通过。
- 新包：`artifacts/portable-1.0.0-bilingual-20260923/ScreenKeeper-1.0.0-Portable-x64.zip`，68,894,923 字节，27 个文件；同目录提供 `ScreenKeeper-1.0.0-SHA256SUMS.txt` 和开发用 `zip-verification.json`（后者不随包）。
- 新 ZIP SHA256：`a661b7c6258facde44cb932acb3aef023ecc9c9b3468643964536dad44f32446`。
- Python `zipfile` CRC／解压检查通过，解压后逐文件 SHA256 与打包源相同。两份帮助与 docs/portable 原稿逐字节一致；25 个非帮助文件与上一轮 ZIP 全部相同，包括 EXE、原生依赖、配置、版本说明和许可。未包含用户 Data、日志、测试辅助文件或凭据。旧包保持原路径和原 SHA256，未覆盖。
- 本轮没有修改客户端或重新构建／启动客户端，没有重复 UI、硬件或干净系统验收；继续保留上一轮“EXE 未签名、干净系统完整操作未验收”的限制。帮助变更不构成对历史硬件故障的新验证。
- 已同步任务设计、计划、README 和文档入口，并更新 AGENTS.md 的帮助原稿与打包约定；保留原有未提交更改，本轮未提交、推送或上传产物。
