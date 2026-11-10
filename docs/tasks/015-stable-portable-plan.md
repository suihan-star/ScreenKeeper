# 015 · 1.0.0 正式便携版整理计划

关联 [任务设计](015-stable-portable-design.md)。保留 013／014 及双语帮助已有变更，不提交、推送或移动标签。

- [x] 整理双语发布说明、帮助入口与版本边界。
- [x] 冻结可追溯源码及逐文件清单，按用户指定的 1.0.0 构建 x64／x86。
- [x] 从该源码构建测试，完成双架构常规回归并记录结果。
- [x] 补齐依赖许可，核验解压、文件版本、架构、自包含加载及 SHA256。
- [x] 核对旧包与标签未变，完善交付记录及 AGENTS.md。

## 实际结果

### 成品与源码身份

- 交付目录：`artifacts/stable-1.0.0-20260924/release/`。两个便携 ZIP 均为 1.0.0、构建 20260924；每包 28 个文件，附双语使用说明、14 组 FAQ、双语发布说明、VERSION、上游许可及依赖许可。
- x64：`ScreenKeeper-1.0.0-Portable-x64.zip`，68,898,963 字节；SHA256 `519a68e64b61042bae78c92c6d6075ab58a54de6680260018322e4955c64dec7`。
- x86：`ScreenKeeper-1.0.0-Portable-x86.zip`，63,973,377 字节；SHA256 `edf731361a5f61743533c421c20b6a6286b1382a7d46a3c6b3c7a6c33be9a0a6`。
- 源码：`ScreenKeeper-1.0.0-Source-20260924.zip`，2,594,427 字节；SHA256 `3558d2e75101dc4585782e91938372a1c9e705e0274a275bfb4bf83033acacd0`。包含应用、测试、解决方案、许可、编辑配置与本次随包帮助共 279 个冻结输入，另附源码清单、构建身份及重建说明；不是完整 Git 历史或设计稿归档。
- 冻结输入来自 main `88492d243a65314f013ef1290ff9897543ce63ff` 加已完成的未提交 x86 修复及帮助。逐文件清单 SHA256 为 `caef228ed7f1615d0809ad3bf004d246e27ab7e3d15c2af9741a5a0373e384b2`，用于 EXE 的 `SourceRevisionId=snapshot.<该值>`；FileVersion 为 `1.0.0.0`，ProductVersion 为 `1.0.0+snapshot.<该值>`。不将基线提交冒充完整构建源码。
- 同目录附双语发布说明、BUILD-INFO.json 和 `ScreenKeeper-1.0.0-SHA256SUMS.txt`。后者包含上述三个 ZIP、发布说明和构建信息的校验值。维护用日志、探针、打包脚本在父目录，不混入用户 ZIP。

### 执行的构建与检查

- SDK 8.0.425；从冻结目录 `artifacts/stable-1.0.0-20260924/source` 分别执行 `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64` 与 `-r win-x86`，均成功。共同参数为 `--self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:MinVerVersionOverride=1.0.0 -p:SourceRevisionId=snapshot.<清单 SHA256> -p:RuntimeFrameworkVersion=8.0.31 --ignore-failed-sources`，输出到独立 payload/x64 或 payload/x86。未执行 build.ps1 的旧产物清理或安装器步骤。
- 从该快照执行 `dotnet test OLED-Sleeper.sln -c Release -p:MinVerVersionOverride=1.0.0 -p:SourceRevisionId=snapshot.<清单 SHA256>`，SCREENKEEPER_EXPECT_ARCH=x64；592 项，590 通过、2 项显式原生测试跳过、0 失败，TRX 为父目录 `tests/x64/full-x64.trx`。
- 对同一测试输出执行 `dotnet vstest OLED-Sleeper.Tests.dll /Platform:x86`，设置 SCREENKEEPER_EXPECT_ARCH=x86、DOTNET_ROOT_X86／DOTNET_ROOT(x86) 为 014 的私有 .NET 8.0.31 x86 运行时，DOTNET_MULTILEVEL_LOOKUP=0；同样 590 通过、2 跳过、0 失败，见 `tests/x86/full-x86.trx`。测试断言实际进程架构，不以 VSTest 协调器的 x64 标题代替。
- 实际 deps.json 的 8 个运行组件版本与先前许可清单一致，另分别核对对应 x64／x86 的 NETCore 和 WindowsDesktop 8.0.31 运行时许可。MinVer／ILLink 无运行资产，不列作运行组件。
- 两 ZIP 均通过 CRC、完整解压、全部文件 SHA256 一致性及 PE 架构检查；每包 EXE 与 5 个原生 DLL 架构一致。帮助与冻结原稿一致，LICENSE 与根许可一致；未含 Data、日志、PDB、凭据或测试探针。源码 ZIP 的全部冻结输入与清单逐项一致，且构建后仍与工作区对应源码相同。
- 对两包重新解压的 EXE 使用已有 PackageProbe startup hook，设置不存在的外部 DOTNET_ROOT 并关闭多级查找。均成功使用自包含 .NET 8.0.31，实际架构分别 X64／X86，便携标识和中英文／WPF 资源正常，版本为上述正式构建身份。检查在业务 Main 前退出，未创建 Data 或执行显示器服务；不是完整 UI 验收。EXE 签名状态均为 NotSigned。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 与 `git diff --check` 均退出码 0。包校验结果为 `package-verification.json`，旧包／标签与测试汇总为 `final-verification.json`。
- 首次打包前的清单校验发现本地脚本将 LF 转为 Windows CRLF，导致清单文件字节与预先计算的身份不一致；改为固定 UTF-8／LF 字节写入后校验通过。源码、预先计算的身份和已编译程序均未变化，不绕过校验。

### 保留边界与维护审查

- 构建／测试保留 NU1900：NuGet 漏洞数据源不可达。没有禁用 TLS 或漏洞检查，不宣称漏洞审计通过。
- 保留 014 的用户实测反馈与其范围；本次未重复真实亮度／Gamma／多屏操作，也未运行 Windows 10 原生 32 位环境。新包未签名；没有生成、安装或验收安装器。014 已确认的可选原生渲染测试跨 dispatcher 问题未扩展处理。
- 三个历史 ZIP（原 1.0.0 x64、双语 1.0.0 x64、1.0.1-preview.1 x86）校验值均未变；HEAD 仍为 `88492d2`，历史 `screenkeeper-v1.0.0` 仍指向 `6654714143cc93dba8d128f7dc59f1f2f11e0af9`。用户配置与运行客户端未改动。
- 已更新任务设计／计划、README、CHANGELOG、文档索引、开发说明及双语帮助，并新增 `docs/releases/1.0.0.md`。AGENTS.md 已检查并补充用户指定版本的归档构建须保留精确源码和独立身份的长期约束。
- 本轮未提交、推送、创建或移动标签，也未向发行平台上传；所有既有未提交变更保留。

## 2026-10-14 · 修复版正式包计划

关联 [设计追加范围](015-stable-portable-design.md#2026-10-14--修复版正式双架构包) 与 [018 修复记录](018-crash-color-investigation-plan.md)。用户已确认 20261009.3 测试版使用正常。

- [x] 核对工作区与既有变更、构建环境及前次正式包流程。
- [x] 对比当前应用及测试与已验收快照：274 个文件全部一致，无清单外新增代码文件。
- [x] 确认正式版本、整理双语发布说明并冻结可追溯源码。
- [x] 构建 x64／x86，运行两种实际架构的完整常规测试。
- [x] 核验两包版本、许可、CRC、解压内容、自包含入口及源码归档。
- [x] 保存校验值、旧包保留证据和真实验收边界；审查 AGENTS.md。

### 20261014 正式交付

- 用户明确选择“仍用 1.0.0，以新构建日期区分”。最终目录为 `artifacts/stable-1.0.0-20261014/release/`，版本 1.0.0、构建 20261014；当前运行目录与旧 Data 未改动。
- 冻结输入 280 个，其中应用／测试 274 个文件逐项匹配已验收 20261009.3 测试源码；双语通用帮助保持原样。清单 SHA256 为 `8bb26ea7eb9876a12218dc42c2388239503f600ce93869b922bb989427743096`，程序集使用 `SourceRevisionId=snapshot.<该值>`。基线仍为 `88492d243a65314f013ef1290ff9897543ce63ff` 加已完成的未提交变更。
- x64：`ScreenKeeper-1.0.0-Portable-x64.zip`，68,900,900 字节，SHA256 `756e151e500e29bf7afa031df0a3c405eca650319713ad76270fdbd252991ac4`。
- x86：`ScreenKeeper-1.0.0-Portable-x86.zip`，63,975,319 字节，SHA256 `891fa21f67653c08661e9d613243f35619c7144fcc7c32c08846778fb3f2232b`。
- 源码：`ScreenKeeper-1.0.0-Source-20261014.zip`，2,599,208 字节，SHA256 `1931d3653a272e3becd425bf845ab8538801420bbf68c8d36f4ab8dc2c0de5ea`；包含冻结输入、清单、构建身份及重建说明。发布目录另附 SOURCE-FILES.json、BUILD-INFO.json、双语发布说明与 SHA256SUMS。

### 本次构建与验证

- SDK 8.0.425，.NET／Windows Desktop 8.0.31，依赖版本未升级。从新冻结目录依次执行 `dotnet publish OLED-Sleeper/OLED-Sleeper.csproj -c Release -r win-x64` 与 `-r win-x86`；参数为 `--self-contained true -p:Portable=true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:MinVerVersionOverride=1.0.0 -p:SourceRevisionId=snapshot.<清单 SHA256> -p:RuntimeFrameworkVersion=8.0.31 -p:UseSharedCompilation=false -m:1 -nodeReuse:false --ignore-failed-sources`，输出到本构建独立 payload/{arch}/ScreenKeeper。两次均成功。
- `dotnet build OLED-Sleeper.Tests/OLED-Sleeper.Tests.csproj -c Release -p:BaseOutputPath=bin/Acceptance/ -p:MinVerVersionOverride=1.0.0 -p:SourceRevisionId=snapshot.<清单 SHA256> -p:UseSharedCompilation=false -m:1 -nodeReuse:false --ignore-failed-sources` 成功。
- 对该快照测试程序集依次运行 `dotnet vstest <OLED-Sleeper.Tests.dll> /Platform:x64` 和 `/Platform:x86`，分别设置 `SCREENKEEPER_EXPECT_ARCH`；x86 使用已有私有 8.0.31 运行时与 `DOTNET_MULTILEVEL_LOOKUP=0`。两架构均 654 项：652 通过、2 项显式原生桌面测试跳过、0 失败；位数断言通过。结果在本构建 `tests/full-x64.trx` 与 `tests/full-x86.trx`。此前还对已验收测试输出完成一次 x86 全量预检查，结果相同，独立保存在 `artifacts/release-tools-20261014/preflight-tests/`。
- 两便携 ZIP 各 28 文件，EXE 与 5 个原生 DLL 的 PE 架构一致；所有非 project 依赖标识与前正式版一致。各架构 10 个组件目录、15 个许可文本逐文件匹配先前已核验许可；包括 NuGet 原包未自带、此前已按官方源码提交补齐的文本。
- 包 CRC、完整解压、逐文件 SHA256、中文文件名、帮助与冻结原稿一致性、源码 ZIP 输入逐项校验均通过；不含用户 Data、恢复记录、日志、转储、调试符号或凭据。解压后的 EXE 均确认 FileVersion=1.0.0.0、ProductVersion=1.0.0+snapshot.<本次清单>。
- 对两份解压 EXE 使用已检查的 PackageProbe，并设置不存在的外部运行时路径、关闭多级查找：X64／X86、自包含 .NET 8.0.31、Portable=true、WPF／英文资源及精确源码身份均通过。探针在业务 Main 前结束，没有创建 Data 或启动显示器服务；不能据此声称完整 UI／真机验收。
- 历史 20260924.2 的两 ZIP 与源码包、20261009.3 测试 ZIP 校验值保持不变；HEAD 和原 `screenkeeper-v1.0.0` 标签不变。详细结果见 `package-verification.json`、`executable-verification.json`、`final-verification.json`。
- `dotnet format OLED-Sleeper.sln --verify-no-changes --no-restore` 退出码 0；针对工作区警告追加 `--verbosity diagnostic` 复核，确认是同一 NuGet 漏洞数据源不可达，266 个文件没有格式差异。`git diff --check` 退出码 0，保留 Git 的已有 LF／CRLF 提示。

### 维护与边界

- 本次只整理发布文档和本地构建产物，没有更改应用、测试或依赖。已更新 README、CHANGELOG、文档索引、015 设计／计划，以及新的 `docs/releases/1.0.0-20261014.md`；历史发布说明和原始验收记录保留。
- 构建仍报告 NU1900（NuGet 漏洞数据源不可达），没有停用审计或 TLS，不宣称在线漏洞检查成功。程序仍未签名；本次未重复真实 Gamma／亮度操作，未在 Windows 10 原生 32 位环境验收，也未生成安装器。NVIDIA 副屏未启用问题不列为本包已修复事项。
- 已检查 AGENTS.md，本次无需更新：既有双架构便携、源码快照、同版本独立构建标识、许可及硬件验收边界已经覆盖本轮，没有新增长期规则。
- 本轮未提交、推送、创建／移动标签、上传官网或发行平台。用户版本选择前的内部 1.0.1 候选未打包或交付，已标记 NOT-FOR-RELEASE；正式交付仅使用上述 1.0.0／20261014 目录。
